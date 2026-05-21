using System.Collections.Generic;
using JetBrains.Annotations;
using RimWorld;
using Verse;

namespace XylXenos.Genes
{
    [UsedImplicitly]
    public class ChemicalImmunity : Gene
    {
        public void RemoveInvalidHediffs()
        {
            HashSet<HediffDef> hediffDefsToRemove = [];

            foreach (var chemicalDef in DefDatabase<ChemicalDef>.AllDefs)
            {
                if (!pawn.ChemicalIsAllowedByGenes(chemicalDef))
                {
                    if (chemicalDef.toleranceHediff != null)
                        hediffDefsToRemove.Add(chemicalDef.toleranceHediff);
                    if (chemicalDef.addictionHediff != null)
                        hediffDefsToRemove.Add(chemicalDef.addictionHediff);
                }
            }

            var hediffs = new List<Hediff>(pawn.health.hediffSet.hediffs);
            foreach (var hediff in hediffs)
            {
                if (hediffDefsToRemove.Contains(hediff.def))
                    pawn.health.RemoveHediff(hediff);
            }
        }

        public override void PostAdd()
        {
            RemoveInvalidHediffs();
        }

        public override void PostRemove()
        {
            RemoveInvalidHediffs();
        }
    }
}
