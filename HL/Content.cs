using AbyssCLI.AML;
using AbyssCLI.Tool;

namespace AbyssCLI.HL;

internal class Content : ContextedTask
{
    private readonly AbyssURL _url;
    private TaskCompletionReference<Cache.CachedResource> _document_cache_ref; //only to keep cache live.
    private readonly DeallocStack _dealloc_stack;
    internal readonly Document _document;
    internal readonly AmlMetadata _metadata;
    private readonly int _ui_element_id = RenderID.ComponentId;

    public Content(AbyssURL url, AmlMetadata metadata = default)
    {
        _url = url;
        _dealloc_stack = new();
        _document = new(_dealloc_stack);
        _metadata = metadata;
    }

    protected override void OnNoExecution() { }
    protected override void SynchronousInit()
    {
        Client.Client.RenderWriter.ConsolePrint("||>opening content(" + _url.ToString() + ")<||"); //debug

        // critical component construction
        Client.Client.RenderWriter.CreateElement(0, _document._root_element_id);
        _dealloc_stack.Add(new(_document._root_element_id, DeallocEntry.EDeallocType.RendererElement));

        // parse metadata
        if (_metadata.is_item)
        {
            Client.Client.RenderWriter.CreateItem(
                _ui_element_id,
                _metadata.sharer_hash,
                Google.Protobuf.ByteString.CopyFrom(_metadata.uuid.ToByteArray())
            );
            _dealloc_stack.Add(new(_ui_element_id, DeallocEntry.EDeallocType.RendererUiItem));
            _document.body.setTransformAsValues(_metadata.pos, _metadata.rot);
        }
        _document._title = _metadata.title;
    }
    protected override async Task AsyncTask(CancellationToken token)
    {
        // load main document. this may override metadata.
        _document_cache_ref = Client.Client.Cache.GetReference(_url.ToString());
        Cache.CachedResource doc_resource = await _document_cache_ref.Task.WaitAsync(token);
        if (doc_resource is not Cache.Text || doc_resource.MIMEType != "text/aml")
        {
            throw new Exception("fatal:::MIME mismatch: " + (doc_resource.MIMEType == "" ? "<unspecified>" : doc_resource.MIMEType));
        }
        string raw_document = await (doc_resource as Cache.Text).ReadAsync(token);

        ParseUtil.ParseAMLDocument(_document, raw_document, token);
        _document._js_dispatcher.Start(token);
    }
    protected override void OnSuccess() =>
        Client.Client.RenderWriter.ConsolePrint("||>loaded content(" + _url.ToString() + ") this should not be printed<||"); //debug
    protected override void OnStop()
    {
        QuickHide();
        Client.Client.RenderWriter.ConsolePrint("||>stopped loading content(" + _url.ToString() + ")<||"); //debug
    }
    protected override void OnFail(Exception e)
    {
        QuickHide();
        Client.Client.RenderWriter.ConsolePrint("||>failed loading content(" + _url.ToString() + ")<||"); //debug
        Client.Client.CerrWriteLine(e.ToString());
    }
    protected override void SynchronousExit()
    {
        _document._js_dispatcher.Interrupt(); // stop running scripts (if exists)
        _document._js_dispatcher.Join();
        _dealloc_stack.FreeAll();
        Client.Client.RenderWriter.ConsolePrint("||>closed content(" + _url.ToString() + ")<||"); //debug
    }

    private void QuickHide()
    {
        Client.Client.RenderWriter.MoveElement(_document._root_element_id, -1);
        Client.Client.RenderWriter.DeleteItem(_ui_element_id);
    }
}
