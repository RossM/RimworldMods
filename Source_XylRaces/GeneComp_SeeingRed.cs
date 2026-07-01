namespace XylXenos;

public class GeneCompProperties_SeeingRed : GeneCompProperties
{
    public float chance = 1.0f;
    public HediffDef hediffDef;

    public GeneCompProperties_SeeingRed()
    {
        compClass = typeof(GeneComp_SeeingRed);
    }
}

public class GeneComp_SeeingRed : GeneComp, IEventListener
{
    [NotNull]
    public GeneCompProperties_SeeingRed Props => (GeneCompProperties_SeeingRed)props;

    private const int checkInterval = 60;
    public HashSet<Thing> extraEnemies;

    public override void CompExposeData()
    {
        Scribe_Collections.Look(ref extraEnemies, nameof(extraEnemies), LookMode.Reference);
    }

    public override void CompTickInterval(int delta)
    {
        if (!Pawn.IsHashIntervalTick(checkInterval, delta))
            return;
        if (extraEnemies != null)
        {
            Hediff hediff = Pawn.health.hediffSet.GetFirstHediffOfDef(Props.hediffDef);
            if (hediff == null)
                extraEnemies.Clear();
        }
    }

    public bool ForceHostility(Thing thing)
    {
        return extraEnemies != null && extraEnemies.Contains(thing);
    }

    public override IEnumerable<StatDrawEntry> SpecialDisplayStats(StatRequest request)
    {
        yield return new StatDrawEntry(StatCategoryDefOf.PawnCombat, "XylRageChanceLabel".TranslateSimple(),
            Props.chance.ToStringPercent(), "XylRageChanceDesc".TranslateSimple(), 1);
    }

    public void Notify_DamageTaken(DamageInfo damageInfo)
    {
        if (Active)
            return;

        Hediff hediff = Pawn.health.hediffSet.GetFirstHediffOfDef(Props.hediffDef);

        if (hediff == null && !Rand.Chance(Props.chance))
            return;
        if (Pawn.Downed)
            return;

        hediff ??= Pawn.health.AddHediff(Props.hediffDef);
        if (hediff == null)
            return;

        (extraEnemies ??= []).Add(damageInfo.Instigator);

        var comp = hediff.TryGetComp<HediffComp_Disappears>();
        if (comp == null)
            return;
        comp.ticksToDisappear = comp.disappearsAfterTicks;
    }

    public void RegisterWith(EventManager manager)
    {
        manager.Register<DamageInfo>(EventDefOf.PreTakeDamage, Pawn, Notify_DamageTaken);
    }

    public void PreUnregister(EventManager manager)
    {
    }
}
