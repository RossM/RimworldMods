using HarmonyLib;
using JetBrains.Annotations;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
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
                InstructionMatcher.RedirectMethodRule(AccessTools.Method(typeof(HediffSet), nameof(HediffSet.GetFirstHediffOfDef), [typeof(HediffDef), typeof(bool)]),
                    AccessTools.Method(typeof(PatchLactation), nameof(GetFirstHediffOfDef)), minMatches: 0),
                InstructionMatcher.RedirectMethodRule(AccessTools.Method(typeof(HediffSet), nameof(HediffSet.HasHediff), [typeof(HediffDef), typeof(bool)]),
                    AccessTools.Method(typeof(PatchLactation), nameof(HasHediff)), minMatches: 0),
            }
        };

        public static Hediff GetFirstHediffOfDef(HediffSet hediffSet, HediffDef def, bool mustBeVisible)
        {
            using (new ProfileBlock())
            {
                if (def == HediffDefOf.Lactating && mustBeVisible == false)
                    return hediffSet.pawn.HediffsWithComp<HediffComp_Lactating>().FirstOrDefault();
                return hediffSet.GetFirstHediffOfDef(def, mustBeVisible);
            }
        }

        public static bool HasHediff(HediffSet hediffSet, HediffDef def, bool mustBeVisible)
        {
            using (new ProfileBlock())
            {
                if (def == HediffDefOf.Lactating && mustBeVisible == false)
                    return hediffSet.pawn.HediffsWithComp<HediffComp_Lactating>().Any();
                return hediffSet.HasHediff(def, mustBeVisible);
            }
        }

        [Feature(nameof(Hyperlactation)), HarmonyTranspiler, UsedImplicitly, HarmonyPatch(typeof(ChildcareUtility), "CanBreastfeed")]
        public static IEnumerable<CodeInstruction> CanBreastfeed_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var instructionsList = new List<CodeInstruction>(instructions);
            Fixup.MatchAndReplace(ref instructionsList, generator);
            return instructionsList;
        }

        [Feature(nameof(Hyperlactation)), HarmonyTranspiler, UsedImplicitly, HarmonyPatch(typeof(ChildcareUtility), "CanBreastfeedNow")]
        public static IEnumerable<CodeInstruction> CanBreastfeedNow_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var instructionsList = new List<CodeInstruction>(instructions);
            Fixup.MatchAndReplace(ref instructionsList, generator);
            return instructionsList;
        }

        [Feature(nameof(Hyperlactation)), HarmonyTranspiler, UsedImplicitly, HarmonyPatch(typeof(ChildcareUtility), "SuckleFromLactatingPawn")]
        public static IEnumerable<CodeInstruction> SuckleFromLactatingPawn_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var instructionsList = new List<CodeInstruction>(instructions);
            Fixup.MatchAndReplace(ref instructionsList, generator);
            return instructionsList;
        }

        [Feature(nameof(Hyperlactation)), HarmonyTranspiler, UsedImplicitly, HarmonyPatch(typeof(QuestPart_LendColonistsToFaction), "QuestPartTick")]
        public static IEnumerable<CodeInstruction> QuestPartTick_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var instructionsList = new List<CodeInstruction>(instructions);
            Fixup.MatchAndReplace(ref instructionsList, generator);
            return instructionsList;
        }

        [Feature(nameof(Hyperlactation)), HarmonyTranspiler, UsedImplicitly, HarmonyPatch(typeof(Need_Food), "FoodFallPerTickAssumingCategory")]
        public static IEnumerable<CodeInstruction> FoodFallPerTickAssumingCategory_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var instructionsList = new List<CodeInstruction>(instructions);
            Fixup.MatchAndReplace(ref instructionsList, generator);
            return instructionsList;
        }

        [Feature(nameof(Hyperlactation)), HarmonyTranspiler, UsedImplicitly, HarmonyPatch(typeof(ITab_Pawn_Feeding), "DrawRow")]
        public static IEnumerable<CodeInstruction> DrawRow_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var instructionsList = new List<CodeInstruction>(instructions);
            Fixup.MatchAndReplace(ref instructionsList, generator);
            return instructionsList;
        }
    }
}
