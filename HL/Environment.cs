using AbyssCLI.AML;
using AbyssCLI.Tool;
using System.Xml;

namespace AbyssCLI.HL
{
    internal class Environment : ContextedTask
    {
        private readonly AbyssLib.Host _host;
        private readonly AbyssURL _url;
        private readonly int element_id = RenderID.ElementId;
        private TaskCompletionReference<Cache.CachedResource> __document_cache_ref; //only to keep cache live.
        public Environment(AbyssLib.Host host, AbyssURL url)
        {
            _host = host;
            _url = url;
        }
        protected override void OnNoExecution() { }
        protected override void SynchronousInit()
        {
            //debug
            Client.Client.RenderWriter.ConsolePrint("||>opening environment(" + element_id.ToString() + ")<||");
            Client.Client.RenderWriter.CreateElement(0, element_id);
        }
        protected override async Task AsyncTask(CancellationToken token)
        {
            var document_cache_entry = Client.Client.Cache.Get(_url.ToString());
            if (!document_cache_entry.TryGetReference(out __document_cache_ref))
            {
                throw new Exception("fatal:::failed to get document resource reference");
            }
            var doc_resource = await __document_cache_ref.Task.WaitAsync(token);
            if (doc_resource is not Cache.Text || doc_resource.MIMEType != "text/aml")
            {
                throw new Exception("fatal:::MIME mismatch");
            }
            var raw_document = await (doc_resource as Cache.Text).ReadAsync(token);

            XmlDocument xml_document = new();
            xml_document.LoadXml(raw_document);
            var doctype = xml_document.DocumentType?.Name ?? string.Empty;
            if (doctype != "aml" && doctype != "AML")
                throw new Exception("doctype mismatch: " + doctype);

            XmlElement aml = null;
            foreach (XmlNode node in xml_document.DocumentElement.ChildNodes)
            {
                if (node.NodeType == XmlNodeType.Element)
                {
                    aml = node as XmlElement;
                    break; // Found the first element, exit the loop
                }
            }
            if (aml == null)
                throw new Exception("<aml> tag not found");
        }
        protected override void OnSuccess()
        {
            //debug
            Client.Client.RenderWriter.ConsolePrint("||>loaded environment(" + element_id.ToString() + ")<||");
        }
        protected override void OnStop()
        {
            //debug
            Client.Client.RenderWriter.ConsolePrint("||>stopped loading environment(" + element_id.ToString() + ")<||");
        }
        protected override void OnFail(Exception e)
        {
            Client.Client.CerrWriteLine(e.ToString());
        }
        protected override void SynchronousExit()
        {
            //debug
            Client.Client.RenderWriter.DeleteElement(element_id);
            Client.Client.RenderWriter.ConsolePrint("||>closed environment(" + element_id.ToString() + ")<||");
        }

        //thread-safe kills - TODO
        //public void Close()
    }
}
