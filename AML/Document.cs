using AbyssCLI.Cache;
using AbyssCLI.Tool;
using Microsoft.ClearScript.V8;

namespace AbyssCLI.AML;

#nullable enable
#pragma warning disable IDE1006 //naming convension
/// <summary>
/// [MEMO]
/// When disposing elements in _detached_elements,
/// it should be noted that some of them may have Rc.DoRefExist == false, but
/// actually it may be before the initial reference creation.
/// </summary>
public class Document
{
    private readonly ContextedTask _root_context;
    private int _ui_element_id = 0;
    private readonly AmlMetadata _metadata;
    private readonly DeallocStack _dealloc_stack;
    public ElementLifespanMan _elem_lifespan_man;
    private readonly JavaScriptDispatcher _js_dispatcher;
    public bool IsUiInitialized => _ui_element_id == 0;
    public AmlMetadata Metadata => _metadata;

    //document constructor must not allocate any resource that needs to be deallocated.
    public Document(ContextedTask root_context, AmlMetadata metadata)
    {
        _root_context = root_context;
        _metadata = metadata;
        _dealloc_stack = new();
        head = new();
        body = new(this);
        _elem_lifespan_man = new(body);
        var js_engine_constraints = new V8RuntimeConstraints();
        _js_dispatcher = new(js_engine_constraints, this, new Console(), new(_elem_lifespan_man));
        _title = string.Empty;
    }
    public void Init()
    {
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
    public void AddToDeallocStack(DeallocEntry entry) =>
        _dealloc_stack.Add(entry);

    /// <summary>
    /// CreateResourceLink creates ResourceLink rooted on this content.
    /// All ResourceLink variabls in this content must be generated here.
    /// </summary>
    /// <param name="src"></param>
    /// <param name="async_deploy_action"></param>
    /// <param name="async_remove_action"></param>
    /// <returns></returns>
    public ResourceLink CreateResourceLink(string src,
        Action<CachedResource> async_deploy_action,
        Action<CachedResource> async_remove_action
    ) => new(_root_context, _dealloc_stack, src,
        async_deploy_action, async_remove_action);

    public void StartJavaScript(CancellationToken token) =>
        _js_dispatcher.Start(token);

    /// <summary>
    /// Try to enqueue a javascript script to be executed.
    /// This is thread safe, but fails when the queue is full.
    /// </summary>
    /// <param name="filename"></param>
    /// <param name="script"></param>
    /// <returns></returns>
    public bool TryEnqueueJavaScript(string filename, object script) =>
        _js_dispatcher.TryEnqueue(filename, script);

    public void ScheduleOphanedElementCleanup() =>
        _js_dispatcher.TryEnqueue(string.Empty, new Action(_elem_lifespan_man.CleanupOrphans));

    /// <summary>
    /// Interrupt javascript execution and deactivates document. 
    /// This must be called only after token cancellation.
    /// </summary>
    public void Interrupt()
    {
        body.setActive(false);
        if (IsUiInitialized)
            Client.Client.RenderWriter.ItemSetActive(_ui_element_id, false);
        _js_dispatcher.Interrupt();
    }

    /// <summary>
    /// Waits for javascript dispatcher to finish execution and deallocates all resources.
    /// Calling this is mendatory.
    /// </summary>
    public void Join()
    {
        _js_dispatcher.Join();
        _iconSrc?.Dispose();
        _dealloc_stack.FreeAll();
        _elem_lifespan_man.ClearAll();
        if (IsUiInitialized)
            Client.Client.RenderWriter.DeleteItem(_ui_element_id);
    }

    // inner attributes
    public string _title;
    public BetterResourceLink? _iconSrc;

    public readonly Head head;
    public readonly Body body;

    //features
    public string title
    {
        get => _title;
        set
        {
            _title = value;
            Client.Client.RenderWriter.ItemSetTitle(_ui_element_id, value);
        }
    }
    public string? iconSrc
    {
        get => _iconSrc?.Src;
        set
        {
            if (value == null || value.Length == 0)
            {
                _iconSrc?.Dispose();
                _iconSrc = null;
                return;
            }
            if (_iconSrc != null)
            {
                _iconSrc.IsRemovalRequired = false;
                _iconSrc.Dispose();
            }
            _iconSrc = new(
                value,
                (resource) => //deploy
                {
                    switch (resource)
                    {
                    case StaticResource staticResource:
                        Client.Client.RenderWriter.ItemSetIcon(_ui_element_id, staticResource.ResourceID);
                        break;
                    case StaticSimpleResource staticSimpleResource:
                        Client.Client.RenderWriter.ItemSetIcon(_ui_element_id, staticSimpleResource.ResourceID);
                        break;
                    default:
                        Client.Client.RenderWriter.ConsolePrint("invalid content for icon");
                        break;
                    }
                },
                (resource) => //remove
                {
                    Client.Client.RenderWriter.ItemSetIcon(_ui_element_id, 0);
                }
            );
        }
    }
    public Element createElement(string tag, dynamic options)
    {
        Element result = tag switch
        {
            "o" => new Transform(this, tag, options),
            "obj" => new StaticMesh(this, options),
            "pbrm" => new PbrMaterial(this, options),
            _ => throw new ArgumentException("invalid tag")
        };
        _elem_lifespan_man.Add(result);
        return result;
    }
    public Element? getElementById(string id)
    {
        if (id == null) return null;
        if (id.Length == 0) return null;

        return body.getElementByIdHelper(id);
    }
    public void setEventListener(string event_name, dynamic callback)
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
    public void removeEventListener(string event_name)
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

