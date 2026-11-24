namespace XylXenos.Patches;

[Patch(typeof(Pawn))]
public static class Patch_Pawn
{
    [Feature(nameof(MyDefOf.XylDocile))]
    [Postfix]
    [Target(nameof(Pawn.GetAcceptArrestChance))]
    public static void GetAcceptArrestChance_Postfix(Pawn __instance, ref float __result)
    {
        if (__instance.HasActiveGene(MyDefOf.XylDocile))
            __result = 1f;
    }

    [Feature(typeof(GeneCompProperties_Psycast))]
    [Postfix]
    [Target(nameof(Pawn.HasPsylink), MemberType.Getter)]
    public static void HasPsylink_Postfix(Pawn __instance, ref bool __result)
    {
        if (__instance.GeneTracker_XylXenos is { } geneTracker)
            __result |= geneTracker.hasPsycast;
    }
}
