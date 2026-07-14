namespace Xylib.Patches;

[HarmonyPatch(typeof(SkillUI))]
internal static class Patch_SkillUI
{
    [Feature(nameof(XStatDefOf.XylLearnFactorPassionNone))]
    [Feature(nameof(XStatDefOf.XylLearnFactorPassionMinor))]
    [Feature(nameof(XStatDefOf.XylLearnFactorPassionMajor))]
    [InnerPostfix(typeof(SkillUI), nameof(SkillUI.GetLearningFactor))]
    [Target("GetSkillDescription")]
    public static void GetLearningFactor_Postfix(Passion passion, SkillRecord sk, ref float __result)
    {
        __result = passion switch
        {
            Passion.None => sk.Pawn.GetStatValue(XStatDefOf.XylLearnFactorPassionNone),
            Passion.Minor => sk.Pawn.GetStatValue(XStatDefOf.XylLearnFactorPassionMinor),
            Passion.Major => sk.Pawn.GetStatValue(XStatDefOf.XylLearnFactorPassionMajor),
            _ => throw new ArgumentOutOfRangeException(nameof(passion), passion, null),
        };
    }
}
