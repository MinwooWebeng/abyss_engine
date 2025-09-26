﻿#nullable enable

namespace AbyssCLI.AML;

#pragma warning disable IDE1006 //naming convension
public class StaticMesh : Transform
{
    public StaticMeshResourceLink? _mesh = null;
    public StaticMesh(Document document, object? options) : base(document, "obj", options)
    {
        if (!Attributes.TryGetValue("src", out string? mesh_src))
            return;
        src = mesh_src;
    }
    public override bool IsChildAllowed(Element child) =>
        child is PbrMaterial;

    public string? src
    {
        get => _mesh?.Src;
        set
        {
            _mesh?.Dispose();
            if (value == null || value.Length == 0)
            {
                _mesh = null;
                return;
            }
            _mesh = new(value, ElementId);
        }
    }
    public class StaticMeshResourceLink(string src, int element_id) : BetterResourceLink(src)
    {
        public override void Deploy()
        {
            if (Resource == null)
                return;
            if (!Resource.MIMEType.StartsWith("model") && Resource.MIMEType != "application/x-tgif")
            {
                Client.Client.RenderWriter.ConsolePrint("invalid content type for mesh: " + Resource.MIMEType);
                return;
            }
            Client.Client.RenderWriter.ElemAttachResource(element_id, Resource.ResourceID, ResourceRole.Mesh);
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
