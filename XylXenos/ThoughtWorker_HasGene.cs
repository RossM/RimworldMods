namespace XylXenos;

[UsedFromXml]
public class ThoughtWorker_HasGene : ThoughtWorker
{
    protected override ThoughtState CurrentSocialStateInternal(Pawn p, Pawn other)
    {
        if (def.GetModExtension<DefModExtension_Thought>()?.gene is not { } geneDef)
            return false;

        if (!p.RaceProps.Humanlike)
            return false;
        if (!RelationsUtility.PawnsKnowEachOther(p, other))
            return false;
        if (p.story.traits.HasTrait(TraitDefOf.Psychopath))
            return false;
        if (PawnUtility.IsBiologicallyOrArtificiallyBlind(p))
            return false; 
        if (!other.HasActiveGene(geneDef))
            return false;
        return true;
    }
}
