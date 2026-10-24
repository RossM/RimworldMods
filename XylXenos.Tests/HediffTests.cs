namespace XylXenos.Tests;

[TestFixture(TestType.Playing)]
public sealed class HediffTests : PawnTestFixture
{
    [Test]
    public void RemovalCompTransfersSeverityAndBodyPart()
    {
        HediffDef replacement = HediffDef();
        HediffDef def = HediffDef(new HediffCompProperties_GiveHediffOnRemoval { hediff = replacement, inheritSeverity = true });
        BodyPartRecord part = pawn.RaceProps.body.corePart;
        Hediff condition = pawn.health.AddHediff(def, part);
        condition.Severity = 0.65f;
        pawn.health.RemoveHediff(condition);
        Hediff result = pawn.health.hediffSet.GetFirstHediffOfDef(replacement);
        Expect.AreApproximatelyEqual(0.65f, result.Severity);
        Expect.ReferencesAreEqual(part, result.Part);
    }

    [Test]
    public void GrowthModesChangeSeverityRateAndModifyInstanceStageOnly()
    {
        var first = new GrowthMode { severityPerDay = 0.2f, causesNoPain = true, enableHediffGivers = false, allowTend = false };
        var second = new GrowthMode { severityPerDay = -0.1f };
        HediffDef def = HediffDef(new HediffCompProperties_GrowthModeExt { modes = [first, second] });
        def.hediffClass = typeof(HediffWithCompsExt);
        def.tendable = true;
        def.stages = [new HediffStage { painOffset = 0.3f, hediffGivers = [new HediffGiver_Event { hediff = HediffDef() }] }];
        var condition = (HediffWithCompsExt)pawn.health.AddHediff(def);
        var comp = condition.TryGetComp<HediffComp_GrowthModeExt>();
        Expect.AreApproximatelyEqual(0.2f, comp.SeverityChangePerDay());
        Expect.AreApproximatelyEqual(0, condition.CurStage!.painOffset);
        Expect.IsNull(condition.CurStage.hediffGivers);
        Expect.IsFalse(condition.TendableNow());
        Expect.AreApproximatelyEqual(0.3f, def.stages[0].painOffset);
        Expect.IsNotEmpty(def.stages[0].hediffGivers);
        comp.ChangeGrowthMode();
        Expect.ReferencesAreEqual(second, comp.GrowthMode);
        Expect.AreApproximatelyEqual(-0.1f, comp.SeverityChangePerDay());
        Expect.AreApproximatelyEqual(0.3f, condition.CurStage.painOffset);
    }

    [Test]
    public void PetrificationTendingSwitchesGrowthModeAtQualityThreshold()
    {
        GrowthMode first = new(), second = new();
        HediffDef def = HediffDef(new HediffCompProperties_GrowthModeExt { modes = [first, second] },
            new HediffCompProperties_PetrificationTendDuration { changeModeAtTotalTendQuality = 0.5f });
        def.hediffClass = typeof(HediffWithCompsExt);
        var condition = (HediffWithCompsExt)pawn.health.AddHediff(def);
        var growth = condition.TryGetComp<HediffComp_GrowthModeExt>();
        var tend = condition.TryGetComp<HediffComp_PetrificationTendDuration>();
        tend.CompTended(0.75f, 1f, 1);
        float adjustment = 0;
        tend.CompPostTickInterval(ref adjustment, 15);
        Expect.ReferencesAreEqual(second, growth.GrowthMode);
        tend.CompPostTickInterval(ref adjustment, 15);
        Expect.ReferencesAreEqual(second, growth.GrowthMode, "Consumed tend quality must not switch mode twice.");
    }

    [Test]
    public void SaturationGainsWithSourceAndLosesWithoutIt()
    {
        HediffDef source = HediffDef();
        var condition = (HediffWithComps)HediffMaker.MakeHediff(HediffDef(new HediffCompProperties_Saturation
        {
            sourceHediff = source, severityGainPerDay = 0.8f, severityLossPerDay = -0.3f,
        }), pawn);
        var comp = condition.TryGetComp<HediffComp_Saturation>();
        Expect.AreApproximatelyEqual(-0.3f, comp.SeverityChangePerDay());
        Hediff effect = pawn.health.AddHediff(source);
        Expect.AreApproximatelyEqual(0.8f, comp.SeverityChangePerDay());
        pawn.health.RemoveHediff(effect);
        Expect.AreApproximatelyEqual(-0.3f, comp.SeverityChangePerDay());
    }

