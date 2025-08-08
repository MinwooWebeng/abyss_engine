namespace AbyssCLI.AML;

#pragma warning disable IDE1006 //naming convension
public partial class Document
{
    internal readonly int _root_element_id = RenderID.ElementId;
    private readonly DeallocStack _dealloc_stack;
    internal readonly JavaScriptDispatcher _js_dispatcher;

    //document constructor must not allocate any resource that needs to be deallocated.
    internal Document(DeallocStack dealloc_stack)
    {
        _dealloc_stack = dealloc_stack;
        _js_dispatcher = new(new(), this, new Console());
        head = new(_dealloc_stack);
        body = new(_dealloc_stack);
    }

    // inner attributes
    internal string _title; // TODO

    // exposed to JS
    public readonly Head head;
    public readonly Body body;
    public string doctype => "AML";
    public string title
    {
        get => _title; set => _title = value; //TODO
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
}
#pragma warning restore IDE1006 //naming convension

