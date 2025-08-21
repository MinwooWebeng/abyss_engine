#nullable enable

using AbyssCLI.Cache;

namespace AbyssCLI.AML
{
#pragma warning disable IDE1006 //naming convension
    public class Mesh : Placement
    {
        private readonly Document _document;
        internal ResourceLink? _mesh = null;
        internal Mesh(DeallocStack dealloc_stack, Document document, string tag, object options) : base(dealloc_stack, tag, options)
        {
            _document = document;

            if (!_attributes.TryGetValue("src", out var mesh_src)) return;
            src = mesh_src;
        }

        public string src
        {
            set
            {
                if (value == null || value.Length == 0)
                {
                    _mesh?.SynchronousCleanup();
                    _mesh = null;
                    return;
                }
                _mesh?.SynchronousCleanup(skip_remove: true);
                _mesh = _document.CreateResourceLink(value,
                    (resource) =>
                    {
                        switch (resource)
                        {
                            //case StaticSimpleResource
                        }
                    },
                    (resource) =>
                    {
                        //Client.Client.RenderWriter.
                    }
                );
            }
        }
    }
#pragma warning restore IDE1006 //naming convension
}
