namespace XylXenos;

public class GrowthMode
{
    public bool causesNoPain = false;
    public bool allowTend = true;
    public float changeMtbDays = -1;
    public float severityPerDay = 0f;
    public FloatRange severityPerDayRange = FloatRange.Zero;
    [MustTranslate] public string label;
    [MustTranslate] public string message;
    public MessageTypeDef messageType;
    [MustTranslate] public string tipString;
}

[UsedFromXml]
public class HediffCompProperties_GrowthModeExt : HediffCompProperties_SeverityPerDay
{
    public List<GrowthMode> modes;

    public HediffCompProperties_GrowthModeExt()
    {
        compClass = typeof(HediffComp_GrowthModeExt);
    }

    public override void ResolveReferences(HediffDef parent)
    {
        base.ResolveReferences(parent);

        if (parent.hediffClass == typeof(HediffWithComps))
            parent.hediffClass = typeof(HediffWithCompsExt);
    }

    public override IEnumerable<string> ConfigErrors(HediffDef parentDef)
    {
        foreach (var error in base.ConfigErrors(parentDef))
            yield return error;

        if (!typeof(HediffWithCompsExt).IsAssignableFrom(parentDef.hediffClass))
            yield return "hediffClass must be HediffWithCompsExt or a subclass thereof";
    }
}

public class HediffComp_GrowthModeExt : HediffComp_SeverityPerDay, IHediffCompExt
{
    public HediffCompProperties_GrowthModeExt TProps => (HediffCompProperties_GrowthModeExt)props;

    public override string CompLabelInBracketsExtra => GrowthMode.label;

    public override string CompTipStringExtra => GrowthMode.tipString;

    public bool AllowTend => GrowthMode.allowTend;

    public bool CausesNoPain => GrowthMode.causesNoPain;

    public GrowthMode GrowthMode
    {
        get => TProps.modes[growthModeIndex];
        set => growthModeIndex = TProps.modes.IndexOf(value);
    }

    public int growthModeIndex;

    public override void CompExposeData()
    {
        Scribe_Values.Look(ref growthModeIndex, nameof(growthModeIndex));
    }

    public override void CompPostPostAdd(DamageInfo? dinfo)
    {
        base.CompPostPostAdd(dinfo);

        SetGrowthMode(TProps.modes[0]);
    }

    public override float SeverityChangePerDay()
    {
        return severityPerDay;
    }

    public override void CompPostTickInterval(ref float severityAdjustment, int delta)
    {
        base.CompPostTickInterval(ref severityAdjustment, delta);

        float mtbDays = GrowthMode.changeMtbDays;

        if (mtbDays > 0 && Rand.MTBEventOccurs(mtbDays, GenDate.TicksPerDay, delta))
        {
            ChangeGrowthMode();
        }
    }

    public virtual void ChangeGrowthMode()
    {
        SetGrowthMode(TProps.modes.Where(mode => mode != GrowthMode).RandomElement());

        if (!GrowthMode.message.NullOrEmpty() && PawnUtility.ShouldSendNotificationAbout(Pawn))
        {
            Messages.Message(
                GrowthMode.message.Formatted(Pawn.Named("PAWN")),
                Pawn,
                GrowthMode.messageType ?? MessageTypeDefOf.NegativeHealthEvent);
        }
    }

    private void SetGrowthMode(GrowthMode mode)
    {
        GrowthMode = mode;
        severityPerDay = GrowthMode.severityPerDay + GrowthMode.severityPerDayRange.RandomInRange;
        if (parent is HediffWithCompsExt ext)
            ext.Notify_CompStateChange();
    }

    public override IEnumerable<Gizmo> CompGetGizmos()
    {
        if (!DebugSettings.ShowDevGizmos)
            yield break;

        yield return new Command_Action
        {
            defaultLabel = $"DEV: Change {parent.LabelBase} growth mode",
            action = ChangeGrowthMode,
        };
    }
}
