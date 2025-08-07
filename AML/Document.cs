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
    }

    // inner attributes
    internal string _title; // TODO
    internal object _transform = (new Vector3(), new Quaternion()); 
    //(Vector3, Quaternion) or TaskCompletionReference<Cache.CachedResource>

    // exposed to JS
    public readonly Head head = new();
    public readonly Body body = new();
    public readonly string doctype = "AML";
    public string title
    {
        get => _title; set => _title = value; //TODO
    }
    public Vector3 pos
    {
        get
        {
            return _transform switch
            {
                (Vector3 position, Quaternion _) => position,
                _ => default,
            };
        }
    }
    public Quaternion rot
    {
        get
        {
            return _transform switch
            {
                (Vector3 _, Quaternion rotation) => rotation,
                _ => default,
            };
        }
    }
    public Element createElement(string tag, dynamic options) => tag switch
    {
        _ => new Element(tag, options)
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
    //public void setTransform(object any)
    //{
    //}
    public void setTransformAsValues(Vector3 pos, Quaternion rot)
    {
        _transform = (pos, rot);
        Client.Client.RenderWriter.ElemSetTransform(_root_element_id, pos.MarshalForABI(), rot.MarshalForABI());
    }
}
#pragma warning restore IDE1006 //naming convension

