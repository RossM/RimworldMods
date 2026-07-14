namespace Xylib.Patches;

[HarmonyPatch(typeof(PawnGenerator))]
internal static class Patch_PawnGenerator
{
    [Feature(nameof(EventDefOf.PreGeneratePawnBioAndName))]
    [InnerPrefix(typeof(PawnGenerator), "GenerateGenes")]
    [Target("TryGenerateNewPawnInternal")]
    public static void GenerateGenes_Prefix(ref XenotypeDef? xenotype, XenotypeDef? __state)
    {
        xenotype = __state;
    }

    [Feature(nameof(EventDefOf.PostGenerateInitialHediffs))]
    [HarmonyPostfix]
    [HarmonyPatch("GenerateInitialHediffs")]
    public static void GenerateInitialHediffs_Postfix(Pawn pawn)
    {
        EventManager.Instance.Notify(EventDefOf.PostGenerateInitialHediffs, pawn);
    }

    [Feature(nameof(EventDefOf.PreGeneratePawnBioAndName))]
    [InnerPrefix(typeof(PawnBioAndNameGenerator), nameof(PawnBioAndNameGenerator.GiveAppropriateBioAndNameTo))]
    [Target("TryGenerateNewPawnInternal")]
    public static void GiveAppropriateBioAndNameTo_Prefix(
        Pawn pawn,
        PawnGenerationRequest request,
        ref XenotypeDef? xenotype,
        out XenotypeDef? __state)
    {
        var data = new PawnGenerationData(request, xenotype);
        EventManager.Instance.Notify(EventDefOf.PreGeneratePawnBioAndName, pawn, data);

        __state = xenotype = data.xenotype;
    }

    [Feature(nameof(EventDefOf.PostGenerateNewPawn))]
    [HarmonyPostfix]
    [HarmonyPatch(nameof(PawnGenerator.RedressPawn))]
    public static void RedressPawn_Postfix(Pawn pawn, PawnGenerationRequest request)
    {
        EventManager.Instance.Notify(EventDefOf.PostRedressPawn, pawn, request);
    }

    [Feature(nameof(EventDefOf.PostGenerateNewPawn))]
    [HarmonyPostfix]
    [HarmonyPatch("TryGenerateNewPawnInternal")]
    public static void TryGenerateNewPawnInternal_Postfix(ref Pawn? __result, ref PawnGenerationRequest request)
    {
        if (__result == null)
            return;
        EventManager.Instance.Notify(EventDefOf.PostGenerateNewPawn, __result, request);
    }
}
