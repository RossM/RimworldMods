namespace Xylib;

[DefOf]
public static class XStatDefOf
{
    public static StatDef MeleeWeapon_AverageArmorPenetration;

    /// <summary>
    ///     How sensitive the character is to drug effects. Characters with high drug sensitivity are more affected by the
    ///     same dose, and build up tolerance faster.
    /// </summary>
    public static StatDef XylDrugEffectMultiplier;

    /// <summary>Multiplies the character's chance becoming addicted to a drug.</summary>
    public static StatDef XylGlobalAddictionChanceFactor;

    /// <summary>The character's multiplier on the learning rate for skills with burning passion.</summary>
    public static StatDef XylLearnFactorPassionMajor;

    /// <summary>The character's multiplier on the learning rate for skills with regular passion.</summary>
    public static StatDef XylLearnFactorPassionMinor;

    /// <summary>The character's multiplier on the learning rate for skills with no passion.</summary>
    public static StatDef XylLearnFactorPassionNone;

    /// <summary>Chance to dodge a ranged attack that would've otherwise hit.</summary>
    public static StatDef XylRangedDodgeChance;

    /// <summary>A multiplier on how quickly this character's resistance falls when they are being recruited.</summary>
    public static StatDef XylResistanceFallRate;

    /// <summary>Affects the average amount of time before the character will rebel as a slave.</summary>
    public static StatDef XylSlaveRebellionMtbFactor;

    /// <summary>A multiplier on how quickly this character's will falls when they are being enslaved.</summary>
    public static StatDef XylWillFallRate;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    static XStatDefOf()
    {
        DefOfHelper.EnsureInitializedInCtor(typeof(XStatDefOf));
    }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
}
