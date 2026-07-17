namespace Xylib.Patches;

[HarmonyPatch(typeof(SkillRecord))]
internal static class Patch_SkillRecord
{
    [Feature(nameof(XStatDefOf.XylLearnFactorPassionNone))]
    [Feature(nameof(XStatDefOf.XylLearnFactorPassionMinor))]
    [Feature(nameof(XStatDefOf.XylLearnFactorPassionMajor))]
    [Postfix]
    [Target(nameof(SkillRecord.LearnRateFactor))]
    public static void LearnRateFactor_Postfix(SkillRecord __instance, ref float __result)
    {
        DebugAssert.NotNull(__instance.Pawn);

        __result *= __instance.passion switch
        {
            Passion.None => __instance.Pawn.GetStatValue(XStatDefOf.XylLearnFactorPassionNone) / SkillRecord.LearnFactorPassionNone,
            Passion.Minor => __instance.Pawn.GetStatValue(XStatDefOf.XylLearnFactorPassionMinor) / SkillRecord.LearnFactorPassionMinor,
            Passion.Major => __instance.Pawn.GetStatValue(XStatDefOf.XylLearnFactorPassionMajor) / SkillRecord.LearnFactorPassionMajor,
            _ => throw new ArgumentOutOfRangeException(),
        };
    }
}
