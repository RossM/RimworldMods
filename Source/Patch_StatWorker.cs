using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace XylRacesCore
{
    [HarmonyPatch(typeof(StatWorker))]
    public class Patch_StatWorker
    {
        [HarmonyTranspiler, HarmonyPatch(nameof(StatWorker.GetOffsetsAndFactorsExplanation))]
        static IEnumerable<CodeInstruction> GetOffsetsAndFactorsExplanation_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var instructionsList = new List<CodeInstruction>(instructions);

            var substitutions = new List<TranspilerUtil.Substitution>()
            {
                new()
                {
                    // Matches in the line "if (stat.capacityOffsets != null)"
                    Match =
                    [
                        CodeInstruction.LoadField(typeof(StatDef), nameof(StatDef.capacityOffsets))
                    ],
                    Replace =
                    [
                        // Toss the input that was going to be used for the load
                        new CodeInstruction(OpCodes.Pop),

                        // Read this, req, sb, and whitespace from arguments
                        CodeInstruction.LoadArgument(0),
                        CodeInstruction.LoadArgument(1),
                        CodeInstruction.LoadArgument(2),
                        CodeInstruction.LoadArgument(4),
                        // Call our new function
                        CodeInstruction.Call(() => GetOffsetsAndFactorsExplanation_CapacityOffsets),

                        // Put a null on the stack so the "if (capacityOffsets != null)" block is skipped
                        new CodeInstruction(OpCodes.Ldnull)
                    ]
                },
                new()
                {
                    // Matches in the line "if (stat.capacityFactors != null)"
                    Match = new[]
                    {
                        CodeInstruction.LoadField(typeof(StatDef), nameof(StatDef.capacityFactors)),
                    },
                    Replace = new[]
                    {
                        // Toss the input that was going to be used for the load
                        new CodeInstruction(OpCodes.Pop),

                        // Read this, req, sb, and whitespace from arguments
                        CodeInstruction.LoadArgument(0),
                        CodeInstruction.LoadArgument(1),
                        CodeInstruction.LoadArgument(2),
                        CodeInstruction.LoadArgument(4),
                        // Call our new function
                        CodeInstruction.Call(() => GetOffsetsAndFactorsExplanation_CapacityFactors),

                        // Put a null on the stack so the "if (capacityOffsets != null)" block is skipped
                        new CodeInstruction(OpCodes.Ldnull),
                    }
                },
            };

            if (!TranspilerUtil.MatchAndReplace(substitutions, ref instructionsList, out string reason))
                Log.Error(string.Format("XylRacesCore.Patch_StatWorker.GetOffsetsAndFactorsExplanation_Transpiler: {0}", reason));
            return instructionsList;
        }

        private static TaggedString HediffLabel(Hediff_SubstituteCapacity foundHediff)
        {
            return (foundHediff.LabelCap + ": " +
                    foundHediff.CompProperties.originalCapacity.LabelCap +
                    " -> " + foundHediff.CompProperties.substituteCapacity.LabelCap);
        }

        static void GetOffsetsAndFactorsExplanation_CapacityOffsets(StatWorker instance, RimWorld.StatRequest req,
            StringBuilder sb, string whitespace)
        {
            var pawn = (Pawn)req.Thing;
            var stat = (StatDef)AccessTools.Field(typeof(StatWorker), "stat").GetValue(instance);

            if (stat.capacityOffsets == null)
                return;

            sb.AppendLine(whitespace + ("StatsReport_Health".CanTranslate()
                ? "StatsReport_Health".Translate()
                : "StatsReport_HealthFactors".Translate()));
            foreach (PawnCapacityOffset item in stat.capacityOffsets.OrderBy((PawnCapacityOffset hfa) =>
                         hfa.capacity.listOrder))
            {
                PawnCapacityDef capacity = item.capacity;

                Hediff_SubstituteCapacity foundHediff = FindHediffFor(pawn, capacity, stat);
                if (foundHediff != null)
                    capacity = foundHediff.CompProperties.substituteCapacity;

                float offset = item.GetOffset(pawn.health.capacities.GetLevel(capacity));
                string offsetInfo = Mathf.Min(pawn.health.capacities.GetLevel(capacity), item.max).ToStringPercent() +
                                    ", " + "HealthOffsetScale".Translate(item.scale + "x");
                if (item.max < 999f)
                {
                    offsetInfo += ", " + "HealthFactorMaxImpact".Translate(item.max.ToStringPercent());
                }

                sb.AppendLine(whitespace + "    " + capacity.GetLabelFor(pawn).CapitalizeFirst() + ": " +
                              offset.ToStringSign() + instance.ValueToString(offset, finalized: false) + " (" +
                              offsetInfo + ")");

                if (foundHediff != null)
                {
                    sb.AppendLine(whitespace + "        " + HediffLabel(foundHediff));
                }
            }
        }

        static void GetOffsetsAndFactorsExplanation_CapacityFactors(StatWorker instance, RimWorld.StatRequest req,
            StringBuilder sb, string whitespace)
        {
            var pawn = (Pawn)req.Thing;
            var stat = (StatDef)AccessTools.Field(typeof(StatWorker), "stat").GetValue(instance);
            if (stat.capacityFactors == null)
                return;

            sb.AppendLine(whitespace + ("StatsReport_Health".CanTranslate()
                ? "StatsReport_Health".Translate()
                : "StatsReport_HealthFactors".Translate()));

            foreach (PawnCapacityFactor item in stat.capacityFactors.OrderBy((PawnCapacityFactor hfa) =>
                         hfa.capacity.listOrder))
            {
                PawnCapacityDef capacity = item.capacity;

                Hediff_SubstituteCapacity foundHediff = FindHediffFor(pawn, capacity, stat);
                if (foundHediff != null)
                    capacity = foundHediff.CompProperties.substituteCapacity;

                float factor = item.GetFactor(pawn.health.capacities.GetLevel(capacity));
                string factorInfo = "HealthFactorPercentImpact".Translate(item.weight.ToStringPercent());
                if (item.max < 999f)
                {
                    factorInfo += ", " + "HealthFactorMaxImpact".Translate(item.max.ToStringPercent());
                }

                if (item.allowedDefect != 0f)
                {
                    factorInfo += ", " +
                                  "HealthFactorAllowedDefect".Translate((1f - item.allowedDefect).ToStringPercent());
                }

                sb.AppendLine(whitespace + "    " + capacity.GetLabelFor(pawn).CapitalizeFirst() + ": x" +
                              factor.ToStringPercent() + " (" + factorInfo + ")");

                if (foundHediff != null)
                {
                    var modifier = pawn.health.capacities.GetLevel(foundHediff.CompProperties.substituteCapacity) -
                                pawn.health.capacities.GetLevel(foundHediff.CompProperties.originalCapacity);
                    sb.AppendLine(whitespace + "        " + HediffLabel(foundHediff) + " (" + modifier.ToStringPercentSigned() + ")");
                }
            }
        }

        static HediffStage GetOffsetsAndFactorsExplanation_CurStage(Hediff hediff)
        {
            if (hediff is Hediff_SubstituteCapacity { Active: true })
                return null;

            return hediff.CurStage;
        }

        [HarmonyTranspiler, HarmonyPatch(nameof(StatWorker.GetValueUnfinalized))]
        static IEnumerable<CodeInstruction> GetValueUnfinalized_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var instructionsList = new List<CodeInstruction>(instructions);

            var substitutions = new List<TranspilerUtil.Substitution>()
            {
                new()
                {
                    // Match in the line "float num = GetBaseValueFor(req);"
                    Match =
                    [
                        CodeInstruction.Call(typeof(StatWorker), nameof(StatWorker.GetBaseValueFor)),
                        CodeInstruction.StoreLocal(0),
                    ],
                },
                new()
                {
                    // Matches in the line "if (stat.capacityOffsets != null)"
                    Match = new[]
                    {
                        CodeInstruction.LoadField(typeof(StatDef), nameof(StatDef.capacityOffsets)),
                    },
                    Replace = new[]
                    {
                        // Toss the input that was going to be used for the load
                        new CodeInstruction(OpCodes.Pop),

                        // Read this, req from arguments
                        CodeInstruction.LoadArgument(0),
                        CodeInstruction.LoadArgument(1),
                        // Read num from locals
                        CodeInstruction.LoadLocal(0),
                        // Call our new function
                        CodeInstruction.Call(() => GetValueUnfinalized_CapacityOffsets),
                        // Save num to locals
                        CodeInstruction.StoreLocal(0),

                        // Put a null on the stack so the "if (capacityFactors != null)" block is skipped
                        new CodeInstruction(OpCodes.Ldnull),
                    }
                },
                new()
                {
                    // Matches in the line "if (stat.capacityFactors != null)"
                    Match = new[]
                    {
                        CodeInstruction.LoadField(typeof(StatDef), nameof(StatDef.capacityFactors)),
                    },
                    Replace = new[]
                    {
                        // Toss the input that was going to be used for the load
                        new CodeInstruction(OpCodes.Pop),

                        // Read this, req from arguments
                        CodeInstruction.LoadArgument(0),
                        CodeInstruction.LoadArgument(1),
                        // Read num from locals
                        CodeInstruction.LoadLocal(0),
                        // Call our new function
                        CodeInstruction.Call(() => GetValueUnfinalized_CapacityFactors),
                        // Save num to locals
                        CodeInstruction.StoreLocal(0),

                        // Put a null on the stack so the "if (capacityFactors != null)" block is skipped
                        new CodeInstruction(OpCodes.Ldnull),
                    }
                },
            };

            if (!TranspilerUtil.MatchAndReplace(substitutions, ref instructionsList, out string reason))
                Log.Error(string.Format("XylRacesCore.Patch_StatWorker.GetValueUnfinalized_Transpiler: {0}", reason));
            return instructionsList;
        }

        static float GetValueUnfinalized_CapacityOffsets(StatWorker instance, StatRequest req, float num)
        {
            var pawn = (Pawn)req.Thing;
            var stat = (StatDef)AccessTools.Field(typeof(StatWorker), "stat").GetValue(instance);
            if (stat.capacityOffsets == null)
                return num;

            foreach (PawnCapacityOffset pawnCapacityOffset in stat.capacityOffsets)
            {
                PawnCapacityDef capacity = pawnCapacityOffset.capacity;

                Hediff_SubstituteCapacity foundHediff = FindHediffFor(pawn, capacity, stat);
                if (foundHediff != null)
                    capacity = foundHediff.CompProperties.substituteCapacity;

                num += pawnCapacityOffset.GetOffset(pawn.health.capacities.GetLevel(capacity));
            }

            return num;
        }

        static float GetValueUnfinalized_CapacityFactors(StatWorker instance, StatRequest req, float num)
        {
            var pawn = (Pawn)req.Thing;
            var stat = (StatDef)AccessTools.Field(typeof(StatWorker), "stat").GetValue(instance);
            if (stat.capacityFactors == null)
                return num;

            foreach (PawnCapacityFactor pawnCapacityFactor in stat.capacityFactors)
            {
                PawnCapacityDef capacity = pawnCapacityFactor.capacity;

                Hediff_SubstituteCapacity foundHediff = FindHediffFor(pawn, capacity, stat);
                if (foundHediff != null)
                    capacity = foundHediff.CompProperties.substituteCapacity;

                float factor = pawnCapacityFactor.GetFactor(pawn.health.capacities.GetLevel(capacity));
                num = Mathf.Lerp(num, num * factor, pawnCapacityFactor.weight);
            }
            return num;
        }

        private static Hediff_SubstituteCapacity FindHediffFor(Pawn pawn, PawnCapacityDef capacity, StatDef stat)
        {
            return pawn.health.hediffSet.hediffs.OfType<Hediff_SubstituteCapacity>().FirstOrDefault(CheckFn);

            bool CheckFn(Hediff_SubstituteCapacity h)
            {
                if (h.CompProperties.originalCapacity != capacity)
                    return false;
                if (!h.Active)
                    return false;
                if (h.CompProperties.excludeStats != null && h.CompProperties.excludeStats.Contains(stat))
                    return false;
                return true;
            }
        }
    }
}
