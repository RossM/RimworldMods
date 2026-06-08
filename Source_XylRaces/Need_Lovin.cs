namespace XylXenos;

[UsedFromXml]
public class Need_Lovin(Pawn pawn) : Need(pawn), INotificationListener
{
    public const float FallPerDay = 0.3f;

    public override void NeedInterval()
    {
        CurLevel -= 150 * FallPerDay / GenDate.TicksPerDay;
    }

    public override void SetInitialLevel()
    {
        CurLevel = 1.0f;
    }

    public void Notify_PostLovin()
    {
        CurLevel = 1.0f;
    }

    public void RegisterWith(NotificationManager manager)
    {
        manager.Register(NotificationDefOf.PostLovin, pawn, Notify_PostLovin);
    }

    public void PreUnregister(NotificationManager manager)
    {
    }
}
