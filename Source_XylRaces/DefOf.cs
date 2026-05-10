using JetBrains.Annotations;
using RimWorld;
using Verse;

namespace XylRacesCore
{
    [RimWorld.DefOf, UsedImplicitly]
    public static class DefOf
    {
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

        [MayRequire("Xylthixlm.Races.Nixie")]
        public static GeneDef XylAquatic;

        [MayRequire("Xylthixlm.Races.Nixie")] 
        public static GeneDef XylDrugSensitive;
        
        [MayRequire("Xylthixlm.Races.Chyrr")] 
        public static GeneDef XylEcholocation;

        #endregion

        [MayRequire("Xylthixlm.Races.Nixie")]
        public static EffecterDef XylShowerSplash;

        public static BiomeDef TemperateSwamp;

        [MayRequire("Xylthixlm.Races.Nixie")]
        public static FactionDef XylTribeGentleNixie;

        [MayRequire("Xylthixlm.Races.Nixie")]
        public static PawnKindDef XylSelkie;

        [MayRequire("Xylthixlm.Races.Nixie")]
        public static JobDef XylTakeShower;

        [MayRequire("Xylthixlm.Races.Bossaps")]
        public static JobDef XylMilkHuman;
    }
}