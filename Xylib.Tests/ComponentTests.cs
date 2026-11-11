using static Xylib.Tests.TestDefOf;

namespace Xylib.Tests;

[TestFixture(TestType.Playing)]
public sealed class ComponentTests : PawnTestFixture
{
    [Test, LoadIfBiotechActive]
    public void BonusGenesAddsAndRemovesOnlyItsOwnGenes()
    {
        GeneDef existing = XylibTestGene, bonus = XylibTestOtherGene;
        pawn.genes.AddGene(existing, false);
        Gene gene = pawn.genes.AddGene(XylibTestAddBonus, true);
        Expect.IsTrue(pawn.genes.Xenogenes.Any(g => g.def == bonus));
        Expect.AreEqual(1, pawn.genes.GenesListForReading.Count(g => g.def == existing));
        pawn.genes.RemoveGene(gene);
        Expect.IsFalse(pawn.genes.GenesListForReading.Any(g => g.def == bonus));
        Expect.IsTrue(pawn.genes.GenesListForReading.Any(g => g.def == existing));
    }

    [Test, LoadIfBiotechActive]
    public void BonusXenogeneReplacesAllConflictingXenogenesAndPreservesEndogenes()
    {
        GeneDef first = XylibTestConflictA, second = XylibTestConflictB, endogene = XylibTestConflictC;
        GeneDef bonus = XylibTestConflictD, unrelated = XylibTestGene;
        pawn.genes.AddGene(first, true);
        pawn.genes.AddGene(second, true);
        Gene preserved = pawn.genes.AddGene(endogene, false);
        Gene other = pawn.genes.AddGene(unrelated, true);
        Gene gene = pawn.genes.AddGene(XylibTestReplaceXenogenes, true);
        Expect.IsFalse(pawn.genes.GenesListForReading.Any(g => g.def == first || g.def == second));
        Expect.IsTrue(pawn.genes.Xenogenes.Any(g => g.def == bonus));
        Expect.IsTrue(pawn.genes.Endogenes.Contains(preserved));
        Expect.IsTrue(pawn.genes.Xenogenes.Contains(other));
        pawn.genes.RemoveGene(gene);
        Expect.IsFalse(pawn.genes.GenesListForReading.Any(g => g.def == bonus));
        Expect.IsTrue(pawn.genes.Endogenes.Contains(preserved));
        Expect.IsTrue(pawn.genes.Xenogenes.Contains(other));
    }

    [Test, LoadIfBiotechActive]
    public void BonusEndogeneReplacesConflictingEndogenesUsingConfiguredGeneType()
    {
        GeneDef existing = XylibTestConflictA, xenogene = XylibTestConflictB;
        GeneDef bonus = XylibTestConflictC, unrelated = XylibTestGene;
        pawn.genes.AddGene(existing, false);
        Gene preserved = pawn.genes.AddGene(xenogene, true);
        Gene other = pawn.genes.AddGene(unrelated, false);

        pawn.genes.AddGene(XylibTestReplaceEndogenes, true);
        Expect.IsFalse(pawn.genes.Endogenes.Any(g => g.def == existing));
        Expect.IsTrue(pawn.genes.Endogenes.Any(g => g.def == bonus));
        Expect.IsTrue(pawn.genes.Xenogenes.Contains(preserved));
        Expect.IsTrue(pawn.genes.Endogenes.Contains(other));
    }

    [Test, LoadIfBiotechActive]
    public void BonusXenogeneReplacesExistingGeneDespiteLowerVanillaOverridePriority()
    {
        GeneDef existing = Beauty_Ugly;
        GeneDef bonus = Beauty_Beautiful;
        Expect.IsTrue(GeneUtility.Overrides(existing, bonus, true, true));
        Expect.IsFalse(GeneUtility.Overrides(bonus, existing, true, true));
        Gene oldGene = pawn.genes.AddGene(existing, true);

        pawn.genes.AddGene(XylibTestBonusBeauty, true);

        Expect.IsFalse(pawn.genes.GenesListForReading.Contains(oldGene));
        Expect.IsTrue(pawn.genes.Xenogenes.Single(g => g.def == bonus).Active);
    }

    [Test, LoadIfBiotechActive]
    public void BonusEndogeneReplacesExistingGeneDespiteLowerVanillaOverridePriority()
    {
        GeneDef existing = Beauty_Ugly;
        GeneDef bonus = Beauty_Beautiful;
        Expect.IsTrue(GeneUtility.Overrides(existing, bonus, false, false));
        Expect.IsFalse(GeneUtility.Overrides(bonus, existing, false, false));
        Gene oldGene = pawn.genes.AddGene(existing, false);

        pawn.genes.AddGene(XylibTestBonusBeauty, false);

        Expect.IsFalse(pawn.genes.GenesListForReading.Contains(oldGene));
        Expect.IsTrue(pawn.genes.Endogenes.Single(g => g.def == bonus).Active);
    }

