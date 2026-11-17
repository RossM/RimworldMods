using System.Xml;

namespace XylXenos;

/// <summary>
/// This updates genes on existing pawns which have had a <see cref="DefModExtension_GeneWithComps"/> added to use
/// <see cref="GeneWithComps"/> as their gene class.
/// </summary>
[UsedFromReflection]
internal class BackCompatibilityConverter_GeneWithComps : BackCompatibilityConverter
{
    public override bool AppliesToVersion(int majorVer, int minorVer) => true;

    public override string? BackCompatibleDefName(Type defType, string defName, bool forDefInjections = false, XmlNode? node = null)
    {
        return null;
    }

    public override Type? GetBackCompatibleType(Type baseType, string providedClassName, XmlNode? node)
    {
        if (baseType == typeof(Gene) && providedClassName is "Gene" or "Verse.Gene"
            && node != null && HasGeneWithCompsExtension(node))
            return typeof(GeneWithComps);

        return null;
    }

    private static bool HasGeneWithCompsExtension(XmlNode node)
    {
        XmlElement? defNode = node["def"];
        if (defNode == null || defNode.InnerText is "" or "null")
            return false;

        string defName = BackCompatibility.BackCompatibleDefName(typeof(GeneDef), defNode.InnerText, node: defNode);
        return DefDatabase<GeneDef>.GetNamedSilentFail(defName)?.Extension_GeneWithComps != null;
    }

    public override void PostExposeData(object obj)
    {
        if (Scribe.mode != LoadSaveMode.PostLoadInit)
            return;

        if (obj is GeneWithComps { comps: null } geneWithComps)
            geneWithComps.InitializeComps();
    }
}
