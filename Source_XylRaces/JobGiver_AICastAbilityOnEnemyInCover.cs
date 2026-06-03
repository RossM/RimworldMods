namespace XylXenos;

public class JobGiver_AICastAbilityOnEnemyInCover : JobGiver_AICastAbility
{
    private static readonly List<Pawn> potentialTargets = [];

    protected override LocalTargetInfo GetTarget(Pawn caster, Ability ability)
    {
        //  CoverUtility.CalculateOverallBlockChance
        potentialTargets.Clear();

        foreach (var target in caster.Map.attackTargetsCache.GetPotentialTargetsFor(caster))
        {
            if (target.Thing is not Pawn pawn)
                continue;

            if (pawn.equipment?.PrimaryEq?.PrimaryVerb?.IsMeleeAttack ?? true)
                continue;

            if (CoverUtility.CalculateOverallBlockChance(pawn, caster.Position, caster.Map) <= 0f)
                continue;

            if (!ability.CanApplyOn((LocalTargetInfo)pawn))
                continue;

            potentialTargets.Add(pawn);
        }

        if (potentialTargets.Count == 0)
            return LocalTargetInfo.Invalid;

        return potentialTargets.RandomElement();
    }
}
