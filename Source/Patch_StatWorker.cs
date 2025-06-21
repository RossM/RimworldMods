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
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            bool foundCapacityFactors = false;
            bool foundCapacityOffsets = false;
            bool foundCurStage = false;

            foreach (var instruction in instructions)
            {
                // Matches in the line "if (stat.capacityOffsets != null)"
                if (!foundCapacityOffsets &&
                    instruction.LoadsField(AccessTools.Field(typeof(StatDef), nameof(StatDef.capacityOffsets))))
                {
                    // stat.CapacityOffsets -> GetOffsetsAndFactorsExplanation_CapacityOffsets(); null

                    foundCapacityOffsets = true;

                    // Toss the input that was going to be used for the load
                    yield return new CodeInstruction(OpCodes.Pop);

                    // Read this, req, sb, and whitespace from arguments
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Ldarg_1);
                    yield return new CodeInstruction(OpCodes.Ldarg_2);
                    yield return new CodeInstruction(OpCodes.Ldarg, 4);
#pragma warning disable CS8974 // Converting method group to non-delegate type
                    // Call our new function
                    yield return new CodeInstruction(OpCodes.Call,
                        AccessTools.Method(typeof(Patch_StatWorker),
                            nameof(GetOffsetsAndFactorsExplanation_CapacityOffsets)));
#pragma warning restore CS8974 // Converting method group to non-delegate type

                    // Put a null on the stack so the "if (capacityOffsets != null)" block is skipped
                    yield return new CodeInstruction(OpCodes.Ldnull);
                    continue;
                }

                // Matches in the line "if (stat.capacityFactors != null)"
                if (!foundCapacityFactors &&
                    instruction.LoadsField(AccessTools.Field(typeof(StatDef), nameof(StatDef.capacityFactors))))
                {
                    // stat.CapacityFactors -> GetOffsetsAndFactorsExplanation_CapacityFactors(); null

                    foundCapacityFactors = true;

                    // Toss the input that was going to be used for the load
                    yield return new CodeInstruction(OpCodes.Pop);

                    // Read this, req, sb, and whitespace from arguments
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Ldarg_1);
                    yield return new CodeInstruction(OpCodes.Ldarg_2);
                    yield return new CodeInstruction(OpCodes.Ldarg, 4);
#pragma warning disable CS8974 // Converting method group to non-delegate type
                    // Call our new function
                    yield return new CodeInstruction(OpCodes.Call,
                        AccessTools.Method(typeof(Patch_StatWorker),
                            nameof(GetOffsetsAndFactorsExplanation_CapacityFactors)));
#pragma warning restore CS8974 // Converting method group to non-delegate type

                    // Put a null on the stack so the "if (capacityFactors != null)" block is skipped
                    yield return new CodeInstruction(OpCodes.Ldnull);
                    continue;
                }

                // Matches in the line "HediffStage curStage = hediffs[num4].CurStage;"
                if (!foundCurStage &&
                    instruction.Calls(AccessTools.PropertyGetter(typeof(Hediff), nameof(Hediff.CurStage))))
                {
                    // hediffs[num4].CurStage -> GetOffsetsAndFactorsExplanation_CurStage(hediffs[num4])

                    foundCurStage = true;

                    // We have the hediff on the stack, so we just call our method to check it
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Patch_StatWorker),
                        nameof(GetOffsetsAndFactorsExplanation_CurStage)));
                    // The HediffStage or null is now on the stack
                    
                    continue;
                }

                yield return instruction;
            }
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

                Hediff_SubstituteCapacity foundHediff = pawn.health.hediffSet.hediffs
                    .OfType<Hediff_SubstituteCapacity>()
                    .FirstOrDefault(h => h.CompProperties.originalCapacity == capacity && h.Active);
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

                Hediff_SubstituteCapacity foundHediff = pawn.health.hediffSet.hediffs
                    .OfType<Hediff_SubstituteCapacity>()
                    .FirstOrDefault(h => h.CompProperties.originalCapacity == capacity && h.Active);
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
                    sb.AppendLine(whitespace + "        " + HediffLabel(foundHediff));
                }
            }
        }

        static HediffStage GetOffsetsAndFactorsExplanation_CurStage(Hediff hediff)
        {
            if (hediff is Hediff_SubstituteCapacity { Active: true })
                return null;

            return hediff.CurStage;
        }
    }
}
