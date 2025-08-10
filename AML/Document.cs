using AbyssCLI.Tool;
using Newtonsoft.Json.Linq;
using System.Reflection;

namespace AbyssCLI.AML;

#pragma warning disable IDE1006 //naming convension
public class Document
{
    private readonly ContextedTask _root_context;
    private readonly int _root_element_id = RenderID.ElementId;
    private int _ui_element_id = 0;
    private readonly AmlMetadata _metadata;
    private readonly DeallocStack _dealloc_stack;
    private readonly JavaScriptDispatcher _js_dispatcher;
    internal bool IsUiInitialized => _ui_element_id == 0;
    internal AmlMetadata Metadata => _metadata;

    //document constructor must not allocate any resource that needs to be deallocated.
    internal Document(ContextedTask root_context, AmlMetadata metadata)
    {
        _root_context = root_context;
        _metadata = metadata;
        _dealloc_stack = new();
        _js_dispatcher = new(new(), this, new Console());
        head = new(_dealloc_stack);
        body = new(_dealloc_stack);
    }
    internal void Init()
    {
        Client.Client.RenderWriter.CreateElement(0, _root_element_id);
        _dealloc_stack.Add(new(_root_element_id, DeallocEntry.EDeallocType.RendererElement));
        
        body.setTransformAsValues(_metadata.pos, _metadata.rot);
        title = _metadata.title;

        if (_metadata.is_item) InitUI();
    }
    private void InitUI()
    {
        _ui_element_id = RenderID.ElementId;

        Client.Client.RenderWriter.CreateItem(
            _ui_element_id,
            _metadata.sharer_hash,
            Google.Protobuf.ByteString.CopyFrom(_metadata.uuid.ToByteArray())
        );
        _dealloc_stack.Add(new(_ui_element_id, DeallocEntry.EDeallocType.RendererUiItem));
    }
    /// <summary>
    /// Add an entry to the deallocation stack. 
    /// warning: _dealloc_stack is not thread safe.
    /// All calls of this must be called synchronously by architecture.
    /// </summary>
    /// <param name="entry"></param>
    internal void AddToDeallocStack(DeallocEntry entry) =>
        _dealloc_stack.Add(entry);
    internal MediaLink CreateMediaLink(string src)
    {
        MediaLink result = new(src);
        _root_context.Attach(result);
        return result;
    }

    internal void StartJavaScript(CancellationToken token) =>
        _js_dispatcher.Start(token);
    /// <summary>
    /// Try to enqueue a javascript script to be executed.
    /// This is thread safe, but fails when the queue is full.
    /// </summary>
    /// <param name="filename"></param>
    /// <param name="script"></param>
    /// <returns></returns>
    internal bool TryEnqueueJavaScript(string filename, object script) => 
        _js_dispatcher.TryEnqueue(filename, script);
    /// <summary>
    /// Interrupt javascript execution and deactivates document. 
    /// This must be called only after token cancellation.
    /// </summary>
    internal void Interrupt()
    {
        Client.Client.RenderWriter.ElemSetActive(_root_element_id, false);
        if (IsUiInitialized)
            Client.Client.RenderWriter.ItemSetActive(_ui_element_id, false);
        _js_dispatcher.Interrupt();
    }
    /// <summary>
    /// Waits for javascript dispatcher to finish execution and deallocates all resources.
    /// Calling this is mendatory.
    /// </summary>
    internal void Join()
    {
        _js_dispatcher.Join();
        _dealloc_stack.FreeAll();
    }

    // inner attributes
    internal string _title;
    internal MediaLink _iconSrc;

    // exposed to JS
    public readonly Head head;
    public readonly Body body;
    public string title
    {
        get => _title; 
        set
        {
            _title = value;
            Client.Client.RenderWriter.ItemSetTitle(_ui_element_id, value);
        }
    }
    public string iconSrc
    {
        get => _iconSrc.src;
        set
        {
            _iconSrc.Stop();
            _iconSrc.Join(); //clears previous media link. this is mendatory.

            _iconSrc = new MediaLink(_dealloc_stack, value);
            //TODO: pass icon resource to renderer.
        }
    }
    public Element createElement(string tag, dynamic options) => tag switch
    {
        "o" => new Placement(_dealloc_stack, tag, options),
        _ => new Element(_dealloc_stack, tag, options)
    };
    public Element getElementById(string id)
    {
        if (id == null) return null;
        if (id.Length == 0) return null;

        Element res = head.getElementByIdHelper(id);
        if (res != null) return res;

        res = body.getElementByIdHelper(id);
        return res;
    }
    public void addEventListener(string event_name, string id, dynamic callback)
    {
        //If same id is used, throw an exception.
        switch (event_name)
        {
            case "click":
                break;
            case "keydown":
                break;
            case "keyup":
                break;
            case "mousedown":
                break;
            case "mouseup":
                break;
            default:
                throw new Exception("unknown event: " + event_name);
        }
    }
    public void removeEventListener(string event_name, string id)
    {
        //TODO: remove event listener by id.
    }
    public void clearEventListeners(string event_name)
    {
        switch (event_name)
        {
            case "click":
                break;
            case "keydown":
                break;
            case "keyup":
                break;
            case "mousedown":
                break;
            case "mouseup":
                break;
            default:
                throw new Exception("unknown event: " + event_name);
        }
    }
}
#pragma warning restore IDE1006 //naming convension

