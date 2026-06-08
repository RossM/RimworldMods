namespace XylXenos;

[UsedFromXml]
public class Need_Lovin(Pawn pawn) : Need(pawn)
{
    public bool Satisfied => CurLevel >= ThreshSatisfied;

    public Hediff_LovinAddiction LovinAddictionHediff => pawn.HediffsOfType<Hediff_LovinAddiction>().SingleOrDefault();

    public const float ThreshSatisfied = 0.01f;

    public override float CurLevel
    {
        get => base.CurLevel;
        set
        {
            bool oldSatisfied = Satisfied;
            base.CurLevel = value;
            if (Satisfied != oldSatisfied)
                CategoryChanged();
        }
    }

    public override void NeedInterval()
    {
        CurLevel -= def.fallPerDay * (150f / GenDate.TicksPerDay);
    }

    public override void SetInitialLevel()
    {
        CurLevel = 1.0f;
    }

    public void CategoryChanged()
    {
        LovinAddictionHediff?.Notify_NeedCategoryChanged();
    }
}
