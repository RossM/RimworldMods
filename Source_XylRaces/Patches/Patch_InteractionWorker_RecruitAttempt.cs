namespace XylXenos.Patches;

[HarmonyPatch(typeof(InteractionWorker_RecruitAttempt))]
public static class Patch_InteractionWorker_RecruitAttempt
{
    [Feature(nameof(DefOf.XylResistanceFallRate))]
    [InfixPostfix(typeof(StatExtension), nameof(StatExtension.GetStatValue))]
    [InfixPatch(nameof(InteractionWorker_RecruitAttempt.Interacted))]
    public static void GetStatValue_Postfix(StatDef stat, Pawn recipient, ref float __result)
    {
        if (stat == StatDefOf.NegotiationAbility)
            __result *= recipient.GetStatValue(DefOf.XylResistanceFallRate);
    }
}
