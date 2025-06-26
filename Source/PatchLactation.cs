using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace XylRacesCore
{
    [HarmonyPatch]
    public static class PatchLactation
    {
        public static HediffDef Hyperlactating = null;

        private static readonly InstructionMatcher Fixup = new()
        {
            Rules =
            {
                new()
                {
                    Min = 0, Max = 0,
                    Mode = InstructionMatcher.OutputMode.Replace,
                    Pattern =
                    [
                        CodeInstruction.LoadField(typeof(Pawn), "health"),
                        CodeInstruction.LoadField(typeof(Pawn_HealthTracker), "hediffSet"),
                        new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(HediffDefOf), "Lactating")),
                        new CodeInstruction(OpCodes.Ldc_I4_0),
                        new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(HediffSet), "GetFirstHediffOfDef")),
                    ],
                    Output =
                    [
                        CodeInstruction.Call(() => Gene_Hyperlactation.GetPawnLactationHediff),
                    ]
                },
                new()
                {
                    Min = 0, Max = 0,
                    Mode = InstructionMatcher.OutputMode.Replace,
                    Pattern =
                    [
                        CodeInstruction.LoadField(typeof(Pawn), "health"),
                        CodeInstruction.LoadField(typeof(Pawn_HealthTracker), "hediffSet"),
                        new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(HediffDefOf), "Lactating")),
                        new CodeInstruction(OpCodes.Ldc_I4_0),
                        new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(HediffSet), "HasHediff", [typeof(HediffDef), typeof(bool)])),
                    ],
                    Output =
                    [
                        CodeInstruction.Call(() => Gene_Hyperlactation.HasPawnLactationHediff),
                    ]
                },
            }
        };

        [HarmonyPatch(typeof(RaceProperties), "NutritionEatenPerDayExplanation")]
        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> NutritionEatenPerDayExplanation_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var instructionsList = new List<CodeInstruction>(instructions);
            if (!Fixup.MatchAndReplace(ref instructionsList, out string reason, generator))
                Log.Error(string.Format("XylRacesCore.PatchLactation.NutritionEatenPerDayExplanation_Transpiler: {0}", reason));
            return instructionsList;
        }

        [HarmonyPatch(typeof(ChildcareUtility), "CanBreastfeed")]
        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> CanBreastfeed_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var instructionsList = new List<CodeInstruction>(instructions);
            if (!Fixup.MatchAndReplace(ref instructionsList, out string reason, generator))
                Log.Error(string.Format("XylRacesCore.PatchLactation.CanBreastfeed_Transpiler: {0}", reason));
            return instructionsList;
        }

        [HarmonyPatch(typeof(ChildcareUtility), "CanBreastfeedNow")]
        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> CanBreastfeedNow_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var instructionsList = new List<CodeInstruction>(instructions);
            if (!Fixup.MatchAndReplace(ref instructionsList, out string reason, generator))
                Log.Error(string.Format("XylRacesCore.PatchLactation.CanBreastfeedNow_Transpiler: {0}", reason));
            return instructionsList;
        }

        [HarmonyPatch(typeof(ChildcareUtility), "SuckleFromLactatingPawn")]
        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> SuckleFromLactatingPawn_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var instructionsList = new List<CodeInstruction>(instructions);
            if (!Fixup.MatchAndReplace(ref instructionsList, out string reason, generator))
                Log.Error(string.Format("XylRacesCore.PatchLactation.SuckleFromLactatingPawn_Transpiler: {0}", reason));
            return instructionsList;
        }

        [HarmonyPatch(typeof(QuestPart_LendColonistsToFaction), "QuestPartTick")]
        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> QuestPartTick_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var instructionsList = new List<CodeInstruction>(instructions);
            if (!Fixup.MatchAndReplace(ref instructionsList, out string reason, generator))
                Log.Error(string.Format("XylRacesCore.PatchLactation.QuestPartTick_Transpiler: {0}", reason));
            return instructionsList;
        }

        //[HarmonyPatch(typeof(PregnancyUtility), "ApplyBirthOutcome")]
        //[HarmonyTranspiler]
        //static IEnumerable<CodeInstruction> ApplyBirthOutcome_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        //{
        //    var instructionsList = new List<CodeInstruction>(instructions);
        //    if (!Fixup.MatchAndReplace(ref instructionsList, out string reason, generator))
        //        Log.Error(string.Format("XylRacesCore.PatchLactation.Transpiler: {0}", reason));
        //    return instructionsList;
        //}

        [HarmonyPatch(typeof(Need_Food), "FoodFallPerTickAssumingCategory")]
        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> FoodFallPerTickAssumingCategory_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var instructionsList = new List<CodeInstruction>(instructions);
            if (!Fixup.MatchAndReplace(ref instructionsList, out string reason, generator))
                Log.Error(string.Format("XylRacesCore.PatchLactation.FoodFallPerTickAssumingCategory: {0}", reason));
            return instructionsList;
        }

        //[HarmonyPatch(typeof(ITab_Pawn_Feeding), "FillTab", [typeof(Pawn), typeof(Rect), typeof(Vector2), typeof(Vector2), typeof(List<Pawn>)])]
        //[HarmonyTranspiler]
        //static IEnumerable<CodeInstruction> FillTab_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        //{
        //    var instructionsList = new List<CodeInstruction>(instructions);
        //    if (!Fixup.MatchAndReplace(ref instructionsList, out string reason, generator))
        //        Log.Error(string.Format("XylRacesCore.PatchLactation.Transpiler: {0}", reason));
        //    return instructionsList;
        //}

        [HarmonyPatch(typeof(ITab_Pawn_Feeding), "DrawRow")]
        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> DrawRow_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var instructionsList = new List<CodeInstruction>(instructions);
            if (!Fixup.MatchAndReplace(ref instructionsList, out string reason, generator))
                Log.Error(string.Format("XylRacesCore.PatchLactation.DrawRow_Transpiler: {0}", reason));
            return instructionsList;
        }

    }
}
