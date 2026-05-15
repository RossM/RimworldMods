using HarmonyLib;
using JetBrains.Annotations;
using RimWorld;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using TranspilerUtil;
using Verse;

namespace XylRacesCore.Patches
{
    [HarmonyPatch(typeof(InteractionWorker_EnslaveAttempt))]
    public static class Patch_InteractionWorker_EnslaveAttempt
    {
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

        [Feature(nameof(DefOf.XylWillFallRate)), HarmonyTranspiler, UsedImplicitly,
         HarmonyPatch(nameof(InteractionWorker_EnslaveAttempt.Interacted))]
        public static IEnumerable<CodeInstruction> Interacted_Transpiler(IEnumerable<CodeInstruction> instructions,
                                                                         ILGenerator generator,
                                                                         MethodBase method)
        {
            var instructionsList = new List<CodeInstruction>(instructions);
            Fixup_Interacted.MatchAndReplace(method, ref instructionsList, generator);
            return instructionsList;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float GetStatValue_Wrapper(Thing thing, StatDef stat, Pawn recipient, bool applyPostProcess, int cacheStaleAfterTicks)
        {
            float value = thing.GetStatValue(stat, applyPostProcess, cacheStaleAfterTicks);
            if (stat == StatDefOf.NegotiationAbility)
                value *= recipient.GetStatValue(DefOf.XylWillFallRate);
            return value;
        }
    }
}
