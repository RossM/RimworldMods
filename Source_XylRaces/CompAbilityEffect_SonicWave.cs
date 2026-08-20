namespace XylXenos;

[UsedFromXml]
public class CompProperties_AbilitySonicWave : CompProperties_AbilityEffectWithDuration
{
    public float range;
    public float radius;
    public bool canHitFilledCells;

    public PawnCapacityDef? durationMultiplierCapacity;

    public CompProperties_AbilitySonicWave()
    {
        compClass = typeof(CompAbilityEffect_SonicWave);
    }
}

public class CompAbilityEffect_SonicWave : CompAbilityEffect_WithDuration
{
    public new CompProperties_AbilitySonicWave Props => (CompProperties_AbilitySonicWave)props;

    [Unsaved] private readonly List<IntVec3> tmpCells = [];

    public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
    {
        DebugAssert.NotNull(parent.pawn.Map);

        Map map = parent.pawn.Map;
        foreach (IntVec3 item in AffectedCells(target))
        {
            var thingList = item.GetThingList(map);
            foreach (var targetPawn in thingList.OfType<Pawn>())
            {
                if (!targetPawn.RaceProps.IsFlesh)
                    continue;
                targetPawn.stances.stunner.StunFor(GetDurationSeconds(targetPawn).SecondsToTicks(), parent.pawn, addBattleLog: false);
            }
        }
    }

    private new float GetDurationSeconds(Pawn targetPawn)
    {
        var value = base.GetDurationSeconds(targetPawn);

        if (Props.durationMultiplierCapacity != null)
            value *= targetPawn.health.capacities.GetLevel(Props.durationMultiplierCapacity);

        return value;
    }


    public override void DrawEffectPreview(LocalTargetInfo target)
    {
        GenDraw.DrawFieldEdges(AffectedCells(target));
    }

    public override bool AICanTargetNow(LocalTargetInfo target)
    {
        DebugAssert.NotNull(parent.pawn.Map);

        bool affectsAlly = false;
        bool affectsEnemy = false;

        if (parent.pawn.Faction != null)
            foreach (IntVec3 item in AffectedCells(target))
            {
                List<Thing> thingList = item.GetThingList(parent.pawn.Map);
                foreach (var targetPawn in thingList.OfType<Pawn>())
                {
                    if (targetPawn.RaceProps.IsFlesh && !targetPawn.stances.stunner.Stunned)
                    {
                        if (targetPawn != parent.pawn && targetPawn.Faction == parent.pawn.Faction)
                            affectsAlly = true;
                        else if (targetPawn.HostileTo(parent.pawn))
                            affectsEnemy = true;
                    }
                }
            }

        return affectsEnemy && !affectsAlly;
    }

    private List<IntVec3> AffectedCells(LocalTargetInfo target)
    {
        DebugAssert.NotNull(parent.pawn.Map);

        tmpCells.Clear();
        IntVec3 targetPosition = target.Cell.ClampInsideMap(parent.pawn.Map);
        if (parent.pawn.Position == targetPosition)
            return tmpCells;

        int cellsInRadius = GenRadial.NumCellsInRadius(Props.radius);
        for (int i = 0; i < cellsInRadius; i++)
        {
            IntVec3 intVec2 = targetPosition + GenRadial.RadialPattern[i];
            if (CanUseCell(intVec2))
                tmpCells.Add(intVec2);
        }

        return tmpCells;

        bool CanUseCell(IntVec3 c)
        {
            if (!c.InBounds(parent.pawn.Map))
                return false;
            if (c == parent.pawn.Position)
                return false;
            if (!Props.canHitFilledCells && c.Filled(parent.pawn.Map))
                return false;
            if (!c.InHorDistOf(targetPosition, Props.radius))
                return false;

            return true;
        }
    }
}
