using HarmonyLib;
using JetBrains.Annotations;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using TranspilerUtil;
using Verse;
using XylRacesCore.Genes;

namespace XylRacesCore.Patches
{
    [HarmonyPatch]
    public static class PatchLactation
    {
        private static readonly InstructionMatcher Fixup = new()
        {
            Rules =
            {
                InstructionMatcher.MakeRedirectRule(AccessTools.Method(typeof(HediffSet), nameof(HediffSet.GetFirstHediffOfDef), [typeof(HediffDef), typeof(bool)]),
                    AccessTools.Method(typeof(PatchLactation), nameof(GetFirstHediffOfDef_Wrapper)), minMatches: 0),
                InstructionMatcher.MakeRedirectRule(AccessTools.Method(typeof(HediffSet), nameof(HediffSet.HasHediff), [typeof(HediffDef), typeof(bool)]),
                    AccessTools.Method(typeof(PatchLactation), nameof(HasHediff_Wrapper)), minMatches: 0),
            }
        };

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Hediff GetFirstHediffOfDef_Wrapper(HediffSet __instance, HediffDef def, bool mustBeVisible)
        {
            if (def == HediffDefOf.Lactating && mustBeVisible == false)
                return __instance.pawn.HediffsWithComp<HediffComp_Lactating>().FirstOrDefault();
            return __instance.GetFirstHediffOfDef(def, mustBeVisible);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool HasHediff_Wrapper(HediffSet __instance, HediffDef def, bool mustBeVisible)
        {
            if (def == HediffDefOf.Lactating && mustBeVisible == false)
                return __instance.pawn.HediffsWithComp<HediffComp_Lactating>().Any();
            return __instance.HasHediff(def, mustBeVisible);
        }

        [Feature(nameof(Hyperlactation)), HarmonyTranspiler, UsedImplicitly, HarmonyPatch(typeof(ChildcareUtility), "CanBreastfeed")]
        public static IEnumerable<CodeInstruction> CanBreastfeed_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator, MethodBase method)
        {
            var instructionsList = new List<CodeInstruction>(instructions);
            Fixup.MatchAndReplace(method, ref instructionsList, generator);
            return instructionsList;
        }

        [Feature(nameof(Hyperlactation)), HarmonyTranspiler, UsedImplicitly, HarmonyPatch(typeof(ChildcareUtility), "CanBreastfeedNow")]
        public static IEnumerable<CodeInstruction> CanBreastfeedNow_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator, MethodBase method)
        {
            var instructionsList = new List<CodeInstruction>(instructions);
            Fixup.MatchAndReplace(method, ref instructionsList, generator);
            return instructionsList;
        }

        [Feature(nameof(Hyperlactation)), HarmonyTranspiler, UsedImplicitly, HarmonyPatch(typeof(ChildcareUtility), "SuckleFromLactatingPawn")]
        public static IEnumerable<CodeInstruction> SuckleFromLactatingPawn_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator, MethodBase method)
        {
            var instructionsList = new List<CodeInstruction>(instructions);
            Fixup.MatchAndReplace(method, ref instructionsList, generator);
            return instructionsList;
        }

        [Feature(nameof(Hyperlactation)), HarmonyTranspiler, UsedImplicitly, HarmonyPatch(typeof(QuestPart_LendColonistsToFaction), "QuestPartTick")]
        public static IEnumerable<CodeInstruction> QuestPartTick_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator, MethodBase method)
        {
            var instructionsList = new List<CodeInstruction>(instructions);
            Fixup.MatchAndReplace(method, ref instructionsList, generator);
            return instructionsList;
        }

        [Feature(nameof(Hyperlactation)), HarmonyTranspiler, UsedImplicitly, HarmonyPatch(typeof(Need_Food), "FoodFallPerTickAssumingCategory")]
        public static IEnumerable<CodeInstruction> FoodFallPerTickAssumingCategory_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator, MethodBase method)
        {
            var instructionsList = new List<CodeInstruction>(instructions);
            Fixup.MatchAndReplace(method, ref instructionsList, generator);
            return instructionsList;
        }

        [Feature(nameof(Hyperlactation)), HarmonyTranspiler, UsedImplicitly, HarmonyPatch(typeof(ITab_Pawn_Feeding), "DrawRow")]
        public static IEnumerable<CodeInstruction> DrawRow_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator, MethodBase method)
        {
            var instructionsList = new List<CodeInstruction>(instructions);
            Fixup.MatchAndReplace(method, ref instructionsList, generator);
            return instructionsList;
        }
    }
}
