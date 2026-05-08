using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JetBrains.Annotations;
using RimWorld;
using Verse;

namespace XylRacesCore.Genes
{
    [UsedImplicitly]
    public class ChemicalImmunity : Gene
    {
        public void RemoveInvalidHediffs()
        {
            HashSet<HediffDef> hediffDefsToRemove = new();

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
