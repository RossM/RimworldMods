namespace XylXenos.Patches;

[HarmonyPatch(typeof(StatWorker_SuppressionFallRate))]
public static class Patch_StatWorker_SuppressionFallRate
{
    [Feature("TODO")]
    [Prefix]
    [Target(nameof(StatWorker_SuppressionFallRate.GetValueUnfinalized))]
    public static bool GetValueUnfinalized_Prefix(
        StatRequest req,
        bool applyPostProcess,
        Func<StatRequest, bool, float> __base,
        ref float __result)
    {
        __result = __base(req, applyPostProcess);
        return false;
    }

    [Feature("TODO")]
    [Prefix]
    [Target(nameof(StatWorker_SuppressionFallRate.GetExplanationForTooltip))]
    public static bool GetExplanationForTooltip_Prefix(
        StatWorker_SuppressionFallRate __instance,
        StatRequest req,
        ref string __result)
    {
        __result = ((StatWorker_SuppressionFallRate_Fixed)__instance).GetExplanationForTooltip(req);
        return false;
    }
}
