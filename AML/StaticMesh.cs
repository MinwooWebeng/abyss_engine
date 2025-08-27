#nullable enable

namespace AbyssCLI.AML
{
#pragma warning disable IDE1006 //naming convension
    public class StaticMesh : Transform
    {
        private readonly Document _document;
        public ResourceLink? _mesh = null;
        public StaticMesh(Document document, object options) : base(document, "obj", options)
        {
            _document = document;

            if (!Attributes.TryGetValue("src", out var mesh_src)) return;
            src = mesh_src;
        }
        public override bool IsChildAllowed(Element child) =>
            child is PbrMaterial;

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

                //variable to be captured, shared within this resource link.
                int resource_id = 0;
                _mesh = _document.CreateResourceLink(value,
                    (resource) =>
                    {
                        if (!resource.MIMEType.StartsWith("model"))
                        {
                            Client.Client.RenderWriter.ConsolePrint("invalid content type for mesh");
                            return;
                        }
                        resource_id = resource.ResourceID;
                        Client.Client.RenderWriter.ElemAttachResource(ElementId, resource_id, ResourceRole.Mesh);
                    },
                    (resource) =>
                    {
                        if (resource_id > 0)
                            Client.Client.RenderWriter.ElemDetachResource(ElementId, resource_id);
                    }
                );
            }
        }
    }
#pragma warning restore IDE1006 //naming convension
}
