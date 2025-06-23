using AbyssCLI.Aml;
using AbyssCLI.Tool;

namespace AbyssCLI.CAbstraction
{
    internal class Environment(AbyssLib.Host host, AbyssURL URL, int base_element)
    {
        public readonly AbyssURL URL = URL;
        public readonly int base_element = base_element;
        public void Activate()
        {
            Client.Client.RenderWriter.CreateElement(0, base_element);
            _documentImpl.Activate();
        }
        public Task CloseAsync()
        {
            Client.Client.RenderWriter.DeleteElement(base_element);
            return _documentImpl.CloseAsync();
        }
        private readonly DocumentImpl _documentImpl = new(
            new Tool.Contexted(),
            host,
            new ResourceLoader(host, URL),
            URL,
            Aml.DocumentImpl._defaultTransform,
            base_element);
    }
}