    [Test, LoadIfBiotechActive]
    public void BonusCookingAptitudeReplacesTerribleAptitudeDuringXenotypeApplication()
    {
        GeneDef existing = AptitudeTerrible_Cooking;
        GeneDef bonus = AptitudePoor_Cooking;
        Expect.IsTrue(bonus.ConflictsWith(existing));
        Expect.IsFalse(pawn.skills.GetSkill(SkillDefOf.Cooking).TotallyDisabled);
        GeneDef owner = XylibTestBonusCooking;
        // Keep the aptitude first. XML cross-references can append implied genes after ordinary genes.
        pawn.genes.SetXenotype(new XenotypeDef { inheritable = true, genes = [existing, owner] });

        Expect.IsFalse(pawn.genes.GenesListForReading.Any(g => g.def == existing));
        Expect.IsTrue(pawn.genes.Endogenes.Single(g => g.def == bonus).Active);
        Expect.IsFalse(pawn.genes.GenesListForReading.Any(g => g.def == owner));
    }

    [Test, LoadIfBiotechActive]
    public void DeferredBonusGenesResolveNestedBonusesAfterBaseGenes()
    {
        GeneDef existing = XylibTestConflictA, bonus = XylibTestConflictB;
        GeneDef nested = XylibTestNestedBonus, owner = XylibTestDeferredBonus;

        Pawn generated = PawnGenerator.GeneratePawn(new PawnGenerationRequest(PawnKindDefOf.Colonist,
            Faction.OfPlayer, fixedBiologicalAge: 25, fixedChronologicalAge: 25,
            canGeneratePawnRelations: false, forceNoGear: true, forcedXenotype: XenotypeDefOf.Baseliner,
            forcedCustomXenotype: new CustomXenotype { name = "nested bonuses", inheritable = true, genes = [owner, existing] }));
        things.Add(generated);

        Expect.IsTrue(generated.genes.Endogenes.Single(g => g.def == bonus).Active);
        Expect.IsFalse(generated.genes.GenesListForReading.Any(g => g.def == existing || g.def == nested || g.def == owner));
    }

