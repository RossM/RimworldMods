using System;
using System.Runtime.CompilerServices;
using HarmonyLib;
using RimWorld;
using TranspilerUtil;

namespace XylXenos.Patches
{
    [HarmonyPatch(typeof(SkillUI))]
    public static class Patch_SkillUI
    {
        [Feature(nameof(DefOf.XylLearnFactorPassionNone))]
        [Feature(nameof(DefOf.XylLearnFactorPassionMinor))]
        [Feature(nameof(DefOf.XylLearnFactorPassionMajor))]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [InfixWrapper(typeof(SkillUI), nameof(SkillUI.GetLearningFactor))]
        [InfixPatch("GetSkillDescription")]
        public static float GetLearningFactor_Wrapper(Passion passion, SkillRecord sk)
        {
            return passion switch
            {
                Passion.None => sk.Pawn.GetStatValue(DefOf.XylLearnFactorPassionNone),
                Passion.Minor => sk.Pawn.GetStatValue(DefOf.XylLearnFactorPassionMinor),
                Passion.Major => sk.Pawn.GetStatValue(DefOf.XylLearnFactorPassionMajor),
                _ => throw new ArgumentOutOfRangeException(nameof(passion), passion, null)
            };
        }
    }
}
