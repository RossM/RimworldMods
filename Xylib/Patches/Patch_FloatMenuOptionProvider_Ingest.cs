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

        string text = !clickedThing.def.ingestible!.ingestCommandString.NullOrEmpty()
            ? clickedThing.def.ingestible.ingestCommandString.Formatted(clickedThing.LabelShort)
            : "ConsumeThing".Translate(clickedThing.LabelShort, clickedThing);

        string reason = PatchHelpers.GetChemicalBlockedByGenesReason(context, chemical);

        __result = new FloatMenuOption($"{text}: {reason}", null);
    }
}
