namespace XylXenos;

[UsedFromXml]
public class CompProperties_AbilityToxicBurst : CompProperties_AbilityReleaseGas
{
    public float AIUseRadius;

    public CompProperties_AbilityToxicBurst()
    {
        compClass = typeof(CompAbilityEffect_ToxicBurst);
    }
}

public class CompAbilityEffect_ToxicBurst : CompAbilityEffect_ReleaseGas
{
    private new CompProperties_AbilityToxicBurst Props => (CompProperties_AbilityToxicBurst)props;

    public override bool AICanTargetNow(LocalTargetInfo target)
    {
        DebugAssert.NotNull(parent.pawn.Map);

        int cellsInRadius = GenRadial.NumCellsInRadius(Props.AIUseRadius);

        for (int i = 0; i < cellsInRadius; i++)
        {
            IntVec3 c = parent.pawn.Position + GenRadial.RadialPattern[i];
            if (!c.InBounds(parent.pawn.Map))
                continue;

            foreach (Thing thing in c.GetThingList(parent.pawn.Map))
            {
                if (thing is Pawn pawn && pawn != parent.pawn && pawn.HostileTo(parent.pawn) && GasUtility.IsAffectedByExposure(pawn)
                    && !pawn.IsPsychologicallyInvisible())
                    return true;
            }
        }

        return false;
    }
}
