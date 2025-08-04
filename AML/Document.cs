using AbyssCLI.ABI;

namespace AbyssCLI.AML
{
#pragma warning disable IDE1006 //naming convension
    public class Document
    {
        private readonly int _element_id;
        private readonly RenderActionWriter _renderer;
        internal Document(RenderActionWriter renderer, string documentURI)
        {
            _element_id = RenderID.ElementId;
            _renderer = renderer;
            this.documentURI = documentURI;

            //construction
            _renderer.CreateElement(0, _element_id);
        }
        public readonly Body body;
        public readonly string doctype = "AML";
        public readonly string documentURI;
        public readonly Head head;
        private string _title;
        public string title
        {
            get => _title;
            set
            {
                _title = value;
                _renderer.ItemSetTitle(_element_id, title);
            }
        }

        public static Element createElement(string tag, dynamic options) => tag switch
        {
            "head" => new Head(options),
            "body" => new Body(options),
            _ => new Element(tag, options)
        };
        public Element getElementById(string id)
        {
            if (id == null) return null;
            if (id.Length == 0) return null;

            var res = head.getElementByIdHelper(id);
            if (res != null) return res;

            res = body.getElementByIdHelper(id);
            return res;
        }
    }
#pragma warning restore IDE1006 //naming convension
}
