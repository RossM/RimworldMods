namespace XylXenos.Patches;

[HarmonyPatch(typeof(HediffComp_Lactating))]
public static class Patch_HediffComp_Lactating
{
    [Feature(nameof(Config.Feature.Bugfix_Lactation))]
    [Prefix]
    [Target(nameof(HediffComp_Lactating.TryCharge))]
    public static void TryCharge_Prefix(HediffComp_Lactating __instance, ref float desiredChargeAmount)
    {
        DebugAssert.NotNull(__instance.Pawn);

        if (!Settings.instance.ShouldFixLactationBugsFor(__instance.Pawn))
            return;

        // Fixes a bug where lactation kept consuming food even when full, despite the hediff tooltip saying it doesn't
        desiredChargeAmount = Mathf.Min(desiredChargeAmount, __instance.Props.fullChargeAmount - __instance.Charge);
    }
}
