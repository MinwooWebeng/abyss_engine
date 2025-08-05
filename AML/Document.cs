using Microsoft.ClearScript.V8;

namespace AbyssCLI.AML;

#pragma warning disable IDE1006 //naming convension
public class Document
{
    internal readonly int _root_element_id = RenderID.ElementId;
    private readonly DeallocStack _dealloc_stack;
    private readonly V8ScriptEngine _js_engine;
    internal Document(DeallocStack dealloc_stack)
    {
        Client.Client.RenderWriter.CreateElement(0, _root_element_id);
        _dealloc_stack = dealloc_stack;
        _dealloc_stack.Add(new(_root_element_id));

        _js_engine = new(
            new V8RuntimeConstraints()
            {
                MaxOldSpaceSize = 64 * 1024 * 1024
            }
        );
        _js_engine.Script.document = this;
        _js_engine.Script.console = new Console();
    }

    // exposed to JS
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
            Client.Client.RenderWriter.ItemSetTitle(_root_element_id, title);
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

