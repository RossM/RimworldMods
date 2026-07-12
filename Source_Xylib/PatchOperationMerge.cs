using System.Xml;

namespace Xylib;

/// <summary>
///     Adds XML to each node selected by <see cref="PatchOperationPathed.xpath" />, creating missing containers and
///     adding to existing ones as needed.
/// </summary>
/// <remarks>
///     <para>
///         Use this when a normal <c>PatchOperationAdd</c> would create duplicate parent elements. For example, adding
///         <c>&lt;exclusionTags&gt;&lt;li&gt;Foo&lt;/li&gt;&lt;/exclusionTags&gt;</c> to a <c>GeneDef</c> will create
///         <c>exclusionTags</c> if it is missing, or add <c>Foo</c> to the existing <c>exclusionTags</c> list if it is
///         already present. The same pattern is useful for lists such as <c>nullifyingGenes</c>, <c>modExtensions</c>,
///         and <c>comps</c>.
///     </para>
///     <para>
///         Matching containers are merged recursively, so nested XML can be extended in one operation. List items are
///         usually added as new entries, but items in <c>modExtensions</c> and <c>comps</c> are merged by their
///         <c>Class</c> attribute. This lets a patch add fields to an existing mod extension or comp instead of adding a
///         duplicate entry with the same class.
///     </para>
///     <para>
///         Set <c>Merge="true"</c> on an element in <see cref="value" /> to force it to merge with a matching element,
///         or <c>Merge="false"</c> to force it to be added as a separate node. Set <see cref="order" /> to
///         <see cref="Order.Prepend" /> to insert new entries before existing entries instead of after them. Set
///         <see cref="debug" /> to true to log the selected node before and after the merge.
///     </para>
/// </remarks>
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
