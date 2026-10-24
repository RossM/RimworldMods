namespace XylXenos.Tests;

[TestFixture(TestType.Playing)]
public sealed class GeneCompTests : PawnTestFixture
{
    [Test]
    public void EmergencyReservesTriggersAtThresholdAndEntersCooldown()
    {
        HediffDef trigger = HediffDef(), reserves = HediffDef();
        var gene = (GeneWithComps)pawn.genes.AddGene(GeneDef(new GeneCompProperties_EmergencyReserves
        {
            hediff = reserves, severityRange = new FloatRange(0.7f),
            triggers = [new GeneCompProperties_EmergencyReserves.HediffSeverity { hediff = trigger, minSeverity = 0.5f }],
        }), false);
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
        var gene = (GeneWithComps)pawn.genes.AddGene(GeneDef(new GeneCompProperties_Flight()), false);
        var comp = gene.GetComp<GeneComp_Flight>()!;
        var clothing = new ThingDef { modExtensions = [new DefModExtension_Thing_Flight { allowsFlight = false }] };
        Expect.IsFalse(GeneComp_Flight.ApparelAllowsFlight(clothing));
        Expect.IsTrue(GeneComp_Flight.ApparelAllowsFlight(new ThingDef()));
        // With no worn apparel, an apparel-change event clears a stale restriction.
        comp.flightAllowedByApparel = false;
        EventManager.Instance.Notify(EventDefOf.PostApparelChanged, pawn);
        Expect.IsTrue(comp.flightAllowedByApparel);
    }

    [Test]
    public void HyperlactationAddsHediffButStopsWhileMalnourished()
    {
        HediffDef health = HediffDef();
        var gene = (GeneWithComps)pawn.genes.AddGene(GeneDef(new GeneCompProperties_Hyperlactation { hediff = health }), false);
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
        HediffDef high = HediffDef(new HediffCompProperties_Source()), addiction = HediffDef();
        pawn.genes.AddGene(GeneDef(new GeneCompProperties_LoveEuphoria
        {
            need = NeedDefOf.Rest, highHediff = high, addictionHediff = addiction,
            addictionChance = 1, needOffset = 0.3f,
        }), false);
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
        var allowed = new MeditationFocusDef();
        var other = new MeditationFocusDef();
        var gene = (GeneWithComps)pawn.genes.AddGene(GeneDef(new GeneCompProperties_MeditationFocus
        {
            allowedMeditationFocusTypes = [allowed],
        }), false);
        var comp = gene.GetComp<GeneComp_MeditationFocus>()!;
        Expect.IsTrue(comp.EnablesMeditationType(allowed));
        Expect.IsFalse(comp.EnablesMeditationType(other));
    }

    [Test]
    public void MilkableUsesChargeQuantityToggleAndCooldown()
    {
        HediffDef health = HediffDef(new HediffCompProperties_Chargeable { fullChargeAmount = 1, ticksToFullCharge = 1000 });
        var hediff = (HediffWithComps)pawn.health.AddHediff(health);
        var charge = hediff.TryGetComp<HediffComp_Chargeable>();
        charge.GreedyConsume(float.MaxValue);
        charge.TryCharge(0.75f);
        var gene = (GeneWithComps)pawn.genes.AddGene(GeneDef(new GeneCompProperties_Milkable
        {
            hediff = health, item = ThingDefOf.Silver, chargePerItem = 0.25f, ticksPerFullnessStage = 20,
        }), false);
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

    [Test]
    public void PsycastCompInitializesPsyfocus()
    {
        if (!ModsConfig.RoyaltyActive) { Test.Skip("Requires Royalty."); return; }
        pawn.psychicEntropy.OffsetPsyfocusDirectly(-1);
        pawn.genes.AddGene(GeneDef(new GeneCompProperties_Psycast()), false);
        Expect.GreaterThan(pawn.psychicEntropy.CurrentPsyfocus, 0f);
        Expect.IsTrue(PawnExtraData<GeneTracker_XylXenos>.Get(pawn).hasPsycast);
    }

    [Test]
    public void RegenerationHealsNaturallyHealingInjuries()
    {
        var injury = (Hediff_Injury)HediffMaker.MakeHediff(HediffDefOf.Cut, pawn, pawn.RaceProps.body.corePart);
        injury.Severity = 5;
        pawn.health.AddHediff(injury);
        var gene = (GeneWithComps)pawn.genes.AddGene(GeneDef(new GeneCompProperties_Regeneration { healthPerHour = 10 }), false);
        float before = injury.Severity;
        gene.GetComp<GeneComp_Regeneration>()!.CompTickInterval(60);
        Expect.LessThan(injury.Severity, before);
    }

    [Test]
    public void SeeingRedRemembersAttackerAndClearsAfterHediffEnds()
    {
        HediffDef rage = HediffDef();
        GenSpawn.Spawn(pawn, new IntVec3(70, 0, 70), Find.CurrentMap);
        var gene = (GeneWithComps)pawn.genes.AddGene(GeneDef(new GeneCompProperties_SeeingRed { hediffDef = rage, chance = 1 }), false);
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
