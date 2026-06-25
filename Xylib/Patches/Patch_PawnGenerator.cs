namespace Xylib.Patches;

[HarmonyPatch(typeof(PawnGenerator))]
public class Patch_PawnGenerator
{
    private static XenotypeDef xenotypeOverride;

    [Feature(nameof(EventDefOf.PreGeneratePawnBioAndName))]
    [InfixPrefix(typeof(PawnGenerator), "GenerateGenes")]
    [InfixPatch("TryGenerateNewPawnInternal")]
    public static void GenerateGenes_Prefix(ref XenotypeDef xenotype)
    {
        xenotype = xenotypeOverride;
    }

    [Feature(nameof(EventDefOf.PreGeneratePawnBioAndName))]
    [InfixPrefix(typeof(PawnBioAndNameGenerator), nameof(PawnBioAndNameGenerator.GiveAppropriateBioAndNameTo))]
    [InfixPatch("TryGenerateNewPawnInternal")]
    public static void GiveAppropriateBioAndNameTo_Prefix(
        Pawn pawn,
        PawnGenerationRequest request,
        ref XenotypeDef xenotype)
    {
        var data = new PawnGenerationData(request, xenotype);
        EventManager.Instance.Notify(EventDefOf.PreGeneratePawnBioAndName, pawn, data);

        xenotypeOverride = xenotype = data.xenotype;
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
    public static void TryGenerateNewPawnInternal_Postfix(ref Pawn __result, ref PawnGenerationRequest request)
    {
        if (__result == null)
            return;
        EventManager.Instance.Notify(EventDefOf.PostGenerateNewPawn, __result, request);
    }
}
