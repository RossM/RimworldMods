namespace Xylib.Patches;

[HarmonyPatch(typeof(InteractionWorker_EnslaveAttempt))]
internal static class Patch_InteractionWorker_EnslaveAttempt
{
    [Feature(nameof(XStatDefOf.XylWillFallRate))]
    [Postfix]
    [Inner(typeof(StatExtension), nameof(StatExtension.GetStatValue))]
    [Target(nameof(InteractionWorker_EnslaveAttempt.Interacted))]
    public static void GetStatValue_Postfix(StatDef stat, Pawn recipient, ref float __result)
    {
        if (stat == StatDefOf.NegotiationAbility)
            __result *= recipient.GetStatValue(XStatDefOf.XylWillFallRate);
    }
}
