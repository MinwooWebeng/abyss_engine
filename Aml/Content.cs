using AbyssCLI.Tool;

namespace AbyssCLI.Aml
{
    internal class Content(AbyssLib.Host host, AbyssURL URL, float[] Transform)
    {
        public AbyssURL URL = URL;
        public float[] Transform = Transform;
        public void Activate()
        {
            _documentImpl.Activate();
        }
        public void Close() => _documentImpl.Close();
        public Task CloseAsync() => _documentImpl.CloseAsync();
        private readonly DocumentImpl _documentImpl = new(
                new Tool.Contexted(),
                host,
                new ResourceLoader(host, URL),
                URL,
                Transform);
    }
}
