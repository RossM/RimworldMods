using System.Xml;

namespace Xylib;

/// <summary>
///     Replaces genes in XenotypeDef gene lists and disables the original genes for xenotype creation and gene set generation.
/// </summary>
/// <remarks>
///     Use <c>&lt;Operation Class="Xylib.PatchOperation_ReplaceGenes"&gt;</c> with a <c>&lt;entries&gt;</c> list.
///     Each <c>&lt;li&gt;</c> contains <c>&lt;original&gt;</c> and <c>&lt;replacement&gt;</c> GeneDef defNames.
///     Originals must be unique. The operation fails without changing the XML if any original GeneDef is missing.
///     Each xenotype gene entry is replaced once, so replacements are not chained.
/// </remarks>
[UsedFromXml]
public class PatchOperationReplaceGenes : PatchOperation
{
    [UsedFromXml]
    public class Replacement
    {
        public required string original;
        public required string replacement;
    }

    public required List<Replacement> entries;

    [SuppressMessage("ReSharper", "ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract")]
    public override IEnumerable<string> ConfigErrors()
    {
        foreach (var error in base.ConfigErrors())
            yield return error;

        if (entries is not { Count: > 0 })
        {
            yield return $"{nameof(entries)} is null or empty";
            yield break;
        }

        var originals = new HashSet<string>(StringComparer.Ordinal);
        foreach (var pair in entries)
        {
            if (pair is null)
            {
                yield return $"{nameof(entries)} contains a null pair";
                continue;
            }

            if (string.IsNullOrWhiteSpace(pair.original))
                yield return $"{nameof(Replacement.original)} is null or empty";
            else if (!originals.Add(pair.original))
                yield return $"duplicate {nameof(Replacement.original)}: {pair.original}";

            if (string.IsNullOrWhiteSpace(pair.replacement))
                yield return $"{nameof(Replacement.replacement)} is null or empty";
        }
    }

    protected override bool ApplyWorker(XmlDocument xml)
    {
        ArgumentNullException.ThrowIfNull(xml);

        if (ConfigErrors().Any())
            return false;

        var replacementByOriginal = entries.ToDictionary(pair => pair.original, pair => pair.replacement, StringComparer.Ordinal);
        var genes = xml.SelectNodes("Defs/GeneDef")!.Cast<XmlNode>()
            .Where(node => node["defName"] is { } defName && replacementByOriginal.ContainsKey(defName.InnerText)).ToList();
        if (genes.Select(node => node["defName"]!.InnerText).Distinct(StringComparer.Ordinal).Count() != replacementByOriginal.Count)
            return false;

        foreach (var gene in genes)
        {
            GetOrCreateChild(gene, nameof(GeneDef.canGenerateInGeneSet)).InnerText = "false";

            XmlNode extension = GetOrCreateModExtension(gene, typeof(DefModExtension_GeneWithComps).FullName!);

            GetOrCreateChild(extension, nameof(DefModExtension_GeneWithComps.showInXenotypeCreation)).InnerText = "false";
        }

        foreach (XmlNode gene in xml.SelectNodes("Defs/XenotypeDef/genes/li")!)
        {
            if (replacementByOriginal.TryGetValue(gene.InnerText, out var replacement))
                gene.InnerText = replacement;
        }

        return true;
    }

    private static XmlNode GetOrCreateModExtension(XmlNode parent, string name)
    {
        // Get or create <modExtensions><li Class="{name}">.
        var modExtensions = GetOrCreateChild(parent, "modExtensions");
        var extension = modExtensions.SelectSingleNode($"li[@Class='{name}']");
        if (extension is null)
        {
            var element = parent.OwnerDocument!.CreateElement("li");
            element.SetAttribute("Class", name);
            extension = modExtensions.AppendChild(element)!;
        }

        return extension;
    }

    private static XmlElement GetOrCreateChild(XmlNode parent, string name)
    {
        return parent[name] ?? (XmlElement)parent.AppendChild(parent.OwnerDocument!.CreateElement(name))!;
    }
}
