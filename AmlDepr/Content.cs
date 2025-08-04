//using AbyssCLI.Tool;

//namespace AbyssCLI.Aml
//{
//    internal class Content(AbyssLib.Host host, string sharer_hash, Guid uuid, AbyssURL URL, float[] Transform, int base_element)
//    {
//        public readonly string sharer_hash = sharer_hash;
//        public readonly Guid uuid = uuid;
//        public readonly AbyssURL URL = URL;
//        public float[] Transform = Transform;
//        public readonly int base_element = base_element;
//        public void Activate()
//        {
//            _documentImpl.Activate();
//        }
//        public void Close() => _documentImpl.Close();
//        public Task CloseAsync() => _documentImpl.CloseAsync();
//        private readonly DocumentImpl _documentImpl = new(
//                new Tool.Contexted(),
//                host,
//                new ResourceLoader(host, URL),
//                URL,
//                Transform,
//                base_element);
//    }
//}
