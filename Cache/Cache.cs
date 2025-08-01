using AbyssCLI.Tool;

namespace AbyssCLI.Cache
{
    /// <summary>
    /// In abyss browser, we cache abyst resources according to the Cache-Control header.
    /// We have monolithic cache, which entry contains 1) resource 2) origin 3) response headers. (and more)
    /// However, non-semantic response header fields are excluded.
    /// </summary>
    internal class Cache(Action<HttpRequestMessage> http_requester)
    {
        private readonly Action<HttpRequestMessage> _http_requester = http_requester;
        private readonly Dictionary<string, RcTaskCompletionSource<CachedResource>> _inner = [];

        public RcTaskCompletionSource<CachedResource> Get(string key)
        {
            lock (_inner)
            {
                if (_inner.TryGetValue(key, out var entry))
                {
                    return entry;
                }
                RcTaskCompletionSource<CachedResource> new_entry = new();
                _inner.Add(key, new_entry);
                _http_requester.Invoke(new(HttpMethod.Get, key));
                return new_entry;
            }
        }
        public bool TryRemove(string key)
        {
            lock (_inner)
            {
                if (_inner.TryGetValue(key, out var entry))
                {
                    if (entry.TryClose())
                    {
                        entry.TrySetResult(CachedResource.DefaultFailedResource);
                        _inner.Remove(key);
                        return true;
                    }
                    return false;
                }
                return true;
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
            }
        }
    }
}
