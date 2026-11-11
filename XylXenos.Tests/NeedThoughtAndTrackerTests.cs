using static XylXenos.Tests.TestDefOf;
using UnityEngine;

namespace XylXenos.Tests;

[TestFixture(TestType.Playing)]
public sealed class NeedThoughtAndTrackerTests : PawnTestFixture
{
    [Test]
    public void WetnessNeedMapsLevelsToCategoriesAndMovesTowardInstantLevel()
    {
        var need = new Need_Wetness(pawn) { def = XylXenosTestWetnessNeed };
        foreach ((float level, WetnessCategory category) in new[]
        {
            (0f, WetnessCategory.Parched), (0.1f, WetnessCategory.VeryDry), (0.3f, WetnessCategory.Dry),
            (0.6f, WetnessCategory.Neutral), (1f, WetnessCategory.Wet),
        })
        {
            need.CurLevel = level;
            Expect.AreEqual(category, need.CurCategory);
        }
        need.CurLevel = 0.5f;
        Expect.AreApproximatelyEqual(0, need.CurInstantLevel, "An unspawned pawn has no wetness source.");
        need.NeedInterval();
        Expect.AreApproximatelyEqual(0.5f, need.CurLevel, "Unspawned pawns' seeker needs are frozen by vanilla.");
        GenSpawn.Spawn(pawn, new IntVec3(70, 0, 70), Find.CurrentMap);
        need.NeedInterval();
        Expect.LessThan(need.CurLevel, 0.5f);
        Expect.GreaterThanOrEqualTo(need.CurLevel, 0f);
    }

    [Test]
    public void NeedThoughtUsesNeedCategoryAndRejectsMissingNeed()
    {
        var worker = new ThoughtWorker_Need { def = XylXenosTestRestThought };
        pawn.needs.rest.CurLevel = 0.2f;
        Expect.AreEqual((int)pawn.needs.rest.CurCategory, worker.CurStageGetter(pawn.needs.rest));
        Expect.AreEqual((int)pawn.needs.rest.CurCategory, worker.CurrentState(pawn).StageIndex);
        var missingNeed = new ThoughtWorker_Need { def = XylXenosTestMissingNeedThought };
        Expect.IsFalse(missingNeed.CurrentState(pawn).Active);
    }

    [Test]
    public void PawnStatusThoughtDistinguishesPrisonerAndFreeman()
    {
        var prisoner = new ThoughtWorker_PawnStatus { def = XylXenosTestPrisonerThought };
        var freeman = new ThoughtWorker_PawnStatus { def = XylXenosTestFreemanThought };
        Expect.IsFalse(prisoner.CurrentState(pawn).Active);
        Expect.IsTrue(freeman.CurrentState(pawn).Active);
    }

    [Test]
    public void HasGeneSocialThoughtRequiresGeneOnKnownOtherPawn()
    {
        GeneDef gene = XylXenosTestGene;
        var worker = new ThoughtWorker_HasGene { def = XylXenosTestHasGeneThought };
        Pawn other = PawnGenerator.GeneratePawn(new PawnGenerationRequest(PawnKindDefOf.Colonist,
            Faction.OfPlayer, canGeneratePawnRelations: false, forcedXenotype: XenotypeDefOf.Baseliner));
        things.Add(other);
        pawn.relations.AddDirectRelation(PawnRelationDefOf.Parent, other);
        Expect.IsFalse(worker.CurrentSocialState(pawn, other).Active);
        other.genes.AddGene(gene, false);
        Expect.IsTrue(worker.CurrentSocialState(pawn, other).Active);
        Expect.IsFalse(worker.CurrentSocialState(other, pawn).Active);
    }

