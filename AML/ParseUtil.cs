using System.Xml;

namespace AbyssCLI.AML;

internal static class ParseUtil
{
    internal static void ParseAMLDocument(Document target, string document, CancellationToken token)
    {
        XmlDocument xml_document = new();
        xml_document.LoadXml(document);
        string doctype = xml_document.DocumentType?.Name ?? string.Empty;
        if (doctype != "aml" && doctype != "AML")
            throw new Exception("doctype mismatch: " + doctype);

        XmlElement aml_elem = null;
        foreach (XmlNode node in xml_document.DocumentElement.ChildNodes)
        {
            if (node.NodeType == XmlNodeType.Element)
            {
                aml_elem = node as XmlElement;
                break; // Found the first element, exit the loop
            }
        }
        if (aml_elem == null)
            throw new Exception("<aml> tag not found");

        bool is_head_parsed = false;
        bool is_body_parsed = false;
        bool is_warned = false;
        foreach (XmlNode node in aml_elem)
        {
            if (node.NodeType != XmlNodeType.Element)
                continue;
            switch (node.Name)
            {
            case "head" when !is_head_parsed && !is_body_parsed: // head must be parsed before body
                ParseHead(target, node as XmlElement, token);
                is_head_parsed = true;
                break;
            case "body" when !is_body_parsed:
                ParseBody(target.body, node as XmlElement, token);
                is_body_parsed = true;
                break;
            default:
                if (!is_warned)
                {
                    Client.Client.CerrWriteLine("Warning: <aml> may only have a <head> and a <body>, where <head> must come before <body>");
                    is_warned = true;
                }
                break;
            }
        }
    }
    private static void ParseHead(Document document, XmlElement head_elem, CancellationToken token)
    {
        foreach (XmlNode child in head_elem.ChildNodes)
        {
            if (child.NodeType != XmlNodeType.Element)
                continue;
            switch (child.Name)
            {
            case "script":
            {
                ParseScript(document.head, child as XmlElement, token);
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
                Client.Client.RenderWriter.ItemSetTitle(document._root_element_id, text_node.Value);
            }
            break;
            }
        }
    }
    private static void ParseScript(Head head, XmlElement script_elem, CancellationToken token)
    {
        // src - defer is the default behavior.
        string src = script_elem.GetAttribute("src");
        if (src != null && src.Length > 0)
        {
            var script_src = Client.Client.Cache.GetReference(src);
            head._scripts.Add((src, script_src));
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
            Client.Client.CerrWriteLine("Warning: text <script> should only have text");
            return;
        }
        head._scripts.Add((String.Empty, text_node.Value));
    }
    private static void ParseBody(Body target, XmlElement body_elem, CancellationToken token)
    {
    }
}
