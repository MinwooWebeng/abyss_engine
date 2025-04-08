using System.Xml;

namespace AbyssCLI.Aml
{
    internal sealed class BodyImpl : AmlNode
    {
        public BodyImpl(AmlNode context, XmlNode xml_node, float[] transform)
            : base(context)
        {
            _root_elem = RenderID.ElementId;
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
            RenderActionWriter.CreateElement(0, _root_elem);
            RenderActionWriter.ElemSetPos(
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
            RenderActionWriter.MoveElement(_root_elem, -1);
        }
        protected override void CleanupSelfCallback()
        {
            RenderActionWriter.DeleteElement(_root_elem);
        }
        public static string Tag => "body";

        private readonly int _root_elem;
        private readonly float[] _transform;
    }
}
