namespace Xylib;

[UsedFromXml]
public class FoodGroupDef : Def
{
    public FoodTypeFlags foodTypes;
    public bool exact;
    public bool humanlike;
    public FleshTypeDef? fleshType;

    public StatDef? rawNutritionStat;
    public StatDef? cookedNutritionStat;
    public StatDef? rawFoodPoisonChanceStat;
}

[UsedFromXml]
public class Config : Def
{
    public static Config Instance
    {
        get
        {
            field ??= DefDatabase<Config>.GetNamed("XylibConfig");
            DebugAssert.NotNull(field);

            return field;
        }
    }

    public required Dictionary<HediffDef, StatDef> resistanceStatByHediff;
}
