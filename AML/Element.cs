using Microsoft.ClearScript;

namespace AbyssCLI.AML
{
#pragma warning disable IDE1006 //naming convension
    public class Element
    {
        internal readonly Dictionary<string, object> js_properties = [];
        internal Element parent;
        internal readonly List<Element> _children;
        internal Element(string tag, object options)
        {
            tagName = tag;
            if (options is ScriptObject optionsObj)
            {
                foreach (var property in optionsObj.PropertyNames)
                {
                    var value = optionsObj.GetProperty(property);
                    switch (property)
                    {
                        case "id":
                            if (value is string)
                            {
                                js_properties[property] = value;
                            }
                            break;
                        default:
                            js_properties[property] = value;
                            break;
                    }
                }
            }
        }
        internal Element getElementByIdHelper(string _id)
        {
            if (js_properties.TryGetValue("id", out var id) && (string)id == _id)
            {
                return this;
            }
            return null;
        }

        //properties
        public Element[] children
        {
            get => [.. _children];
        }
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
            parent._children.Remove(this);
        }
    }
#pragma warning restore IDE1006 //naming convension
}
