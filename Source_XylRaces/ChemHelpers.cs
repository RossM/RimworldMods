namespace XylXenos;

public static class ChemHelpers
{
    public static bool ChemicalIsAllowedByGenes(this Pawn pawn, ChemicalDef chemicalDef)
    {
        var defExtension = chemicalDef?.GetModExtension<DefModExtension_Chemical>();
        if (defExtension == null)
            return true;

        if (!defExtension.prohibitedGenes.NullOrEmpty() && defExtension.prohibitedGenes.Any(pawn.HasActiveGene))
            return false;
        if (!defExtension.requiredGenesAll.NullOrEmpty() && !defExtension.requiredGenesAll.All(pawn.HasActiveGene))
            return false;
        if (!defExtension.requiredGenesAny.NullOrEmpty() && !defExtension.requiredGenesAny.Any(pawn.HasActiveGene))
            return false;

        return true;
    }

    public static bool ChemicalIsAllowedByGenes(this Pawn pawn, ThingDef drug)
    {
        ChemicalDef chemical = DrugStatsUtility.GetChemical(drug);
        if (chemical == null)
            return true;

        return pawn.ChemicalIsAllowedByGenes(chemical);
    }

    [DebugOutput("Economy")]
    public static void DrugGeneRequirements()
    {
        TableDataGetter<ThingDef>[] columns =
        [
            new("defName", thingDef => thingDef.defName),
            new("label", thingDef => thingDef.LabelCap),
            new("prohibitedGenes",
                thingDef => DrugStatsUtility.GetChemical(thingDef)?.GetModExtension<DefModExtension_Chemical>()?.prohibitedGenes
                    ?.Select(geneDef => geneDef.defName).ToCommaList() ?? ""),
            new("requiredGenesAll",
                thingDef => DrugStatsUtility.GetChemical(thingDef)?.GetModExtension<DefModExtension_Chemical>()?.requiredGenesAll
                    ?.Select(geneDef => geneDef.defName).ToCommaList() ?? ""),
            new("requiredGenesAny",
                thingDef => DrugStatsUtility.GetChemical(thingDef)?.GetModExtension<DefModExtension_Chemical>()?.requiredGenesAny
                    ?.Select(geneDef => geneDef.defName).ToCommaList() ?? ""),
        ];
        DebugTables.MakeTablesDialog(
            DefDatabase<ThingDef>.AllDefs.Where(thingDef => thingDef.IsDrug).OrderBy(thingDef => thingDef.BaseMarketValue), columns);
    }
}
