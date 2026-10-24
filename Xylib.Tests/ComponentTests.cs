namespace Xylib.Tests;

[TestFixture(TestType.Playing)]
public sealed class ComponentTests : PawnTestFixture
{
    [Test, LoadIfBiotechActive]
    public void BonusGenesAddsAndRemovesOnlyItsOwnGenes()
    {
        GeneDef existing = GeneDef();
        GeneDef bonus = GeneDef();
        pawn.genes.AddGene(existing, false);
        GeneDef owner = GeneDef(new GeneCompProperties_BonusGenes
        {
            maker = new GeneSetMakerDef { root = new GeneSetMaker_List { genes = [existing, bonus] } },
        });
        Gene gene = pawn.genes.AddGene(owner, true);
        Expect.IsTrue(pawn.genes.Xenogenes.Any(g => g.def == bonus));
        Expect.AreEqual(1, pawn.genes.GenesListForReading.Count(g => g.def == existing));
        pawn.genes.RemoveGene(gene);
        Expect.IsFalse(pawn.genes.GenesListForReading.Any(g => g.def == bonus));
        Expect.IsTrue(pawn.genes.GenesListForReading.Any(g => g.def == existing));
    }

    [Test]
    public void CongenitalHediffsRespondToGenerationEvent()
    {
        HediffDef health = HediffDef();
        pawn.genes.AddGene(GeneDef(new GeneCompProperties_CongenitalHediffs
        {
            hediffs = [new HediffGiver_Event { hediff = health }],
        }), false);
        Expect.IsFalse(pawn.health.hediffSet.HasHediff(health));
        EventManager.Instance.Notify(EventDefOf.PostGenerateInitialHediffs, pawn);
        Expect.IsTrue(pawn.health.hediffSet.HasHediff(health));
    }

    [Test]
    public void PermanentHediffsAreAddedResetAndRemovedWithGene()
    {
        HediffDef health = HediffDef();
        Gene gene = pawn.genes.AddGene(GeneDef(new GeneCompProperties_PermanentHediffs
        {
            hediffs = [new HediffGiver_Event { hediff = health }],
        }), false);
        Hediff hediff = pawn.health.hediffSet.GetFirstHediffOfDef(health);
        Expect.IsNotNull(hediff);
        hediff.Severity = 0.8f;
        gene.Reset();
        Expect.AreApproximatelyEqual(health.initialSeverity, hediff.Severity);
        pawn.genes.RemoveGene(gene);
        Expect.IsFalse(pawn.health.hediffSet.HasHediff(health));
    }

    [Test]
    public void ExtraApparelHonorsKindTagsAndExplicitBypass()
    {
        var apparel = new ThingDef { apparel = new ApparelProperties { tags = ["TestAllowed"] } };
        PawnKindDef original = pawn.kindDef;
        try
        {
            pawn.kindDef = new PawnKindDef { apparelTags = ["TestOther"] };
            Expect.IsFalse(GeneComp_ExtraApparel.ValidApparel(pawn, apparel));
            Expect.IsTrue(GeneComp_ExtraApparel.ValidApparel(pawn, apparel, ignoreRestrictions: true));
            pawn.kindDef.apparelTags = ["TestAllowed"];
            Expect.IsTrue(GeneComp_ExtraApparel.ValidApparel(pawn, apparel));
            pawn.kindDef.apparelDisallowTags = ["TestAllowed"];
            Expect.IsFalse(GeneComp_ExtraApparel.ValidApparel(pawn, apparel));
        }
        finally { pawn.kindDef = original; }
    }

    [Test]
    public void StartingItemsClampStacksAndLimitPossessionsToTwo()
    {
        var item = new ThingDef { stackLimit = 7 };
        var gene = (GeneWithComps)pawn.genes.AddGene(GeneDef(new GeneCompProperties_StartingItems
        {
            items = [new StartingItemOption { item = item, count = new IntRange(20) },
                new StartingItemOption { item = item, count = new IntRange(3) }],
        }), false);
        List<ThingDefCount> possessions = [new(item, 1)];
        EventManager.Instance.Notify(EventDefOf.InGeneratePossessions, pawn, possessions);
        Expect.AreEqual(2, possessions.Count);
        Expect.AreEqual(7, possessions[1].Count);
        gene.GetComp<GeneComp_StartingItems>()!.Notify_InGeneratePossessions(possessions);
        Expect.AreEqual(2, possessions.Count);
    }

    [Test]
    public void GeneticHediffRequiresAnActiveGene()
    {
        GeneDef gene = GeneDef();
        var hediff = (HediffWithComps)HediffMaker.MakeHediff(HediffDef(new HediffCompProperties_Genetic { gene = gene }), pawn);
        var comp = hediff.TryGetComp<HediffComp_Genetic>();
        Expect.IsTrue(comp.CompShouldRemove);
        Gene instance = pawn.genes.AddGene(gene, false);
        Expect.IsFalse(comp.CompShouldRemove);
        pawn.genes.RemoveGene(instance);
        Expect.IsTrue(comp.CompShouldRemove);
    }

