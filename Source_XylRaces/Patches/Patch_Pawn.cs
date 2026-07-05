namespace XylXenos.Patches;

[HarmonyPatch(typeof(Pawn))]
public static class Patch_Pawn
{
    [Feature(typeof(GeneCompProperties_Psycast))]
    [HarmonyPostfix]
    [HarmonyPatch(nameof(Pawn.HasPsylink), MethodType.Getter)]
    public static void HasPsylink_Postfix(Pawn __instance, ref bool __result)
    {
        if (__instance.GeneTracker_XylXenos is { } geneTracker)
            __result |= geneTracker.hasPsycast;
    }
}
