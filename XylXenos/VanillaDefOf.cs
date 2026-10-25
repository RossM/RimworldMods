namespace XylXenos;

#pragma warning disable CA2211
#pragma warning disable CS8618
[DefOf]
public static class VanillaDefOf
{
    public static BiomeDef TemperateSwamp;
    public static GeneCategoryDef Cosmetic_Skin;

    static VanillaDefOf()
    {
        DefOfHelper.EnsureInitializedInCtor(typeof(DefOf));
    }
}
