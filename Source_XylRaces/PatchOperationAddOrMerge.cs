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
public class PatchOperationAddOrMerge : PatchOperationPathed
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
                Log.Message($"{xpath} -> {xmlNode.OuterXml}");

            result = true;
            XmlDocument xmlNodeOwnerDocument = xmlNode.OwnerDocument;
            if (xmlNodeOwnerDocument == null)
                continue;

            var childNodes = node.ChildNodes.OfType<XmlNode>().ToList();
            if (order == Order.Prepend)
                childNodes.Reverse();

            foreach (XmlNode childNode in childNodes)
            {
                // Debug.Log($"[{nameof(PatchOperationAddOrMerge)}] Pre-merge: {xmlNode.OuterXml}");

                Merge(xmlNode, childNode, xmlNodeOwnerDocument);

                // Debug.Log($"[{nameof(PatchOperationAddOrMerge)}] Post-merge: {xmlNode.OuterXml}");
            }
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
                targetNode.InnerText = child.Value;
                break;
            }
            default: throw new NotSupportedException();
        }
    }

    private bool CanMerge(XmlNode first, XmlNode second) =>
        first.NodeType == XmlNodeType.Element && (first.Name != "li" || first.Attributes?["Class"] != null) && first.Name == second.Name &&
        first.Attributes?["Class"]?.Value == second.Attributes?["Class"]?.Value;
}
