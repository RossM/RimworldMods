namespace Xylib.Patches;

[HarmonyPatch]
internal static class PatchMutation
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(Hediff_Shambler), nameof(Hediff_Shambler.PostRemoved))]
    public static void PostRemoved_Postfix(Pawn ___pawn)
    {
        EventManager.Instance.Notify(EventDefOf.PostMutated, ___pawn);
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(MutantUtility), nameof(MutantUtility.ResurrectAsShambler))]
    public static void ResurrectAsShambler_Postfix(Pawn pawn)
    {
        EventManager.Instance.Notify(EventDefOf.PostMutated, pawn);
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(Pawn_MutantTracker), nameof(Pawn_MutantTracker.Revert))]
    public static void Revert_Postfix(Pawn ___pawn)
    {
        EventManager.Instance.Notify(EventDefOf.PostMutated, ___pawn);
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(DebugToolsPawns), "RevertMutant")]
    public static void RevertMutant_Postfix(Pawn p)
    {
        EventManager.Instance.Notify(EventDefOf.PostMutated, p);
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(MutantUtility), nameof(MutantUtility.SetPawnAsMutantInstantly))]
    public static void SetPawnAsMutantInstantly_Postfix(Pawn pawn)
    {
        EventManager.Instance.Notify(EventDefOf.PostMutated, pawn);
    }
}
