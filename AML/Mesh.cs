#nullable enable

using AbyssCLI.Cache;

namespace AbyssCLI.AML
{
#pragma warning disable IDE1006 //naming convension
    public class Mesh : Placement
    {
        private readonly Document _document;
        internal ResourceLink? _mesh = null;
        internal Mesh(DeallocStack dealloc_stack, Document document, object options) : base(dealloc_stack, "obj", options)
        {
            _document = document;

            if (!_attributes.TryGetValue("src", out var mesh_src)) return;
            src = mesh_src;
        }

        public string? src
        {
            get => _mesh?.Src;
            set
            {
                if (value == null || value.Length == 0)
                {
                    _mesh?.SynchronousCleanup();
                    _mesh = null;
                    return;
                }
                _mesh?.SynchronousCleanup();

                //variable to be captured
                int resource_id = 0;
                _mesh = _document.CreateResourceLink(value,
                    (resource) =>
                    {
                        if (resource is not StaticSimpleResource mesh_file)
                        {
                            Client.Client.RenderWriter.ConsolePrint("invalid content type for mesh");
                            return;
                        }
                        resource_id = mesh_file.ResourceID;
                        Client.Client.RenderWriter.ElemAttachResource(_element_id, resource_id);
                    },
                    (resource) =>
                    {
                        if (resource_id > 0)
                            Client.Client.RenderWriter.ElemDetachResource(_element_id, resource_id);
                    }
                );
            }
        }
    }
#pragma warning restore IDE1006 //naming convension
}
