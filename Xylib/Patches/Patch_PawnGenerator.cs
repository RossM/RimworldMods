namespace Xylib.Patches;

[Patch(typeof(PawnGenerator))]
internal static class Patch_PawnGenerator
{
    [Feature(typeof(XenotypeSetWithDefault))]
    [Prefix]
    [Inner(typeof(PawnGenerator), "XenotypesAvailableFor.AddOrAdjust")]
    [Target(nameof(PawnGenerator.XenotypesAvailableFor))]
    public static bool AddOrAdjust_Prefix(
        XenotypeChance xenotypeChance,
        FactionDef? factionDef,
        Faction? faction,
        [Field(Scope.Outer)] Dictionary<XenotypeDef, float> ___tmpXenotypeChances)
    {
        DebugAssert.NotNull(___tmpXenotypeChances);
        DebugAssert.NotNull(xenotypeChance.xenotype);

        XenotypeSet? xenotypeSet = (faction?.def ?? factionDef)?.xenotypeSet;
        if (xenotypeSet is not XenotypeSetWithDefault withDefault)
            return true;

        if (xenotypeChance.xenotype != withDefault.defaultXenotype)
        {
            if (___tmpXenotypeChances.ContainsKey(xenotypeChance.xenotype))
                ___tmpXenotypeChances[xenotypeChance.xenotype] += xenotypeChance.chance;
            else
                ___tmpXenotypeChances.Add(xenotypeChance.xenotype, xenotypeChance.chance);
        }

        return false;
    }

    [Feature(typeof(XenotypeSetWithDefault))]
    [Postfix]
    [Inner(typeof(XenotypeDefOf), nameof(XenotypeDefOf.Baseliner))]
    [Target(nameof(PawnGenerator.XenotypesAvailableFor))]
    public static void XenotypeDefOf_Baseliner_Postfix(FactionDef? factionDef, Faction? faction, ref XenotypeDef? __result)
    {
        if ((faction?.def ?? factionDef)?.xenotypeSet is XenotypeSetWithDefault withDefault)
            __result = withDefault.defaultXenotype;
    }

    [Feature(nameof(EventDefOf.PreGeneratePawnBioAndName))]
    [Prefix]
    [Inner(typeof(PawnGenerator), "GenerateGenes")]
    [Target("TryGenerateNewPawnInternal")]
    public static void GenerateGenes_Prefix(ref XenotypeDef? xenotype, [State] PawnGenerationData data)
    {
        xenotype = data.xenotype;
    }

    [Feature(nameof(EventDefOf.PostGenerateInitialHediffs))]
    [Postfix]
    [Target("GenerateInitialHediffs")]
    public static void GenerateInitialHediffs_Postfix(Pawn pawn)
    {
        EventManager.Instance.Notify(EventDefOf.PostGenerateInitialHediffs, pawn);
    }

    [Feature(nameof(EventDefOf.PreGeneratePawnBioAndName))]
    [Prefix]
    [Inner(typeof(PawnBioAndNameGenerator), nameof(PawnBioAndNameGenerator.GiveAppropriateBioAndNameTo))]
    [Target("TryGenerateNewPawnInternal")]
    public static void GiveAppropriateBioAndNameTo_Prefix(
        Pawn pawn,
        PawnGenerationRequest request,
        ref XenotypeDef? xenotype,
        [State] out PawnGenerationData data)
    {
        data = new PawnGenerationData(request, xenotype);
        EventManager.Instance.Notify(EventDefOf.PreGeneratePawnBioAndName, pawn, data);

        xenotype = data.xenotype;
    }

    [Feature(nameof(EventDefOf.PostGenerateNewPawn))]
    [Postfix]
    [Target(nameof(PawnGenerator.RedressPawn))]
    public static void RedressPawn_Postfix(Pawn pawn, PawnGenerationRequest request)
    {
        EventManager.Instance.Notify(EventDefOf.PostRedressPawn, pawn, request);
    }

    [Feature(nameof(EventDefOf.PostGenerateNewPawn))]
    [Postfix]
    [Target("TryGenerateNewPawnInternal")]
    public static void TryGenerateNewPawnInternal_Postfix(ref Pawn? __result, ref PawnGenerationRequest request)
    {
        if (__result == null)
            return;
        EventManager.Instance.Notify(EventDefOf.PostGenerateNewPawn, __result, request);
    }
}
