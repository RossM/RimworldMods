namespace XylXenos;

[UsedFromXml]
public class ThoughtWorker_MilkableFullness : ThoughtWorker
{
    protected override ThoughtState CurrentStateInternal(Pawn p)
    {
        if (ThoughtUtility.ThoughtNullified(p, def) || def.GetModExtension<DefModExtension_Thought>()?.gene is not { } geneDef)
            return ThoughtState.Inactive;

        var gene = p.FirstActiveGene(geneDef);

        if ((gene as GeneWithComps)?.GetComp<GeneComp_Milkable>() is not { } comp)
            return ThoughtState.Inactive;

        var fullnessStage = comp.FullnessStage;
        return fullnessStage >= 0
            ? ThoughtState.ActiveAtStage(Math.Min(fullnessStage, def.stages.Count - 1))
            : ThoughtState.Inactive;
    }
}
