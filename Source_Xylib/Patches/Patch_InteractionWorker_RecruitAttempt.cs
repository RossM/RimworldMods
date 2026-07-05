namespace Xylib.Patches;

[HarmonyPatch(typeof(InteractionWorker_RecruitAttempt))]
internal static class Patch_InteractionWorker_RecruitAttempt
{
    [Feature(nameof(XStatDefOf.XylResistanceFallRate))]
    [InfixPostfix(typeof(StatExtension), nameof(StatExtension.GetStatValue))]
    [InfixPatch(nameof(InteractionWorker_RecruitAttempt.Interacted))]
    public static void GetStatValue_Postfix(StatDef stat, Pawn recipient, ref float __result)
    {
        if (stat == StatDefOf.NegotiationAbility)
            __result *= recipient.GetStatValue(XStatDefOf.XylResistanceFallRate);
    }
}
