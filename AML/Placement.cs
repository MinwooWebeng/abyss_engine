namespace AbyssCLI.AML
{
    public class Placement : Element
    {
        private readonly int _element_id = RenderID.ElementId;
        internal object _transform = (new Vector3(), new Quaternion());
        internal Placement(string tag, object options) : base(tag, options)
        {
        }
        public Vector3 pos
        {
            get
            {
                return _transform switch
                {
                    (Vector3 position, Quaternion _) => position,
                    _ => default,
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
                    _ => default,
                };
            }
        }
        public void setTransformAsValues(Vector3 pos, Quaternion rot)
        {
            _transform = (pos, rot);
            Client.Client.RenderWriter.ElemSetTransform(, pos.MarshalForABI(), rot.MarshalForABI());
        }
    }
}
