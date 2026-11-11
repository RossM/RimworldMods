using static XylXenos.Tests.TestDefOf;

namespace XylXenos.Tests;

[TestFixture(TestType.Playing)]
public sealed class GeneCompTests : PawnTestFixture
{
    [Test]
    public void BonusGenesReplaceLaterCookingGeneDuringPawnGeneration()
    {
        GeneDef existing = AptitudeTerrible_Cooking;
        GeneDef bonus = AptitudePoor_Cooking;
        GeneDef owner = XylXenosTestBonusCooking;
        // Template-generated genes can be appended after the bonus source when XML references resolve.
        var xenotype = new CustomXenotype { name = "bonus cooking", inheritable = true, genes = [owner, existing] };

        Pawn generated = PawnGenerator.GeneratePawn(new PawnGenerationRequest(PawnKindDefOf.Colonist,
            Faction.OfPlayer, forceGenerateNewPawn: true,
            validatorPreGear: candidate => !candidate.skills.GetSkill(SkillDefOf.Cooking).TotallyDisabled,
            fixedBiologicalAge: 25, fixedChronologicalAge: 25,
            canGeneratePawnRelations: false, forceNoGear: true, forcedXenotype: XenotypeDefOf.Baseliner,
            forcedCustomXenotype: xenotype));
        things.Add(generated);

        Expect.IsFalse(generated.skills.GetSkill(SkillDefOf.Cooking).TotallyDisabled);
        Expect.IsFalse(generated.genes.GenesListForReading.Any(g => g.def == existing));
        Expect.IsTrue(generated.genes.Endogenes.Single(g => g.def == bonus).Active);
        Expect.IsFalse(generated.genes.GenesListForReading.Any(g => g.def == owner));
    }

    [Test]
    public void EmergencyReservesTriggersAtThresholdAndEntersCooldown()
    {
        HediffDef trigger = XylXenosTestHediff, reserves = XylXenosTestReserves;
        var gene = (GeneWithComps)pawn.genes.AddGene(XylXenosTestEmergencyReserves, false);
        var comp = gene.GetComp<GeneComp_EmergencyReserves>()!;
        Expect.IsTrue(gene.ImmuneToVacuumBurns);
        Hediff condition = pawn.health.AddHediff(trigger);
        comp.CompTickInterval(60);
        Expect.IsFalse(pawn.health.hediffSet.HasHediff(reserves));
        condition.Severity = 0.5f;
        comp.CompTickInterval(60);
        Expect.AreApproximatelyEqual(0.7f, pawn.health.hediffSet.GetFirstHediffOfDef(reserves).Severity);
        Expect.IsFalse(gene.ImmuneToVacuumBurns);
        comp.CompTickInterval(60);
        Expect.AreEqual(1, pawn.health.hediffSet.hediffs.Count(h => h.def == reserves));
    }

    [Test]
    public void FlightCompChecksApparelExtensionAndRechecksOnEvent()
    {
        var gene = (GeneWithComps)pawn.genes.AddGene(XylXenosTestFlight, false);
        var comp = gene.GetComp<GeneComp_Flight>()!;
        Expect.IsFalse(GeneComp_Flight.ApparelAllowsFlight(ThingDefOf.Apparel_Parka));
        Expect.IsTrue(GeneComp_Flight.ApparelAllowsFlight(Apparel_Jacket));
        // With no worn apparel, an apparel-change event clears a stale restriction.
        comp.flightAllowedByApparel = false;
        EventManager.Instance.Notify(EventDefOf.PostApparelChanged, pawn);
        Expect.IsTrue(comp.flightAllowedByApparel);
    }

    [Test]
    public void HyperlactationAddsHediffButStopsWhileMalnourished()
    {
        HediffDef health = XylXenosTestHediff;
        var gene = (GeneWithComps)pawn.genes.AddGene(XylXenosTestHyperlactation, false);
        Hediff condition = pawn.health.hediffSet.GetFirstHediffOfDef(health);
        Expect.AreApproximatelyEqual(1, condition.Severity);
        pawn.health.RemoveHediff(condition);
        pawn.health.AddHediff(HediffDefOf.Malnutrition);
        gene.GetComp<GeneComp_Hyperlactation>()!.CompTickInterval(60);
        Expect.IsFalse(pawn.health.hediffSet.HasHediff(health));
    }

