namespace XylXenos.Genes;

public class SeeingRedInfo
{
    public float chance = 1.0f;
    public HediffDef hediffDef;
}

public class SeeingRed : GeneExt
{
    [NotNull]
    public SeeingRedInfo SeeingRedDefExt => DefExt.seeingRed!;

    private const int checkInterval = 60;
    public HashSet<Thing> extraEnemies;

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Collections.Look(ref extraEnemies, nameof(extraEnemies), LookMode.Reference);
    }

    public override void TickInterval(int delta)
    {
        base.TickInterval(delta);
        if (!pawn.IsHashIntervalTick(checkInterval, delta))
            return;
        if (extraEnemies != null)
        {
            Hediff hediff = pawn.health.hediffSet.GetFirstHediffOfDef(SeeingRedDefExt.hediffDef);
            if (hediff == null)
                extraEnemies.Clear();
        }
    }

    public bool ForceHostility(Thing thing)
    {
        return extraEnemies != null && extraEnemies.Contains(thing);
    }

    public override IEnumerable<StatDrawEntry> SpecialDisplayStats()
    {
        yield return new StatDrawEntry(StatCategoryDefOf.PawnCombat, "XylRageChanceLabel".TranslateSimple(),
            SeeingRedDefExt.chance.ToStringPercent(), "XylRageChanceDesc".TranslateSimple(), 1);
    }

    public void Notify_DamageTaken(DamageInfo damageInfo)
    {
        Hediff hediff = pawn.health.hediffSet.GetFirstHediffOfDef(SeeingRedDefExt.hediffDef);

        if (hediff == null && !Rand.Chance(SeeingRedDefExt.chance))
            return;
        if (pawn.Downed)
            return;

        hediff ??= pawn.health.AddHediff(SeeingRedDefExt.hediffDef);
        if (hediff == null)
            return;

        (extraEnemies ??= []).Add(damageInfo.Instigator);

        var comp = hediff.TryGetComp<HediffComp_Disappears>();
        if (comp == null)
            return;
        comp.ticksToDisappear = comp.disappearsAfterTicks;
    }

    public override void RegisterWith(NotificationManager manager)
    {
        base.RegisterWith(manager);

        manager.Register<DamageInfo>(NotificationEvent.PreDamageTaken, pawn, Notify_DamageTaken);
    }
}