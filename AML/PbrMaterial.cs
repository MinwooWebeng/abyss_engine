using AbyssCLI.Cache;
using System.ComponentModel.DataAnnotations;

#nullable enable
namespace AbyssCLI.AML
{
#pragma warning disable IDE1006 //naming convension
    public class PbrMaterial : Element
    {
        private readonly Document _document;
        private ResourceLink? _albedo = null;
        private ResourceLink? _normal = null;
        private ResourceLink? _roughness = null;
        private ResourceLink? _metalic = null;
        private ResourceLink? _specular = null;
        private ResourceLink? _opacity = null;
        private ResourceLink? _emission = null;
        public PbrMaterial(Document document, object options) : base(document, "pbrm", options)
        {
            _document = document;
        }
        public override bool IsParentAllowed(Element element) => element is StaticMesh;
        public string? albedo
        {
            get => _albedo?.Src;
            set => Setter(ref _albedo, value, ResourceRole.Albedo);
        }
        public string? normal
        {
            get => _normal?.Src;
            set => Setter(ref _normal, value, ResourceRole.Normal);
        }
        public string? roughness
        {
            get => _roughness?.Src;
            set => Setter(ref _roughness, value, ResourceRole.Roughness);
        }
        public string? metalic
        {
            get => _metalic?.Src;
            set => Setter(ref _metalic, value, ResourceRole.Metalic);
        }
        public string? specular
        {
            get => _specular?.Src;
            set => Setter(ref _specular, value, ResourceRole.Specular);
        }
        public string? opacity
        {
            get => _opacity?.Src;
            set => Setter(ref _opacity, value, ResourceRole.Opacity);
        }
        public string? emission
        {
            get => _emission?.Src;
            set => Setter(ref _emission, value, ResourceRole.Emission);
        }

        private void Setter(ref ResourceLink? target, string? value, ResourceRole role)
        {
            if (value == null || value.Length == 0)
            {
                target?.SynchronousCleanup();
                target = null;
                return;
            }
            target?.SynchronousCleanup(skip_remove: true);

            //variable to be captured, shared within this resource link.
            int resource_id = 0;
            target = _document.CreateResourceLink(value,
                (resource) =>
                {
                    if (!resource.MIMEType.StartsWith("image"))
                        throw new Exception("non-image resources cannot be used as a pbr texture");

                    resource_id = resource.ResourceID;
                    Client.Client.RenderWriter.ElemAttachResource(ElementId, resource_id, role);
                },
                (resource) =>
                {
                    if (resource_id != 0)
                        Client.Client.RenderWriter.ElemDetachResource(ElementId, resource_id);
                }
            );
        }
    }
#pragma warning restore IDE1006 //naming convension
}
