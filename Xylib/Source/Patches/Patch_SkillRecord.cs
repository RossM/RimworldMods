namespace Xylib.Patches;

[Patch(typeof(SkillRecord))]
internal static class Patch_SkillRecord
{
    [Feature(nameof(XStatDefOf.XylLearnFactorPassionNone))]
    [Feature(nameof(XStatDefOf.XylLearnFactorPassionMinor))]
    [Feature(nameof(XStatDefOf.XylLearnFactorPassionMajor))]
    [Postfix]
    // We need to use an inner postfix rather than a regular postfix here because Vanilla Skills Expanded applies a
    // destructive prefix to LearnRateFactor().
    [Inner(typeof(SkillRecord), nameof(SkillRecord.LearnRateFactor))]
    [Target(typeof(SkillRecord), nameof(SkillRecord.Learn))]
    [Target(typeof(BookOutcomeDoerGainSkillExp), nameof(BookOutcomeDoerGainSkillExp.GetBenefitsString))]
    public static void LearnRateFactor_Postfix(SkillRecord __instance, ref float __result)
    {
        DebugAssert.NotNull(__instance.Pawn);

        __result *= __instance.passion switch
        {
            Passion.None => __instance.Pawn.GetStatValue(XStatDefOf.XylLearnFactorPassionNone) / SkillRecord.LearnFactorPassionNone,
            Passion.Minor => __instance.Pawn.GetStatValue(XStatDefOf.XylLearnFactorPassionMinor) / SkillRecord.LearnFactorPassionMinor,
            Passion.Major => __instance.Pawn.GetStatValue(XStatDefOf.XylLearnFactorPassionMajor) / SkillRecord.LearnFactorPassionMajor,
            // Vanilla Skills Expanded adds new passion types. Ignore them.
            _ => 1,
        };
    }
}
