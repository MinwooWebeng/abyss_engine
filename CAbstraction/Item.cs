using AbyssCLI.Aml;
using AbyssCLI.Tool;

namespace AbyssCLI.CAbstraction
{
    internal class Item(AbyssLib.Host host, string sharer_hash, Guid uuid, AbyssURL URL, int base_element, float[] spawn_transform)
    {
        public readonly string sharer_hash = sharer_hash;
        public readonly Guid uuid = uuid;
        public readonly AbyssURL URL = URL;
        public readonly int base_element = base_element;
        public readonly float[] spawn_transform = spawn_transform;
        public void Activate()
        {
            Client.Client.RenderWriter.CreateItem(base_element, sharer_hash, Google.Protobuf.ByteString.CopyFrom(uuid.ToByteArray()));
            _documentImpl.Activate();
        }
        public Task CloseAsync()
        {
            Client.Client.RenderWriter.DeleteItem(base_element);
            return _documentImpl.CloseAsync();
        }
        private readonly DocumentImpl _documentImpl = new(
            new Tool.Contexted(),
            host,
            new ResourceLoader(host, URL),
            URL,
            spawn_transform,
            base_element);
    }
}
