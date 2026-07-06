namespace Xylib;

[DefOf]
public static class XStatDefOf
{
    public static StatDef MeleeWeapon_AverageArmorPenetration;
    public static StatDef XylBloodLossResistance;

    /// <summary>A multiplier on how nutritious cooked animal products are for this person.</summary>
    public static StatDef XylCookedAnimalProductNutritionFactor;

    /// <summary>A multiplier on how nutritious cooked fungus is for this person.</summary>
    public static StatDef XylCookedFungusNutritionFactor;

    /// <summary>A multiplier on how nutritious cooked meat is for this person.</summary>
    public static StatDef XylCookedMeatNutritionFactor;

    /// <summary>A multiplier on how nutritious cooked vegetables are for this person.</summary>
    public static StatDef XylCookedVegetableNutritionFactor;

    /// <summary>
    ///     How sensitive the character is to drug effects. Characters with high drug sensitivity are more affected by the
    ///     same dose, and build up tolerance faster.
    /// </summary>
    public static StatDef XylDrugEffectMultiplier;

    public static StatDef XylDrugOverdoseResistance;

    /// <summary>Multiplies the character's chance becoming addicted to a drug.</summary>
    public static StatDef XylGlobalAddictionChanceFactor;

    public static StatDef XylHeatstrokeResistance;

    public static StatDef XylHypothermiaResistance;

    /// <summary>The character's multiplier on the learning rate for skills with burning passion.</summary>
    public static StatDef XylLearnFactorPassionMajor;

    /// <summary>The character's multiplier on the learning rate for skills with regular passion.</summary>
    public static StatDef XylLearnFactorPassionMinor;

    /// <summary>The character's multiplier on the learning rate for skills with no passion.</summary>
    public static StatDef XylLearnFactorPassionNone;

    public static StatDef XylMalnutritionResistance;

    /// <summary>Chance to dodge a ranged attack that would've otherwise hit.</summary>
    public static StatDef XylRangedDodgeChance;

    /// <summary>A multiplier to the chance of getting food poisoning from eating raw animal products.</summary>
    public static StatDef XylRawAnimalProductFoodPoisonChanceFactor;

    /// <summary>
    ///     A multiplier on how nutritious raw animal products are for this person. Note that since meals usually have
    ///     more nutrition than their raw ingredients, a boost to this stat may only mean the person gets the same nutrition
    ///     from raw food as if it were cooked.
    /// </summary>
    public static StatDef XylRawAnimalProductNutritionFactor;

    /// <summary>A multiplier to the chance of getting food poisoning from eating raw fungus.</summary>
    public static StatDef XylRawFungusFoodPoisonChanceFactor;

    /// <summary>
    ///     A multiplier on how nutritious raw fungus is for this person. Note that since meals usually have more
    ///     nutrition than their raw ingredients, a boost to this stat may only mean the person gets the same nutrition from
    ///     raw food as if it were cooked.
    /// </summary>
    public static StatDef XylRawFungusNutritionFactor;

    /// <summary>A multiplier to the chance of getting food poisoning from eating raw meat.</summary>
    public static StatDef XylRawMeatFoodPoisonChanceFactor;

    /// <summary>
    ///     A multiplier on how nutritious raw meat is for this person. Note that since meals usually have more nutrition
    ///     than their raw ingredients, a boost to this stat may only mean the person gets the same nutrition from raw food as
    ///     if it were cooked.
    /// </summary>
    public static StatDef XylRawMeatNutritionFactor;

    /// <summary>A multiplier to the chance of getting food poisoning from eating raw vegetables.</summary>
    public static StatDef XylRawVegetableFoodPoisonChanceFactor;

    /// <summary>
    ///     A multiplier on how nutritious raw vegetables are for this person. Note that since meals usually have more
    ///     nutrition than their raw ingredients, a boost to this stat may only mean the person gets the same nutrition from
    ///     raw food as if it were cooked.
    /// </summary>
    public static StatDef XylRawVegetableNutritionFactor;

    /// <summary>A multiplier on how quickly this character's resistance falls when they are being recruited.</summary>
    public static StatDef XylResistanceFallRate;

    /// <summary>Affects the average amount of time before the character will rebel as a slave.</summary>
    public static StatDef XylSlaveRebellionMtbFactor;

    /// <summary>A multiplier on how quickly this character's will falls when they are being enslaved.</summary>
    public static StatDef XylWillFallRate;

    static XStatDefOf()
    {
        DefOfHelper.EnsureInitializedInCtor(typeof(XStatDefOf));
    }
}
