using AbyssCLI.AML;
using AbyssCLI.Tool;
using System.Collections.Generic;

namespace AbyssCLI.HL;

internal class Environment(AbyssLib.Host host, AbyssURL url) : ContextedTask
{
    private readonly AbyssLib.Host _host = host;
    private readonly AbyssURL _url = url;
    private TaskCompletionReference<Cache.CachedResource> __document_cache_ref; //only to keep cache live.
    private readonly Document _document = new();
    private readonly DeallocStack _dealloc_stack = new();

    protected override void OnNoExecution() { }
    protected override void SynchronousInit() =>
        Client.Client.RenderWriter.ConsolePrint("||>opening environment(" + _url.ToString() + ")<||"); //debug
    protected override async Task AsyncTask(CancellationToken token)
    {
        RcTaskCompletionSource<Cache.CachedResource> document_cache_entry = Client.Client.Cache.Get(_url.ToString());
        if (!document_cache_entry.TryGetReference(out __document_cache_ref))
        {
            throw new Exception("fatal:::failed to get document resource reference");
        }
        Cache.CachedResource doc_resource = await __document_cache_ref.Task.WaitAsync(token);
        if (doc_resource is not Cache.Text || doc_resource.MIMEType != "text/aml")
        {
            throw new Exception("fatal:::MIME mismatch");
        }
        string raw_document = await (doc_resource as Cache.Text).ReadAsync(token);

        await ParseUtil.ParseAMLDocumentAsync(token, _document, raw_document);
    }
    protected override void OnSuccess() =>
        Client.Client.RenderWriter.ConsolePrint("||>loaded environment(" + _url.ToString() + ")<||"); //debug
    protected override void OnStop() =>
        Client.Client.RenderWriter.ConsolePrint("||>stopped loading environment(" + _url.ToString() + ")<||"); //debug
    protected override void OnFail(Exception e) => Client.Client.CerrWriteLine(e.ToString());
    protected override void SynchronousExit()
    {
        _dealloc_stack.FreeAll();
        Client.Client.RenderWriter.ConsolePrint("||>closed environment(" + _url.ToString() + ")<||"); //debug
    }

    public override void Stop()
    {
        Client.Client.RenderWriter.MoveElement(_document._root_element_id, -1);
        base.Stop();
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
