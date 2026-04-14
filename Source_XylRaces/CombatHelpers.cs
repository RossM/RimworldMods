using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JetBrains.Annotations;
using RimWorld;
using Verse;

namespace XylRacesCore
{
    public static class CombatHelpers
    {

        [DefOf]
        public static class Defs
        {
            [UsedImplicitly] public static StatDef XylRangedDodgeChance;
        }

        public static float GetRangedDodgeChance(Pawn target)
        {
            if (target.DeadOrDowned)
                return 0;

            return target.GetStatValue(Defs.XylRangedDodgeChance);
        }
    }
}
