using UnityEngine;

namespace XylXenos.Tests;

[TestFixture(TestType.Playing)]
public sealed class NeedThoughtAndTrackerTests : PawnTestFixture
{
    [Test]
    public void WetnessNeedMapsLevelsToCategoriesAndMovesTowardInstantLevel()
    {
        var need = new Need_Wetness(pawn)
        {
            def = new NeedDef { label = "test wetness", seekerFallPerHour = 0.2f, seekerRisePerHour = 0.4f },
        };
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
        var def = new ThoughtDef
        {
            defName = "TestNeedThought", thoughtClass = typeof(Thought_Situational), validWhileDespawned = true,
            stages = Enumerable.Range(0, 5).Select(_ => new ThoughtStage { label = "test stage" }).ToList(),
            modExtensions = [new DefModExtension_Thought_Need { need = NeedDefOf.Rest }],
        };
        var worker = new ThoughtWorker_Need { def = def };
        pawn.needs.rest.CurLevel = 0.2f;
        Expect.AreEqual((int)pawn.needs.rest.CurCategory, worker.CurStageGetter(pawn.needs.rest));
        Expect.AreEqual((int)pawn.needs.rest.CurCategory, worker.CurrentState(pawn).StageIndex);
        def.GetModExtension<DefModExtension_Thought_Need>().need = new NeedDef();
        Expect.IsFalse(worker.CurrentState(pawn).Active);
    }

    [Test]
    public void PawnStatusThoughtDistinguishesPrisonerAndFreeman()
    {
        var extension = new DefModExtension_Thought_PawnStatus { status = DefModExtension_Thought_PawnStatus.StatusMode.Prisoner };
        var worker = new ThoughtWorker_PawnStatus
        {
            def = new ThoughtDef { defName = "TestStatusThought", validWhileDespawned = true, modExtensions = [extension], stages = [new ThoughtStage()] },
        };
        Expect.IsFalse(worker.CurrentState(pawn).Active);
        extension.status = DefModExtension_Thought_PawnStatus.StatusMode.Freeman;
        Expect.IsTrue(worker.CurrentState(pawn).Active);
    }

    [Test]
    public void HasGeneSocialThoughtRequiresGeneOnKnownOtherPawn()
    {
        GeneDef gene = GeneDef();
        var worker = new ThoughtWorker_HasGene
        {
            def = new ThoughtDef
            {
                defName = "TestGeneThought", validWhileDespawned = true, modExtensions = [new DefModExtension_Thought { gene = gene }],
                stages = [new ThoughtStage()],
            },
        };
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
        var worker = new ThoughtWorker_HerdInstinct
        {
            def = new ThoughtDef { defName = "TestHerdThought", stages = Enumerable.Range(0, 4).Select(_ => new ThoughtStage()).ToList() },
        };
        var thought = new Thought_Situational_HerdInstinct { pawn = pawn, def = worker.def };
        Expect.LessThan(thought.MoodOffset(), 0f);
        GenSpawn.Spawn(pawn, new IntVec3(70, 0, 70), Find.CurrentMap);
        Expect.AreEqual(Find.CurrentMap.mapPawns.ColonistsSpawnedCount, Thought_Situational_HerdInstinct.ColonistCount(pawn));
        Expect.IsTrue(worker.CurrentState(pawn).Active);
    }

    [Test]
    public void MilkableThoughtClampsFullnessToLastThoughtStage()
    {
        GeneDef geneDef = GeneDef(new GeneCompProperties_Milkable { hediff = HediffDef(), item = ThingDefOf.Silver, ticksPerFullnessStage = 10 });
        var gene = (GeneWithComps)pawn.genes.AddGene(geneDef, false);
        var worker = new ThoughtWorker_MilkableFullness
        {
            def = new ThoughtDef
            {
                defName = "TestFullnessThought", validWhileDespawned = true, stages = [new ThoughtStage(), new ThoughtStage()],
                modExtensions = [new DefModExtension_Thought { gene = geneDef }],
            },
        };
        Expect.IsFalse(worker.CurrentState(pawn).Active);
        gene.GetComp<GeneComp_Milkable>()!.fullSinceTick = Find.TickManager.TicksGame - 100;
        Expect.AreEqual(1, worker.CurrentState(pawn).StageIndex);
    }

    [Test]
    public void SourceThoughtFormatsSourcePawnFromHediff()
    {
        HediffDef def = HediffDef(new HediffCompProperties_Source());
        def.hediffClass = typeof(HediffWithCompsExt);
        var hediff = (HediffWithCompsExt)pawn.health.AddHediff(def);
        hediff.TryGetComp<HediffComp_Source>().other = pawn;
        var worker = new ThoughtWorker_WithSource { def = new ThoughtDef { hediff = def } };
        string label = worker.PostProcessLabel(pawn, "Source: {SOURCE_label}");
        Expect.IsTrue(label.Contains(pawn.LabelShort));
        Expect.IsFalse(label.Contains("{SOURCE"));
    }

    [Test]
    public void XenosTrackerMultipliesFactorsAddsBonusesAndClearsOnRemoval()
    {
        var damage = new DamageDef();
        var joy = new JoyGiverDef();
        var faction = new FactionDef();
        var thought = new GeneIngestionThoughtOverride { thoughts = [new ThoughtDef()] };
        Gene first = pawn.genes.AddGene(GeneDef(
            new GeneCompProperties_MeleeDamageFactors { factors = [new DamageFactor { damageDef = damage, factor = 2, chanceBonus = 0.1f }] },
            new GeneCompProperties_JoyGiverChances { factors = [new JoyGiverFactor { joyGiver = joy, factor = 0.5f }] },
            new GeneCompProperties_DisableHostility { factions = [faction] },
            new GeneCompProperties_IngestionThoughtOverrides { overrides = [thought] }), false);
        Gene second = pawn.genes.AddGene(GeneDef(
            new GeneCompProperties_MeleeDamageFactors { factors = [new DamageFactor { damageDef = damage, factor = 3, chanceBonus = 0.2f }] },
            new GeneCompProperties_JoyGiverChances { factors = [new JoyGiverFactor { joyGiver = joy, factor = 0.5f }] }), false);
        var tracker = PawnExtraData<GeneTracker_XylXenos>.Get(pawn);
        Expect.AreApproximatelyEqual(6, tracker.meleeDamageFactors![damage]);
        Expect.AreApproximatelyEqual(0.3f, tracker.meleeChanceBonuses![damage]);
        Expect.AreApproximatelyEqual(0.25f, tracker.joyGiverChanceFactors![joy]);
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
