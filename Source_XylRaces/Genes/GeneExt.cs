using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JetBrains.Annotations;
using UnityEngine;
using Verse;

namespace XylXenos.Genes
{
    public class GeneExt : Gene
    {
        [NotNull] public GeneDefExt DefExt => this.DefExt()!;

        public override bool Active
        {
            get
            {
                if (!base.Active)
                    return false;
                if (DefExt.gender != null && DefExt.gender != pawn.gender)
                    return false;
                if (DefExt.geneType == GeneType.Endogene && !pawn.genes.HasEndogene(def))
                    return false;
                if (DefExt.geneType == GeneType.Xenogene && !pawn.genes.HasXenogene(def))
                    return false;
                return true;
            }
        }

        public virtual IEnumerable<ThingDefCount> GetStartingItems()
        {
            if (DefExt.startingItems.NullOrEmpty())
                yield break;

            foreach (var startingItem in DefExt.startingItems)
            {
                if (!Rand.Chance(startingItem.chance))
                    continue;
                yield return new(startingItem.item, Mathf.Clamp(startingItem.count.RandomInRange, 1, startingItem.item.stackLimit));
            }
        }
    }
}
