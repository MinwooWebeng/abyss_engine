namespace AbyssCLI.AML
{
#pragma warning disable IDE1006 //naming convension
    // element that has transform.
    public class Placement : Element
    {
        protected readonly int _element_id = RenderID.ElementId;
        internal (Vector3, Quaternion) _transform = (new(), new());
        //(Vector3, Quaternion) or TaskCompletionReference<Cache.CachedResource>
        internal Placement(DeallocStack dealloc_stack, string tag, object options) : base(dealloc_stack, tag, options)
        {
            Client.Client.RenderWriter.CreateElement(-1, _element_id);
            dealloc_stack.Add(new(_element_id, DeallocEntry.EDeallocType.RendererElement));

            //apply attributes
            foreach (var entry in _attributes)
            {
                switch (entry.Key)
                {
                case "pos":
                    pos = entry.Value;
                    break;
                case "rot":
                    rot = entry.Value;
                    break;
                default:
                    break;
                }
            }
        }
        public string pos
        {
            set
            {
                _transform.Item1 = new(value);
                Client.Client.RenderWriter.ElemSetTransform(
                    _element_id,
                    _transform.Item1.MarshalForABI(),
                    _transform.Item2.MarshalForABI()
                );
            }
            get
            {
                return _transform switch
                {
                    (Vector3 position, Quaternion _) => position.ToString(),
                    _ => "undefined",
                };
            }
        }
        public string rot
        {
            set
            {
                _transform.Item2 = new(value);
                Client.Client.RenderWriter.ElemSetTransform(
                    _element_id,
                    _transform.Item1.MarshalForABI(),
                    _transform.Item2.MarshalForABI()
                );
            }
            get
            {
                return _transform switch
                {
                    (Vector3 _, Quaternion rotation) => rotation.ToString(),
                    _ => "undefined",
                };
            }
        }
        public void setActive(bool active)
        {
            Client.Client.RenderWriter.ElemSetActive(_element_id, active);
        }
        public void setTransformAsValues(Vector3 pos, Quaternion rot)
        {
            _transform = (pos, rot);
            Client.Client.RenderWriter.ElemSetTransform(_element_id, pos.MarshalForABI(), rot.MarshalForABI());
        }
        public override void appendChild(Element child)
        {
            if (child is Placement p_child)
            {
                Client.Client.RenderWriter.MoveElement(p_child._element_id, _element_id);
            }
            base.appendChild(child);
        }
        public override void remove()
        {
            Client.Client.RenderWriter.MoveElement(_element_id, -1);
            base.remove();
        }
    }
#pragma warning restore IDE1006 //naming convension
}
