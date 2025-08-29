using AbyssCLI.Cache;
using AbyssCLI.Tool;
using System.Text;

#nullable enable
namespace AbyssCLI.AML
{
    public sealed class BetterResourceLink : IDisposable
    {
        public readonly string Src;
        private readonly Action<CachedResource> _remove_action;
        public bool IsRemovalRequired = true;
        private readonly TaskCompletionSource<byte> _tcs = new();
        private readonly Task<Cache.CachedResource?> _inner_task;
        public BetterResourceLink(
            string src,
            Action<CachedResource> deploy_action,
            Action<CachedResource> remove_action)
        {
            Src = src;
            _remove_action = remove_action;
            _inner_task = Task.Run(async () =>
            {
                using TaskCompletionReference<CachedResource> cache_rsc_ref = Client.Client.Cache.GetReference(src);

                if (await Task.WhenAny(cache_rsc_ref.Task, _tcs.Task)
                is not Task<Cache.CachedResource> resource_task) //cancelled
                    return null;

                var resource = resource_task.Result;
                deploy_action(resource);
                return resource;
            });
        }

        private bool _disposed = false;
        public void Dispose()
        {
            if (_disposed) return;

            _tcs.SetResult(0);
            _inner_task.Wait(); //This is kinda unavoidable; JS main is expected to call Dispose().
            var resource = _inner_task.Result;
            if (IsRemovalRequired && resource != null)
                _remove_action(resource);

            _disposed = true;
        }
    }
}
