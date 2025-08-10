using AbyssCLI.Tool;
using Google.Protobuf.WellKnownTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AbyssCLI.AML
{
    /// <summary>
    /// When a media source is attached to dom elements, they are attached as a MediaLink object.
    /// This is not exposed to JS, and only exists internally.
    /// When MediaLink object is active, it waits for TaskCompletionReference<CachedResource>.
    /// When CachedResource arrives, it fires UIActions.
    /// When this object is removed from DOM, it must be called Stop()
    /// This should revert UIActions.
    /// </summary>
    internal class MediaLink : ContextedTask
    {
        public readonly string src;
        private readonly DeallocEntry _dealloc_entry;
        internal MediaLink(DeallocStack dealloc_stack, string _src)
        {
            src = _src;
            var cache_rsc_ref = Client.Client.Cache.GetReference(src);
            var dealloc_entry = new DeallocEntry(cache_rsc_ref);
            dealloc_stack.Add(dealloc_entry);
        }
        public override void Join()
        {
            base.Join();
            _dealloc_entry.Free(); //removes media from dealloc stack.
        }

        protected override Task AsyncTask(CancellationToken token) => throw new NotImplementedException();
        protected override void OnFail(Exception e) => throw new NotImplementedException();
        protected override void OnNoExecution() => throw new NotImplementedException();
        protected override void OnStop() => throw new NotImplementedException();
        protected override void OnSuccess() => throw new NotImplementedException();
        protected override void SynchronousExit() => throw new NotImplementedException();
        protected override void SynchronousInit() => throw new NotImplementedException();
    }
}