    [Test]
    public void StageRecoveryLabelMeasuresRemainingCurrentStage()
    {
        HediffDef def = HediffDef(new HediffCompProperties_SeverityPerDay_Stage
        {
            severityPerDay = -0.5f, showHoursToRecoverStages = new IntRange(1),
        });
        def.stages = [new HediffStage(), new HediffStage { minSeverity = 0.5f }];
        var condition = (HediffWithComps)pawn.health.AddHediff(def);
        condition.Severity = 0.75f;
        var comp = condition.TryGetComp<HediffComp_SeverityPerDay_Stage>();
        Expect.AreEqual(("12" + "LetterHour".Translate()).ToString(), comp.CompLabelInBracketsExtra);
        condition.Severity = 0.25f;
        Expect.IsNull(comp.CompLabelInBracketsExtra);
    }

    [Test]
    public void BioRejectionCountsImplantsRatherThanStoredSeverity()
    {
        HediffDef def = HediffDef();
        def.hediffClass = typeof(Hediff_BioRejection);
        var condition = (Hediff_BioRejection)pawn.health.AddHediff(def);
        Expect.AreApproximatelyEqual(0, condition.Severity);
        HediffDef implant = HediffDef();
        implant.countsAsAddedPartOrImplant = true;
        Hediff added = pawn.health.AddHediff(implant, pawn.RaceProps.body.corePart);
        Expect.AreApproximatelyEqual(1, condition.Severity);
        condition.Severity = 99;
        Expect.AreApproximatelyEqual(1, condition.Severity);
        pawn.health.RemoveHediff(added);
        Expect.AreApproximatelyEqual(0, condition.Severity);
        Expect.IsFalse(condition.ShouldRemove);
    }

    [Test]
    public void PetrifiedFleshClampsSeverityToPartHealthAndUsesRelativeStages()
    {
        HediffDef def = HediffDef();
        def.hediffClass = typeof(Hediff_PetrifiedFlesh);
        def.maxSeverity = float.MaxValue;
        def.stages = [new HediffStage(), new HediffStage { minSeverity = 0.5f }];
        BodyPartRecord part = pawn.RaceProps.body.corePart;
        var condition = (Hediff_PetrifiedFlesh)pawn.health.AddHediff(def, part);
        condition.Severity = part.def.GetMaxHealth(pawn) * 2f;
        Expect.AreApproximatelyEqual(1, condition.RelativeSeverity);
        Expect.AreEqual(1, condition.CurStageIndex);
        condition.Severity = part.def.GetMaxHealth(pawn) * 0.25f;
        Expect.AreEqual(0, condition.CurStageIndex);
    }

    [Test]
    public void DietDependencyUsesConfiguredNutritionConversion()
    {
        HediffDef def = HediffDef();
        def.hediffClass = typeof(Hediff_DietDependency);
        def.modExtensions = [new DefModExtension_Hediff_DietDependency { severityReductionPerNutrition = 2 }];
        def.stages = Enumerable.Range(0, Hediff_DietDependency.StageCount).Select(i => new HediffStage { minSeverity = i }).ToList();
        var condition = (Hediff_DietDependency)pawn.health.AddHediff(def);
        condition.Severity = 3;
        Expect.IsTrue(condition.ShouldSatisfy);
        Expect.AreApproximatelyEqual(1.5f, condition.NutritionWantedToSatisfy());
        Thing food = ThingMaker.MakeThing(ThingDefOf.MealSimple);
        things.Add(food);
        float nutrition = FoodUtility.NutritionForEater(pawn, food);
        Expect.IsTrue(condition.ValidateFood(food));
        condition.Notify_IngestedThing(food, 1);
        Expect.AreApproximatelyEqual(3 - nutrition * 2, condition.Severity);
        EventManager.Instance.Notify(EventDefOf.PostSatisfyChemicalGenes, pawn);
        Expect.AreApproximatelyEqual(0, condition.Severity);
        Expect.IsFalse(condition.ShouldRemove);
    }

