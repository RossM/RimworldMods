using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Text;
using HarmonyLib;
using JetBrains.Annotations;
using RimWorld;
using TranspilerUtil;
using Verse;

namespace XylRacesCore.Patches
{
    [HarmonyPatch(typeof(RaceProperties))]
    public static class Patch_RaceProperties
    {
        public static bool Enabled => enabled.Value;
        public static Lazy<bool> enabled = new(() => Config.FeatureEnabled(Config.Feature.FixLactationBugs));

        private static readonly InstructionMatcher Fixup_NutritionEatenPerDayExplanation = new()
        {
            Rules =
            {
                InstructionMatcher.MakeRedirectRule(
                    AccessTools.Method(typeof(HediffSet), nameof(HediffSet.GetFirstHediffOfDef), [typeof(HediffDef), typeof(bool)]),
                    AccessTools.Method(typeof(Patch_RaceProperties), nameof(GetFirstHediffOfDef_Wrapper))),

                new()
                {
                    Min = 1, Max = 0,
                    Mode = InstructionMatcher.OutputMode.InsertBefore,
                    Pattern =
                    [
                        // stringBuilder.AppendLine("StatsReport_FinalValue" ...
                        CodeInstruction.LoadLocal(0),
                        new CodeInstruction(OpCodes.Ldstr, "StatsReport_FinalValue"),
                    ],
                    Output =
                    [
                        // LactationExplanation(stringBuilder, pawn);
                        CodeInstruction.LoadLocal(0),
                        CodeInstruction.LoadArgument(0),
                        CodeInstruction.Call(() => AddLactationExplanation),
                    ]
                },
            }
        };

        [Feature(nameof(Config.Feature.FixLactationBugs))]
        [HarmonyTranspiler]
        [UsedImplicitly]
        [HarmonyPatch(typeof(RaceProperties), "NutritionEatenPerDayExplanation")]
        public static IEnumerable<CodeInstruction> NutritionEatenPerDayExplanation_Transpiler(
            IEnumerable<CodeInstruction> instructions,
            ILGenerator generator,
            MethodBase method)
        {
            var instructionsList = new List<CodeInstruction>(instructions);
            Fixup_NutritionEatenPerDayExplanation.MatchAndReplace(method, ref instructionsList, generator);
            return instructionsList;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Hediff GetFirstHediffOfDef_Wrapper(HediffSet __instance, HediffDef def, bool mustBeVisible)
        {
            // See comment in Patch_RaceProperties. There is a bug around lactation nutrition in the base game which causes
            // lactating pawns to need too much food. This turns out to be a problem for bossaps balance-wise, so I'm
            // fixing the bug.
            if (Enabled)
                return null;

            return PatchLactation.GetFirstHediffOfDef_Wrapper(__instance, def, mustBeVisible);
        }

        public static void AddLactationExplanation(StringBuilder stringBuilder, Pawn pawn)
        {
            if (!Enabled)
                return;

            Hediff firstLactationHediff = PatchLactation.GetFirstHediffOfDef_Wrapper(pawn.health.hediffSet, HediffDefOf.Lactating, false);
            var hediffComp_Lactating = firstLactationHediff?.TryGetComp<HediffComp_Lactating>();
            if (hediffComp_Lactating != null)
            {
                stringBuilder.AppendLine(
                    $"{firstLactationHediff.LabelBaseCap}: {hediffComp_Lactating.AddedNutritionPerDay().ToStringWithSign()}");
                stringBuilder.AppendLine();
            }
        }

        [Feature(nameof(Config.Feature.FixLactationBugs))]
        [HarmonyPrefix]
        [UsedImplicitly]
        [HarmonyPatch(nameof(RaceProperties.NutritionEatenPerDay))]
        static bool GetTotalNutritionNeededPerDay(Pawn p, ref string __result)
        {
            // There is a bug in the base game that causes the nutrition from lactation to be counted twice, once as part of
            // NutritionEatenPerDay which is used to calculate food fall per tick, and then the lactation hediff itself also
            // directly consumes food per tick. This correctly displays that effect.
            float lactationNutritionUsed = PatchLactation.GetFirstHediffOfDef_Wrapper(p.health.hediffSet, HediffDefOf.Lactating, false)
                ?.TryGetComp<HediffComp_Lactating>()?.AddedNutritionPerDay() ?? 0;

            __result = (p.needs.food.FoodFallPerTickAssumingCategory(HungerCategory.Fed) * 60000f + lactationNutritionUsed)
                .ToString("0.##");

            return false;
        }
    }
}
