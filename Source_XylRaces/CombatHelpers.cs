namespace XylXenos;

public static class CombatHelpers
{
    public static float GetRangedDodgeChance(Pawn target)
    {
        if (target.DeadOrDowned)
            return 0;
        if (target.GetPosture() != PawnPosture.Standing)
            return 0;

        return target.GetStatValue(XStatDefOf.XylRangedDodgeChance);
    }
}
