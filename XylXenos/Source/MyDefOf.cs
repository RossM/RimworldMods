namespace XylXenos;

#pragma warning disable CA2211
#pragma warning disable CS8618
[DefOf]
public static class MyDefOf
{
    public static GeneDef XylBioRejection;
    public static GeneDef XylDocile;
    public static GeneDef XylEcholocation;
    public static StatDef XylTreeCuttingSpeed;
    public static StatDef XylWetnessFallRate;

    [MayRequire("Ludeon.RimWorld.Odyssey")]
    public static PawnKindDef XylSelkie;

    static MyDefOf()
    {
        DefOfHelper.EnsureInitializedInCtor(typeof(MyDefOf));
    }
}
