using JetBrains.Annotations;
using RimWorld;
using Verse;

namespace XylXenos
{
    [RimWorld.DefOf]
    public static class DefOf
    {
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
        public static StatDef XylMalnutritionProgressionFactor;
        public static StatDef XylRangedDodgeChance;
        public static StatDef XylRawAnimalProductFoodPoisonChanceOffset;
        public static StatDef XylRawAnimalProductNutritionFactor;
        public static StatDef XylRawFungusFoodPoisonChanceOffset;
        public static StatDef XylRawFungusNutritionFactor;
        public static StatDef XylRawMeatFoodPoisonChanceOffset;
        public static StatDef XylRawMeatNutritionFactor;
        public static StatDef XylRawNonMeatFoodPoisonChanceOffset;
        public static StatDef XylRawNonMeatNutritionFactor;
        public static StatDef XylResistanceFallRate;
        public static StatDef XylWillFallRate;

        #endregion

        #region GeneDefs

        public static GeneDef XylEcholocation;

        #endregion

        #region Miscellaneous

        public static BiomeDef TemperateSwamp;

        public static FactionDef XylTribeGentleNixie;

        public static PawnKindDef XylSelkie;

        public static JobDef XylTakeShower;

        #endregion
    }
}
