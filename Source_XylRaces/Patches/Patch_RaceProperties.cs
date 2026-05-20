using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Text;
using HarmonyLib;
using RimWorld;
using TranspilerUtil;
using Verse;

namespace XylXenos.Patches
{
    [HarmonyPatch(typeof(RaceProperties))]
    public static class Patch_RaceProperties
    {
        private static readonly InstructionMatcher Fixup_NutritionEatenPerDayExplanation = new()
        {
            Rules =
            {
                InstructionMatcher.MakeRedirectRule(nameof(HediffSet.GetFirstHediffOfDef), GetFirstHediffOfDef_Wrapper),

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

        [Feature(Config.Feature.FixLactationBugs)]
        [HarmonyTranspiler]
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
            if (Settings.instance.ShouldFixLactationBugsFor(__instance.pawn))
                return null;

            return PatchLactation.GetFirstHediffOfDef_Wrapper(__instance, def, mustBeVisible);
        }

        public static void AddLactationExplanation(StringBuilder stringBuilder, Pawn pawn)
        {
            if (!Settings.instance.ShouldFixLactationBugsFor(pawn))
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

        [Feature(Config.Feature.FixLactationBugs)]
        [HarmonyPrefix]
        [HarmonyPatch(nameof(RaceProperties.NutritionEatenPerDay))]
        static bool GetTotalNutritionNeededPerDay_Prefix(Pawn p, ref string __result)
        {
            if (!Settings.instance.ShouldFixLactationBugsFor(p))
                return true;

            // There is a bug in the base game that causes the nutrition from lactation to be counted twice, once as part of
            // NutritionEatenPerDay which is used to calculate food fall per tick, and then the lactation hediff itself also
            // directly consumes food per tick. This correctly displays that effect.
            float lactationNutritionUsed = PatchLactation.GetFirstHediffOfDef_Wrapper(p.health.hediffSet, HediffDefOf.Lactating, false)
                ?.TryGetComp<HediffComp_Lactating>()?.AddedNutritionPerDay() ?? 0;

            __result = (p.needs.food.FoodFallPerTickAssumingCategory(HungerCategory.Fed) * GenDate.TicksPerDay + lactationNutritionUsed)
                .ToString("0.##");

            return false;
        }
    }
}
