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

        public bool Active
        {
            get
            {
                float originalLevel = pawn.health.capacities.GetLevel(CompProperties.originalCapacity);
                float substituteLevel = pawn.health.capacities.GetLevel(CompProperties.substituteCapacity);
                return CompProperties.mode switch
                {
                    HediffCompProperties_SubstituteCapacity.SubstitutionMode.Maximum => (substituteLevel > originalLevel),
                    HediffCompProperties_SubstituteCapacity.SubstitutionMode.Minimum => (substituteLevel < originalLevel),
                    _ => true
                };
            }
        }

        public override string Description
        {
            get
            {
                var sb = new StringBuilder(base.Description);

                sb.AppendLine();
                sb.AppendLine();

                ExtraDescription(sb);

                return sb.ToString();
            }
        }

        private void ExtraDescription(StringBuilder sb)
        {
            sb.Append(CompProperties.substituteCapacity.LabelCap + " is used instead of " + CompProperties.originalCapacity.LabelCap
                      + " for stat calculations");
            if (CompProperties.mode == HediffCompProperties_SubstituteCapacity.SubstitutionMode.Maximum)
                sb.Append(" if it is higher");
            if (CompProperties.mode == HediffCompProperties_SubstituteCapacity.SubstitutionMode.Minimum)
                sb.Append(" if it is lower");
            sb.Append(".");
        }

        public override IEnumerable<StatDrawEntry> SpecialDisplayStats(StatRequest req)
        {
            foreach (var statDrawEntry in base.SpecialDisplayStats(req))
                yield return statDrawEntry;

            float difference = pawn.health.capacities.GetLevel(CompProperties.substituteCapacity) -
                               pawn.health.capacities.GetLevel(CompProperties.originalCapacity);
            if (!Active)
                difference = 0;

            var sb = new StringBuilder();
            ExtraDescription(sb);

            yield return new StatDrawEntry(StatCategoryDefOf.CapacityEffects, "Effective " + CompProperties.originalCapacity.label,
                difference.ToStringPercentSigned(), sb.ToString(), 1);
        }

        public bool Validate(StatDef statDef, PawnCapacityDef pawnCapacityDef)
        {
            if (CompProperties.originalCapacity != pawnCapacityDef)
                return false;
            if (!Active)
                return false;
            if (CompProperties.excludeStats != null && CompProperties.excludeStats.Contains(statDef))
                return false;
            return true;
        }

        public TaggedString DescriptionFor(Pawn pawn)
        {
            float modifier = (pawn.health.capacities.GetLevel(CompProperties.substituteCapacity) -
                              pawn.health.capacities.GetLevel(CompProperties.originalCapacity));
            return LabelCap + ": " + CompProperties.originalCapacity.LabelCap + " -> " + 
                   CompProperties.substituteCapacity.LabelCap + " (" + modifier.ToStringPercentSigned() + ")";
        }
    }
}
