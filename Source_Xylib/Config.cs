namespace Xylib;

[UsedFromXml]
public class FoodGroupDef : Def
{
    public FoodTypeFlags foodTypes;
    public bool humanlike;
    public FleshTypeDef? fleshType;

    public StatDef? rawNutritionStat;
    public StatDef? cookedNutritionStat;
    public StatDef? rawFoodPoisonChanceStat;
}

[UsedFromXml]
public class Config : Def
{
    public static Config Instance => field ??= DefDatabase<Config>.GetNamed("XylibConfig");

    public Dictionary<HediffDef, StatDef> resistanceStatByHediff;
}
