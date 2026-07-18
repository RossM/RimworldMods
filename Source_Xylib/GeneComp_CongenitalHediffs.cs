namespace Xylib;

[UsedFromXml]
[PublicAPI]
public class GeneCompProperties_CongenitalHediffs : GeneCompProperties
{
    public required List<HediffGiver_Event> hediffs;

    public GeneCompProperties_CongenitalHediffs()
    {
        compClass = typeof(GeneComp_CongenitalHediffs);
    }
}

[PublicAPI]
public class GeneComp_CongenitalHediffs : GeneComp, IEventListener
{
    public GeneCompProperties_CongenitalHediffs Props => (GeneCompProperties_CongenitalHediffs)props;

    public void Notify_PostGeneratedInitialHediffs()
    {
        foreach (var hediff in Props.hediffs)
            hediff.EventOccurred(Pawn);
    }

    void IEventListener.RegisterWith(EventManager manager)
    {
        manager.Register(EventDefOf.PostGenerateInitialHediffs, Pawn, Notify_PostGeneratedInitialHediffs);
    }

    void IEventListener.PreUnregister(EventManager manager) { }
}
