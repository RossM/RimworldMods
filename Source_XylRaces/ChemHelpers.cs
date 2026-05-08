using System.Linq;
using RimWorld;
using Verse;

namespace XylRacesCore;

public static class ChemHelpers
{
    public static bool ChemicalIsAllowedByGenes(this Pawn pawn, ChemicalDef chemicalDef)
    {
        var defExtension = chemicalDef.GetModExtension<ChemicalModExtension>();
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
}