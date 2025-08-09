using System.Xml;

namespace AbyssCLI.AML;

internal static class ParseUtil
{
    internal static void ParseAMLDocument(Document target, string document, CancellationToken token)
    {
        XmlDocument xml_document = new();
        xml_document.LoadXml(document);
        string doctype = xml_document.DocumentType?.Name ?? string.Empty;
        if (doctype != "aml")
            throw new Exception("doctype mismatch: " + doctype);

        XmlElement aml_elem = xml_document.DocumentElement;
        if (aml_elem == null || aml_elem.NodeType != XmlNodeType.Element || aml_elem.Name != "aml")
            throw new Exception("no <aml> : " + aml_elem?.Name ?? "");

        bool is_head_parsed = false;
        bool is_body_parsed = false;
        bool is_warned = false;
        foreach (XmlNode node in aml_elem.ChildNodes)
        {
            if (node.NodeType != XmlNodeType.Element)
                continue;
            switch (node.Name)
            {
            case "head" when !is_head_parsed && !is_body_parsed: // head must be parsed before body
                ParseHead(target, node as XmlElement);
                is_head_parsed = true;
                break;
            case "body" when !is_body_parsed:
                ParseBody(target.body, node as XmlElement, token);
                is_body_parsed = true;
                break;
            default:
                if (!is_warned)
                {
                    Client.Client.CerrWriteLine("Warning: found <" + node.Name + ">: <aml> may only have a <head> and a <body>, where <head> must come before <body>");
                    is_warned = true;
                }
                break;
            }
        }
    }
    private static void ParseHead(Document document, XmlElement head_elem)
    {
        foreach (XmlNode child in head_elem.ChildNodes)
        {
            if (child.NodeType != XmlNodeType.Element)
                continue;
            switch (child.Name)
            {
            case "script":
            {
                ParseScript(document, child as XmlElement);
            }
            break;
            case "title":
            {
                XmlNode text_node = child.FirstChild;
                if (text_node == null)
                    continue;
                if (text_node.NodeType != XmlNodeType.Text)
                {
                    Client.Client.CerrWriteLine("Warning: <title> tag must only have text content");
                    continue;
                }
                document.title = text_node.Value;
            }
            break;
            }
        }
    }
    private static void ParseScript(Document document, XmlElement script_elem)
    {
        // src - defer is the default behavior.
        string src = script_elem.GetAttribute("src");
        if (src != null && src.Length > 0)
        {
            Tool.TaskCompletionReference<Cache.CachedResource> script_src = Client.Client.Cache.GetReference(src);
            document.AddToDeallocStack(new(script_src));

            if (!document.TryEnqueueJavaScript(src, script_src))
            {
                Client.Client.CerrWriteLine("Ignored: too many scripts");
            }
            return;
        }

        // direct text script
        XmlNode text_node = script_elem.FirstChild;
        if (text_node == null)
        {
            Client.Client.CerrWriteLine("Warning: empty <script>");
        }
        if (text_node.NodeType != XmlNodeType.Text)
        {
            Client.Client.CerrWriteLine("Error: text <script> should only have text");
            return;
        }
        if (!document.TryEnqueueJavaScript(string.Empty, text_node.Value))
        {
            Client.Client.CerrWriteLine("Ignored: too many scripts");
        }
    }
    private static void ParseBody(Body target, XmlElement body_elem, CancellationToken token)
    {
    }
}
