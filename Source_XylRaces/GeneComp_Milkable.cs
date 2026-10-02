namespace XylXenos;

[UsedFromXml]
public class GeneCompProperties_Milkable : GeneCompProperties
{
    public required ThingDef item;
    public float chargePerItem = 0.1f;
    public required HediffDef hediff;
    public List<ThoughtDef>? milkedThoughts;
    public int ticksPerFullnessStage = GenDate.TicksPerDay;


    public GeneCompProperties_Milkable()
    {
        compClass = typeof(GeneComp_Milkable);
    }
}

public class GeneComp_Milkable : GeneComp
{
    public GeneCompProperties_Milkable Props => (GeneCompProperties_Milkable)props;

    public Texture2D ExtraIcon => parent.DefExt.ExtraIcon;

    public int ItemCount => Mathf.FloorToInt((Chargeable?.Charge ?? 0) / Props.chargePerItem);

    public bool ItemsFull => Chargeable != null && Chargeable.Charge >= Chargeable.Props.fullChargeAmount;

    public bool ReadyToMilk =>
        Active &&
        allowMilking &&
        Find.TickManager.TicksGame > lastMilkedTick + milkingCooldownDays * GenDate.TicksPerDay &&
        ItemCount >= 1;

    private const int CheckInterval = 60;

    public int FullnessStage => fullSinceTick is { } value
        ? Mathf.FloorToInt((float)(Find.TickManager.TicksGame - value) / Props.ticksPerFullnessStage)
        : -1;

    private int lastChargeableUpdateTick = -1;
    public bool allowMilking = true;
    public int milkingCooldownDays = 1;

    public int? fullSinceTick;
    public int lastMilkedTick = int.MinValue;

    public HediffComp_Chargeable? Chargeable
    {
        get
        {
            int curTick = Find.TickManager.TicksGame;
            if (lastChargeableUpdateTick != curTick)
            {
                lastChargeableUpdateTick = curTick;
                field = Pawn.health.hediffSet.GetFirstHediffOfDef(Props.hediff).TryGetComp<HediffComp_Chargeable>();
            }

            return field;
        }
    }

    public override void CompExposeData()
    {
        Scribe_Values.Look(ref fullSinceTick, nameof(fullSinceTick));
        Scribe_Values.Look(ref lastMilkedTick, nameof(lastMilkedTick));
        Scribe_Values.Look(ref allowMilking, nameof(allowMilking), defaultValue: true);
        Scribe_Values.Look(ref milkingCooldownDays, nameof(milkingCooldownDays), defaultValue: 1);
    }

    private static TaggedString LabelForFrequency(int days)
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
        if (Pawn is { IsColonistPlayerControlled: false, IsPrisonerOfColony: false })
            yield break;
        if (Pawn.Drafted)
            yield break;

        DebugAssert.NotNull(ExtraIcon);

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
        lastMilkedTick = Find.TickManager.TicksGame;
    }

    public override void CompTickInterval(int delta)
    {
        if (!Pawn.IsHashIntervalTick(CheckInterval, delta))
            return;

        if (ItemsFull)
            fullSinceTick ??= Find.TickManager.TicksGame;
        else
            fullSinceTick = null;
    }

    public override IEnumerable<StatDrawEntry> SpecialDisplayStats()
    {
        if (!Active)
            yield break;
        if (Chargeable is not { } chargeable)
            yield break;

        float itemsPerDay = chargeable.Props.fullChargeAmount * GenDate.TicksPerDay /
                            (chargeable.Props.ticksToFullCharge * Props.chargePerItem);
        yield return new StatDrawEntry(StatCategoryDefOf.PawnFood, "XylMilkProductionLabel".Translate(Props.item.label).CapitalizeFirst(),
            "PerDay".Translate(itemsPerDay.ToStringByStyle(ToStringStyle.FloatOne)),
            "XylMilkProductionDesc".Translate(Props.item.label).CapitalizeFirst(), 1);
    }

    public void Notify_Milked(Pawn doer)
    {
        lastMilkedTick = Find.TickManager.TicksGame;

        if (Props.milkedThoughts is not { Count: > 0 })
            return;

        foreach (var thoughtDef in Props.milkedThoughts)
            Pawn.needs.mood?.thoughts.memories.TryGainMemory(thoughtDef, doer);
    }
}
