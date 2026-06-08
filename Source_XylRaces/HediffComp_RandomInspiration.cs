namespace XylXenos;

[UsedFromXml]
public class HediffCompProperties_RandomInspiration : HediffCompProperties
{
    public float mtbDays;
    [MustTranslate] public string letter;

    public HediffCompProperties_RandomInspiration()
    {
        compClass = typeof(HediffComp_RandomInspiration);
    }
}

public class HediffComp_RandomInspiration : HediffComp
{
    public HediffCompProperties_RandomInspiration Props => (HediffCompProperties_RandomInspiration)props;

    public const int checkFrequency = 150;

    public override void CompPostTickInterval(ref float severityAdjustment, int delta)
    {
        if (!Pawn.IsHashIntervalTick(checkFrequency, delta))
            return;

        if (Pawn.Inspired)
            return;

        if (Rand.MTBEventOccurs(Props.mtbDays, GenDate.TicksPerDay, checkFrequency))
        {
            GiveInspiration();
        }
    }

    private void GiveInspiration()
    {
        var inspiration = Pawn.mindState.inspirationHandler.GetRandomAvailableInspirationDef();
        if (inspiration == null)
            return;

        Pawn partner = Pawn.HediffsOfType<Hediff_LovinAddiction>().FirstOrDefault()?.sourcePawn;

        Pawn.mindState.inspirationHandler.TryStartInspiration(inspiration, Props.letter.Formatted(Pawn.Named("PAWN"), partner.Named("PARTNER")));
    }
}
