using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
        private static readonly FieldInfo statField = AccessTools.Field(typeof(StatWorker), "stat");

        [HarmonyTranspiler, HarmonyPatch(nameof(StatWorker.GetOffsetsAndFactorsExplanation))]
        static IEnumerable<CodeInstruction> GetOffsetsAndFactorsExplanation_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var instructionsList = new List<CodeInstruction>(instructions);

            var instructionMatcher = new InstructionMatcher()
            {
                LocalTypes =
                {
                    typeof(Pawn),
                    typeof(PawnCapacityDef),
                    typeof(Hediff_SubstituteCapacity),
                },
                Rules =
                {
                    new()
                    {
                        Match =
                        [
                            // Match to find the local "pawn" is stored in
                            new CodeInstruction(OpCodes.Isinst, typeof(Pawn)),
                            CodeInstruction.StoreLocal(0),
                        ]
                    },

                    new()
                    {
                        Min = 1, Max = 0,
                        PreserveOriginal = true,
                        KeepLocals = false,
                        Match =
                        [
                            CodeInstruction.LoadField(typeof(PawnCapacityOffset), nameof(PawnCapacityOffset.capacity)),
                        ],
                        Replace =
                        [
                            CodeInstruction.StoreLocal(1),
                            // Hediff_SubstituteCapacity foundHediff = FindHediffFor(pawn, capacity, stat);
                            // Load pawn
                            CodeInstruction.LoadLocal(0),
                            // Load capacity
                            CodeInstruction.LoadLocal(1),
                            // Load this.stat
                            CodeInstruction.LoadArgument(0),
                            CodeInstruction.LoadField(typeof(StatWorker), "stat"),
                            // Call FindHediffFor
                            CodeInstruction.Call(() => FindHediffFor),
                            // Save a copy of the hediff
                            new CodeInstruction(OpCodes.Dup),
                            CodeInstruction.StoreLocal(2), 
                            // capacity = ConditionalSetCapacity(foundHediff, capacity);
                            // Load the capacity
                            CodeInstruction.LoadLocal(1),
                            // Call ConditionalSetCapacity (because I don't want to emit an if)
                            CodeInstruction.Call(() => ConditionalSetCapacity), 
                        ]
                    },
                    new()
                    {
                        Min = 1, Max = 1,
                        PreserveOriginal = true,
                        KeepLocals = false,
                        Chained = true,
                        Match =
                        [
                            // sb.AppendLine(whitespace + "    " + text + ": " + offset.ToStringSign() + text2 + " (" + text3 + ")");
                            CodeInstruction.Call(typeof(StringBuilder), nameof(StringBuilder.AppendLine), [typeof(string)]),
                        ],
                        Replace =
                        [
                            // AppendSubstitutionDescription(sb, whitespace, foundHediff, pawn);
                            // sb
                            CodeInstruction.LoadArgument(2),
                            // whitespace
                            CodeInstruction.LoadArgument(4),
                            // foundHediff
                            CodeInstruction.LoadLocal(2),
                            // pawn
                            CodeInstruction.LoadLocal(0),
                            // Call
                            CodeInstruction.Call(() => AppendSubstitutionDescription),
                        ],
                    },
                    new()
                    {
                        Min = 1, Max = 0,
                        PreserveOriginal = true,
                        KeepLocals = false,
                        Match =
                        [
                            CodeInstruction.LoadField(typeof(PawnCapacityFactor), nameof(PawnCapacityFactor.capacity)),
                        ],
                        Replace =
                        [
                            CodeInstruction.StoreLocal(1),
                            // Hediff_SubstituteCapacity foundHediff = FindHediffFor(pawn, capacity, stat);
                            // Load pawn
                            CodeInstruction.LoadLocal(0),
                            // Load capacity
                            CodeInstruction.LoadLocal(1),
                            // Load this.stat
                            CodeInstruction.LoadArgument(0),
                            CodeInstruction.LoadField(typeof(StatWorker), "stat"),
                            // Call FindHediffFor
                            CodeInstruction.Call(() => FindHediffFor),
                            // Save a copy of the hediff
                            new CodeInstruction(OpCodes.Dup),
                            CodeInstruction.StoreLocal(2), 
                            // capacity = ConditionalSetCapacity(foundHediff, capacity);
                            // Load the capacity
                            CodeInstruction.LoadLocal(1),
                            // Call ConditionalSetCapacity (because I don't want to emit an if)
                            CodeInstruction.Call(() => ConditionalSetCapacity),
                        ]
                    },
                    new()
                    {
                        Min = 1, Max = 1,
                        PreserveOriginal = true,
                        KeepLocals = false,
                        Chained = true,
                        Match =
                        [
                            // sb.AppendLine(whitespace + "    " + text8 + ": x" + text9 + " (" + text10 + ")");
                            CodeInstruction.Call(typeof(StringBuilder), nameof(StringBuilder.AppendLine), [typeof(string)]),
                        ],
                        Replace =
                        [
                            // AppendSubstitutionDescription(sb, whitespace, foundHediff, pawn);
                            // sb
                            CodeInstruction.LoadArgument(2),
                            // whitespace
                            CodeInstruction.LoadArgument(4),
                            // foundHediff
                            CodeInstruction.LoadLocal(2),
                            // pawn
                            CodeInstruction.LoadLocal(0),
                            // Call
                            CodeInstruction.Call(() => AppendSubstitutionDescription),
                        ],
                    }
                }
            };
            if (!instructionMatcher.MatchAndReplace(ref instructionsList, out string reason, generator))
                Log.Error(string.Format("XylRacesCore.Patch_StatWorker.GetOffsetsAndFactorsExplanation_Transpiler: {0}", reason));
            return instructionsList;
        }

        static void GetOffsetsAndFactorsExplanation_CapacityOffsets(StatWorker instance, RimWorld.StatRequest req,
            StringBuilder sb, string whitespace)
        {
            var pawn = (Pawn)req.Thing;
            var stat = (StatDef)statField.GetValue(instance);

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
                    sb.AppendLine(whitespace + "        " + foundHediff.DescriptionFor(pawn));
            }
        }

        static void GetOffsetsAndFactorsExplanation_CapacityFactors(StatWorker instance, RimWorld.StatRequest req,
            StringBuilder sb, string whitespace)
        {
            var pawn = (Pawn)req.Thing;
            var stat = (StatDef)statField.GetValue(instance);
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
                capacity = ConditionalSetCapacity(foundHediff, capacity);

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

                AppendSubstitutionDescription(sb, whitespace, foundHediff, pawn);
            }
        }

        private static void AppendSubstitutionDescription(StringBuilder sb, string whitespace,
            Hediff_SubstituteCapacity foundHediff, Pawn pawn)
        {
            if (foundHediff != null) 
                sb.AppendLine(whitespace + "        " + foundHediff.DescriptionFor(pawn));
        }

        private static PawnCapacityDef ConditionalSetCapacity(Hediff_SubstituteCapacity foundHediff, PawnCapacityDef capacity)
        {
            if (foundHediff != null)
                capacity = foundHediff.CompProperties.substituteCapacity;
            return capacity;
        }

        static HediffStage GetOffsetsAndFactorsExplanation_CurStage(Hediff hediff)
        {
            if (hediff is Hediff_SubstituteCapacity { Active: true })
                return null;

            return hediff.CurStage;
        }

        [HarmonyTranspiler, HarmonyPatch(nameof(StatWorker.GetValueUnfinalized))]
        static IEnumerable<CodeInstruction> GetValueUnfinalized_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var instructionsList = new List<CodeInstruction>(instructions);

            var instructionMatcher = new InstructionMatcher()
            {
                LocalTypes = 
                {
                    typeof(Pawn),
                    typeof(PawnCapacityDef),
                },
                Rules =
                {
                    new()
                    {
                        Match =
                        [
                            // Match to find the local "pawn" is stored in
                            new CodeInstruction(OpCodes.Isinst, typeof(Pawn)),
                            CodeInstruction.StoreLocal(0), 
                        ]
                    },

                    new()
                    {
                        Min = 1, Max = 0,
                        PreserveOriginal = true,
                        Match =
                        [
                            CodeInstruction.LoadField(typeof(PawnCapacityOffset), nameof(PawnCapacityOffset.capacity)),
                        ],
                        Replace =
                        [
                            CodeInstruction.StoreLocal(1),
                            // Load pawn
                            CodeInstruction.LoadLocal(0),
                            // Load capacity
                            CodeInstruction.LoadLocal(1),
                            // Load this.stat
                            CodeInstruction.LoadArgument(0), 
                            CodeInstruction.LoadField(typeof(StatWorker), "stat"),
                            // Call GetSubstituteCapacity
                            CodeInstruction.Call(() => GetSubstituteCapacity), 
                        ]
                    },

                    new()
                    {
                        Min = 1, Max = 0,
                        PreserveOriginal = true,
                        Match =
                        [
                            CodeInstruction.LoadField(typeof(PawnCapacityFactor), nameof(PawnCapacityFactor.capacity)),
                        ],
                        Replace =
                        [
                            CodeInstruction.StoreLocal(1),
                            // Load pawn
                            CodeInstruction.LoadLocal(0),
                            // Load capacity
                            CodeInstruction.LoadLocal(1),
                            // Load this.stat
                            CodeInstruction.LoadArgument(0),
                            CodeInstruction.LoadField(typeof(StatWorker), "stat"),
                            // Call GetSubstituteCapacity
                            CodeInstruction.Call(() => GetSubstituteCapacity),
                        ]
                    },
                }
            };
            if (!instructionMatcher.MatchAndReplace(ref instructionsList, out string reason, generator))
                Log.Error(string.Format("XylRacesCore.Patch_StatWorker.GetValueUnfinalized_Transpiler: {0}", reason));
            return instructionsList;
        }

        private static PawnCapacityDef GetSubstituteCapacity(Pawn pawn, PawnCapacityDef capacity, StatDef stat)
        {
            Hediff_SubstituteCapacity foundHediff = FindHediffFor(pawn, capacity, stat);
            if (foundHediff != null)
                capacity = foundHediff.CompProperties.substituteCapacity;
            return capacity;
        }

        private static Hediff_SubstituteCapacity FindHediffFor(Pawn pawn, PawnCapacityDef capacity, StatDef stat)
        {
            return pawn.health.hediffSet.hediffs.OfType<Hediff_SubstituteCapacity>().FirstOrDefault(hediff => hediff.Validate(stat, capacity));
        }
    }
}
