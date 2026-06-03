namespace XylXenos;

[UsedFromXml]
public class JobGiver_AICastAbilityOnAlly : JobGiver_AICastAbility
{
    private static readonly List<Pawn> potentialTargets = [];

    public bool selfOnly;

    protected override LocalTargetInfo GetTarget(Pawn caster, Ability ability)
    {
        if (caster.Faction == null)
            return LocalTargetInfo.Invalid;

        potentialTargets.Clear();
        if (selfOnly)
            potentialTargets.Add(caster);
        else
            potentialTargets.AddRange(caster.Map.mapPawns.PawnsInFaction(caster.Faction));
        potentialTargets.Shuffle();

        foreach (Pawn pawn in potentialTargets)
        {
            LocalTargetInfo target = pawn;

            var giveHediffEffect = ability.CompOfType<CompAbilityEffect_GiveHediff>();
            if (giveHediffEffect != null && pawn.health.hediffSet.HasHediff(giveHediffEffect.Props.hediffDef))
                continue;

            if (ability.CanApplyOn(target))
                return target;
        }

        return LocalTargetInfo.Invalid;
    }


    public override ThinkNode DeepCopy(bool resolve = true)
    {
        JobGiver_AICastAbilityOnAlly copy = (JobGiver_AICastAbilityOnAlly)base.DeepCopy(resolve);
        copy.selfOnly = selfOnly;
        return copy;
    }
}
