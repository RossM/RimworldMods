using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using HarmonyLib;
using RimWorld;
using TranspilerUtil;
using Verse;

namespace XylXenos.Patches
{
    [HarmonyPatch(typeof(InteractionWorker_RecruitAttempt))]
    public static class Patch_InteractionWorker_RecruitAttempt
    {
        private static readonly InstructionMatcher.Rule Fixup_GetStatValue
            = InstructionMatcher.MakeRedirectRule(StatExtension.GetStatValue, GetStatValue_Wrapper);

        [Feature(nameof(DefOf.XylResistanceFallRate))]
        [HarmonyTranspiler]
        [HarmonyPatch(nameof(InteractionWorker_RecruitAttempt.Interacted))]
        public static IEnumerable<CodeInstruction> Interacted_Transpiler(
            IEnumerable<CodeInstruction> instructions,
            ILGenerator generator,
            MethodBase method)
        {
            var instructionsList = new List<CodeInstruction>(instructions);
            new InstructionMatcher()
            {
                Rules =
                {
                    Fixup_GetStatValue
                }
            }.MatchAndReplace(method, ref instructionsList, generator);
            return instructionsList;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float GetStatValue_Wrapper(Thing thing, StatDef stat, Pawn recipient, bool applyPostProcess, int cacheStaleAfterTicks)
        {
            float value = thing.GetStatValue(stat, applyPostProcess, cacheStaleAfterTicks);
            if (stat == StatDefOf.NegotiationAbility)
                value *= recipient.GetStatValue(DefOf.XylResistanceFallRate);
            return value;
        }
    }
}
