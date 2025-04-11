using AbyssCLI.Tool;
using System.Text;
using System.Xml;

namespace AbyssCLI.Aml
{
    internal sealed class DocumentImpl(Contexted root,
        AbyssLib.Host host,
        ResourceLoader resourceLoader, AbyssURL url, float[] transform)
        : AmlNode(root, resourceLoader)
    {
        public static readonly float[] _defaultTransform = [0.0f, 0.0f, 0.0f, 1.0f, 0.0f, 0.0f, 0.0f];
        protected override async Task ActivateSelfCallback(CancellationToken token)
        {
            if (!ResourceLoader.IsValid)
            {
                Client.Client.Cerr.WriteLine("invalid origin: failed to construct ResourceLoader");
                return;
            }

            var response = await ResourceLoader.TryHttpGetAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                ResponseCode = response.StatusCode;
                Client.Client.Cerr.WriteLine(ResponseCode.ToString());
                return;
            }

            //AML parsing
            XmlDocument doc = new();
            doc.LoadXml(Encoding.UTF8.GetString(await response.Content.ReadAsByteArrayAsync(token)));
            // Check for the DOCTYPE
            if (doc.DocumentType == null || doc.DocumentType.Name != "AML")
                throw new Exception("DOCTYPE mismatch");

            XmlNode aml_node = doc.SelectSingleNode("/aml");
            if (aml_node == null || aml_node.ParentNode != doc)
                throw new Exception("<aml> not found");

            XmlNode head_node = aml_node.SelectSingleNode("head");
            if (head_node == null)
            {
                Children.Add(new HeadImpl(this, doc.CreateNode(XmlNodeType.Element, "head", null), host, this));
            }
            else
            {
                Children.Add(new HeadImpl(this, head_node, host, this));
            }

            var body_node = aml_node.SelectSingleNode("body");
            if (body_node == null)
            {
                Children.Add(new BodyImpl(this, doc.CreateNode(XmlNodeType.Element, "body", null), _defaultTransform));
            }
            else
            {
                Children.Add(new BodyImpl(this, body_node, transform));
            }
        }
        private readonly AbyssLib.Host host = host;
        private readonly AbyssURL url = url;

        //valid only after Activation
        public HeadImpl Head => Children[0] as HeadImpl;
        public BodyImpl Body => Children[1] as BodyImpl;

        public System.Net.HttpStatusCode ResponseCode;
    }
}
