namespace XylXenos.Patches;

[HarmonyPatch(typeof(PawnGenerator))]
public static class Patch_PawnGenerator
{
    private static XenotypeDef xenotypeOverride;

    [Feature(typeof(XenotypeSetWithDefault))]
    [InfixPrefix(typeof(PawnGenerator), "<XenotypesAvailableFor>g__AddOrAdjust|49_0")]
    [InfixPatch(nameof(PawnGenerator.XenotypesAvailableFor))]
    public static bool AddOrAdjust_Prefix(XenotypeChance xenotypeChance, FactionDef factionDef, Faction faction)
    {
        if (xenotypeChance.xenotype != ((faction?.def ?? factionDef)?.xenotypeSet).DefaultXenotype)
        {
            if (PawnGenerator.tmpXenotypeChances.ContainsKey(xenotypeChance.xenotype))
            {
                PawnGenerator.tmpXenotypeChances[xenotypeChance.xenotype] += xenotypeChance.chance;
            }
            else
            {
                PawnGenerator.tmpXenotypeChances.Add(xenotypeChance.xenotype, xenotypeChance.chance);
            }
        }

        return false;
    }

    [Feature(nameof(EventDefOf.PawnGenerationEarly))]
    [InfixPrefix(typeof(PawnGenerator), "GenerateGenes")]
    [InfixPatch("TryGenerateNewPawnInternal")]
    public static void GenerateGenes_Prefix(ref XenotypeDef xenotype)
    {
        xenotype = xenotypeOverride;
    }

    [Feature(nameof(DefModExtension_Gene.congenitalHediffs))]
    [HarmonyPostfix]
    [HarmonyPatch("GenerateInitialHediffs")]
    public static void GenerateInitialHediffs_Postfix(Pawn pawn)
    {
        PatchHelpers.GenerateCongenitalHediffs(pawn);
    }

    [Feature(nameof(DefModExtension_Gene.femaleChance))]
    [Feature(nameof(EventDefOf.PawnGenerationEarly))]
    [InfixPrefix(typeof(PawnBioAndNameGenerator), nameof(PawnBioAndNameGenerator.GiveAppropriateBioAndNameTo))]
    [InfixPatch("TryGenerateNewPawnInternal")]
    public static void GiveAppropriateBioAndNameTo_Prefix(
        Pawn pawn,
        PawnGenerationRequest request,
        XenotypeDef xenotype)
    {
        var data = new PawnGenerationEarlyData(request, xenotype);
        EventManager.Instance.Notify(EventDefOf.PawnGenerationEarly, pawn, data);

        xenotypeOverride = data.xenotype;

        PatchHelpers.ModifyGenderByGenes(pawn, request, data.xenotype);
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

    [Feature(typeof(XenotypeSetWithDefault))]
    [InfixPostfix(typeof(XenotypeDefOf), nameof(XenotypeDefOf.Baseliner))]
    [InfixPatch(nameof(PawnGenerator.XenotypesAvailableFor))]
    public static void XenotypeDefOf_Baseliner_Postfix(FactionDef factionDef, Faction faction, ref XenotypeDef __result)
    {
        __result = ((faction?.def ?? factionDef)?.xenotypeSet).DefaultXenotype;
    }
}
