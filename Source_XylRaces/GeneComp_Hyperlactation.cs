namespace XylXenos;

public class GeneCompProperties_Hyperlactation : GeneCompProperties
{
    public ThingDef item;
    public float chargePerItem = 0.1f;
    public HediffDef hediff;
    [CanBeNull] public List<ThoughtDef> milkedThoughts;
    public int ticksPerSorenessStage = 60000;

    public GeneCompProperties_Hyperlactation()
    {
        compClass = typeof(GeneComp_Hyperlactation);
    }
}

public class GeneComp_Hyperlactation : GeneComp
{
    [NotNull]
    public GeneCompProperties_Hyperlactation Props => (GeneCompProperties_Hyperlactation)props;

    public Texture2D ExtraIcon => parent.DefExt.ExtraIcon;

    public HediffComp_Lactating Lactating =>
        lactatingInternal ??= Pawn.health.hediffSet.GetHediffComps<HediffComp_Lactating>().FirstOrDefault();

    public int MilkCount => Mathf.FloorToInt((Lactating?.Charge ?? 0) / Props.chargePerItem);

    private const int checkInterval = 60;
    public bool allowMilking = true;
    public bool onlyMilkWhenFull = true;

    public int? fullSinceTick;

    private HediffComp_Lactating lactatingInternal;

    public override void CompExposeData()
    {
        Scribe_Values.Look(ref fullSinceTick, nameof(fullSinceTick));
        Scribe_Values.Look(ref allowMilking, nameof(allowMilking));
        Scribe_Values.Look(ref onlyMilkWhenFull, nameof(onlyMilkWhenFull), true);
    }

    public override IEnumerable<Gizmo> CompGetGizmos()
    {
        if (!Active)
            yield break;
        if (!Pawn.Spawned)
            yield break;
        if (!Pawn.IsColonistPlayerControlled && !Pawn.IsPrisonerOfColony)
            yield break;
        if (Pawn.Drafted)
            yield break;

        yield return new Command_Toggle
        {
            defaultLabel = "XylCommandMilkLabel".TranslateSimple(),
            defaultDesc = "XylCommandMilkDesc".TranslateSimple(),
            isActive = () => allowMilking,
            toggleAction = () => { allowMilking = !allowMilking; },
            icon = ExtraIcon,
        };

        if (allowMilking)
        {
            yield return new Command_Toggle
            {
                defaultLabel = "XylCommandMilkOnlyWhenFullLabel".TranslateSimple(),
                defaultDesc = "XylCommandMilkOnlyWhenFullDesc".TranslateSimple(),
                isActive = () => onlyMilkWhenFull,
                toggleAction = () => { onlyMilkWhenFull = !onlyMilkWhenFull; },
                icon = ExtraIcon,
            };
        }
    }

    public override void CompPostPostAdd()
    {
        AddHediff();
    }

    public override void CompTickInterval(int delta)
    {
        if (!Pawn.IsHashIntervalTick(checkInterval, delta))
            return;

        AddHediff();

        if (Lactating != null && Lactating.Charge >= Lactating.Props.fullChargeAmount)
            fullSinceTick ??= Find.TickManager.TicksGame;
        else
            fullSinceTick = null;
    }

    private void AddHediff()
    {
        if (!Active)
            return;

        if (Pawn.health.hediffSet.HasHediff(HediffDefOf.Malnutrition))
            return;

        if (Pawn.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.Lactating) is { } lactatingHediff)
            Pawn.health.RemoveHediff(lactatingHediff);

        Hediff hediff = Pawn.health.GetOrAddHediff(Props.hediff);
        hediff.Severity = 1.0f;

        if (Lactating?.parent != hediff)
            lactatingInternal = null;
    }

    public bool ReadyToMilk()
    {
        if (!Active)
            return false;
        if (!allowMilking)
            return false;

        var requiredCount = 1;
        if (onlyMilkWhenFull)
            requiredCount = Mathf.FloorToInt(Lactating.Props.fullChargeAmount / Props.chargePerItem);

        return MilkCount >= requiredCount;
    }

    public bool TryGetSoreness(out int soreness)
    {
        soreness = -1;
        if (fullSinceTick == null)
            return false;
        soreness = Mathf.FloorToInt(
            (float)(Find.TickManager.TicksGame - fullSinceTick.Value) / Props.ticksPerSorenessStage);
        return true;
    }

    public override IEnumerable<StatDrawEntry> SpecialDisplayStats()
    {
        if (!Active)
            yield break;
        float milkPerDay = Lactating.Props.fullChargeAmount * GenDate.TicksPerDay /
                           (Lactating.Props.ticksToFullCharge * Props.chargePerItem);
        yield return new StatDrawEntry(StatCategoryDefOf.PawnFood, "XylMilkProductionLabel".TranslateSimple(),
            "PerDay".Translate(milkPerDay.ToStringByStyle(ToStringStyle.FloatOne)),
            "XylMilkProductionDesc".TranslateSimple(), 1);
    }
}
