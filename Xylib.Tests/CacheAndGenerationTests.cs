namespace Xylib.Tests;

[TestFixture(TestType.Playing)]
public sealed class CacheAndGenerationTests : PawnTestFixture
{
    [Test]
    public void GeneCacheInvalidatesAfterRealGeneAddAndRemoval()
    {
        GeneDef def = GeneDef(new ProbeProperties());
        var cache = PawnExtraData<GeneAndHediffCache>.Get(pawn);
        Expect.IsEmpty(cache.GetGenesWithDef(def));
        Expect.IsEmpty(cache.GetGenesOfType<GeneWithComps>());
        Expect.IsEmpty(cache.GetGenesWithComp<ProbeComp>());
        Gene gene = pawn.genes.AddGene(def, false);
        Expect.ReferencesAreEqual(gene, cache.GetGenesWithDef(def).Single());
        Expect.ReferencesAreEqual(gene, cache.GetGenesOfType<GeneWithComps>().Single());
        Expect.ReferencesAreEqual(gene, cache.GetGenesWithComp<ProbeComp>().Single());
        pawn.genes.RemoveGene(gene);
        Expect.IsEmpty(cache.GetGenesWithDef(def));
        Expect.IsEmpty(cache.GetGenesOfType<GeneWithComps>());
        Expect.IsEmpty(cache.GetGenesWithComp<ProbeComp>());
    }

    [Test]
    public void HediffCacheInvalidatesAllQueryKindsAfterAddAndRemoval()
    {
        HediffDef def = HediffDef(new HediffCompProperties_Source());
        def.modExtensions = [new TestExtension()];
        var cache = PawnExtraData<GeneAndHediffCache>.Get(pawn);
        Expect.IsEmpty(cache.GetHediffsWithDef(def));
        Expect.IsEmpty(cache.GetHediffsOfType<HediffWithComps>());
        Expect.IsEmpty(cache.GetHediffsWithComp<HediffComp_Source>());
        Expect.IsEmpty(cache.GetHediffsWithModExtension<TestExtension>());
        Hediff hediff = pawn.health.AddHediff(def);
        Expect.ReferencesAreEqual(hediff, cache.GetHediffsWithDef(def).Single());
        Expect.ReferencesAreEqual(hediff, cache.GetHediffsOfType<HediffWithComps>().Single());
        Expect.ReferencesAreEqual(hediff, cache.GetHediffsWithComp<HediffComp_Source>().Single());
        Expect.ReferencesAreEqual(hediff, cache.GetHediffsWithModExtension<TestExtension>().Single());
        pawn.health.RemoveHediff(hediff);
        Expect.IsEmpty(cache.GetHediffsWithDef(def));
        Expect.IsEmpty(cache.GetHediffsOfType<HediffWithComps>());
        Expect.IsEmpty(cache.GetHediffsWithComp<HediffComp_Source>());
        Expect.IsEmpty(cache.GetHediffsWithModExtension<TestExtension>());
    }

    [Test]
    public void PawnExtraDataCreatesOneInitializedInstancePerPawn()
    {
        var first = PawnExtraData<TestPawnData>.Get(pawn);
        var again = PawnExtraData<TestPawnData>.Get(pawn);
        Expect.ReferencesAreEqual(first, again);
        Expect.ReferencesAreEqual(pawn, first.Pawn);
        Expect.AreEqual(1, first.initializations);
    }

