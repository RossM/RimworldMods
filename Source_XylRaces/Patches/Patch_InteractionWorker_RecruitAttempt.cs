using HarmonyLib;
using JetBrains.Annotations;
using RimWorld;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using TranspilerUtil;
using Verse;

namespace XylRacesCore.Patches
{
    [HarmonyPatch(typeof(InteractionWorker_RecruitAttempt))]
    public static class Patch_InteractionWorker_RecruitAttempt
    {
        [DefOf]
        private static class Defs
        {
            [UsedImplicitly]
            public static StatDef XylResistanceFallRate;
        }

        private static readonly InstructionMatcher Fixup_Interacted = new()
        {
            Rules =
            {
                InstructionMatcher.MakeRedirectRule(
                    AccessTools.Method(typeof(StatExtension), nameof(StatExtension.GetStatValue)),
                    AccessTools.Method(typeof(Patch_InteractionWorker_EnslaveAttempt), nameof(GetStatValue_Wrapper))
                )
            }
        };

        [Feature(nameof(Defs.XylResistanceFallRate)), HarmonyTranspiler, UsedImplicitly, HarmonyPatch(nameof(InteractionWorker_RecruitAttempt.Interacted))]
        public static IEnumerable<CodeInstruction> Interacted_Transpiler(IEnumerable<CodeInstruction> instructions,
            ILGenerator generator, MethodBase method)
        {
            var instructionsList = new List<CodeInstruction>(instructions);
            Fixup_Interacted.MatchAndReplace(method, ref instructionsList, generator);
            return instructionsList;
        }

        public static float GetStatValue_Wrapper(Thing thing, StatDef stat, Pawn recipient, bool applyPostProcess, int cacheStaleAfterTicks)
        {
            float value = thing.GetStatValue(stat, applyPostProcess, cacheStaleAfterTicks);
            if (stat == StatDefOf.NegotiationAbility)
                value *= recipient.GetStatValue(Defs.XylResistanceFallRate);
            return value;
        }
    }
}
