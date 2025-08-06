using Microsoft.ClearScript.V8;
using System.Numerics;

namespace AbyssCLI.AML;

#pragma warning disable IDE1006 //naming convension
public class Document
{
    internal readonly int _root_element_id = RenderID.ElementId;
    private readonly DeallocStack _dealloc_stack;
    internal readonly V8ScriptEngine _js_engine;
    internal readonly AmlMetadata _metadata;

    //document constructor must not allocate any resource that needs to be deallocated.
    internal Document(DeallocStack dealloc_stack)
    {
        _dealloc_stack = dealloc_stack;
        _js_engine = new(
            new V8RuntimeConstraints()
            {
                MaxOldSpaceSize = 64 * 1024 * 1024
            }
        );
        _js_engine.Script.document = this;
        _js_engine.Script.console = new Console();
    }
    internal void ExecuteScript(string file_name, string script_text) //interesting?
    {
        try
        {
            _js_engine.Execute(new Microsoft.ClearScript.DocumentInfo(file_name), script_text);
        }
        catch (Microsoft.ClearScript.ScriptEngineException ex)
        {
            Client.Client.CerrWriteLine("javascript error: " + ex.Message);
            Client.Client.CerrWriteLine("stack trace: " + ex.ErrorDetails);
        }
    }

    // inner attributes
    internal string _title; // TODO
    internal object _transform; //null or (Vector3, Quaternion) or TaskCompletionReference<Cache.CachedResource>

    // exposed to JS
    public readonly Body body = new();
    public readonly string doctype = "AML";
    public readonly Head head = new();
    public string title
    {
        get => _title; set => _title = value;//TODO
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
    //public void setTransform()
    //{
    //}
    public void setTransformAsValues(Vector3 pos, Quaternion rot)
    {
        ABI.Vec3 abi_pos = new();
        ABI.Vec4 abi_quat = new();
        _transform = (pos, rot);
        Client.Client.RenderWriter.ElemSetTransform(_root_element_id, abi_pos, abi_quat);
    }
}
#pragma warning restore IDE1006 //naming convension

