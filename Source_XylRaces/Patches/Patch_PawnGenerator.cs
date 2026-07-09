namespace XylXenos.Patches;

[HarmonyPatch(typeof(PawnGenerator))]
public static class Patch_PawnGenerator
{
    [Feature(typeof(XenotypeSetWithDefault))]
    [InfixPrefix(typeof(PawnGenerator), "<XenotypesAvailableFor>g__AddOrAdjust|49_0")]
    [InfixPatch(nameof(PawnGenerator.XenotypesAvailableFor))]
    public static bool AddOrAdjust_Prefix(XenotypeChance xenotypeChance, FactionDef? factionDef, Faction? faction)
    {
        DebugAssert.NotNull(PawnGenerator.tmpXenotypeChances);
        DebugAssert.NotNull(xenotypeChance.xenotype);

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

    [Feature(nameof(Config.Feature.Bugfix_Misc))]
    [InfixPrefix(typeof(PawnGenerator), "<GenerateSkills>g__CreatePassion|53_0")]
    [InfixPatch("GenerateSkills")]
    public static bool CreatePassion_Prefix(Pawn pawn, SkillRecord record, ref int minorPassions)
    {
        if (!Settings.instance.fixGeneticPassions || !PatchHelpers.ShouldGetGeneticPassion(pawn, record, minorPassions))
            return true;

        record.passion = Passion.Major;
        minorPassions--;
        return false;
    }

    [Feature(typeof(XenotypeSetWithDefault))]
    [InfixPostfix(typeof(XenotypeDefOf), nameof(XenotypeDefOf.Baseliner))]
    [InfixPatch(nameof(PawnGenerator.XenotypesAvailableFor))]
    public static void XenotypeDefOf_Baseliner_Postfix(FactionDef? factionDef, Faction? faction, ref XenotypeDef? __result)
    {
        __result = ((faction?.def ?? factionDef)?.xenotypeSet).DefaultXenotype;
    }
}
