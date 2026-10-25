namespace XylXenos;

#pragma warning disable CA2211
#pragma warning disable CS8618
[DefOf]
public static class MyDefOf
{
    public static FactionDef XylTribeGentleNixie;
    public static GeneDef XylBioRejection;
    public static GeneDef XylDocile;
    public static GeneDef XylEcholocation;
    public static PawnKindDef XylSelkie;
    public static StatDef XylTreeCuttingSpeed;

    static MyDefOf()
    {
        DefOfHelper.EnsureInitializedInCtor(typeof(DefOf));
    }
}
