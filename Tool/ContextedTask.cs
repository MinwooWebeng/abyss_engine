﻿#nullable enable
namespace AbyssCLI.Tool
{
    internal abstract class ContextedTask
    {
        protected abstract void OnNoExecution();
        protected abstract void SynchronousInit();
        protected abstract Task AsyncTask(CancellationToken token);
        protected abstract void OnSuccess();
        protected abstract void OnStop();
        protected abstract void OnFail(Exception e);
        protected abstract void SynchronousExit();

        private readonly TaskCompletionSource<(ContextedTask?,TaskCompletionSource<CancellationTokenSource?>)> _parent_init_tcs_tcs = new();
        private readonly TaskCompletionSource<CancellationTokenSource?> _init_tcs = new();
        private readonly CancellationTokenSource _self_stop_tcs = new();
        private readonly TaskCompletionSource _done = new();
        private readonly List<Task> _children_done = [];
        private bool _is_accepting_child = true;
        public ContextedTask()
        {
            Task.Run(async () =>
            {
                var (parent, parent_init_tcs) = await _parent_init_tcs_tcs.Task;
                //parent attached. currently, parrent object has no use.
                //Console.WriteLine(debug_tag + "1");

                var parent_tcs = await parent_init_tcs.Task;
                //Console.WriteLine(debug_tag + "2");
                if (parent_tcs == null || parent_tcs.IsCancellationRequested)
                {
                    //parent was dead.
                    lock (_children_done)
                    {
                        _is_accepting_child = false;
                    }
                    _init_tcs.SetResult(null); //I die

                    OnNoExecution();
                    WaitChildren();
                    //Console.WriteLine(debug_tag + "2b");
                    _done.SetResult();
                    return;
                }
                var tcs = CancellationTokenSource.CreateLinkedTokenSource(parent_tcs.Token, _self_stop_tcs.Token);
                if (parent_tcs.Token.IsCancellationRequested || _self_stop_tcs.Token.IsCancellationRequested)
                    tcs.Cancel();

                SynchronousInit();
                _init_tcs.SetResult(tcs);
                //synchronous init finished, TCS setted -> children will start init.

                try
                {
                    await AsyncTask(tcs.Token);
                    //Console.WriteLine(debug_tag + "3");
                    try //waits for token invalidation.
                    {
                        await Task.Delay(Timeout.Infinite, tcs.Token);
                    }
                    catch (TaskCanceledException) { }
                    //Console.WriteLine(debug_tag + "4");
                    lock (_children_done)
                    {
                        _is_accepting_child = false;
                    }
                    OnSuccess();
                }
                catch (TaskCanceledException)
                {
                    //Console.WriteLine(debug_tag + "5");
                    lock (_children_done)
                    {
                        _is_accepting_child = false;
                    }
                    OnStop();
                }
                catch (Exception e)
                {
                    //Console.WriteLine(debug_tag + "6");
                    lock (_children_done)
                    {
                        _is_accepting_child = false;
                    }
                    OnFail(e);
                }
                finally
                {
                    //Console.WriteLine(debug_tag + "7");
                    _ = tcs.CancelAsync();
                    WaitChildren();
                    //Console.WriteLine(debug_tag + "8");
                    SynchronousExit();
                    _done.SetResult();
                    //Console.WriteLine(debug_tag + "9");
                }
            });
        }
        public void Attach(ContextedTask child)
        {
            lock (_children_done)
            {
                if (_is_accepting_child)
                {
                    _children_done.Add(child._done.Task);
                    child._parent_init_tcs_tcs.SetResult((this,_init_tcs));
                }
                else
                {
                    var null_tcs = new TaskCompletionSource<CancellationTokenSource?>();
                    null_tcs.SetResult(null);
                    child._parent_init_tcs_tcs.SetResult((null,null_tcs));
                }
            }
        }
        public void Stop()
        {
            _self_stop_tcs.CancelAsync();
            //todo: delete from parent - mendatory to stop memory leak.
            //Todo: may propagate some information?
        }
        public void ClearDeadChildren() //this can be called to clear memory leak caused by repeateded children attaching and stopping. 
        {
            lock (_children_done)
            {
                _children_done.RemoveAll(task => task.IsCompleted);
            }
        }
        private void WaitChildren()
        {
            if (_children_done.Count > 0)
                Task.WaitAll([.. _children_done]);
        }
        public class ContextedTaskRoot : ContextedTask
        {
            public ContextedTaskRoot() : base()
            {
                var _virtual_parent_init_tcs = new TaskCompletionSource<CancellationTokenSource?>();
                _parent_init_tcs_tcs.SetResult((this, _virtual_parent_init_tcs));

                var _virtual_parent_cts = new CancellationTokenSource();
                _virtual_parent_init_tcs.SetResult(_virtual_parent_cts);
            }
            protected override void OnNoExecution() { throw new InvalidOperationException(); }
            protected override void SynchronousInit() { }
            protected override Task AsyncTask(CancellationToken token) { return Task.CompletedTask; }
            protected override void OnSuccess() { }
            protected override void OnStop() { }
            protected override void OnFail(Exception e) { throw new InvalidOperationException(); }
            protected override void SynchronousExit() { }
            public Task Join()
            {
                return _done.Task;
            }
        }
    }
}
#nullable disable