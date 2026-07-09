using System.Xml;

#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value

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

    public readonly Order order = Order.Prepend;

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
            if (order == Order.Append)
            {
                foreach (XmlNode childNode in node.ChildNodes)
                {
                    if (xmlNode.ChildNodes.OfType<XmlNode>()
                            .FirstOrDefault(xn => xn.Name == childNode.Name) is { } existingNode)
                    {
                        foreach (XmlNode grandchildNode in childNode.ChildNodes)
                            existingNode.AppendChild(
                                xmlNodeOwnerDocument.ImportNode(grandchildNode, deep: true));
                    }
                    else
                        xmlNode.AppendChild(xmlNodeOwnerDocument.ImportNode(childNode, deep: true));
                }
            }
            else if (order == Order.Prepend)
            {
                for (int num = node.ChildNodes.Count - 1; num >= 0; num--)
                {
                    var childNode = node.ChildNodes[num];
                    DebugAssert.NotNull(childNode);

                    if (xmlNode.ChildNodes.OfType<XmlNode>()
                            .FirstOrDefault(xn => xn.Name == childNode.Name) is { } existingNode)
                    {
                        foreach (XmlNode grandchildNode in childNode.ChildNodes)
                            existingNode.PrependChild(
                                xmlNodeOwnerDocument.ImportNode(grandchildNode, deep: true));
                    }
                    else
                        xmlNode.PrependChild(xmlNodeOwnerDocument.ImportNode(childNode, deep: true));
                }
            }
        }

        return result;
    }
}
