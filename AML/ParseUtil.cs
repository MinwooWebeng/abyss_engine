using System.Xml;

namespace AbyssCLI.AML;

internal static class ParseUtil
{
    internal static async Task ParseAMLDocumentAsync(CancellationToken token, Document target, string document)
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

        foreach (XmlNode node in aml_elem)
        {
            if (node.NodeType != XmlNodeType.Element)
                continue;
            switch (node.Name)
            {
                case "head":
                    await ParseHead(token, target, node as XmlElement);
                    break;
                case "body":
                    await ParseBody(token, target, node as XmlElement);
                    break;
            }
        }
    }
    private static async Task ParseHead(CancellationToken token, Document target, XmlElement aml_elem)
    {
    }
    private static async Task ParseBody(CancellationToken token, Document target, XmlElement aml_elem)
    {
    }
}
