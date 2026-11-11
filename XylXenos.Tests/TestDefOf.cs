namespace XylXenos.Tests;

[DefOf]
internal static class TestDefOf
{
    public static GeneDef AptitudeTerrible_Cooking = null!;
    public static GeneDef AptitudePoor_Cooking = null!;

    public static GeneDef XylXenosTestGene = null!;
    public static GeneDef XylXenosTestBonusCooking = null!;
    public static GeneDef XylXenosTestEmergencyReserves = null!;
    public static GeneDef XylXenosTestFlight = null!;
    public static GeneDef XylXenosTestHyperlactation = null!;
    public static GeneDef XylXenosTestLoveEuphoria = null!;
    public static GeneDef XylXenosTestMeditationFocus = null!;
    [MayRequireRoyalty]
    public static GeneDef XylXenosTestPsycast = null!;
    public static GeneDef XylXenosTestRegeneration = null!;
    public static GeneDef XylXenosTestSeeingRed = null!;
    public static GeneDef XylXenosTestMilkableCharge = null!;
    public static GeneDef XylXenosTestMilkableReady = null!;
    public static GeneDef XylXenosTestMilkableFullness = null!;
    public static GeneDef XylXenosTestDisableHostility = null!;
    public static GeneDef XylXenosTestTrackerFactors = null!;
    public static GeneDef XylXenosTestTrackerMoreFactors = null!;

    public static HediffDef XylXenosTestHediff = null!;
    public static HediffDef XylXenosTestReserves = null!;
    public static HediffDef XylXenosTestSource = null!;
    public static HediffDef XylXenosTestTimedCharge = null!;
    public static HediffDef XylXenosTestFullCharge = null!;
    public static HediffDef XylXenosTestTransferOnRemoval = null!;
    public static HediffDef XylXenosTestGrowthModes = null!;
    public static HediffDef XylXenosTestTendSwitchesMode = null!;
    public static HediffDef XylXenosTestSaturation = null!;
    public static HediffDef XylXenosTestStageRecovery = null!;
    public static HediffDef XylXenosTestBioRejection = null!;
    public static HediffDef XylXenosTestImplant = null!;
    public static HediffDef XylXenosTestPetrifiedFlesh = null!;
    public static HediffDef XylXenosTestDietDependency = null!;
    public static HediffDef XylXenosTestDietCraving = null!;
    public static HediffDef XylXenosTestSubstituteCapacity = null!;
    public static HediffDef XylXenosTestLovinAddiction = null!;
    public static HediffDef XylXenosTestMoteBubble = null!;
    public static HediffDef XylXenosTestInspiration = null!;

    public static NeedDef XylXenosTestLovinNeed = null!;
    public static NeedDef XylXenosTestWetnessNeed = null!;

    public static AbilityDef XylXenosTestSelfTargetAbility = null!;

    public static ThoughtDef XylXenosTestRestThought = null!;
    public static ThoughtDef XylXenosTestMissingNeedThought = null!;
    public static ThoughtDef XylXenosTestPrisonerThought = null!;
    public static ThoughtDef XylXenosTestFreemanThought = null!;
    public static ThoughtDef XylXenosTestHasGeneThought = null!;
    public static ThoughtDef XylXenosTestHerdThought = null!;
    public static ThoughtDef XylXenosTestFullnessThought = null!;
    public static ThoughtDef XylXenosTestSourceThought = null!;

    public static ThingDef Apparel_Jacket = null!;
    public static ThingDef XylBullet_RockToss = null!;
    public static DamageDef XylOil = null!;
    public static JobDef XylSoak = null!;
    public static RecipeDef XylExcisePetrifiedFlesh = null!;

    // Core loads these focus defs even when Royalty is disabled.
    public static MeditationFocusDef Natural = null!;
    public static MeditationFocusDef Dignified = null!;

    static TestDefOf() => DefOfHelper.EnsureInitializedInCtor(typeof(TestDefOf));
}
