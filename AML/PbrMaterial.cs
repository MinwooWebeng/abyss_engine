#nullable enable
namespace AbyssCLI.AML;

#pragma warning disable IDE1006 //naming convension
public class PbrMaterial(Document document, object options) : Element(document, "pbrm", options)
{
    private PbrTextureResourceLink? _albedo = null;
    private PbrTextureResourceLink? _normal = null;
    private PbrTextureResourceLink? _roughness = null;
    private PbrTextureResourceLink? _metalic = null;
    private PbrTextureResourceLink? _specular = null;
    private PbrTextureResourceLink? _opacity = null;
    private PbrTextureResourceLink? _emission = null;

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

    private void Setter(ref PbrTextureResourceLink? target, string? value, ResourceRole role)
    {
        if (value == null || value.Length == 0)
        {
            target?.Dispose();
            target = null;
            return;
        }

        if (target != null)
        {
            target.IsRemovalRequired = false;
            target?.Dispose();
        }

        target = new PbrTextureResourceLink(value, ElementId, role);
    }

    private sealed class PbrTextureResourceLink(string src, int element_id, ResourceRole role)
        : BetterResourceLink(src)
    {
        public override void Deploy()
        {
            if (Resource == null)
                return;
            if (!Resource.MIMEType.StartsWith("image"))
            {
                Client.Client.RenderWriter.ConsolePrint("non-image resources cannot be used as a pbr texture");
                return;
            }
            Client.Client.RenderWriter.ElemAttachResource(element_id, Resource.ResourceID, role);
        }
        public override void Remove()
        {
            if (Resource == null)
                return;
            Client.Client.RenderWriter.ElemDetachResource(element_id, Resource.ResourceID);
        }
    }
}
#pragma warning restore IDE1006 //naming convension

