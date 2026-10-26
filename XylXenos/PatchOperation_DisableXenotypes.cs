using System.Xml;

namespace XylXenos;

[UsedFromXml]
public class PatchOperation_DisableXenotypes : PatchOperation
{
    public string? xenotype = null;

    public XmlContainer patches = new();

    protected override bool ApplyWorker(XmlDocument xml)
    {
        var disabledXenotypes = Settings.instance.disabledXenotypes;

        bool success = false;
        bool anyOperation = false;

        foreach (var disabledXenotype in disabledXenotypes)
        {
            if (xenotype != null && disabledXenotype != xenotype)
                continue;

            XmlNode patchesCopy = patches.node.CloneNode(deep: true);

            foreach (XmlNode text in patchesCopy.SelectNodes(".//text()")!)
                text.Value = text.Value!.Replace("{0}", disabledXenotype);

            foreach (XmlNode childNode in patchesCopy.ChildNodes)
            {
                if (childNode.NodeType != XmlNodeType.Element)
                    continue;

                if (childNode.Name != "li")
                {
                    Log.Error($"Unexpected element in patch XML; got {childNode.Name}, expected 'li'");
                    continue;
                }

                PatchOperation patchOperation = DirectXmlToObject.ObjectFromXml<PatchOperation>(childNode, doPostLoad: false);
                patchOperation.sourceFile = sourceFile;
                success |= patchOperation.Apply(xml);
                anyOperation = true;
            }
        }

        return success || !anyOperation;
    }
}
