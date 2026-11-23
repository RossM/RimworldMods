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
            && node is not null && HasGeneWithCompsExtension(node))
            return typeof(GeneWithComps);

        return null;
    }

    private static bool HasGeneWithCompsExtension(XmlNode node)
    {
        if (node["def"] is not { } defNode || defNode.InnerText is "" or "null")
            return false;

        string defName = BackCompatibility.BackCompatibleDefName(typeof(GeneDef), defNode.InnerText, node: defNode);
        return DefDatabase<GeneDef>.GetNamedSilentFail(defName)?.Extension_GeneWithComps is not null;
    }

    public override void PostExposeData(object obj)
    {
        if (Scribe.mode != LoadSaveMode.PostLoadInit || obj is not Pawn pawn || pawn.genes?.GenesListForReading is not { } genes)
            return;

        foreach (Gene gene in genes)
        {
            if (gene is GeneWithComps { comps: null } geneWithComps)
                geneWithComps.InitializeComps();
        }
    }
}
