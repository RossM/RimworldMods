namespace XylXenos;

[UsedFromXml]
public class ThoughtWorker_MilkableFullness : ThoughtWorker
{
    protected override ThoughtState CurrentStateInternal(Pawn p)
    {
        if (ThoughtUtility.ThoughtNullified(p, def))
            return ThoughtState.Inactive;

        if (def.GetModExtension<DefModExtension_Thought>()?.gene is not { } geneDef)
            return ThoughtState.Inactive;

        var gene = p.FirstActiveGene(geneDef);

        var comp = (gene as GeneWithComps)?.GetComp<GeneComp_Milkable>();
        if (comp == null)
            return ThoughtState.Inactive;

        var fullnessStage = comp.FullnessStage;
        return fullnessStage >= 0
            ? ThoughtState.ActiveAtStage(Math.Min(fullnessStage, def.stages.Count - 1))
            : ThoughtState.Inactive;
    }
}
