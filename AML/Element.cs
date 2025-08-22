using Microsoft.ClearScript;
using System.Xml;

namespace AbyssCLI.AML;

#pragma warning disable IDE1006 //naming convension
public class Element
{
    protected readonly DeallocStack _dealloc_stack; // reference, used by derived classes
    internal readonly Dictionary<string, string> _attributes = [];
    internal Element _parent;
    internal readonly List<Element> _children = [];
    internal Element(DeallocStack dealloc_stack, string tag, object options)
    {
        _dealloc_stack = dealloc_stack;
        tagName = tag;
        if (options is ScriptObject optionsObj)
        {
            foreach (var prop in optionsObj.PropertyNames)
            {
                string value = optionsObj.GetProperty(prop)?.ToString();
                if (value != null)
                    _attributes[prop] = value;
            }
        }
        else if (options is XmlAttributeCollection xmlAttributes)
        {
            foreach (XmlAttribute entry in xmlAttributes)
            {
                _attributes[entry.Name] = entry.Value;
            }
        }
    }
    internal Element getElementByIdHelper(string _id)
    {
        if (_attributes.TryGetValue("id", out string id) && id == _id)
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
        if (child._parent != null) child.remove();
        child._parent = this;
        _children.Add(child);
    }
    public virtual void remove()
    {
        if (_parent == null) return;
        _ = _parent._children.Remove(this);
    }
}
#pragma warning restore IDE1006 //naming convension

