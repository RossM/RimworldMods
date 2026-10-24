using System.Reflection;
using Verse.AI;

namespace XylXenos.Tests;

[TestFixture(TestType.Playing)]
public sealed class AbilityAndAiTests : PawnTestFixture
{
    [Test]
    public void SonicWaveStunsTargetButNeverCaster()
    {
        Map map = Find.CurrentMap;
        GenSpawn.Spawn(pawn, new IntVec3(70, 0, 70), map);
        Pawn target = PawnGenerator.GeneratePawn(new PawnGenerationRequest(PawnKindDefOf.Colonist,
            Faction.OfPlayer, canGeneratePawnRelations: false, forcedXenotype: XenotypeDefOf.Baseliner));
        things.Add(target);
        GenSpawn.Spawn(target, new IntVec3(71, 0, 70), map);
        var comp = new CompAbilityEffect_SonicWave
        {
            parent = new Ability(pawn),
            props = new CompProperties_AbilitySonicWave { radius = 3, durationSecondsOverride = new FloatRange(2) },
        };
        comp.Apply(target, target);
        Expect.IsTrue(target.stances.stunner.Stunned);
        Expect.IsFalse(pawn.stances.stunner.Stunned);
    }

    [Test]
    public void ToxicBurstAiRequiresExposedHostilePawnInRadius()
    {
        Map map = Find.CurrentMap;
        GenSpawn.Spawn(pawn, new IntVec3(70, 0, 70), map);
        var comp = new CompAbilityEffect_ToxicBurst
        {
            parent = new Ability(pawn), props = new CompProperties_AbilityToxicBurst { AIUseRadius = 2 },
        };
        Expect.IsFalse(comp.AICanTargetNow(pawn));
        Pawn enemy = PawnGenerator.GeneratePawn(new PawnGenerationRequest(PawnKindDefOf.Colonist,
            Faction.OfAncientsHostile, canGeneratePawnRelations: false, forcedXenotype: XenotypeDefOf.Baseliner));
        things.Add(enemy);
        GenSpawn.Spawn(enemy, new IntVec3(71, 0, 70), map);
        Expect.IsTrue(comp.AICanTargetNow(enemy));
        enemy.Position = new IntVec3(76, 0, 70);
        Expect.IsFalse(comp.AICanTargetNow(enemy));
    }

    [Test]
    public void RockTossRejectsThingsOutsideChunkCategory()
    {
        var comp = new CompAbilityEffect_RockToss
        {
            parent = new Ability(pawn), props = new CompProperties_AbilityRockToss { projectileDef = new ThingDef() },
        };
        Thing item = ThingMaker.MakeThing(ThingDefOf.Silver);
        things.Add(item);
        Expect.IsFalse(comp.Valid(new LocalTargetInfo(item)));
    }

    [Test]
    public void BlindDamageAppliesHediffToEverySightSource()
    {
        HediffDef blindness = HediffDef();
        var damage = new DamageDef { hediff = blindness };
        BodyPartRecord[] eyes = pawn.health.hediffSet.GetNotMissingParts(tag: BodyPartTagDefOf.SightSource).ToArray();
        Expect.IsNotEmpty(eyes);
        new DamageWorker_Blind().Apply(new DamageInfo(damage, 0.7f), pawn);
        foreach (BodyPartRecord eye in eyes)
        {
            Hediff effect = pawn.health.hediffSet.hediffs.Single(h => h.def == blindness && h.Part == eye);
            Expect.AreApproximatelyEqual(0.7f, effect.Severity);
        }
    }

    [Test]
    public void GeneConditionalTracksPresenceAndCopiesItsConfiguration()
    {
        GeneDef gene = GeneDef();
        var node = new ThinkNode_ConditionalHasGene { gene = gene };
        MethodInfo satisfied = typeof(ThinkNode_ConditionalHasGene).GetMethod("Satisfied", BindingFlags.Instance | BindingFlags.NonPublic)!;
        Expect.IsFalse((bool)satisfied.Invoke(node, [pawn])!);
        pawn.genes.AddGene(gene, false);
        Expect.IsTrue((bool)satisfied.Invoke(node, [pawn])!);
        Expect.ReferencesAreEqual(gene, ((ThinkNode_ConditionalHasGene)node.DeepCopy(false)).gene);
    }

    [Test]
    public void EntropyConditionalChecksThresholdAndCopiesIt()
    {
        if (!ModsConfig.RoyaltyActive) { Test.Skip("Requires Royalty."); return; }
        var node = new ThinkNode_ConditionalPsychicEntropy { maximum = -1 };
        MethodInfo satisfied = typeof(ThinkNode_ConditionalPsychicEntropy).GetMethod("Satisfied", BindingFlags.Instance | BindingFlags.NonPublic)!;
        Expect.IsFalse((bool)satisfied.Invoke(node, [pawn])!);
        node.maximum = pawn.psychicEntropy.EntropyValue;
        Expect.IsTrue((bool)satisfied.Invoke(node, [pawn])!);
        Expect.AreApproximatelyEqual(node.maximum, ((ThinkNode_ConditionalPsychicEntropy)node.DeepCopy(false)).maximum);
    }

