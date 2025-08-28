using AbyssCLI.AML;
using AbyssCLI.Tool;

namespace AbyssCLI.HL;

internal class Content : ContextedTask
{
    private readonly AbyssURL _url;
    internal readonly Document Document;
    internal Content(AbyssURL url, AmlMetadata metadata = null) : base((e) =>
    {
        Client.Client.CerrWriteLine("fatal:::unhandled content exception:" + e.ToString());
    })
    {
        _url = url;
        Document = new(this, metadata ?? new());
    }

    protected override void OnNoExecution() { }
    protected override void SynchronousInit()
    {
        Client.Client.RenderWriter.ConsolePrint("||>opening content(" + _url.ToString() + ")<||"); //debug

        Document.Init();
    }
    protected override async Task AsyncTask(CancellationToken token)
    {
        // load main document. this may override metadata.
        var _document_cache_ref = Client.Client.Cache.GetReference(_url.ToString());
        Document.AddToDeallocStack(new(_document_cache_ref));

        Cache.CachedResource doc_resource = await _document_cache_ref.Task.WaitAsync(token);
        if (doc_resource is not Cache.Text || doc_resource.MIMEType != "text/aml")
        {
            throw new Exception("fatal:::MIME mismatch: " + (doc_resource.MIMEType == "" ? "<unspecified>" : doc_resource.MIMEType));
        }
        string raw_document = await (doc_resource as Cache.Text).ReadAsync(token);

        ParseUtil.ParseAMLDocument(Document, raw_document, token);
        Document.StartJavaScript(token);
        while(true)
        { //temporary: fixed duration cleanup
            await Task.Delay(1000, token);
            Document.ScheduleOphanedElementCleanup();
        }
    }
    protected override void OnSuccess()
    {
        Document.Interrupt();
    }
    protected override void OnStop()
    {
        Document.Interrupt();
        Client.Client.RenderWriter.ConsolePrint("||>stopped loading content(" + _url.ToString() + ")<||"); //debug
    }
    protected override void OnFail(Exception e)
    {
        Document.Interrupt();
        Client.Client.RenderWriter.ConsolePrint("||>failed loading content(" + _url.ToString() + ")<||"); //debug
        Client.Client.CerrWriteLine(e.ToString());
    }
    protected override void SynchronousExit()
    {
        Document.Join();
        Client.Client.RenderWriter.ConsolePrint("||>closed content(" + _url.ToString() + ")<||"); //debug
    }
}
