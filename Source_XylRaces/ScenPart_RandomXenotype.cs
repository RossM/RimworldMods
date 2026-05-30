using RimWorld;
using static XylXenos.Patches.Patch_PawnGenerator;

namespace XylXenos;

[UsedFromXml]
public class ScenPart_RandomXenotype : ScenPart_PawnModifier, INotificationListener
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

    public void Notify_PawnGenerationEarly(Thing thing, PawnGenerationEarlyData data)
    {
        var pawn = thing as Pawn;
        if (pawn == null)
            return;

        if (data.request.ForcedCustomXenotype != null)
            return;

        if (data.xenotype != null && data.xenotype.AllGenes.Any(gene => gene.biostatArc > 0))
            return;

        if (context.Includes(data.request.Context) && Rand.Chance(chance) && pawn.RaceProps.Humanlike)
        {
            var xenotype = DefDatabase<XenotypeDef>.AllDefs.Where(ValidateXenotype).RandomElement();
            data.xenotype = xenotype;
        }
    }

    public override void PreConfigure()
    {
        RegisterWith(NotificationManager.Instance);        
    }

    public void RegisterWith(NotificationManager manager)
    {
        manager.Register<PawnGenerationEarlyData>(NotificationEvent.PawnGenerationEarly, null, Notify_PawnGenerationEarly);
    }
}