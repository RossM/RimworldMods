namespace XylXenos;

public class GeneCompProperties_Hyperlactation : GeneCompProperties
{
    public ThingDef item;
    public float chargePerItem = 0.1f;
    public HediffDef hediff;
    [CanBeNull] public List<ThoughtDef> milkedThoughts;
    public int ticksPerSorenessStage = GenDate.TicksPerDay;

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

    public bool MilkFull => Lactating != null && Lactating.Charge >= Lactating.Props.fullChargeAmount;

    public int SorenessStage => fullSinceTick.HasValue
        ? Mathf.FloorToInt((float)(Find.TickManager.TicksGame - fullSinceTick.Value) / Props.ticksPerSorenessStage)
        : -1;

    private const int checkInterval = 60;
    public bool allowMilking = true;
    public int milkingCooldownDays = 1;

    public int? fullSinceTick;
    public int lastMilkedTick = int.MinValue;

    private HediffComp_Lactating lactatingInternal;

    public bool ReadyToMilk =>
        Active &&
        allowMilking && 
        Find.TickManager.TicksGame > lastMilkedTick + milkingCooldownDays * GenDate.TicksPerDay &&
        MilkCount >= 1;

    public override void CompExposeData()
    {
        Scribe_Values.Look(ref fullSinceTick, nameof(fullSinceTick));
        Scribe_Values.Look(ref lastMilkedTick, nameof(lastMilkedTick));
        Scribe_Values.Look(ref allowMilking, nameof(allowMilking), defaultValue: true);
        Scribe_Values.Look(ref milkingCooldownDays, nameof(milkingCooldownDays), defaultValue: 1);
    }

    public TaggedString LabelForFrequency(int days)
    {
        return days switch
        {
            0 => "XylAnyTime".Translate(),
            1 => "EveryDay".Translate(),
            _ => "EveryDays".Translate(days),
        };
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

        List<FloatMenuOption> rightClickFloatMenuOptions = [];
        for (int i = 0; i <= 3; i++)
        {
            int value = i;
            rightClickFloatMenuOptions.Add(new(LabelForFrequency(i), () => { milkingCooldownDays = value; }));
        }

        yield return new Command_ToggleWithRightClickOptions
        {
            defaultLabel = $"{"XylCommandMilkLabel".TranslateSimple()} ({LabelForFrequency(milkingCooldownDays)})",
            defaultDesc = "XylCommandMilkDesc".TranslateSimple(),
            isActive = () => allowMilking,
            toggleAction = () => { allowMilking = !allowMilking; },
            icon = ExtraIcon,
            rightClickFloatMenuOptions = rightClickFloatMenuOptions,
        };
    }

    public override void CompPostPostAdd()
    {
        AddHediff();
        lastMilkedTick = Find.TickManager.TicksGame;
    }

    public override void CompTickInterval(int delta)
    {
        if (!Pawn.IsHashIntervalTick(checkInterval, delta))
            return;

        AddHediff();

        if (MilkFull)
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

    public void Notify_Milked(Pawn doer)
    {
        lastMilkedTick = Find.TickManager.TicksGame;

        if (!Props.milkedThoughts.NullOrEmpty())
        {
            foreach (var thoughtDef in Props.milkedThoughts)
                Pawn.needs.mood.thoughts.memories.TryGainMemory(thoughtDef, doer);
        }
    }
}