    [Test]
    public void TrackerAggregatesRaceRenderAndUnlockPropertiesThenResets()
    {
        var recipe = new RecipeDef();
        var building = new ThingDef();
        Gene first = pawn.genes.AddGene(GeneDef(
            new GeneCompProperties_RaceModifiers { bodySizeFactor = 2, healthScaleFactor = 3 },
            new GeneCompProperties_RenderNodeModifiers { bodyScale = 2, headScale = 0.5f },
            new GeneCompProperties_UnlockRecipes { recipes = [recipe] },
            new GeneCompProperties_UnlockBuildables { buildables = [building] }), false);
        Gene second = pawn.genes.AddGene(GeneDef(new GeneCompProperties_RaceModifiers { bodySizeFactor = 0.75f }), false);
        var tracker = PawnExtraData<GeneTracker_Xylib>.Get(pawn);
        Expect.AreApproximatelyEqual(1.5f, tracker.bodySizeFactor);
        Expect.AreApproximatelyEqual(3, tracker.healthScaleFactor);
        Expect.AreApproximatelyEqual(2, tracker.bodyScale);
        Expect.AreApproximatelyEqual(0.5f, tracker.headScale);
        Expect.IsTrue(tracker.unlockedRecipes!.Contains(recipe));
        Expect.IsTrue(tracker.unlockedBuildables!.Contains(building));
        pawn.genes.RemoveGene(first);
        pawn.genes.RemoveGene(second);
        Expect.AreApproximatelyEqual(1, tracker.bodySizeFactor);
        Expect.AreApproximatelyEqual(1, tracker.bodyScale);
        Expect.IsEmpty(tracker.unlockedRecipes!);
        Expect.IsEmpty(tracker.unlockedBuildables!);
    }

    [Test]
    public void ListMakerReturnsOnlyNewGenesAndHonorsCount()
    {
        GeneDef existing = GeneDef(), first = GeneDef(), second = GeneDef();
        pawn.genes.AddGene(existing, false);
        var maker = new GeneSetMaker_List { genes = [existing, first, second], count = new IntRange(1) };
        GeneSet generated = maker.Generate(pawn);
        Expect.AreEqual(1, generated.GenesListForReading.Count);
        Expect.IsFalse(generated.GenesListForReading.Contains(existing));
        Expect.IsTrue(generated.GenesListForReading.All(g => g == first || g == second));
        Expect.IsFalse(pawn.genes.GenesListForReading.Any(g => g.def == first));
        Expect.IsFalse(pawn.genes.GenesListForReading.Any(g => g.def == second));
    }

    [Test]
    public void OptionMakerSelectsItsWeightedSubtree()
    {
        GeneDef expected = GeneDef();
        var maker = new GeneSetMaker_Option
        {
            options = [new GeneSetMakerWeight { maker = new GeneSetMaker_List { genes = [expected] }, weight = 1 }],
        };
        Expect.ReferencesAreEqual(expected, maker.Generate(pawn).GenesListForReading.Single());
    }

    [Test]
    public void SubtreeMakerDelegatesToReferencedMaker()
    {
        GeneDef expected = GeneDef();
        var maker = new GeneSetMaker_Subtree { def = new GeneSetMakerDef { root = new GeneSetMaker_List { genes = [expected] } } };
        Expect.ReferencesAreEqual(expected, maker.Generate(pawn).GenesListForReading.Single());
    }

    [Test]
    public void BiostatMakerRejectsOutOfRangeAndProhibitedGenes()
    {
        GeneDef def = GeneDef();
        def.biostatMet = 2;
        var maker = new GeneSetMaker_Biostats { biostatMet = new IntRange(1, 3) };
        Expect.IsTrue(maker.Validate(def, new GeneSet(), GeneType.Xenogene, pawn));
        maker.biostatMet = new IntRange(-3, -1);
        Expect.IsFalse(maker.Validate(def, new GeneSet(), GeneType.Xenogene, pawn));
        maker.biostatMet = new IntRange(1, 3);
        maker.prohibitedGenes = [def];
        Expect.IsFalse(maker.Validate(def, new GeneSet(), GeneType.Xenogene, pawn));
    }

    [Test]
    public void BiostatTotalMakerRetainsExistingGenesWhileAddingValidSet()
    {
        GeneDef existing = GeneDef(), added = GeneDef();
        added.biostatMet = 2;
        pawn.genes.AddGene(existing, false);
        var maker = new GeneSetMaker_BiostatTotal
        {
            biostatMet = new IntRange(2), shuffle = false,
            subMakers = [new GeneSetMaker_List { genes = [added] }],
        };
        Expect.ReferencesAreEqual(added, maker.Generate(pawn).GenesListForReading.Single());
        Expect.IsTrue(pawn.genes.GenesListForReading.Any(g => g.def == existing));
    }
}

public sealed class TestExtension : DefModExtension;
public sealed class TestPawnData : IPawnData
{
    public Pawn Pawn { get; set; } = null!;
    public int initializations;
    public void Init() => initializations++;
}
