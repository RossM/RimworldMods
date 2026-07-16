namespace Xylib.Patches;

[HarmonyPatch(typeof(FloatMenuOptionProvider_Ingest))]
internal static class Patch_FloatMenuOptionProvider_Ingest
{
    [Feature(typeof(DefModExtension_Chemical))]
    [Prefix]
    [Target("GetSingleOptionFor")]
    public static bool GetSingleOptionFor_Prefix(
        FloatMenuOptionProvider_Ingest __instance,
        Thing clickedThing,
        FloatMenuContext context,
        out FloatMenuOption? __result)
    {
        DebugAssert.NotNull(context.FirstSelectedPawn);

        __result = null;
        if (clickedThing.def.ingestible is not { showIngestFloatOption: true })
        {
            return true;
        }

        if (!clickedThing.IngestibleNow || !context.FirstSelectedPawn.RaceProps.CanEverEat(clickedThing.def))
        {
            return true;
        }

        ChemicalDef? chemical = DrugStatsUtility.GetChemical(clickedThing.def);
        if (chemical == null)
            return true;

        if (context.FirstSelectedPawn.ChemicalIsAllowedByGenes(chemical))
            return true;

        string text = !clickedThing.def.ingestible.ingestCommandString.NullOrEmpty()
            ? clickedThing.def.ingestible.ingestCommandString.Formatted(clickedThing.LabelShort)
            : "ConsumeThing".Translate(clickedThing.LabelShort, clickedThing);

        var defExtension = chemical.GetModExtension<DefModExtension_Chemical>();

        if (defExtension is { prohibitedGenes.Count: > 0 })
        {
            // ReSharper disable once VariableHidesOuterVariable
            if (defExtension.prohibitedGenes.FirstOrDefault(gene => context.FirstSelectedPawn.HasActiveGene(gene)) is { } gene)
            {
                __result = new FloatMenuOption($"{text}: {"XylBlockedByGene".Translate(gene.label)}", null);
                return false;
            }
        }

        if (defExtension is { requiredGenesAll.Count: > 0 })
        {
            // ReSharper disable once VariableHidesOuterVariable
            if (defExtension.requiredGenesAll.FirstOrDefault(gene => !context.FirstSelectedPawn.HasActiveGene(gene)) is { } gene)
            {
                __result = new FloatMenuOption($"{text}: {"XylRequiresGene".Translate(gene.label)}", null);
                return false;
            }
        }

        if (defExtension is { requiredGenesAny.Count: > 0 })
        {
            GeneDef firstRequiredGene = defExtension.requiredGenesAny[0];
            DebugAssert.NotNull(firstRequiredGene);

            __result = new FloatMenuOption($"{text}: {"XylRequiresGene".Translate(firstRequiredGene.label)}", null);
            return false;
        }

        // Should never get here

        return true;
    }
}
