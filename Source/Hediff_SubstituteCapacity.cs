using Mono.Cecil;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using static UnityEngine.GraphicsBuffer;

namespace XylRacesCore
{
    public class HediffDefExtension_SubstituteCapacity : DefModExtension
    {
        public enum SubstitutionMode
        {
            Always,
            Maximum,
            Minimum,
        }

        public SubstitutionMode mode;
        public PawnCapacityDef originalCapacity;
        public PawnCapacityDef substituteCapacity;
        public List<StatDef> excludeStats;
    }

    public class Hediff_SubstituteCapacity : HediffWithComps
    {
        public HediffDefExtension_SubstituteCapacity DefExt => def.GetModExtension<HediffDefExtension_SubstituteCapacity>();

        public bool Active
        {
            get
            {
                float originalLevel = pawn.health.capacities.GetLevel(DefExt.originalCapacity);
                float substituteLevel = pawn.health.capacities.GetLevel(DefExt.substituteCapacity);
                return DefExt.mode switch
                {
                    HediffDefExtension_SubstituteCapacity.SubstitutionMode.Maximum => (substituteLevel > originalLevel),
                    HediffDefExtension_SubstituteCapacity.SubstitutionMode.Minimum => (substituteLevel < originalLevel),
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
            string desc = DefExt.mode switch
            {
                HediffDefExtension_SubstituteCapacity.SubstitutionMode.Always => "XylSubstituteCapacityAlwaysDesc",
                HediffDefExtension_SubstituteCapacity.SubstitutionMode.Maximum => "XylSubstituteCapacityHigherDesc",
                HediffDefExtension_SubstituteCapacity.SubstitutionMode.Minimum => "XylSubstituteCapacityLowerDesc",
                _ => throw new NotSupportedException()
            };
            sb.Append(desc.Translate(DefExt.substituteCapacity.label, DefExt.originalCapacity.label)
                .CapitalizeFirst());
        }

        public override IEnumerable<StatDrawEntry> SpecialDisplayStats(StatRequest req)
        {
            foreach (var statDrawEntry in base.SpecialDisplayStats(req))
                yield return statDrawEntry;

            float difference = pawn.health.capacities.GetLevel(DefExt.substituteCapacity) -
                               pawn.health.capacities.GetLevel(DefExt.originalCapacity);
            if (!Active)
                difference = 0;

            var sb = new StringBuilder();
            ExtraDescription(sb);

            yield return new StatDrawEntry(StatCategoryDefOf.CapacityEffects,
                "XylEffectiveCapacity".Translate(DefExt.originalCapacity.label),
                difference.ToStringPercentSigned(), sb.ToString(), 1);
        }

        public bool Validate(StatDef statDef, PawnCapacityDef pawnCapacityDef)
        {
            if (DefExt.originalCapacity != pawnCapacityDef)
                return false;
            if (!Active)
                return false;
            if (DefExt.excludeStats != null && DefExt.excludeStats.Contains(statDef))
                return false;
            return true;
        }

        public TaggedString GetDescription()
        {
            float modifier = (pawn.health.capacities.GetLevel(DefExt.substituteCapacity) -
                              pawn.health.capacities.GetLevel(DefExt.originalCapacity));
            return LabelCap + ": " + DefExt.originalCapacity.LabelCap + " -> " +
                   DefExt.substituteCapacity.LabelCap + " (" + modifier.ToStringPercentSigned() + ")";
        }

        public static Hediff_SubstituteCapacity FindHediffFor(Pawn pawn, PawnCapacityDef capacity, StatDef stat)
        {
            return pawn.health.hediffSet.hediffs.OfType<Hediff_SubstituteCapacity>().FirstOrDefault(hediff => hediff.Validate(stat, capacity));
        }
    }
}
