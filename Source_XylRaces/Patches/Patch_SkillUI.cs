using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using HarmonyLib;
using RimWorld;
using TranspilerUtil;

namespace XylXenos.Patches
{
    [HarmonyPatch(typeof(SkillUI))]
    public static class Patch_SkillUI
    {
        private static readonly InstructionMatcher.Rule Rule_GetLearningFactor
            = InstructionMatcher.MakeRedirectRule(SkillUI.GetLearningFactor, GetLearningFactor_Wrapper);

        [Feature(nameof(DefOf.XylLearnFactorPassionNone),
            nameof(DefOf.XylLearnFactorPassionMinor),
            nameof(DefOf.XylLearnFactorPassionMajor))]
        [HarmonyTranspiler]
        [HarmonyPatch("GetSkillDescription")]
        public static IEnumerable<CodeInstruction> GetSkillDescription_Transpiler(
            IEnumerable<CodeInstruction> instructions,
            ILGenerator generator,
            MethodBase method)
        {
            var instructionsList = new List<CodeInstruction>(instructions);
            new InstructionMatcher()
            {
                Rules =
                {
                    Rule_GetLearningFactor
                }
            }.MatchAndReplace(method, ref instructionsList, generator);
            return instructionsList;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
