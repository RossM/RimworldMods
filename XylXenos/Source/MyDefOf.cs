namespace XylXenos;

#pragma warning disable CA2211
#pragma warning disable CS8618
[DefOf]
public static class MyDefOf
{
    public static GeneDef XylBioRejection;
    public static GeneDef XylDocile;
    public static GeneDef XylEcholocation;
    public static PawnKindDef XylSelkie;
    public static StatDef XylTreeCuttingSpeed;
    public static StatDef XylWetnessFallRate;

    static MyDefOf()
    {
        DefOfHelper.EnsureInitializedInCtor(typeof(MyDefOf));
    }
}
