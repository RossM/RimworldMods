using RimWorld;
using Verse;

namespace XylXenos
{
    [RimWorld.DefOf]
    public static class DefOf
    {
        #region GeneDefs

        public static GeneDef XylEcholocation;

        #endregion

        static DefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(DefOf));
        }

        #region StatDefs

        public static StatDef XylCookedAnimalProductNutritionFactor;
        public static StatDef XylCookedMeatNutritionFactor;
        public static StatDef XylCookedNonMeatNutritionFactor;
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
        public static StatDef XylRawNonMeatFoodPoisonChanceFactor;
        public static StatDef XylRawNonMeatNutritionFactor;
        public static StatDef XylResistanceFallRate;
        public static StatDef XylSlaveRebellionMtbFactor;
        public static StatDef XylWillFallRate;

        #endregion

        #region Miscellaneous

        public static BiomeDef TemperateSwamp;

        public static FactionDef XylTribeGentleNixie;

        public static PawnKindDef XylSelkie;

        public static JobDef XylTakeShower;

        #endregion
    }
}
