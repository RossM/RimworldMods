namespace XylXenos;

/// <summary>
///     The pawn will have psychic entropy with or without a psylink, and any psycast
///     abilities added by this gene in <see cref="GeneDef.abilities" /> will be usable without
///     a psylink.
/// </summary>
[UsedFromXml]
public class GeneCompProperties_Psycast : GeneCompProperties
{
    public GeneCompProperties_Psycast()
    {
        compClass = typeof(GeneComp_Psycast);
    }
}

public class GeneComp_Psycast : GeneComp
{
    public override void CompPostPostAdd()
    {
        Pawn.psychicEntropy?.SetInitialPsyfocusLevel();
    }
}
