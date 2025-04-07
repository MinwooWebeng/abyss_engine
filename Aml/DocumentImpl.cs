using AbyssCLI.ABI;
using AbyssCLI.Tool;
using System.Text;
using System.Xml;

namespace AbyssCLI.Aml
{
    internal sealed class DocumentImpl(Contexted root,
        RenderActionWriter renderActionWriter, StreamWriter cerr,
        ResourceLoader resourceLoader, AbyssURL url, vec3 body_position)
        : AmlNode(root, renderActionWriter, cerr, resourceLoader)
    {
        protected override async Task ActivateSelfCallback(CancellationToken token)
        {
            var response = await ResourceLoader.TryHttpRequestAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                ResponseCode = response.StatusCode;
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
            Children.Add(new HeadImpl(this, head_node, this));

            var body_node = aml_node.SelectSingleNode("body");
            Children.Add(new BodyImpl(this, body_node, body_position));
        }
        private readonly AbyssURL url = url;

        //valid only after Activation
        public HeadImpl Head => Children[0] as HeadImpl;
        public BodyImpl Body => Children[1] as BodyImpl;

        public System.Net.HttpStatusCode ResponseCode;
    }
}
