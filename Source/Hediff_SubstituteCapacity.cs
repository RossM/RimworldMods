using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using UnityEngine;
using Verse;
using static UnityEngine.GraphicsBuffer;

namespace XylRacesCore
{
    public class Hediff_SubstituteCapacity : HediffWithComps
    {
        public HediffCompProperties_SubstituteCapacity CompProperties => GetComp<HediffComp_SubstituteCapacity>().Properties;

        [Unsaved(false)]
        private HediffStage stage;

        [Unsaved(false)] 
        private bool processing = false;

        public override HediffStage CurStage
        {
            get
            {
                if (processing)
                    return stage;
                try
                {
                    processing = true;

                    stage = new HediffStage();
                    if (def.stages != null)
                    {
                        foreach (var field in typeof(HediffStage).GetFields())
                        {
                            field.SetValue(stage, field.GetValue(def.stages[CurStageIndex]));
                        }
                    }

                    stage.statOffsets = new List<StatModifier>();
                    stage.statFactors = new List<StatModifier>();

                    if (Active)
                    {
                        foreach (var statDef in DefDatabase<StatDef>.AllDefs)
                        {
                            PawnCapacityOffset pawnCapacityOffset = statDef.capacityOffsets?
                                .Where(o => o.capacity == CompProperties.originalCapacity).FirstOrDefault();
                            if (pawnCapacityOffset != null)
                            {
                                float offset = GetOffset(pawnCapacityOffset);

                                if (offset != 0)
                                {
                                    stage.statOffsets.Add(new StatModifier()
                                        { stat = statDef, value = offset });
                                }
                            }

                            PawnCapacityFactor pawnCapacityFactor = statDef.capacityFactors?
                                .Where(o => o.capacity == CompProperties.originalCapacity).FirstOrDefault();
                            if (pawnCapacityFactor != null)
                            {
                                float factor = GetFactor(pawnCapacityFactor);

                                if (factor != 1.0f)
                                {
                                    stage.statFactors.Add(new StatModifier()
                                        { stat = statDef, value = factor });
                                }
                            }
                        }
                    }

                    return stage;
                }
                finally
                {
                    processing = false;
                }
            }
        }

        public bool Active
        {
            get
            {
                float originalLevel = pawn.health.capacities.GetLevel(CompProperties.originalCapacity);
                float substituteLevel = pawn.health.capacities.GetLevel(CompProperties.substituteCapacity);
                if (CompProperties.mode == HediffCompProperties_SubstituteCapacity.SubstitutionMode.Maximum &&
                    substituteLevel < originalLevel)
                    return false;
                if (CompProperties.mode == HediffCompProperties_SubstituteCapacity.SubstitutionMode.Minimum &&
                    substituteLevel > originalLevel)
                    return false;
                return true;
            }
        }

        private float GetOffset(PawnCapacityOffset pawnCapacityOffset)
        {
            float originalOffset = pawnCapacityOffset.GetOffset(pawn.health.capacities.GetLevel(CompProperties.originalCapacity));
            float substituteOffset = pawnCapacityOffset.GetOffset(pawn.health.capacities.GetLevel(CompProperties.substituteCapacity));

            return substituteOffset - originalOffset;
        }

        private float GetFactor(PawnCapacityFactor pawnCapacityFactor)
        {
            float originalFactor = pawnCapacityFactor.GetFactor(pawn.health.capacities.GetLevel(CompProperties.originalCapacity));
            float substituteFactor = pawnCapacityFactor.GetFactor(pawn.health.capacities.GetLevel(CompProperties.substituteCapacity));

            originalFactor = Mathf.Lerp(1, originalFactor, pawnCapacityFactor.weight);
            substituteFactor = Mathf.Lerp(1, substituteFactor, pawnCapacityFactor.weight);

            if (originalFactor == 0)
                return 1.0f;

            return substituteFactor / originalFactor;
        }
    }
}
