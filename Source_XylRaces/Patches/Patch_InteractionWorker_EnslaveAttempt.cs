namespace XylXenos.Patches;

[HarmonyPatch(typeof(InteractionWorker_EnslaveAttempt))]
public static class Patch_InteractionWorker_EnslaveAttempt
{
    [Feature(nameof(DefOf.XylWillFallRate))]
    [InfixPostfix(typeof(StatExtension), nameof(StatExtension.GetStatValue))]
    [InfixPatch(nameof(InteractionWorker_EnslaveAttempt.Interacted))]
    public static void GetStatValue_Postfix(StatDef stat, Pawn recipient, ref float __result)
    {
        if (stat == StatDefOf.NegotiationAbility)
            __result *= recipient.GetStatValue(DefOf.XylWillFallRate);
    }
}
