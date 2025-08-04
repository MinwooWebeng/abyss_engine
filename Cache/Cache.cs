using AbyssCLI.Abyst;
using AbyssCLI.Tool;

namespace AbyssCLI.Cache
{
    /// <summary>
    /// In abyss browser, we cache abyst resources according to the Cache-Control header.
    /// We have monolithic cache, which entry contains 1) resource 2) origin 3) response headers. (and more)
    /// However, non-semantic response header fields are excluded.
    /// </summary>
    public class Cache(Action<HttpRequestMessage> http_requester, Action<AbystRequestMessage> abyst_requester)
    {
        private readonly Action<HttpRequestMessage> _http_requester = http_requester;
        private readonly Action<AbystRequestMessage> _abyst_requester = abyst_requester;
        private readonly Dictionary<string, RcTaskCompletionSource<CachedResource>> _inner = []; //lock this.
        private readonly LinkedList<RcTaskCompletionSource<CachedResource>> _outdated_inner = [];
        public void Add(string key, RcTaskCompletionSource<CachedResource> value)
        {
            lock (_inner)
            {
                ThreadUnsafeMoveEntryToOutdatedIfExists(key);
                _inner.Add(key, value);
            }
        }
        public RcTaskCompletionSource<CachedResource> Get(string key)
        {
            object requestMessage = key switch
            {
                string s when s.StartsWith("http") => new HttpRequestMessage(HttpMethod.Get, key),
                string s when s.StartsWith("abyst") => new AbystRequestMessage(HttpMethod.Get, key),
                _ => throw new Exception("invalid address"),
            };

            lock (_inner)
            {
                if (_inner.TryGetValue(key, out var entry))
                {
                    return entry;
                }
                RcTaskCompletionSource<CachedResource> new_entry = new();
                _inner.Add(key, new_entry);

                switch (requestMessage)
                {
                    case HttpRequestMessage hrm:
                        _http_requester.Invoke(hrm);
                        break;
                    case AbystRequestMessage arm:
                        _abyst_requester.Invoke(arm);
                        break;
                }
                return new_entry;
            }
        }
        public void Remove(string key)
        {
            lock (_inner)
            {
                ThreadUnsafeMoveEntryToOutdatedIfExists(key);
            }
        }
        private void ThreadUnsafeMoveEntryToOutdatedIfExists(string key)
        {
            if (_inner.TryGetValue(key, out var old))
            {
                _inner.Remove(key);
                old.TrySetResult(CachedResource.DefaultFailedResource);
                _outdated_inner.AddLast(old);
            }
        }
        static private readonly TimeSpan CacheTimeout = TimeSpan.FromMinutes(3);
        public void RemoveOld()
        {
            lock (_inner)
            {
                var now = DateTime.Now;
                List<string> olds = [];
                foreach (var entry in _inner)
                {
                    if (entry.Value.TryGetLastAccess(out var last_access)
                        && now - last_access > CacheTimeout
                        && entry.Value.TryClose())
                    {
                        entry.Value.TrySetResult(CachedResource.DefaultFailedResource);
                        olds.Add(entry.Key);
                    }
                }
                foreach (var old in olds)
                {
                    _inner.Remove(old);
                }

                for (var node = _outdated_inner.First; node != null;)
                {
                    var next = node.Next;
                    if (node.Value.TryClose())
                    {
                        _outdated_inner.Remove(node);
                    }
                    node = next;
                }
            }
        }
    }
}
