using System.Diagnostics.CodeAnalysis;

namespace XylXenos;

[UsedFromXml]
public class CompProperties_AbilityRockToss : CompProperties_EffectWithDest
{
    public float minRange;
    public float forcedMissRadius;
    public required ThingDef projectileDef;
    public bool applyMortarMissRadiusFactor;

    public CompProperties_AbilityRockToss()
    {
        compClass = typeof(CompAbilityEffect_RockToss);
    }

    [SuppressMessage("ReSharper", "ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract")]
    public override IEnumerable<string> ConfigErrors(AbilityDef parentDef)
    {
        if (projectileDef is null)
            yield return $"{nameof(projectileDef)} is null";
    }
}

public class CompAbilityEffect_RockToss : CompAbilityEffect_WithDest, ITargetingSource
{
    public new CompProperties_AbilityRockToss Props => (CompProperties_AbilityRockToss)props;

    public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
    {
        // During targeting, there are two targets. However, when we actually get activated by
        // JobDriver_CastWithHeldThing, the destination (target B) becomes the primary target. Otherwise
        // the targeter would abort because target A is in our inventory and no longer spawned.
        base.Apply(target, dest);
        Log.Message($"target={target}, dest={dest}, CarriedThing={parent.pawn.carryTracker.CarriedThing}");
        if (parent.pawn.carryTracker.CarriedThing != null)
            LaunchProjectile(target);
    }

    private void LaunchProjectile(LocalTargetInfo target)
    {
        Pawn pawn = parent.pawn;
        Projectile projectile = (Projectile)GenSpawn.Spawn(Props.projectileDef, pawn.Position, pawn.Map);
        Log.Message($"projectile={projectile} ({projectile.GetType()}");
        Thing thing = pawn.carryTracker.CarriedThing;
        if (projectile.GetComp<CompThingContainer>()?.innerContainer.TryAddOrTransfer(thing) is not true)
        {
            Log.Warning("Failed to add thing to projectile: projectile={projectile} thing={thing}");
            return;
        }

        if (Props.forcedMissRadius > 0.5f)
        {
            float forcedMissRadius = Props.forcedMissRadius;
            if (Props.applyMortarMissRadiusFactor)
                forcedMissRadius *= pawn.GetStatValue(StatDefOf.MortarMissRadiusFactor);
            forcedMissRadius = VerbUtility.CalculateAdjustedForcedMiss(forcedMissRadius, target.Cell - pawn.Position);
            if (forcedMissRadius > 0.5f)
            {
                int cellsInRadius = GenRadial.NumCellsInRadius(forcedMissRadius);
                int patternIndex = Rand.Range(0, cellsInRadius);
                IntVec3 forcedMissTarget = target.Cell + GenRadial.RadialPattern[patternIndex];
                if (forcedMissTarget != target.Cell)
                {
                    projectile.Launch(pawn, pawn.DrawPos, forcedMissTarget, target, ProjectileHitFlags.NonTargetWorld,
                        parent.verb.preventFriendlyFire);
                    return;
                }
            }
        }

        projectile.Launch(pawn, pawn.DrawPos, target, target, ProjectileHitFlags.IntendedTarget | ProjectileHitFlags.NonTargetWorld,
            parent.verb.preventFriendlyFire);
    }

    public override bool Valid(LocalTargetInfo target, bool showMessages = false)
    {
        if (target.Thing?.def.thingCategories?.Contains(ThingCategoryDefOf.StoneChunks) is not true)
            return false;

        return base.Valid(target, showMessages);
    }

    public override void DrawEffectPreview(LocalTargetInfo target)
    {
        if (Props.range > 0f)
        {
            GenDraw.DrawRadiusRing(target.Cell, Props.range, Color.white,
                c => c.DistanceTo(target.Cell) >= Props.minRange && GenSight.LineOfSight(target.Cell, c, parent.pawn.Map));
        }
    }

    public override bool CanHitTarget(LocalTargetInfo target)
    {
        if (target.Cell.Impassable(parent.pawn.Map))
            return false;
        if (target.Cell.DistanceTo(selectedTarget.Cell) < Props.minRange)
            return false;
        if (!GenSight.LineOfSight(selectedTarget.Cell, target.Cell, parent.pawn.Map))
            return false;

        return base.CanHitTarget(target);
    }

    void ITargetingSource.DrawHighlight(LocalTargetInfo target)
    {
        if (Props.range > 0f)
        {
            GenDraw.DrawRadiusRing(selectedTarget.Cell, Props.range, Color.white,
                c => c.DistanceTo(selectedTarget.Cell) >= Props.minRange
                     && GenSight.LineOfSight(selectedTarget.Cell, c, parent.pawn.Map));
        }

        if (target.IsValid)
        {
            GenDraw.DrawTargetHighlight(target);
            if (Props.projectileDef.projectile.explosionRadius > 0f)
            {
                GenDraw.DrawRadiusRing(target.Cell, Props.projectileDef.projectile.explosionRadius, Color.white);
            }
        }
    }
}
