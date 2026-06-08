namespace XylXenos;

[UsedFromXml]
public class JobGiver_AICastAbilityOnTarget : JobGiver_AICastAbility
{
    public bool targetSelf;
    public bool targetEnemies;
    public bool targetAllies;
    public bool onlyTargetMelee;
    public bool onlyTargetRanged;
    public bool onlyInCover;
    public bool avoidHittingNonEnemies = true;
    public float minDistance = 0f;

    private static readonly List<Pawn> potentialTargets = [];

    private static readonly SimpleCurve distanceWeight =
    [
        new(0.0f, 1.0f),
        new(25.0f, 0.1f),
        new(50.0f, 0.01f),
    ];

    // ReSharper disable once ParameterHidesMember
    protected override LocalTargetInfo GetTarget(Pawn caster, Ability ability)
    {
        potentialTargets.Clear();

        if (!ability.CanCast)
            return LocalTargetInfo.Invalid;

        IEnumerable<IAttackTarget> targets;

        if (targetAllies)
            targets = caster.Map.mapPawns.PawnsInFaction(caster.Faction);
        else if (targetEnemies)
            targets = caster.Map.attackTargetsCache.GetPotentialTargetsFor(caster);
        else if (targetSelf)
            targets = [caster];
        else
            return LocalTargetInfo.Invalid;

        foreach (var target in targets)
        {
            bool badTarget = false;

            if (target.Thing is not Pawn targetPawn)
                continue;

            if (targetPawn.Downed)
                continue;

            if (!ability.def.verbProperties.targetParams.CanTarget(targetPawn))
                continue;

            if (minDistance > 0f && targetPawn.Position.DistanceTo(caster.Position) < minDistance)
                continue;

            if (!caster.CanSee(targetPawn))
                continue;

            var giveHediffEffect = ability.CompOfType<CompAbilityEffect_GiveHediff>();
            if (giveHediffEffect != null && targetPawn.health.hediffSet.HasHediff(giveHediffEffect.Props.hediffDef))
                continue;

            var forceJobEffect = ability.CompOfType<CompAbilityEffect_ForceJob>();
            if (forceJobEffect != null && targetPawn.CurJobDef == forceJobEffect.Props.jobDef)
                continue;

            if (targetEnemies && avoidHittingNonEnemies)
            {
                foreach (var affectedTarget in ability.GetAffectedTargets((LocalTargetInfo)targetPawn))
                {
                    // Avoid hitting pawns with a faction we might anger. We allow hitting factionless pawns here,
                    // such as wild animals.
                    if (affectedTarget.Thing is Pawn { Faction: not null } otherPawn && !otherPawn.HostileTo(caster))
                        badTarget = true;
                }

                if (badTarget)
                    continue;
            }

            if (onlyTargetRanged && targetPawn.equipment?.PrimaryEq?.PrimaryVerb?.IsMeleeAttack != false)
                continue;
            if (onlyTargetMelee && targetPawn.equipment?.PrimaryEq?.PrimaryVerb?.IsMeleeAttack == false)
                continue;

            if (onlyInCover)
            {
                if (CoverUtility.CalculateOverallBlockChance(targetPawn, caster.Position, caster.Map) <= 0f)
                    continue;
                if (targetPawn.pather.Moving)
                    continue;
            }

            if (!ability.CanApplyOn((LocalTargetInfo)targetPawn))
                continue;

            potentialTargets.Add(targetPawn);
        }

        if (potentialTargets.Count == 0)
            return LocalTargetInfo.Invalid;

        return potentialTargets.RandomElementByWeight(target => distanceWeight.Evaluate(target.Position.DistanceTo(caster.Position)));
    }

    public override ThinkNode DeepCopy(bool resolve = true)
    {
        JobGiver_AICastAbilityOnTarget copy = (JobGiver_AICastAbilityOnTarget)base.DeepCopy(resolve);
        copy.targetAllies = targetAllies;
        copy.targetEnemies = targetEnemies;
        copy.targetSelf = targetSelf;
        copy.onlyTargetMelee = onlyTargetMelee;
        copy.onlyTargetRanged = onlyTargetRanged;
        copy.onlyInCover = onlyInCover;
        copy.avoidHittingNonEnemies = avoidHittingNonEnemies;
        copy.minDistance = minDistance;
        return copy;
    }
}
