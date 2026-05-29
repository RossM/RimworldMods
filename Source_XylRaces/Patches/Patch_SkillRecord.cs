namespace XylXenos.Patches;

[HarmonyPatch(typeof(SkillRecord))]
public static class Patch_SkillRecord
{
    [Feature(nameof(DefOf.XylLearnFactorPassionNone))]
    [Feature(nameof(DefOf.XylLearnFactorPassionMinor))]
    [Feature(nameof(DefOf.XylLearnFactorPassionMajor))]
    [HarmonyPostfix]
    [HarmonyPatch(nameof(SkillRecord.LearnRateFactor))]
    public static void LearnRateFactor_Postfix(SkillRecord __instance, bool direct, ref float __result)
    {
        __result *= __instance.passion switch
        {
            Passion.None => __instance.Pawn.GetStatValue(DefOf.XylLearnFactorPassionNone) / SkillRecord.LearnFactorPassionNone,
            Passion.Minor => __instance.Pawn.GetStatValue(DefOf.XylLearnFactorPassionMinor) / SkillRecord.LearnFactorPassionMinor,
            Passion.Major => __instance.Pawn.GetStatValue(DefOf.XylLearnFactorPassionMajor) / SkillRecord.LearnFactorPassionMajor,
            _ => throw new ArgumentOutOfRangeException()
        };
    }
}