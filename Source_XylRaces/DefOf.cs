#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
namespace XylXenos;

[RimWorld.DefOf]
public static class DefOf
{
    public static BiomeDef TemperateSwamp;

    public static FactionDef XylTribeGentleNixie;

    public static GeneCategoryDef Cosmetic_Skin;

    public static GeneDef XylEcholocation;

    public static HediffDef XylCultistSong;

    public static JobDef XylTakeShower;

    public static PawnKindDef XylSelkie;

    static DefOf()
    {
        DefOfHelper.EnsureInitializedInCtor(typeof(DefOf));
    }
}
