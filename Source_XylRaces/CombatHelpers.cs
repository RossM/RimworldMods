using JetBrains.Annotations;
using RimWorld;
using Verse;

namespace XylRacesCore
{
    public static class CombatHelpers
    {
        public static float GetRangedDodgeChance(Pawn target)
        {
            if (target.DeadOrDowned)
                return 0;
            if (target.GetPosture() != PawnPosture.Standing)
                return 0;

            return target.GetStatValue(DefOf.XylRangedDodgeChance);
        }
    }
}
