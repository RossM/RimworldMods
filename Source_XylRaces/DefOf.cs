using JetBrains.Annotations;
using RimWorld;
using Verse;

namespace XylRacesCore
{
    [RimWorld.DefOf]
    public static class DefOf
    {
        #region StatDefs

        [UsedImplicitly] public static StatDef XylCookedAnimalProductNutritionFactor;
        [UsedImplicitly] public static StatDef XylCookedMeatNutritionFactor;
        [UsedImplicitly] public static StatDef XylCookedNonMeatNutritionFactor;
        [UsedImplicitly] public static StatDef XylDrugEffectMultiplier;
        [UsedImplicitly] public static StatDef XylGlobalAddictionChanceFactor;
        [UsedImplicitly] public static StatDef XylHypothermiaProgressionFactor;
        [UsedImplicitly] public static StatDef XylMalnutritionProgressionFactor;
        [UsedImplicitly] public static StatDef XylRangedDodgeChance;
        [UsedImplicitly] public static StatDef XylRawAnimalProductFoodPoisonChanceOffset;
        [UsedImplicitly] public static StatDef XylRawAnimalProductNutritionFactor;
        [UsedImplicitly] public static StatDef XylRawFungusFoodPoisonChanceOffset;
        [UsedImplicitly] public static StatDef XylRawFungusNutritionFactor;
        [UsedImplicitly] public static StatDef XylRawMeatFoodPoisonChanceOffset;
        [UsedImplicitly] public static StatDef XylRawMeatNutritionFactor;
        [UsedImplicitly] public static StatDef XylRawNonMeatFoodPoisonChanceOffset;
        [UsedImplicitly] public static StatDef XylRawNonMeatNutritionFactor;
        [UsedImplicitly] public static StatDef XylResistanceFallRate;
        [UsedImplicitly] public static StatDef XylWillFallRate;

        #endregion

        #region GeneDefs

        [UsedImplicitly, MayRequire("Xylthixlm.Races.Nixie")]
        public static GeneDef XylAquatic;

        [UsedImplicitly, MayRequire("Xylthixlm.Races.Bossaps")]
        public static GeneDef XylDocile;

        [UsedImplicitly, MayRequire("Xylthixlm.Races.Nixie")] 
        public static GeneDef XylDrugSensitive;
        
        [UsedImplicitly, MayRequire("Xylthixlm.Races.Chyrr")] 
        public static GeneDef XylEcholocation;

        #endregion

        [UsedImplicitly, MayRequire("Xylthixlm.Races.Nixie")]
        public static EffecterDef XylShowerSplash;

        [UsedImplicitly] public static BiomeDef TemperateSwamp;

        [UsedImplicitly, MayRequire("Xylthixlm.Races.Nixie")]
        public static FactionDef XylTribeGentleNixie;

        [UsedImplicitly, MayRequire("Xylthixlm.Races.Nixie")]
        public static PawnKindDef XylSelkie;

        [UsedImplicitly, MayRequire("Xylthixlm.Races.Nixie")]
        public static JobDef XylTakeShower;

        [UsedImplicitly, MayRequire("Xylthixlm.Races.Bossaps")]
        public static JobDef XylMilkHuman;
    }
}