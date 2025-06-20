using System.Xml;

namespace AbyssCLI.Aml
{
    internal sealed class BodyImpl : AmlNode
    {
        public BodyImpl(AmlNode context, XmlNode xml_node, float[] transform, int base_element)
            : base(context)
        {
            _root_elem = base_element;
            _transform = transform;
            foreach (XmlNode child in xml_node?.ChildNodes)
            {
                Children.Add(child.Name switch
                {
                    "o" => new GroupImpl(this, _root_elem, child),
                    "mesh" => new MeshImpl(this, _root_elem, child),
                    _ => throw new Exception("Invalid tag in <body>"),
                });
            }
        }
        protected override Task ActivateSelfCallback(CancellationToken token)
        {
            Client.Client.RenderWriter.CreateElement(0, _root_elem);
            Client.Client.RenderWriter.ElemSetPos(
                _root_elem, 
                new ABI.Vec3{
                    X = _transform[0],
                    Y = _transform[1],
                    Z = _transform[2],
                }, 
                new ABI.Vec4
                {
                    W = _transform[3],
                    X = _transform[4],
                    Y = _transform[5],
                    Z = _transform[6],
                });
            return Task.CompletedTask;
        }
        protected override void DeceaseSelfCallback()
        {
            Client.Client.RenderWriter.MoveElement(_root_elem, -1);
        }
        protected override void CleanupSelfCallback()
        {
            Client.Client.RenderWriter.DeleteElement(_root_elem);
        }
        public static string Tag => "body";

        private readonly int _root_elem;
        private readonly float[] _transform;
    }
}
