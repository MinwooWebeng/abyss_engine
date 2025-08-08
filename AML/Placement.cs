namespace AbyssCLI.AML
{
#pragma warning disable IDE1006 //naming convension
    // element that has transform.
    public class Placement : Element
    {
        private readonly int _element_id = RenderID.ElementId;
        internal object _transform = (new Vector3(), new Quaternion());
        //(Vector3, Quaternion) or TaskCompletionReference<Cache.CachedResource>
        internal Placement(DeallocStack _dealloc_stack, string tag, object options) : base(_dealloc_stack, tag, options)
        {
            Client.Client.RenderWriter.CreateElement(-1, _element_id);
            _dealloc_stack.Add(new(_element_id, DeallocEntry.EDeallocType.RendererElement));
        }
        public Vector3 pos
        {
            get
            {
                return _transform switch
                {
                    (Vector3 position, Quaternion _) => position,
                    _ => new(),
                };
            }
        }
        public Quaternion rot
        {
            get
            {
                return _transform switch
                {
                    (Vector3 _, Quaternion rotation) => rotation,
                    _ => new(),
                };
            }
        }
        public void setTransformAsValues(Vector3 pos, Quaternion rot)
        {
            _transform = (pos, rot);
            Client.Client.RenderWriter.ElemSetTransform(_element_id, pos.MarshalForABI(), rot.MarshalForABI());
        }
        public override void appendChild(Element child)
        {
            if (child is Placement p_child)
                Client.Client.RenderWriter.MoveElement(p_child._element_id, _element_id);
            base.appendChild(child);
        }
    }
#pragma warning restore IDE1006 //naming convension
}
