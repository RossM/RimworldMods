using System.Xml;

namespace XylXenos;

// This works like PatchOperationAdd, except if the node to be added already exists, the new node's children are added
// to the existing mode. For example, if the existing node is
//
// <def>
//   <list>
//     <li>Foo</li>
//   </list>
// </def>
//
// and value is
//
// <list>
//   <li>Bar</li>
// </list>
//
// the result will be
//
// <def>
//   <list>
//     <li>Foo</li>
//     <li>Bar</li>
//   </list>
// </def>
[UsedFromXml]
public class PatchOperationMerge : PatchOperationPathed
{
    public enum Order
    {
        Append,
        Prepend
    }

    public XmlContainer? value;

    public readonly Order order = Order.Append;

    public readonly bool debug = false;

    public override IEnumerable<string> ConfigErrors()
    {
        if (xpath is null)
            yield return $"{nameof(xpath)} is null";
        if (value is null)
            yield return $"{nameof(value)} is null";
        else if (value.node is null)
            yield return $"{nameof(value)} is not an XML element";
    }

    protected override bool ApplyWorker(XmlDocument xml)
    {
        if (xml == null)
            throw new ArgumentNullException(nameof(xml));

        DebugAssert.NotNull(xpath);
        DebugAssert.NotNull(value);
        DebugAssert.NotNull(value.node);

        XmlNode node = value.node;
        var result = false;

        XmlNodeList? nodes = xml.SelectNodes(xpath);

        DebugAssert.NotNull(nodes);
        foreach (XmlNode xmlNode in nodes)
        {
            if (debug)
                Debug.Log($"[{nameof(PatchOperationMerge)}] xpath: {xpath}");

            result = true;
            XmlDocument? xmlNodeOwnerDocument = xmlNode.OwnerDocument;
            if (xmlNodeOwnerDocument == null)
                continue;

            var childNodes = node.ChildNodes.OfType<XmlNode>().ToList();
            if (order == Order.Prepend)
                childNodes.Reverse();

            if (debug)
                Debug.Log($"[{nameof(PatchOperationMerge)}] Pre-merge: {xmlNode.OuterXml}");

            foreach (XmlNode childNode in childNodes)
                Merge(xmlNode, childNode, xmlNodeOwnerDocument);

            if (debug)
                Debug.Log($"[{nameof(PatchOperationMerge)}] Post-merge: {xmlNode.OuterXml}");
        }

        return result;
    }

    private void Merge(XmlNode targetNode, XmlNode child, XmlDocument xmlNodeOwnerDocument)
    {
        if (targetNode.ChildNodes.OfType<XmlNode>().FirstOrDefault(xn => CanMerge(xn, child)) is { } mergeTarget)
        {
            var grandchildren = child.ChildNodes.OfType<XmlNode>().ToList();
            if (order == Order.Prepend)
                grandchildren.Reverse();

            foreach (XmlNode grandchild in grandchildren)
                Merge(mergeTarget, grandchild, xmlNodeOwnerDocument);

            return;
        }

        // ReSharper disable once SwitchStatementHandlesSomeKnownEnumValuesWithDefault
        switch (child.NodeType)
        {
            case XmlNodeType.Element:
            {
                switch (order)
                {
                    case Order.Append: targetNode.AppendChild(xmlNodeOwnerDocument.ImportNode(child, deep: true)); break;
                    case Order.Prepend: targetNode.PrependChild(xmlNodeOwnerDocument.ImportNode(child, deep: true)); break;
                    default: throw new ArgumentOutOfRangeException();
                }

                break;
            }
            case XmlNodeType.Text:
            {
                DebugAssert.NotNull(child.Value);

                targetNode.InnerText = child.Value;
                break;
            }
            default: throw new NotSupportedException();
        }
    }

    private bool CanMerge(XmlNode first, XmlNode second)
    {
        if (first.NodeType != XmlNodeType.Element || second.NodeType != XmlNodeType.Element)
            return false;
        if (first.Name != second.Name)
            return false;
        if (first.Attributes?["Class"]?.Value != second.Attributes?["Class"]?.Value)
            return false;
        return second.Attributes?["Merge"]?.Value?.ToLowerInvariant() switch
        {
            "true" => true,
            "false" => false,
            _ => first.Name != "li" || first.ParentNode?.Name == "modExtensions" || first.ParentNode?.Name == "comps",
        };
    }
}
