using HarmonyLib;
using JetBrains.Annotations;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
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
                new()
                {
                    Min = 0, Max = 0,
                    Mode = InstructionMatcher.OutputMode.Replace,
                    Pattern =
                    [
                        new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(HediffDefOf), nameof(HediffDefOf.Lactating))),
                        new CodeInstruction(OpCodes.Ldc_I4_0),
                        new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(HediffSet), nameof(HediffSet.GetFirstHediffOfDef))),
                    ],
                    Output =
                    [
                        CodeInstruction.Call(() => GetFirstLactationHediff),
                    ]
                },
                new()
                {
                    Min = 0, Max = 0,
                    Mode = InstructionMatcher.OutputMode.Replace,
                    Pattern =
                    [
                        new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(HediffDefOf), nameof(HediffDefOf.Lactating))),
                        new CodeInstruction(OpCodes.Ldc_I4_0),
                        new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(HediffSet), nameof(HediffSet.HasHediff), [typeof(HediffDef), typeof(bool)])),
                    ],
                    Output =
                    [
                        CodeInstruction.Call(() => HasLactationHediff),
                    ]
                },
            }
        };

        public static Hediff GetFirstLactationHediff(HediffSet hediffSet)
        {
            using (new ProfileBlock())
            {
                return hediffSet.pawn.HediffsWithComp<HediffComp_Lactating>().FirstOrDefault();
            }
        }

        public static bool HasLactationHediff(HediffSet hediffSet)
        {
            using (new ProfileBlock())
            {
                return hediffSet.pawn.HediffsWithComp<HediffComp_Lactating>().Any();
            }
        }

        [HarmonyTranspiler, UsedImplicitly, HarmonyPatch(typeof(ChildcareUtility), "CanBreastfeed")]
        public static IEnumerable<CodeInstruction> CanBreastfeed_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var instructionsList = new List<CodeInstruction>(instructions);
            Fixup.MatchAndReplace(ref instructionsList, generator);
            return instructionsList;
        }

        [HarmonyTranspiler, UsedImplicitly, HarmonyPatch(typeof(ChildcareUtility), "CanBreastfeedNow")]
        public static IEnumerable<CodeInstruction> CanBreastfeedNow_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var instructionsList = new List<CodeInstruction>(instructions);
            Fixup.MatchAndReplace(ref instructionsList, generator);
            return instructionsList;
        }

        [HarmonyTranspiler, UsedImplicitly, HarmonyPatch(typeof(ChildcareUtility), "SuckleFromLactatingPawn")]
        public static IEnumerable<CodeInstruction> SuckleFromLactatingPawn_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var instructionsList = new List<CodeInstruction>(instructions);
            Fixup.MatchAndReplace(ref instructionsList, generator);
            return instructionsList;
        }

        [HarmonyTranspiler, UsedImplicitly, HarmonyPatch(typeof(QuestPart_LendColonistsToFaction), "QuestPartTick")]
        public static IEnumerable<CodeInstruction> QuestPartTick_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var instructionsList = new List<CodeInstruction>(instructions);
            Fixup.MatchAndReplace(ref instructionsList, generator);
            return instructionsList;
        }

        [HarmonyTranspiler, UsedImplicitly, HarmonyPatch(typeof(Need_Food), "FoodFallPerTickAssumingCategory")]
        public static IEnumerable<CodeInstruction> FoodFallPerTickAssumingCategory_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var instructionsList = new List<CodeInstruction>(instructions);
            Fixup.MatchAndReplace(ref instructionsList, generator);
            return instructionsList;
        }

        [HarmonyTranspiler, UsedImplicitly, HarmonyPatch(typeof(ITab_Pawn_Feeding), "DrawRow")]
        public static IEnumerable<CodeInstruction> DrawRow_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var instructionsList = new List<CodeInstruction>(instructions);
            Fixup.MatchAndReplace(ref instructionsList, generator);
            return instructionsList;
        }
    }
}