    [Test]
    public void HerdInstinctUsesMapPopulationAndUnspawnedFallback()
    {
        Expect.AreEqual(1, Thought_Situational_HerdInstinct.ColonistCount(pawn));
        var worker = new ThoughtWorker_HerdInstinct { def = XylXenosTestHerdThought };
        var thought = new Thought_Situational_HerdInstinct { pawn = pawn, def = worker.def };
        Expect.LessThan(thought.MoodOffset(), 0f);
        GenSpawn.Spawn(pawn, new IntVec3(70, 0, 70), Find.CurrentMap);
        Expect.AreEqual(Find.CurrentMap.mapPawns.ColonistsSpawnedCount, Thought_Situational_HerdInstinct.ColonistCount(pawn));
        Expect.IsTrue(worker.CurrentState(pawn).Active);
    }

    [Test]
    public void MilkableThoughtClampsFullnessToLastThoughtStage()
    {
        var gene = (GeneWithComps)pawn.genes.AddGene(XylXenosTestMilkableFullness, false);
        var worker = new ThoughtWorker_MilkableFullness { def = XylXenosTestFullnessThought };
        Expect.IsFalse(worker.CurrentState(pawn).Active);
        gene.GetComp<GeneComp_Milkable>()!.fullSinceTick = Find.TickManager.TicksGame - 100;
        Expect.AreEqual(1, worker.CurrentState(pawn).StageIndex);
    }

    [Test]
    public void SourceThoughtFormatsSourcePawnFromHediff()
    {
        HediffDef def = XylXenosTestSource;
        var hediff = (HediffWithCompsExt)pawn.health.AddHediff(def);
        hediff.TryGetComp<HediffComp_Source>().other = pawn;
        var worker = new ThoughtWorker_WithSource { def = XylXenosTestSourceThought };
        string label = worker.PostProcessLabel(pawn, "Source: {SOURCE_label}");
        Expect.IsTrue(label.Contains(pawn.LabelShort));
        Expect.IsFalse(label.Contains("{SOURCE"));
    }

    [Test]
    public void XenosTrackerMultipliesFactorsAddsBonusesAndClearsOnRemoval()
    {
        var first = (GeneWithComps)pawn.genes.AddGene(XylXenosTestTrackerFactors, false);
        Gene second = pawn.genes.AddGene(XylXenosTestTrackerMoreFactors, false);
        FactionDef faction = Faction.OfAncientsHostile.def;
        GeneIngestionThoughtOverride thought = first.def.GetModExtension<DefModExtension_GeneWithComps>()
            .CompProps<GeneCompProperties_IngestionThoughtOverrides>()!.overrides.Single();
        var tracker = pawn.GeneTracker_XylXenos!;
        Expect.AreApproximatelyEqual(6, tracker.meleeDamageFactors![DamageDefOf.Blunt]);
        Expect.AreApproximatelyEqual(0.3f, tracker.meleeChanceBonuses![DamageDefOf.Blunt]);
        Expect.AreApproximatelyEqual(0.25f, tracker.joyGiverChanceFactors![JoyGiverDefOf.Play_Chess]);
        Expect.IsTrue(tracker.disableHostilityFromFactions!.Contains(faction));
        Expect.IsTrue(tracker.ingestionThoughtOverrides!.Contains(thought));
        pawn.genes.RemoveGene(first);
        pawn.genes.RemoveGene(second);
        Expect.IsEmpty(tracker.meleeDamageFactors);
        Expect.IsEmpty(tracker.meleeChanceBonuses);
        Expect.IsEmpty(tracker.joyGiverChanceFactors);
        Expect.IsEmpty(tracker.disableHostilityFromFactions);
        Expect.IsEmpty(tracker.ingestionThoughtOverrides);
    }

    [Test]
    public void SpectrumCanInterpolateEveryAdjacentColorPair()
    {
        var generator = new ColorGenerator_Spectrum { colors = [Color.red, Color.green, Color.blue] };
        bool reachedLastSegment = false;
        using (new RandBlock(72491))
        {
            for (int i = 0; i < 64; i++)
                reachedLastSegment |= generator.NewRandomizedColor().b > 0;
        }
        Expect.IsTrue(reachedLastSegment, "The final green-blue segment must be selectable.");
    }
}
