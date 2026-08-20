namespace Xylib.Patches;

[HarmonyPatch(typeof(StartingPawnUtility))]
internal static class Patch_StartingPawnUtility
{
    [Feature(typeof(CompProperties_Drug))]
    [Prefix]
    [Inner(typeof(List<ThingDefCount>), "Add")]
    [Target("GeneratePossessions")]
    public static bool List_Add_Prefix(List<ThingDefCount> __instance, ThingDefCount item, Pawn pawn)
    {
        var chemical = item.ThingDef?.GetCompProperties<CompProperties_Drug>()?.chemical;
        return chemical == null || pawn.ChemicalIsAllowedByGenes(chemical);
    }

    [Feature(nameof(EventDefOf.InGeneratePossessions))]
    [Postfix]
    [Inner(typeof(Rand), nameof(Rand.Value), MemberType.Getter)]
    [Target("GeneratePossessions")]
    public static void Rand_Value_Postfix(Pawn pawn, [State] ref bool sentEvent)
    {
        if (sentEvent)
            return;
        sentEvent = true;

        var items = Find.GameInitData?.startingPossessions?[pawn];
        EventManager.Instance.Notify(EventDefOf.InGeneratePossessions, pawn, items);
    }
}
