namespace AbyssCLI.AML;

#pragma warning disable IDE1006 //naming convension
public class Document
{
    internal readonly int _root_element_id;
    internal Document()
    {
        _root_element_id = RenderID.ElementId;

        //construction
        _renderer.CreateElement(0, _root_element_id);
    }
    public readonly Body body = new();
    public readonly string doctype = "AML";
    public readonly Head head = new();
    private string _title;
    public string title
    {
        get => _title;
        set
        {
            _title = value;
            _renderer.ItemSetTitle(_root_element_id, title);
        }
    }

    public static Element createElement(string tag, dynamic options) => tag switch
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
}
#pragma warning restore IDE1006 //naming convension

