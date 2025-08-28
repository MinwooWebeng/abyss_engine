using AbyssCLI.Cache;
using AbyssCLI.Tool;

namespace AbyssCLI.AML
{
    /// <summary>
    /// When a resource source is attached to dom elements, they are attached as a ResourceLink object.
    /// This is not exposed to JS, and only exists publicly.
    /// When ResourceLink object is active, it waits for TaskCompletionReference<CachedResource>.
    /// When CachedResource arrives, it fires UIActions.
    /// When this object is removed from DOM, it must be called SynchronousCleanup()
    /// This should revert UIActions.
    /// The two actions should never throw.
    /// </summary>
    public class ResourceLink
    {
        public readonly string Src;
        public readonly DeallocEntry _dealloc_entry;
        private readonly ResourceLinkContextedTask _mlct;
        public ResourceLink(ContextedTask parent_context, DeallocStack dealloc_stack, string src,
            Action<CachedResource> async_deploy_action,
            Action<CachedResource> async_remove_action)
        {
            Src = src;
            var cache_rsc_ref = Client.Client.Cache.GetReference(src);
            _dealloc_entry = new DeallocEntry(cache_rsc_ref);
            dealloc_stack.Add(_dealloc_entry);

            _mlct = new(cache_rsc_ref.Task, async_deploy_action, async_remove_action);
            parent_context.Attach(_mlct);
        }
        public void SynchronousCleanup(bool skip_remove = false)
        {
            _mlct.SynchronousCleanup(skip_remove);
            _dealloc_entry.Free(); //clears TaskCompletionReference

#pragma warning disable CA1816 // GC.SuppressFinalize outside Dispose
            GC.SuppressFinalize(this);
#pragma warning restore CA1816 // GC.SuppressFinalize outside Dispose
        }
        ~ResourceLink() => Client.Client.RenderWriter.ConsolePrint("ResourceLink is not cleaned up. This is bug");
    }
    public class ResourceLinkContextedTask(
        Task<Cache.CachedResource> resource_await,
        Action<CachedResource> async_deploy_action,
        Action<CachedResource> async_remove_action
    ) : ContextedTask(
        (e) =>
        {
            Client.Client.RenderWriter.ConsolePrint("fatal:::unhandled ResourceLinkContextedTask exception:" + e.ToString());
        }
    )
    {
        private CachedResource resource;
        public void SynchronousCleanup(bool skip_remove)
        {
            Stop();
            Join();
            if (resource != null)
            {
                if (!skip_remove)
                    async_remove_action(resource);
                resource = null;
            }
        }

        protected override void OnNoExecution() { }
        protected override void SynchronousInit() { }
        protected override async Task AsyncTask(CancellationToken token)
        {
            try
            {
                resource = await resource_await.WaitAsync(token);
                async_deploy_action(resource);
            }
            catch
            {
                resource = null;
            }
            //this always succeedes.
        }
        protected override void OnSuccess() { }
        protected override void OnStop() => throw new NotImplementedException();
        protected override void OnFail(Exception e) => throw new NotImplementedException();
        protected override void SynchronousExit() { }
    }
}
