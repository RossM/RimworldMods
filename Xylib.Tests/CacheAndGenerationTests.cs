using static Xylib.Tests.TestDefOf;

namespace Xylib.Tests;

[TestFixture(TestType.Playing)]
public sealed class CacheAndGenerationTests : PawnTestFixture
{
    [Test]
    public void GeneCacheInvalidatesAfterRealGeneAddAndRemoval()
    {
        GeneDef def = XylibTestProbe;
        var cache = pawn.GeneAndHediffCache;
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
        HediffDef def = XylibTestCachedHediff;
        var cache = pawn.GeneAndHediffCache;
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
    public void TrackerAggregatesRaceRenderAndUnlockPropertiesThenResets()
    {
        Gene first = pawn.genes.AddGene(XylibTestTrackerFactors, false);
        Gene second = pawn.genes.AddGene(XylibTestTrackerBodySize, false);
        var tracker = pawn.GeneTracker_Xylib!;
        Expect.AreApproximatelyEqual(1.5f, tracker.bodySizeFactor);
        Expect.AreApproximatelyEqual(3, tracker.healthScaleFactor);
        Expect.AreApproximatelyEqual(2, tracker.bodyScale);
        Expect.AreApproximatelyEqual(0.5f, tracker.headScale);
        Expect.IsTrue(tracker.unlockedRecipes!.Contains(CookMealSimple));
        Expect.IsTrue(tracker.unlockedBuildables!.Contains(ThingDefOf.Wall));
        pawn.genes.RemoveGene(first);
        pawn.genes.RemoveGene(second);
        Expect.AreApproximatelyEqual(1, tracker.bodySizeFactor);
        Expect.AreApproximatelyEqual(1, tracker.bodyScale);
        Expect.IsEmpty(tracker.unlockedRecipes!);
        Expect.IsEmpty(tracker.unlockedBuildables!);
    }

    [Test, LoadIfBiotechActive]
    public void ListMakerReturnsOnlyNewGenesAndHonorsCount()
    {
        GeneDef existing = XylibTestGene, first = XylibTestOtherGene, second = XylibTestThirdGene;
        pawn.genes.AddGene(existing, false);
        var maker = new GeneSetMaker_List { genes = [existing, first, second], count = new IntRange(1) };
        GeneSet generated = maker.Generate(pawn);
        Expect.AreEqual(1, generated.GenesListForReading.Count);
        Expect.IsFalse(generated.GenesListForReading.Contains(existing));
        Expect.IsTrue(generated.GenesListForReading.All(g => g == first || g == second));
        Expect.IsFalse(pawn.genes.GenesListForReading.Any(g => g.def == first));
        Expect.IsFalse(pawn.genes.GenesListForReading.Any(g => g.def == second));
    }

    [Test, LoadIfBiotechActive]
    public void ListMakerWithZeroCountReturnsNoGenes()
    {
        GeneDef candidate = XylibTestGene;
        var maker = new GeneSetMaker_List { genes = [candidate], count = IntRange.Zero };

        Expect.IsEmpty(maker.Generate(pawn).GenesListForReading);
        Expect.IsFalse(pawn.genes.GenesListForReading.Any(g => g.def == candidate));
    }

    [Test, LoadIfBiotechActive]
    public void MakerDefRespectsPawnAndXenotypeGenesByDefault()
    {
        pawn.genes.AddGene(XylibTestConflictA, false);
        pawn.genes.SetXenotypeDirect(XylibTestInheritedXenotype);
        GeneSetMakerDef maker = XylibTestRespectConflicts;

        Expect.IsFalse(maker.overrideExistingGenes);
        Expect.IsEmpty(maker.Generate(pawn).GenesListForReading);
    }

    [Test, LoadIfBiotechActive]
    public void MakerDefCanIgnorePawnAndXenotypeGenesDuringGeneration()
    {
        GeneDef existing = XylibTestConflictA, first = XylibTestConflictB, second = XylibTestInheritedB;
        pawn.genes.AddGene(existing, false);
        pawn.genes.SetXenotypeDirect(XylibTestInheritedXenotype);
        GeneSetMakerDef maker = XylibTestIgnoreConflicts;

        GeneSet generated = maker.Generate(pawn);
        Expect.AreEqual(2, generated.GenesListForReading.Count);
        Expect.IsTrue(generated.GenesListForReading.Contains(first));
        Expect.IsTrue(generated.GenesListForReading.Contains(second));
        Expect.ReferencesAreEqual(existing, pawn.genes.GenesListForReading.Single().def);
    }

    [Test, LoadIfBiotechActive]
    public void OptionMakerSelectsItsWeightedSubtree()
    {
        GeneDef expected = XylibTestGene;
        var maker = new GeneSetMaker_Option
        {
            options = [new GeneSetMakerWeight { maker = new GeneSetMaker_List { genes = [expected] }, weight = 1 }],
        };
        Expect.ReferencesAreEqual(expected, maker.Generate(pawn).GenesListForReading.Single());
    }

    [Test, LoadIfBiotechActive]
    public void SubtreeMakerDelegatesToReferencedMaker()
    {
        GeneDef expected = XylibTestGene;
        var maker = new GeneSetMaker_Subtree { def = XylibTestSingleGeneMaker };
        Expect.ReferencesAreEqual(expected, maker.Generate(pawn).GenesListForReading.Single());
    }

    [Test, LoadIfBiotechActive]
    public void BiostatMakerRejectsOutOfRangeAndProhibitedGenes()
    {
        GeneDef def = XylibTestMetabolism;
        var maker = new GeneSetMaker_Biostats { biostatMet = new IntRange(1, 3) };
        Expect.IsTrue(maker.Validate(def, new GeneSet(), GeneType.Xenogene, pawn));
        maker.biostatMet = new IntRange(-3, -1);
        Expect.IsFalse(maker.Validate(def, new GeneSet(), GeneType.Xenogene, pawn));
        maker.biostatMet = new IntRange(1, 3);
        maker.prohibitedGenes = [def];
        Expect.IsFalse(maker.Validate(def, new GeneSet(), GeneType.Xenogene, pawn));
    }

    [Test, LoadIfBiotechActive]
    public void BiostatTotalMakerWithoutShuffleUsesDeclaredOrder()
    {
        GeneDef first = XylibTestConflictA, second = XylibTestConflictB;
        var maker = new GeneSetMaker_BiostatTotal
        {
            count = new IntRange(2), shuffle = false,
            subMakers = [new GeneSetMaker_List { genes = [first] }, new GeneSetMaker_List { genes = [second] }],
        };

        using (new RandBlock(72491))
        {
            for (int i = 0; i < 32; i++)
                Expect.ReferencesAreEqual(first, maker.Generate(pawn).GenesListForReading.Single());
        }
    }

    [Test, LoadIfBiotechActive]
    public void BiostatTotalMakerShufflesBeforeSelectingLimitedSubmakers()
    {
        GeneDef first = XylibTestGene, second = XylibTestOtherGene;
        var maker = new GeneSetMaker_BiostatTotal
        {
            count = IntRange.One, shuffle = true,
            subMakers = [new GeneSetMaker_List { genes = [first] }, new GeneSetMaker_List { genes = [second] }],
        };
        HashSet<GeneDef> selected = [];

        using (new RandBlock(72491))
        {
            for (int i = 0; i < 32; i++)
                selected.Add(maker.Generate(pawn).GenesListForReading.Single());
        }

        Expect.IsTrue(selected.Contains(first));
        Expect.IsTrue(selected.Contains(second), "Shuffling must allow either submaker to fill the single available slot.");
    }

    [Test, LoadIfBiotechActive]
    public void BiostatTotalMakerShufflesExecutionOrderOfConflictingSubmakers()
    {
        GeneDef first = XylibTestConflictA, second = XylibTestConflictB;
        var maker = new GeneSetMaker_BiostatTotal
        {
            count = new IntRange(2), shuffle = true,
            subMakers = [new GeneSetMaker_List { genes = [first] }, new GeneSetMaker_List { genes = [second] }],
        };
        HashSet<GeneDef> selected = [];

        using (new RandBlock(72491))
        {
            for (int i = 0; i < 32; i++)
                selected.Add(maker.Generate(pawn).GenesListForReading.Single());
        }

        Expect.IsTrue(selected.Contains(first));
        Expect.IsTrue(selected.Contains(second), "Either conflicting gene must be able to win by executing its submaker first.");
    }

    [Test, LoadIfBiotechActive]
    public void BiostatTotalMakerRetainsExistingGenesWhileAddingValidSet()
    {
        GeneDef existing = XylibTestGene, added = XylibTestMetabolism;
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
