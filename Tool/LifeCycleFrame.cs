using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AbyssCLI.Tool
{
    internal abstract class LifeCycleFrame
    {
        protected abstract void OnNoExecution();
        protected abstract void SynchronousInit();
        protected abstract Task AsyncTask(CancellationToken token);
        protected abstract void OnStop();
        protected abstract void OnFail(Exception e);
        protected abstract void OnSuccess();
        protected abstract void SynchronousExit();
    }
}
