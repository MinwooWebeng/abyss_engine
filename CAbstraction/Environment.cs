using AbyssCLI.Aml;
using AbyssCLI.Tool;

namespace AbyssCLI.CAbstraction
{
    internal class Environment(AbyssLib.Host host, AbyssURL URL)
    {
        public readonly AbyssURL URL = URL;
        public void Activate()
        {
            _documentImpl.Activate();
        }
        public Task CloseAsync()
        {
            return _documentImpl.CloseAsync();
        }
        private readonly DocumentImpl _documentImpl = new(
            new Tool.Contexted(),
            host,
            new ResourceLoader(host, URL),
            URL,
            Aml.DocumentImpl._defaultTransform,
            0);
    }
}
