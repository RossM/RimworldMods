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
        public ModExtension_GeneDef_GenderRatio DefExt => def.GetModExtension<ModExtension_GeneDef_GenderRatio>();

        public Gender GetGender()
        {
            return Rand.Chance(DefExt.femaleChance) ? Gender.Female : Gender.Male;
        }
    }

    public class ModExtension_GeneDef_GenderRatio : DefModExtension
    {
        public float femaleChance = 0.5f;
    }
}