    [Test]
    public void AbilityTargetAiCanSelectSelfAndCopiesTargetSettings()
    {
        GenSpawn.Spawn(pawn, new IntVec3(70, 0, 70), Find.CurrentMap);
        var ability = new Ability(pawn)
        {
            def = new AbilityDef { verbProperties = new VerbProperties { targetParams = new TargetingParameters { canTargetPawns = true, canTargetSelf = true } } },
        };
        var node = new JobGiver_AICastAbilityOnTarget { targetSelf = true };
        var target = (LocalTargetInfo)typeof(JobGiver_AICastAbilityOnTarget).GetMethod("GetTarget", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(node, [pawn, ability])!;
        Expect.ReferencesAreEqual(pawn, target.Thing);
        var copy = (JobGiver_AICastAbilityOnTarget)node.DeepCopy(false);
        Expect.IsTrue(copy.targetSelf);
        Expect.IsFalse(copy.targetAllies);
    }

    [Test]
    public void WetnessJobGiverUsesWaterAtCurrentPosition()
    {
        Map map = Find.CurrentMap;
        IntVec3 cell = new(70, 0, 70);
        TerrainDef original = cell.GetTerrain(map);
        try
        {
            map.terrainGrid.SetTerrain(cell, TerrainDefOf.WaterShallow);
            map.fogGrid.Unfog(cell);
            GenSpawn.Spawn(pawn, cell, map);
            var jobDef = new JobDef { defName = "TestSoak", driverClass = typeof(JobDriver_Soak) };
            var node = new JobGiver_GetWetness { soakJobDef = jobDef };
            Expect.AreApproximatelyEqual(1, Need_Wetness.GetWetness(cell, map));
            Job job = node.TryIssueJobPackage(pawn, default).Job;
            Expect.IsNotNull(job);
            Expect.ReferencesAreEqual(jobDef, job.def);
            Expect.AreEqual(cell, job.targetA.Cell);
            Expect.IsTrue(new JobDriver_Soak().TryMakePreToilReservations(false));
            JobMaker.ReturnToPool(job);
        }
        finally { map.terrainGrid.SetTerrain(cell, original); }
    }

    [Test]
    public void DietJobGiverPrioritizesCravingAndSelectsInventoryFood()
    {
        GenSpawn.Spawn(pawn, new IntVec3(70, 0, 70), Find.CurrentMap);
        HediffDef def = HediffDef();
        def.hediffClass = typeof(Hediff_DietDependency);
        def.modExtensions = [new DefModExtension_Hediff_DietDependency { severityReductionPerNutrition = 1 }];
        def.stages = Enumerable.Range(0, Hediff_DietDependency.StageCount).Select(i => new HediffStage { minSeverity = i }).ToList();
        var node = new JobGiver_SatisfyDietDependency();
        Expect.AreApproximatelyEqual(0, node.GetPriority(pawn));
        Hediff condition = pawn.health.AddHediff(def);
        condition.Severity = 2;
        Thing food = ThingMaker.MakeThing(ThingDefOf.MealSimple);
        food.stackCount = 4;
        pawn.inventory.innerContainer.TryAdd(food);
        Expect.GreaterThan(node.GetPriority(pawn), 0f);
        Job job = node.TryIssueJobPackage(pawn, default).Job;
        Expect.ReferencesAreEqual(food, job.targetA.Thing);
        Expect.AreEqual(JobDefOf.Ingest, job.def);
        Expect.AreEqual(Math.Min(4, UnityEngine.Mathf.CeilToInt(2 / FoodUtility.NutritionForEater(pawn, food))), job.count);
        JobMaker.ReturnToPool(job);
    }

    [Test]
    public void HuntingVerminRequiresMentalStateAndReachablePrey()
    {
        var state = new MentalState_HuntingVermin { pawn = pawn };
        Expect.IsNull(MentalState_HuntingVermin.FindPawnToKill(pawn));
        Expect.IsFalse(state.IsTargetStillValidAndReachable());
        Expect.IsNull(new JobGiver_HuntingVermin().TryIssueJobPackage(pawn, default).Job);
        Expect.AreEqual(RandomSocialMode.Off, state.SocialModeMax());
    }

    [Test]
    public void SeeingRedMentalStateUsesGeneEnemySet()
    {
        var gene = (GeneWithComps)pawn.genes.AddGene(GeneDef(new GeneCompProperties_SeeingRed { hediffDef = HediffDef() }), false);
        Thing target = ThingMaker.MakeThing(ThingDefOf.Silver);
        things.Add(target);
        var state = new MentalState_SeeingRed { pawn = pawn };
        Expect.IsFalse(state.ForceHostileTo(target));
        gene.GetComp<GeneComp_SeeingRed>()!.extraEnemies = [target];
        Expect.IsTrue(state.ForceHostileTo(target));
        Expect.AreEqual(RandomSocialMode.Off, state.SocialModeMax());
    }

    [Test]
    public void MilkingDriverAndWorkGiverRequireReadyGeneComp()
    {
        HediffDef def = HediffDef(new HediffCompProperties_Chargeable { fullChargeAmount = 1, initialCharge = 1 });
        pawn.health.AddHediff(def);
        var gene = (GeneWithComps)pawn.genes.AddGene(GeneDef(new GeneCompProperties_Milkable { hediff = def, item = ThingDefOf.Silver }), false);
        var comp = gene.GetComp<GeneComp_Milkable>()!;
        var driver = new JobDriver_MilkHuman();
        Expect.IsFalse(driver.ValidateTarget(pawn));
        comp.lastMilkedTick = Find.TickManager.TicksGame - 100;
        comp.milkingCooldownDays = 0;
        Expect.IsTrue(driver.ValidateTarget(pawn));
        comp.allowMilking = false;
        Expect.IsFalse(driver.ValidateTarget(pawn));
        var workGiver = new WorkGiver_InteractWithPawn();
        Expect.IsFalse(workGiver.HasJobOnThing(pawn, pawn));
        Expect.IsFalse(workGiver.HasJobOnThing(pawn, new Thing()));
    }
}