    [Test]
    public void SubstituteCapacityValidatesCapacityAndStatExclusions()
    {
        HediffDef def = HediffDef();
        def.hediffClass = typeof(Hediff_SubstituteCapacity);
        def.modExtensions = [new DefModExtension_Hediff_SubstituteCapacity
        {
            originalCapacity = PawnCapacityDefOf.Sight, substituteCapacity = PawnCapacityDefOf.Hearing,
            mode = DefModExtension_Hediff_SubstituteCapacity.SubstitutionMode.Always,
            excludeStats = [StatDefOf.MoveSpeed],
        }];
        var condition = (Hediff_SubstituteCapacity)pawn.health.AddHediff(def);
        Expect.IsTrue(condition.Validate(StatDefOf.WorkSpeedGlobal, PawnCapacityDefOf.Sight));
        Expect.IsFalse(condition.Validate(StatDefOf.WorkSpeedGlobal, PawnCapacityDefOf.Hearing));
        Expect.IsFalse(condition.Validate(StatDefOf.MoveSpeed, PawnCapacityDefOf.Sight));
    }

    [Test]
    public void LovinNeedFallsAndAddictionStageTracksSatisfaction()
    {
        var needDef = new NeedDef { defName = "TestLovinNeed", label = "test need", needClass = typeof(Need_Lovin), fallPerDay = 0.4f };
        var need = new Need_Lovin(pawn) { def = needDef };
        pawn.needs.AllNeeds.Add(need);
        try
        {
            HediffDef def = HediffDef();
            def.hediffClass = typeof(Hediff_LovinAddiction);
            def.stages = [new HediffStage(), new HediffStage()];
            var condition = (Hediff_LovinAddiction)pawn.health.AddHediff(def);
            need.CurLevel = 0.5f;
            need.NeedInterval();
            Expect.AreApproximatelyEqual(0.5f - 0.4f * 150 / GenDate.TicksPerDay, need.CurLevel);
            Expect.AreEqual(0, condition.CurStageIndex);
            need.CurLevel = 0;
            Expect.IsFalse(need.Satisfied);
            Expect.AreEqual(1, condition.CurStageIndex);
            need.SetInitialLevel();
            Expect.IsTrue(need.Satisfied);
            Expect.AreEqual(0, condition.CurStageIndex);
        }
        finally { pawn.needs.AllNeeds.Remove(need); }
    }

    [Test]
    public void TorporGiverGainsInColdAndRecoversInWarmth()
    {
        GenSpawn.Spawn(pawn, new IntVec3(70, 0, 70), Find.CurrentMap);
        HediffDef def = HediffDef();
        var giver = new HediffGiver_Torpor
        {
            hediff = def, temperatureThreshold = pawn.AmbientTemperature + 10,
            comfyTemperatureImportance = 0, severityGainPerDayPerDegree = 1, severityLossPerDayPerDegree = 1,
        };
        giver.OnIntervalPassed(pawn, null);
        Hediff condition = pawn.health.hediffSet.GetFirstHediffOfDef(def);
        Expect.IsNotNull(condition);
        float before = condition.Severity;
        giver.temperatureThreshold = pawn.AmbientTemperature - 10;
        giver.OnIntervalPassed(pawn, null);
        Expect.LessThan(condition.Severity, before);
    }

    [Test]
    public void RandomNotInSpaceUsesOrdinaryChanceForUnspawnedPawn()
    {
        var giver = new HediffGiver_RandomNotInSpace { hediff = HediffDef() };
        Expect.AreApproximatelyEqual(new HediffGiver_Random().ChanceFactor(pawn), giver.ChanceFactor(pawn));
    }

    [Test]
    public void ParthenogenesisRejectsSterilizedPawn()
    {
        var giver = new HediffGiver_Parthenogenesis();
        Expect.GreaterThan(giver.ChanceFactor(pawn), 0f);
        pawn.health.AddHediff(HediffDefOf.Sterilized);
        Expect.AreApproximatelyEqual(0, giver.ChanceFactor(pawn));
    }
}
