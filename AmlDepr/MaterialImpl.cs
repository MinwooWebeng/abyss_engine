using AbyssCLI.Tool;
using System.Xml;

namespace AbyssCLI.AmlDepr;

[Obsolete]
internal class MaterialImpl : AmlNode
{
    [Obsolete]
    public MaterialImpl(MeshImpl parent_node, XmlNode xml_node)
        : base(parent_node)
    {
        Id = xml_node.Attributes["id"]?.Value;
        if (Id != null)
        {
            ElementDictionary[Id] = this;
        }
        if (int.TryParse(xml_node.Attributes["pos"]?.Value, out int pos))
            Pos = pos;
        Shader = xml_node.Attributes["shader"]?.Value;
        if (Shader == null) { throw new Exception("shader attribute is null in <Material" + (Id == null ? "" : (":" + Id)) + ">"); }

        MaterialWaiterGroup = new();
        _parent_node = parent_node;
        foreach (XmlNode child in xml_node.ChildNodes)
        {
            Children.Add(child.Name switch
            {
                "img" => new ImageImpl(this, child),
                _ => throw new Exception("Invalid tag in <material" + (Id == null ? "" : (":" + Id)) + ">"),
            });
        }
    }

    [Obsolete]
    protected override Task ActivateSelfCallback(CancellationToken token)
    {
        int component_id = RenderID.ComponentId;
        Client.Client.RenderWriter.CreateMaterialV(component_id, Shader);
        if (!MaterialWaiterGroup.TryFinalizeValue(component_id))
        {
            Client.Client.RenderWriter.DeleteMaterial(component_id);
            return Task.CompletedTask;
        }

        if (!_parent_node.MeshWaiterGroup.TryGetValueOrWaiter(out int mesh_id, out _parent_waiter))
        {
            mesh_id = _parent_waiter.GetValue();
        }
        token.ThrowIfCancellationRequested();
        if (mesh_id == 0) //target mesh is not prepared
            return Task.CompletedTask;

        Client.Client.RenderWriter.StaticMeshSetMaterial(mesh_id, Pos, component_id);
        return Task.CompletedTask;
    }

    [Obsolete]
    protected override void DeceaseSelfCallback()
    {
        _parent_waiter?.Finalize(0);
        _ = MaterialWaiterGroup.TryFinalizeValue(0);
    }

    [Obsolete]
    protected override void CleanupSelfCallback()
    {
        int component_id = MaterialWaiterGroup.GetValue();
        if (component_id != 0)
            Client.Client.RenderWriter.DeleteMaterial(component_id);
    }
    public static string Tag => "material";
    public string Id { get; }
    public int Pos { get; }
    public string Shader { get; }

    //TODO: src and mime for custom shader support.
    [Obsolete]
    public WaiterGroup<int> MaterialWaiterGroup; //actually, we don't need this for now.
    //this is only usable after implementing third party shading support.
    //shader compilation will be main wait target.

    private readonly MeshImpl _parent_node;
    [Obsolete]
    private Waiter<int> _parent_waiter;
}
