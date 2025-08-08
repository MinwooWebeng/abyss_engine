using Microsoft.ClearScript;

namespace AbyssCLI.AML;

#pragma warning disable IDE1006 //naming convension
public class Element
{
    protected readonly DeallocStack _dealloc_stack; // reference, used by derived classes
    internal readonly Dictionary<string, object> attributes = [];
    internal Element parent;
    internal readonly List<Element> _children = [];
    internal Element(DeallocStack dealloc_stack, string tag, object options)
    {
        _dealloc_stack = dealloc_stack;
        tagName = tag;
        if (options is ScriptObject optionsObj)
        {
            foreach (string property in optionsObj.PropertyNames)
            {
                object value = optionsObj.GetProperty(property);
                switch (property)
                {
                case "id":
                    if (value is string)
                    {
                        attributes[property] = value;
                    }
                    break;
                default:
                    attributes[property] = value;
                    break;
                }
            }
        }
    }
    internal Element getElementByIdHelper(string _id)
    {
        if (attributes.TryGetValue("id", out object id) && (string)id == _id)
        {
            return this;
        }
        return null;
    }

    //properties
    public Element[] children => [.. _children];
    public readonly string tagName;

    //methods
    public virtual void appendChild(Element child)
    {
        if (child == null) return;
        if (child.parent != null) child.remove();
        child.parent = this;
        _children.Add(child);
    }
    public virtual void remove()
    {
        if (parent == null) return;
        _ = parent._children.Remove(this);
    }
}
#pragma warning restore IDE1006 //naming convension

