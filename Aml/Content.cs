using AbyssCLI.ABI;
using AbyssCLI.Tool;
using static AbyssCLI.AbyssLib;

namespace AbyssCLI.Aml
{
    internal class Content(AbyssLib.Host host, RenderActionWriter renderActionWriter, StreamWriter cerr,
        AbyssURL URL, float[] Transform)
    {
        public AbyssURL URL = URL;
        public float[] Transform = Transform;
        public void Activate() => _documentImpl.Activate();
        public void Close() => _documentImpl.Close();
        public Task CloseAsync() => _documentImpl.CloseAsync();
        private readonly DocumentImpl _documentImpl = new(
                new Tool.Contexted(),
                host,
                renderActionWriter,
                cerr,
                new ResourceLoader(host, cerr, URL),
                URL,
                Transform);
    }
}
