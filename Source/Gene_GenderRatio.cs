using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;

namespace XylRacesCore
{
    public class Gene_GenderRatio : Gene
    {
        public GeneDefExtension_GenderRatio DefExt => def.GetModExtension<GeneDefExtension_GenderRatio>();

        public Gender GetGender()
        {
            return Rand.Chance(DefExt.femaleChance) ? Gender.Female : Gender.Male;
        }
    }

    public class GeneDefExtension_GenderRatio : DefModExtension
    {
        public float femaleChance = 0.5f;
    }
}