    [Test]
    public void LoveEuphoriaAffectsPartnerAndRecordsSource()
    {
        HediffDef high = XylXenosTestSource, addiction = XylXenosTestHediff;
        pawn.genes.AddGene(XylXenosTestLoveEuphoria, false);
        Pawn partner = PawnGenerator.GeneratePawn(new PawnGenerationRequest(PawnKindDefOf.Colonist,
            Faction.OfPlayer, canGeneratePawnRelations: false, forcedXenotype: XenotypeDefOf.Baseliner));
        things.Add(partner);
        partner.needs.rest.CurLevel = 0.2f;
        EventManager.Instance.Notify(EventDefOf.PostLovin, pawn, partner);
        Expect.IsTrue(partner.health.hediffSet.HasHediff(addiction));
        Expect.ReferencesAreEqual(pawn, partner.health.hediffSet.GetFirstHediffOfDef(high).TryGetComp<HediffComp_Source>().OtherPawn);
        Expect.AreApproximatelyEqual(0.5f, partner.needs.rest.CurLevel);
        Expect.IsFalse(pawn.health.hediffSet.HasHediff(high));
    }

    [Test]
    public void MeditationFocusEnablesOnlyConfiguredType()
    {
        var gene = (GeneWithComps)pawn.genes.AddGene(XylXenosTestMeditationFocus, false);
        var comp = gene.GetComp<GeneComp_MeditationFocus>()!;
        Expect.IsTrue(comp.EnablesMeditationType(Natural));
        Expect.IsFalse(comp.EnablesMeditationType(Dignified));
    }

    [Test]
    public void MilkableUsesChargeQuantityToggleAndCooldown()
    {
        HediffDef health = XylXenosTestTimedCharge;
        var hediff = (HediffWithComps)pawn.health.AddHediff(health);
        var charge = hediff.TryGetComp<HediffComp_Chargeable>();
        charge.GreedyConsume(float.MaxValue);
        charge.TryCharge(0.75f);
        var gene = (GeneWithComps)pawn.genes.AddGene(XylXenosTestMilkableCharge, false);
        var comp = gene.GetComp<GeneComp_Milkable>()!;
        Expect.AreEqual(3, comp.ItemCount);
        Expect.IsFalse(comp.ItemsFull);
        comp.lastMilkedTick = Find.TickManager.TicksGame - 100;
        comp.milkingCooldownDays = 0;
        Expect.IsTrue(comp.ReadyToMilk);
        comp.allowMilking = false;
        Expect.IsFalse(comp.ReadyToMilk);
        comp.allowMilking = true;
        comp.Notify_Milked(pawn);
        Expect.IsFalse(comp.ReadyToMilk);
        charge.TryCharge(1);
        comp.CompTickInterval(60);
        Expect.IsTrue(comp.ItemsFull);
        Expect.AreEqual(0, comp.FullnessStage);
        charge.GreedyConsume(float.MaxValue);
        comp.CompTickInterval(60);
        Expect.AreEqual(-1, comp.FullnessStage);
    }

    [Test, LoadIfRoyaltyActive]
    public void PsycastCompInitializesPsyfocus()
    {
        pawn.psychicEntropy.OffsetPsyfocusDirectly(-1);
        pawn.genes.AddGene(XylXenosTestPsycast, false);
        Expect.GreaterThan(pawn.psychicEntropy.CurrentPsyfocus, 0f);
        Expect.IsTrue(pawn.GeneTracker_XylXenos!.hasPsycast);
    }

    [Test]
    public void RegenerationHealsNaturallyHealingInjuries()
    {
        var injury = (Hediff_Injury)HediffMaker.MakeHediff(HediffDefOf.Cut, pawn, pawn.RaceProps.body.corePart);
        injury.Severity = 5;
        pawn.health.AddHediff(injury);
        var gene = (GeneWithComps)pawn.genes.AddGene(XylXenosTestRegeneration, false);
        float before = injury.Severity;
        gene.GetComp<GeneComp_Regeneration>()!.CompTickInterval(60);
        Expect.LessThan(injury.Severity, before);
    }

    [Test]
    public void SeeingRedRemembersAttackerAndClearsAfterHediffEnds()
    {
        HediffDef rage = XylXenosTestHediff;
        GenSpawn.Spawn(pawn, new IntVec3(70, 0, 70), Find.CurrentMap);
        var gene = (GeneWithComps)pawn.genes.AddGene(XylXenosTestSeeingRed, false);
        Thing attacker = ThingMaker.MakeThing(ThingDefOf.Silver);
        things.Add(attacker);
        EventManager.Instance.Notify(EventDefOf.PreTakeDamage, pawn, new DamageInfo(DamageDefOf.Blunt, 1, instigator: attacker));
        var comp = gene.GetComp<GeneComp_SeeingRed>()!;
        Expect.IsTrue(pawn.health.hediffSet.HasHediff(rage));
        Expect.IsTrue(comp.ForceHostility(attacker));
        pawn.health.RemoveHediff(pawn.health.hediffSet.GetFirstHediffOfDef(rage));
        comp.CompTickInterval(60);
        Expect.IsFalse(comp.ForceHostility(attacker));
    }
}
