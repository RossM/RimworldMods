namespace Xylib;

[DefOf]
public static class XStatDefOf
{
    public static StatDef XylCookedAnimalProductNutritionFactor;
    public static StatDef XylCookedFungusNutritionFactor;
    public static StatDef XylCookedMeatNutritionFactor;
    public static StatDef XylCookedVegetableNutritionFactor;
    public static StatDef XylDrugEffectMultiplier;
    public static StatDef XylGlobalAddictionChanceFactor;
    public static StatDef XylHypothermiaProgressionFactor;
    public static StatDef XylLearnFactorPassionMajor;
    public static StatDef XylLearnFactorPassionMinor;
    public static StatDef XylLearnFactorPassionNone;
    public static StatDef XylMalnutritionProgressionFactor;
    public static StatDef XylRangedDodgeChance;
    public static StatDef XylRawAnimalProductFoodPoisonChanceFactor;
    public static StatDef XylRawAnimalProductNutritionFactor;
    public static StatDef XylRawFungusFoodPoisonChanceFactor;
    public static StatDef XylRawFungusNutritionFactor;
    public static StatDef XylRawMeatFoodPoisonChanceFactor;
    public static StatDef XylRawMeatNutritionFactor;
    public static StatDef XylRawVegetableFoodPoisonChanceFactor;
    public static StatDef XylRawVegetableNutritionFactor;
    public static StatDef XylResistanceFallRate;
    public static StatDef XylSlaveRebellionMtbFactor;
    public static StatDef XylWillFallRate;

    static XStatDefOf()
    {
        DefOfHelper.EnsureInitializedInCtor(typeof(XStatDefOf));
    }
}