    [Test, LoadIfBiotechActive]
    public void BonusGenesApplyToPawnLoadedFromScribe()
    {
        GeneDef bonus = XylibTestOtherGene, owner = XylibTestScribedBonus;
        string path = Path.Combine(GenFilePaths.SaveDataFolderPath, "bonus-genes-pawn-test.xml");
        ProgramState previousState = Current.ProgramState;
        Pawn saved = pawn;
        Ideo? ideology = saved.Ideo;
        try
        {
            Scribe.saver.InitSaving(path, "BonusGenesPawnTest");
            Scribe_Deep.Look(ref saved, "pawn");
            Scribe.saver.FinalizeSaving();

            // Remove the original pawn and its transient data, as ending its game would.
            pawn.Destroy();
            if (Find.WorldPawns.Contains(pawn))
                Find.WorldPawns.RemovePawn(pawn);
            pawn.Discard(silentlyRemoveReferences: true);
            things.Remove(pawn);

            // Game.LoadGame uses this state to suppress live life-stage transitions while loading.
            Current.ProgramState = ProgramState.MapInitializing;
            Pawn restored = null!;
            Scribe.loader.InitLoading(path);
            Scribe_Deep.Look(ref restored, "pawn");
            things.Add(restored);
            var directory = (LoadedObjectDirectory)typeof(CrossRefHandler)
                .GetField("loadedObjectDirectory", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
                .GetValue(Scribe.loader.crossRefs)!;
            directory.RegisterLoaded(Faction.OfPlayer);
            if (ideology != null)
                directory.RegisterLoaded(ideology);
            Scribe.loader.FinalizeLoading();
            Current.ProgramState = previousState;

            Find.WorldPawns.PassToWorld(restored, RimWorld.Planet.PawnDiscardDecideMode.KeepForever);
            EventManager.Instance.LoadedGame();
            restored.genes.AddGene(owner, false);

            Expect.IsTrue(restored.genes.Endogenes.Any(g => g.def == bonus),
                "A loaded pawn has completed base gene generation, so newly added bonus genes must apply immediately.");
        }
        finally
        {
            Scribe.ForceStop();
            Current.ProgramState = previousState;
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Test, LoadIfBiotechActive]
    public void PostGenerateGenesDoesNotRepeatAlreadyAppliedBonuses()
    {
        GeneDef first = XylibTestGene, second = XylibTestOtherGene;
        var maker = new GeneSetMakerDef { root = new GeneSetMaker_List { genes = [first] } };
        GeneDef owner = CreateMutableGeneDef(new GeneCompProperties_BonusGenes { maker = maker });
        pawn.genes.AddGene(owner, false);
        Gene originalBonus = pawn.genes.Endogenes.Single(g => g.def == first);

        maker.root = new GeneSetMaker_List { genes = [second] };
        EventManager.Instance.Notify(EventDefOf.PostGenerateGenes, pawn);
        EventManager.Instance.Notify(EventDefOf.PostGenerateGenes, pawn);

        Expect.IsFalse(pawn.genes.GenesListForReading.Any(g => g.def == second));
        Expect.ReferencesAreEqual(originalBonus, pawn.genes.Endogenes.Single(g => g.def == first));
    }

    [Test]
    public void CongenitalHediffsRespondToGenerationEvent()
    {
        HediffDef health = XylibTestHediff;
        pawn.genes.AddGene(XylibTestCongenitalHediffGene, false);
        Expect.IsFalse(pawn.health.hediffSet.HasHediff(health));
        EventManager.Instance.Notify(EventDefOf.PostGenerateInitialHediffs, pawn);
        Expect.IsTrue(pawn.health.hediffSet.HasHediff(health));
    }

    [Test]
    public void PermanentHediffsAreAddedResetAndRemovedWithGene()
    {
        HediffDef health = XylibTestHediff;
        Gene gene = pawn.genes.AddGene(XylibTestPermanentHediffGene, false);
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
        ThingDef apparel = ThingDefOf.Apparel_Parka;
        PawnKindDef original = pawn.kindDef;
        try
        {
            pawn.kindDef = XylibTestOtherApparel;
            Expect.IsFalse(GeneComp_ExtraApparel.ValidApparel(pawn, apparel));
            Expect.IsTrue(GeneComp_ExtraApparel.ValidApparel(pawn, apparel, ignoreRestrictions: true));
            pawn.kindDef = PawnKindDefOf.Colonist;
            Expect.IsTrue(GeneComp_ExtraApparel.ValidApparel(pawn, apparel));
            pawn.kindDef = XylibTestDisallowApparel;
            Expect.IsFalse(GeneComp_ExtraApparel.ValidApparel(pawn, apparel));
        }
        finally { pawn.kindDef = original; }
    }

    [Test]
    public void StartingItemsClampStacksAndLimitPossessionsToTwo()
    {
        ThingDef item = XylibTestSmallStack;
        var gene = (GeneWithComps)pawn.genes.AddGene(XylibTestStartingItems, false);
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
        GeneDef gene = XylibTestGene;
        var hediff = (HediffWithComps)HediffMaker.MakeHediff(XylibTestGeneticHediff, pawn);
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
        var hediff = (HediffWithComps)HediffMaker.MakeHediff(XylibTestSetNeed, pawn);
        var comp = hediff.TryGetComp<HediffComp_SetNeed>();
        pawn.needs.rest.CurLevel = 0.9f;
        float adjustment = 0;
        comp.CompPostTickInterval(ref adjustment, 15);
        Expect.AreApproximatelyEqual(0.6f, pawn.needs.rest.CurLevel);
        pawn.needs.rest.CurLevel = 0.1f;
        comp.CompPostTickInterval(ref adjustment, 15);
        Expect.AreApproximatelyEqual(0.3f, pawn.needs.rest.CurLevel);
        hediff.Severity = 0.8f;
        pawn.needs.rest.CurLevel = 0.1f;
        comp.CompPostTickInterval(ref adjustment, 15);
        Expect.AreApproximatelyEqual(0.1f, pawn.needs.rest.CurLevel);
    }

    [Test]
    public void SourceCompExposesSourcePawnAndOptionalLabel()
    {
        var hediff = (HediffWithCompsExt)HediffMaker.MakeHediff(XylibTestVisibleSource, pawn);
        var comp = hediff.TryGetComp<HediffComp_Source>();
        comp.other = pawn;
        Expect.ReferencesAreEqual(pawn, hediff.SourcePawn);
        Expect.AreEqual(pawn.LabelShort, comp.CompLabelInBracketsExtra);
        var hidden = (HediffWithCompsExt)HediffMaker.MakeHediff(XylibTestHiddenSource, pawn);
        var hiddenComp = hidden.TryGetComp<HediffComp_Source>();
        hiddenComp.other = pawn;
        Expect.IsNull(hiddenComp.CompLabelInBracketsExtra);
    }

    [Test]
    public void ForceMentalStateStartsAndEndsStateWithHediff()
    {
        HediffDef def = XylibTestBerserkHediff;
        var hediff = (HediffWithComps)pawn.health.AddHediff(def);
        Expect.AreEqual(MentalStateDefOf.Berserk, pawn.MentalStateDef);
        Expect.IsFalse(hediff.TryGetComp<HediffComp_ForceMentalState>().CompShouldRemove);
        pawn.health.RemoveHediff(hediff);
        Expect.IsFalse(pawn.InMentalState);
    }

    [Test]
    public void DebugHediffGizmoInvokesConfiguredGiver()
    {
        HediffDef applied = XylibTestHediff, def = XylibTestDebugGiver;
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
        HediffDef result = XylibTestHediff;
        Hediff cause = HediffMaker.MakeHediff(XylibTestOtherHediff, pawn);
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
        HediffDef def = XylibTestExtendedStages;
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
        GeneDef def = XylibTestGene;
        var part = new StatPart_Genetic { gene = def, factor = 2.5f };
        float value = 4;
        part.TransformValue(StatRequest.For(pawn), ref value);
        Expect.AreApproximatelyEqual(4, value);
        pawn.genes.AddGene(def, false);
        part.TransformValue(StatRequest.For(pawn), ref value);
        Expect.AreApproximatelyEqual(10, value);
    }
}
