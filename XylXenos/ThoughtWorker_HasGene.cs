namespace XylXenos;

[UsedFromXml]
public class ThoughtWorker_HasGene : ThoughtWorker
{
    protected override ThoughtState CurrentSocialStateInternal(Pawn p, Pawn other)
    {
        var extension = def.GetModExtension<DefModExtension_Thought>();
        if (extension?.gene is not { } geneDef)
            return false;

        if (!p.RaceProps.Humanlike)
            return false;
        if (!RelationsUtility.PawnsKnowEachOther(p, other))
            return false;
        if (extension.requiresSight && PawnUtility.IsBiologicallyOrArtificiallyBlind(p))
            return false; 
        if (!other.HasActiveGene(geneDef))
            return false;
        return true;
    }
}
