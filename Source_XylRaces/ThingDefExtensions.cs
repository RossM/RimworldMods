using RimWorld;
using Verse;

namespace XylXenos;

public static class ThingDefExtensions
{
    extension(ThingDef foodDef)
    {
        public bool IsRawFoodOrCorpse => foodDef.IsRawHumanFood() || foodDef.IsCorpse;

        public float GetStatBase(StatDef statDef)
        {
            return foodDef.statBases.FirstOrDefault(s => s.stat == statDef)?.value ?? statDef.defaultBaseValue;
        }
    }
}