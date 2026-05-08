using System.Linq;
using JetBrains.Annotations;
using LudeonTK;
using RimWorld;
using Verse;

namespace XylRacesCore;

public static class ChemHelpers
{
    public static bool ChemicalIsAllowedByGenes(this Pawn pawn, ChemicalDef chemicalDef)
    {
        var defExtension = chemicalDef.GetModExtension<ChemicalDefExtension>();
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

    [DebugOutput("Economy"), UsedImplicitly]
    public static void DrugGeneRequirements()
    {
        TableDataGetter<ThingDef>[] columns =
        [
            new("defName", x => x.defName),
            new("label", x => x.LabelCap),
            new("prohibitedGenes",
                x => DrugStatsUtility.GetChemical(x)?.GetModExtension<ChemicalDefExtension>()?.prohibitedGenes
                    ?.Select(g => g.defName).ToCommaList() ?? ""),
            new("requiredGenesAll",
                x => DrugStatsUtility.GetChemical(x)?.GetModExtension<ChemicalDefExtension>()?.requiredGenesAll
                    ?.Select(g => g.defName).ToCommaList() ?? ""),
            new("requiredGenesAny",
                x => DrugStatsUtility.GetChemical(x)?.GetModExtension<ChemicalDefExtension>()?.requiredGenesAny
                    ?.Select(g => g.defName).ToCommaList() ?? ""),
        ];
        DebugTables.MakeTablesDialog(DefDatabase<ThingDef>.AllDefs.Where(x => x.IsDrug).OrderBy(x => x.BaseMarketValue), columns);
    }
}