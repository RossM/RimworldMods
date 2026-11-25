namespace XylXenos;

[UsedFromXml]
public class Need_Lovin(Pawn pawn) : Need(pawn)
{
    public bool Satisfied => CurLevel >= ThreshSatisfied;

    // During loading, SetInitialLevel can be called before the pawn is fully loaded, resulting in a hediff with pawn == null. If we do anything with
    // such a hediff we'll get a NullReferenceException, so here we verify that the hediff is valid before returning it.
    public Hediff_LovinAddiction? LovinAddictionHediff => pawn.HediffsOfType<Hediff_LovinAddiction>().SingleOrDefault();

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
        // During loading the hediff can be in a partially-initialized state that will make
        // Notify_NeedCategoryChanged throw a NullReferenceException
        if (Scribe.mode != LoadSaveMode.Inactive)
            return;
        LovinAddictionHediff?.Notify_NeedCategoryChanged();
    }
}
