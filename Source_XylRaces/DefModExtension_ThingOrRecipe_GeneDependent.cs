namespace XylXenos;

public class DefModExtension_ThingOrRecipe_GeneDependent : DefModExtension
{
    public List<GeneDef> genePrerequisitesAny;

    public bool Validate()
    {
        if (genePrerequisitesAny.NullOrEmpty())
            return true;

        foreach (var gene in genePrerequisitesAny)
        {
            if (Faction.OfPlayer.AllPawns.Any(p => p.HasActiveGene(gene)))
            {
                return true;
            }
        }

        return false;
    }
}