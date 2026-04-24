using HarmonyLib;
using JetBrains.Annotations;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using Verse;
using XylRacesCore.Genes;

namespace XylRacesCore.Patches
{
    [HarmonyPatch(typeof(RaceProperties))]
    public static class Patch_RaceProperties
    {
        public static Lazy<bool> Enabled = new(() => Config.FeatureEnabled(Config.Feature.FixLactationBugs));

        private static readonly InstructionMatcher Fixup_NutritionEatenPerDayExplanation = new()
        {
            Rules =
            {
                new()
                {
                    Min = 1, Max = 0,
                    Mode = InstructionMatcher.OutputMode.Replace,
                    Pattern =
                    [
                        new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(Pawn), nameof(Pawn.health))),
                        new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(Pawn_HealthTracker), nameof(Pawn_HealthTracker.hediffSet))),
                        new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(HediffDefOf), nameof(HediffDefOf.Lactating))),
                        new CodeInstruction(OpCodes.Ldc_I4_0),
                        new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(HediffSet), nameof(HediffSet.GetFirstHediffOfDef))),
                    ],
                    Output =
                    [
                        CodeInstruction.Call(() => GetFirstLactationHediffOrNull),
                    ]
                },

                new()
                {
                    Min = 1, Max = 0,
                    Mode = InstructionMatcher.OutputMode.Replace,
                    Pattern =
                    [
                        // stringBuilder.AppendLine();
                        CodeInstruction.LoadLocal(0),
                        CodeInstruction.Call(typeof(StringBuilder), nameof(StringBuilder.AppendLine), []),
                        new CodeInstruction(OpCodes.Pop),
                        // stringBuilder.AppendLine("StatsReport_FinalValue".Translate() + ": " + NutritionEatenPerDay(p));
                        CodeInstruction.LoadLocal(0),
                        new CodeInstruction(OpCodes.Ldstr, "StatsReport_FinalValue"),
                        CodeInstruction.Call(typeof(Translator), nameof(Translator.Translate), [typeof(string)]),
                        new CodeInstruction(OpCodes.Ldstr, ": "),
                        CodeInstruction.Call(typeof(TaggedString), "op_Addition", [typeof(TaggedString), typeof(string)]),
                        CodeInstruction.LoadArgument(0),
                        CodeInstruction.Call(typeof(RaceProperties), nameof(RaceProperties.NutritionEatenPerDay), [typeof(Pawn)]),
                        CodeInstruction.Call(typeof(TaggedString), "op_Addition", [typeof(TaggedString), typeof(string)]),
                        CodeInstruction.Call(typeof(TaggedString), "op_Implicit", [typeof(TaggedString)]),
                        CodeInstruction.Call(typeof(StringBuilder), nameof(StringBuilder.AppendLine), [typeof(string)]),
                        new CodeInstruction(OpCodes.Pop),
                    ],
                    Output =
                    [
                        CodeInstruction.LoadLocal(0),
                        CodeInstruction.LoadArgument(0),
                        CodeInstruction.Call(() => NutritionEatenPerDayExplanationFinal),
                    ]
                },
            }
        };

        [HarmonyTranspiler, UsedImplicitly, HarmonyPatch(typeof(RaceProperties), "NutritionEatenPerDayExplanation")]
        public static IEnumerable<CodeInstruction> NutritionEatenPerDayExplanation_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var instructionsList = new List<CodeInstruction>(instructions);
            Fixup_NutritionEatenPerDayExplanation.MatchAndReplace(ref instructionsList, generator);
            return instructionsList;
        }

        public static Hediff GetFirstLactationHediffOrNull(Pawn pawn)
        {
            // See comment in Patch_RaceProperties. There is a bug around lactation nutrition in the base game which causes
            // lactating pawns to need too much food. This turns out to be a problem for bossaps balance-wise, so I'm
            // fixing the bug.
            if (Enabled.Value)
                return null;

            using (new ProfileBlock())
            {
                return PatchLactation.GetFirstLactationHediff(pawn.health.hediffSet);
            }
        }

        public static void NutritionEatenPerDayExplanationFinal(StringBuilder stringBuilder, Pawn pawn)
        {
            using (new ProfileBlock())
            {
                if (Config.FeatureEnabled(Config.Feature.FixLactationBugs))
                {
                    Hediff firstLactationHediff = PatchLactation.GetFirstLactationHediff(pawn.health.hediffSet);
                    var hediffComp_Lactating = firstLactationHediff?.TryGetComp<HediffComp_Lactating>();
                    if (hediffComp_Lactating != null)
                    {
                        stringBuilder.AppendLine();
                        stringBuilder.AppendLine(firstLactationHediff.LabelBaseCap + ": " +
                                                 hediffComp_Lactating.AddedNutritionPerDay().ToStringWithSign());
                    }
                }

                stringBuilder.AppendLine();
                stringBuilder.AppendLine("StatsReport_FinalValue".Translate() + ": " +
                                         RaceProperties.NutritionEatenPerDay(pawn));
            }
        }

        [HarmonyPrefix, UsedImplicitly, HarmonyPatch(nameof(RaceProperties.NutritionEatenPerDay))]
        static bool GetTotalNutritionNeededPerDay(Pawn p, ref string __result)
        {
            using (new ProfileBlock())
            {
                // There is a bug in the base game that causes the nutrition from lactation to be counted twice, once as part of
                // NutritionEatenPerDay which is used to calculate food fall per tick, and then the lactation hediff itself also
                // directly consumes food per tick. This correctly displays that effect.
                float lactationNutritionUsed = PatchLactation.GetFirstLactationHediff(p.health.hediffSet)
                    ?.TryGetComp<HediffComp_Lactating>()?.AddedNutritionPerDay() ?? 0;

                __result = (p.needs.food.FoodFallPerTickAssumingCategory(HungerCategory.Fed) * 60000f + lactationNutritionUsed).ToString("0.##");

                return false;
            }
        }
    }
}
