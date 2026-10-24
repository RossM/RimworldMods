namespace XylXenos;

[UsedFromXml]
public class ScenPart_RandomXenotype : ScenPart_PawnModifier, IEventListener
{
    public bool allowArchite;

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref allowArchite, nameof(allowArchite));
    }

    private bool ValidateXenotype(XenotypeDef xenotypeDef)
    {
        return allowArchite || !xenotypeDef.AllGenes.Any(gene => gene.biostatArc > 0);
    }

    public override void DoEditInterface(Listing_ScenEdit listing)
    {
        Rect scenPartRect = listing.GetScenPartRect(this, RowHeight * 4f);
        Widgets.CheckboxLabeled(scenPartRect.TopPartPixels(RowHeight), "Allow archite xenotypes", ref allowArchite);
        DoPawnModifierEditInterface(scenPartRect.BottomPartPixels(RowHeight * 2f));
    }

    public override void PreConfigure()
    {
        EventManager.Instance.AddListener(this);
    }

    public void Notify_PreTryGenerateNewPawnInternal(PawnGenerationData data)
    {
        if (!data.request.KindDef.RaceProps.Humanlike)
            return;
        
        if (data.request.ForcedCustomXenotype != null)
            return;

        if (data.request.ForcedXenotype is { Archite: true })
            return;

        if (!context.Includes(data.request.Context))
            return;

        if (!Rand.Chance(chance))
            return;
        
        var xenotype = DefDatabase<XenotypeDef>.AllDefs.Where(ValidateXenotype).RandomElement();
        data.request.ForcedXenotype = xenotype;
    }

    public void RegisterWith(EventManager manager)
    {
        manager.Register<PawnGenerationData>(EventDefOf.PreGenerateNewPawn, null, Notify_PreTryGenerateNewPawnInternal);
    }

    public void PreUnregister(EventManager manager) { }
}
