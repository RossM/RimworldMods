namespace Xylib.Tests;

[DefOf]
internal static class TestDefOf
{
    public static EventDef XylibTestSignal = null!;
    public static EventDef XylibTestTyped = null!;
    public static EventDef XylibTestDuringScribe = null!;

    [MayRequireBiotech]
    public static GeneDef AptitudeTerrible_Cooking = null!;
    [MayRequireBiotech]
    public static GeneDef AptitudePoor_Cooking = null!;
    [MayRequireBiotech]
    public static GeneDef Beauty_Ugly = null!;
    [MayRequireBiotech]
    public static GeneDef Beauty_Beautiful = null!;

    public static GeneDef XylibTestGene = null!;
    public static GeneDef XylibTestOtherGene = null!;
    public static GeneDef XylibTestThirdGene = null!;
    public static GeneDef XylibTestProbe = null!;
    public static GeneDef XylibTestDerivedProbe = null!;
    public static GeneDef XylibTestMetabolism = null!;
    public static GeneDef XylibTestConflictA = null!;
    public static GeneDef XylibTestConflictB = null!;
    public static GeneDef XylibTestConflictC = null!;
    public static GeneDef XylibTestConflictD = null!;
    public static GeneDef XylibTestInheritedB = null!;
    public static GeneDef XylibTestAddBonus = null!;
    public static GeneDef XylibTestReplaceXenogenes = null!;
    public static GeneDef XylibTestReplaceEndogenes = null!;
    [MayRequireBiotech]
    public static GeneDef XylibTestBonusBeauty = null!;
    [MayRequireBiotech]
    public static GeneDef XylibTestBonusCooking = null!;
    public static GeneDef XylibTestNestedBonus = null!;
    public static GeneDef XylibTestDeferredBonus = null!;
    public static GeneDef XylibTestScribedBonus = null!;

    public static GeneSetMakerDef XylibTestRespectConflicts = null!;
    public static GeneSetMakerDef XylibTestIgnoreConflicts = null!;
    public static GeneSetMakerDef XylibTestSingleGeneMaker = null!;

    public static XenotypeDef XylibTestInheritedXenotype = null!;

    public static GeneDef XylibTestTrackerFactors = null!;
    public static GeneDef XylibTestTrackerBodySize = null!;
    public static GeneDef XylibTestCongenitalHediffGene = null!;
    public static GeneDef XylibTestPermanentHediffGene = null!;
    public static GeneDef XylibTestStartingItems = null!;

    public static HediffDef XylibTestHediff = null!;
    public static HediffDef XylibTestOtherHediff = null!;
    public static HediffDef XylibTestCachedHediff = null!;
    public static HediffDef XylibTestGeneticHediff = null!;
    public static HediffDef XylibTestBerserkHediff = null!;
    public static HediffDef XylibTestDebugGiver = null!;
    public static HediffDef XylibTestExtendedStages = null!;
    public static HediffDef XylibTestSetNeed = null!;
    public static HediffDef XylibTestVisibleSource = null!;
    public static HediffDef XylibTestHiddenSource = null!;

    public static ThingDef XylibTestSmallStack = null!;

    public static PawnKindDef XylibTestOtherApparel = null!;
    public static PawnKindDef XylibTestDisallowApparel = null!;

    public static RecipeDef CookMealSimple = null!;

    static TestDefOf() => DefOfHelper.EnsureInitializedInCtor(typeof(TestDefOf));
}
