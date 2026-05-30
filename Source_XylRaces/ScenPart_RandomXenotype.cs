namespace XylXenos;

[UsedFromXml]
public class ScenPart_RandomXenotype : ScenPart_PawnModifier
{
    public bool allowArchite;

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref allowArchite, nameof(allowArchite));
    }

    protected override void ModifyNewPawn(Pawn pawn)
    {
        var xenotype = DefDatabase<XenotypeDef>.AllDefs.Where(ValidateXenotype).RandomElement();

        // TODO
        // Note that there are a couple of things added by genes in character creation that we don't
        // reset here because doing so requires modifying more than just genes:
        //   * gender
        //   * congenital hediffs
        //
        // The cleanest solution would be to hook things earlier in pawn creation rather than trying
        // to modify the pawn after the fact.
        //
        // Also, probably we shouldn't affect babies.

        List<Gene> list2 = pawn.genes.Endogenes;
        for (int num = list2.Count - 1; num >= 0; num--)
        {
            Gene gene = list2[num];
            if (gene.def.endogeneCategory != EndogeneCategory.Melanin && gene.def.endogeneCategory != EndogeneCategory.HairColor)
                pawn.genes.RemoveGene(gene);
        }
        pawn.genes.SetXenotype(xenotype);
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
}