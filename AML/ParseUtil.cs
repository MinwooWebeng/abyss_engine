using System.Xml;

namespace AbyssCLI.AML;

internal static class ParseUtil
{
    internal static async Task ParseAMLDocumentAsync(Document target, string document, CancellationToken token)
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
        Head target = document.head;
        foreach (XmlNode child in head_elem.ChildNodes)
        {
            if (child.NodeType != XmlNodeType.Element)
                continue;
            switch (child.Name)
            {
            case "script":
            {
                XmlNode text_node = child.FirstChild;
                if (text_node == null)
                {
                    Client.Client.CerrWriteLine("Warning: empty <script>");
                }
                if (text_node.NodeType != XmlNodeType.Text)
                {
                    Client.Client.CerrWriteLine("Warning: <script> tag must only have text content");
                    continue;
                }
                target._scripts.Add(text_node.Value);
            }
            break;
            case "title":
            {
                XmlNode text_node = child.FirstChild;
                if (text_node == null)
                {
                    Client.Client.CerrWriteLine("Warning: empty <script>");
                }
                if (text_node.NodeType != XmlNodeType.Text)
                {
                    Client.Client.CerrWriteLine("Warning: <script> tag must only have text content");
                    continue;
                }

            }
            break;
            }
        }
    }
    private static void ParseBody(Body target, XmlElement body_elem, CancellationToken token)
    {
    }
}
