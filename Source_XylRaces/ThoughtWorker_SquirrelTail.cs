namespace XylXenos;

[UsedFromXml]
public class ThoughtWorker_SquirrelTail : ThoughtWorker
{
    protected override ThoughtState CurrentSocialStateInternal(Pawn p, Pawn other)
    {
        if (!p.RaceProps.Humanlike)
            return false;
        if (!RelationsUtility.PawnsKnowEachOther(p, other))
            return false;
        if (p.story.traits.HasTrait(TraitDefOf.Psychopath))
            return false;
        if (PawnUtility.IsBiologicallyOrArtificiallyBlind(p))
            return false; 
        if (!other.HasActiveGene(DefOf.XylTail_Squirrel))
            return false;
        return true;
    }
}
