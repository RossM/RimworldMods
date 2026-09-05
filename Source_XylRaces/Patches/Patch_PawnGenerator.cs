namespace XylXenos.Patches;

[HarmonyPatch(typeof(PawnGenerator))]
public static class Patch_PawnGenerator
{
    [Feature(typeof(XenotypeSetWithDefault))]
    [Prefix]
    [Inner(typeof(PawnGenerator), "XenotypesAvailableFor.AddOrAdjust")]
    [Target(nameof(PawnGenerator.XenotypesAvailableFor))]
    [PatchOptions(PatchOptions.Debug)]
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

    [Feature(nameof(Config.Feature.Bugfix_Misc))]
    [Prefix]
    [Inner(typeof(PawnGenerator), "GenerateSkills.CreatePassion")]
    [Target("GenerateSkills")]
    public static bool CreatePassion_Prefix(Pawn pawn, SkillRecord record, ref int minorPassions)
    {
        if (!Settings.instance.fixGeneticPassions || !PatchHelpers.ShouldGetGeneticPassion(pawn, record, minorPassions))
            return true;

        record.passion = Passion.Major;
        minorPassions--;
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
}
