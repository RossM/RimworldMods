namespace Xylib.Patches;

[Patch(typeof(FloatMenuOptionProvider_Ingest))]
internal static class Patch_FloatMenuOptionProvider_Ingest
{
    [Feature(typeof(DefModExtension_Chemical))]
    [Postfix]
    [Target("GetSingleOptionFor")]
    public static void GetSingleOptionFor_Postfix(
        FloatMenuOptionProvider_Ingest __instance,
        Thing clickedThing,
        FloatMenuContext context,
        ref FloatMenuOption? __result)
    {
        DebugAssert.NotNull(context.FirstSelectedPawn);

        if (__result == null)
            return;

        ChemicalDef? chemical = DrugStatsUtility.GetChemical(clickedThing.def);
        if (chemical == null)
            return;

        if (context.FirstSelectedPawn.ChemicalIsAllowedByGenes(chemical))
            return;

        string text = !clickedThing.def.ingestible.ingestCommandString.NullOrEmpty()
            ? clickedThing.def.ingestible.ingestCommandString.Formatted(clickedThing.LabelShort)
            : "ConsumeThing".Translate(clickedThing.LabelShort, clickedThing);

        var defExtension = chemical.GetModExtension<DefModExtension_Chemical>();

        if (defExtension is { prohibitedGenes.Count: > 0 })
            // ReSharper disable once VariableHidesOuterVariable
            if (defExtension.prohibitedGenes.FirstOrDefault(gene => context.FirstSelectedPawn.HasActiveGene(gene)) is { } gene)
            {
                __result = new FloatMenuOption($"{text}: {"XylBlockedByGene".Translate(gene.label)}", null);
                return;
            }

        if (defExtension is { requiredGenesAll.Count: > 0 })
            // ReSharper disable once VariableHidesOuterVariable
            if (defExtension.requiredGenesAll.FirstOrDefault(gene => !context.FirstSelectedPawn.HasActiveGene(gene)) is { } gene)
            {
                __result = new FloatMenuOption($"{text}: {"XylRequiresGene".Translate(gene.label)}", null);
                return;
            }

        if (defExtension is { requiredGenesAny.Count: > 0 })
        {
            GeneDef firstRequiredGene = defExtension.requiredGenesAny[0];
            DebugAssert.NotNull(firstRequiredGene);

            __result = new FloatMenuOption($"{text}: {"XylRequiresGene".Translate(firstRequiredGene.label)}", null);
            return;
        }

        // Should never get here
        Log.WarningOnce("Unhandled case in GetSingleOptionFor", "Unexpected state in GetSingleOptionFor".GetHashCode());
    }
}