    [Test]
    public void SetNeedClampsOnlyInConfiguredStages()
    {
        var props = new HediffCompProperties_SetNeed { need = NeedDefOf.Rest, range = new FloatRange(0.3f, 0.6f) };
        var hediff = (HediffWithComps)HediffMaker.MakeHediff(HediffDef(props), pawn);
        var comp = hediff.TryGetComp<HediffComp_SetNeed>();
        pawn.needs.rest.CurLevel = 0.9f;
        float adjustment = 0;
        comp.CompPostTickInterval(ref adjustment, 15);
        Expect.AreApproximatelyEqual(0.6f, pawn.needs.rest.CurLevel);
        pawn.needs.rest.CurLevel = 0.1f;
        comp.CompPostTickInterval(ref adjustment, 15);
        Expect.AreApproximatelyEqual(0.3f, pawn.needs.rest.CurLevel);
        props.stages = new IntRange(5);
        pawn.needs.rest.CurLevel = 0.1f;
        comp.CompPostTickInterval(ref adjustment, 15);
        Expect.AreApproximatelyEqual(0.1f, pawn.needs.rest.CurLevel);
    }

    [Test]
    public void SourceCompExposesSourcePawnAndOptionalLabel()
    {
        var props = new HediffCompProperties_Source();
        HediffDef def = HediffDef(props);
        def.hediffClass = typeof(HediffWithCompsExt);
        var hediff = (HediffWithCompsExt)HediffMaker.MakeHediff(def, pawn);
        var comp = hediff.TryGetComp<HediffComp_Source>();
        comp.other = pawn;
        Expect.ReferencesAreEqual(pawn, hediff.SourcePawn);
        Expect.AreEqual(pawn.LabelShort, comp.CompLabelInBracketsExtra);
        props.showName = false;
        Expect.IsNull(comp.CompLabelInBracketsExtra);
    }

    [Test]
    public void ForceMentalStateStartsAndEndsStateWithHediff()
    {
        HediffDef def = HediffDef(new HediffCompProperties_ForceMentalState { mentalState = MentalStateDefOf.Berserk });
        var hediff = (HediffWithComps)pawn.health.AddHediff(def);
        Expect.AreEqual(MentalStateDefOf.Berserk, pawn.MentalStateDef);
        Expect.IsFalse(hediff.TryGetComp<HediffComp_ForceMentalState>().CompShouldRemove);
        pawn.health.RemoveHediff(hediff);
        Expect.IsFalse(pawn.InMentalState);
    }

    [Test]
    public void DebugHediffGizmoInvokesConfiguredGiver()
    {
        HediffDef applied = HediffDef();
        HediffDef def = HediffDef(new HediffCompProperties { compClass = typeof(HediffComp_DebugHediffGivers) });
        def.stages = [new HediffStage { hediffGivers = [new HediffGiver_Event { hediff = applied }] }];
        var hediff = (HediffWithComps)HediffMaker.MakeHediff(def, pawn);
        bool original = DebugSettings.godMode;
        bool dev = Prefs.DevMode;
        try
        {
            Prefs.DevMode = true;
            DebugSettings.godMode = true;
            var command = (Command_Action)hediff.TryGetComp<HediffComp_DebugHediffGivers>().CompGetGizmos().Single();
            command.action();
            Expect.IsTrue(pawn.health.hediffSet.HasHediff(applied));
        }
        finally { DebugSettings.godMode = original; Prefs.DevMode = dev; }
    }

    [Test]
    public void RandomHediffGiverInheritsSeverityAndRejectsDuplicates()
    {
        HediffDef result = HediffDef();
        Hediff cause = HediffMaker.MakeHediff(HediffDef(), pawn);
        cause.Severity = 0.75f;
        var giver = new HediffGiver_RandomExt { hediff = result, inheritSeverity = true, sendLetter = false };
        Expect.IsTrue(giver.TryApply(pawn, cause));
        Expect.AreApproximatelyEqual(0.75f, pawn.health.hediffSet.GetFirstHediffOfDef(result).Severity);
        Expect.IsFalse(giver.TryApply(pawn, cause));
        Expect.AreEqual(1, pawn.health.hediffSet.hediffs.Count(h => h.def == result));
    }

    [Test]
    public void ExtendedHediffStagesAreClonedAndRebuiltAfterSeverityChanges()
    {
        HediffDef def = HediffDef();
        def.hediffClass = typeof(HediffWithCompsExt);
        def.stages = [new HediffStage { painOffset = 0.1f }, new HediffStage { minSeverity = 0.5f, painOffset = 0.3f }];
        var condition = (HediffWithCompsExt)HediffMaker.MakeHediff(def, pawn);
        HediffStage first = condition.CurStage!;
        Expect.ReferencesAreNotEqual(def.stages[0], first);
        first.painOffset = 9;
        Expect.AreApproximatelyEqual(0.1f, def.stages[0].painOffset);
        condition.Severity = 0.7f;
        Expect.ReferencesAreNotEqual(first, condition.CurStage);
        Expect.AreApproximatelyEqual(0.3f, condition.CurStage!.painOffset);
        condition.Notify_CompStateChange();
        Expect.ReferencesAreNotEqual(first, condition.CurStage);
    }

    [Test]
    public void GeneticStatPartAppliesFactorOnlyWhileGenePresent()
    {
        GeneDef def = GeneDef();
        var part = new StatPart_Genetic { gene = def, factor = 2.5f };
        float value = 4;
        part.TransformValue(StatRequest.For(pawn), ref value);
        Expect.AreApproximatelyEqual(4, value);
        pawn.genes.AddGene(def, false);
        part.TransformValue(StatRequest.For(pawn), ref value);
        Expect.AreApproximatelyEqual(10, value);
    }
}
