using AbyssCLI.AML;
using AbyssCLI.Cache;
using AbyssCLI.Tool;

namespace AbyssCLI.HL;

internal class Environment : ContextedTask
{
    private readonly AbyssURL _url;
    private TaskCompletionReference<Cache.CachedResource> _document_cache_ref; //only to keep cache live.
    private readonly DeallocStack _dealloc_stack;
    private readonly Document _document;
    public Environment(AbyssURL url)
    {
        _url = url;
        _dealloc_stack = new();
        _document = new(_dealloc_stack);
    }

    protected override void OnNoExecution() { }
    protected override void SynchronousInit() =>
        Client.Client.RenderWriter.ConsolePrint("||>opening environment(" + _url.ToString() + ")<||"); //debug
    protected override async Task AsyncTask(CancellationToken token)
    {
        _document_cache_ref = Client.Client.Cache.GetReference(_url.ToString());
        Cache.CachedResource doc_resource = await _document_cache_ref.Task.WaitAsync(token);
        if (doc_resource is not Cache.Text || doc_resource.MIMEType != "text/aml")
        {
            throw new Exception("fatal:::MIME mismatch");
        }
        string raw_document = await (doc_resource as Cache.Text).ReadAsync(token);

        void ExecuteScript(string file_name, string script_text)
        {
            try
            {
                _document._js_engine.Execute(new Microsoft.ClearScript.DocumentInfo(file_name), script_text);
            }
            catch (Microsoft.ClearScript.ScriptEngineException ex)
            {
                Client.Client.CerrWriteLine("javascript error: " + ex.Message);
                Client.Client.CerrWriteLine("stack trace: " + ex.ErrorDetails);
            }
        }

        ParseUtil.ParseAMLDocument(_document, raw_document, token);
        foreach (var script in _document.head._scripts)
        {
            switch (script)
            {
            case (string _, string script_text):
                ExecuteScript("<script>", script_text);
                break;
            case (string file_name, TaskCompletionReference<CachedResource> script_ref):
                CachedResource script_resource = await script_ref.Task.WaitAsync(token);
                if (script_resource is not Cache.Text || script_resource.MIMEType != "text/javascript")
                {
                    Client.Client.CerrWriteLine("fatal:::script resource MIME mismatch: " + script_resource.MIMEType);
                    continue;
                }
                string remote_script_text = await (script_resource as Cache.Text).ReadAsync(token);
                ExecuteScript(file_name, remote_script_text);
                break;
            }
        }
    }
    protected override void OnSuccess() =>
        Client.Client.RenderWriter.ConsolePrint("||>loaded environment(" + _url.ToString() + ") this should not be printed<||"); //debug
    protected override void OnStop()
    {
        Client.Client.RenderWriter.MoveElement(_document._root_element_id, -1);
        Client.Client.RenderWriter.ConsolePrint("||>stopped loading environment(" + _url.ToString() + ")<||"); //debug
    }
    protected override void OnFail(Exception e)
    {
        Client.Client.RenderWriter.MoveElement(_document._root_element_id, -1);
        Client.Client.CerrWriteLine(e.ToString());
    }
    protected override void SynchronousExit()
    {
        _dealloc_stack.FreeAll();
        Client.Client.RenderWriter.ConsolePrint("||>closed environment(" + _url.ToString() + ")<||"); //debug
    }

    //public static new void Stop() => throw new InvalidOperationException("environment cannot be stopped directly, use Close() instead.");
    //public void Close()
    //{
    //    var kill_task = new EnvironmentShutdownTask(); //this only hides the root element
    //    Attach(kill_task);
    //    base.Stop(); // signal token cancellation
    //}
    //private class EnvironmentShutdownTask(int root_element_id) : ContextedTask
    //{
    //    protected override void OnNoExecution() => throw new InvalidOperationException("environment killing task must be executed");
    //    protected override void SynchronousInit() => Client.Client.RenderWriter.MoveElement(root_element_id, -1);
    //    protected override async Task AsyncTask(CancellationToken token) => await Task.CompletedTask;
    //    protected override void OnSuccess() { }
    //    protected override void OnStop() => throw new InvalidOperationException("environment killing task cannot be stopped");
    //    protected override void OnFail(Exception e) => throw new InvalidOperationException("environment killing task cannot fail");
    //    protected override void SynchronousExit() { }
    //}
}
