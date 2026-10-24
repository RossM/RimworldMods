using UnityEngine;

namespace XylXenos.Tests;

[TestFixture(TestType.Playing)]
public sealed class MapAndScenarioTests : PawnTestFixture
{
    [Test]
    public void HostilityOverrideStopsAfterViolationAndResumesAfterCooldown()
    {
        Map map = Find.CurrentMap;
        GenSpawn.Spawn(pawn, new IntVec3(70, 0, 70), map);
        Pawn enemy = PawnGenerator.GeneratePawn(new PawnGenerationRequest(PawnKindDefOf.Colonist,
            Faction.OfAncientsHostile, canGeneratePawnRelations: false, forcedXenotype: XenotypeDefOf.Baseliner));
        things.Add(enemy);
        GenSpawn.Spawn(enemy, new IntVec3(71, 0, 70), map);
        pawn.genes.AddGene(GeneDef(new GeneCompProperties_DisableHostility { factions = [enemy.Faction.def] }), false);
        var manager = new HostilityOverrideManager(map)
        {
            anyOverrides = true, activeOverrides = [(pawn.Faction, enemy.Faction)],
        };
        Expect.IsTrue(manager.HostilityDisabled(enemy, pawn));
        Expect.IsFalse(manager.HostilityDisabled(pawn, enemy));
        manager.Notify_DamageTaken(enemy, new DamageInfo(DamageDefOf.Blunt, 1, instigator: pawn));
        Expect.IsFalse(manager.HostilityDisabled(enemy, pawn));
        manager.lastHostileActionTick[pawn.Faction] = Find.TickManager.TicksGame - HostilityOverrideManager.ViolationDisableTicks - 1;
        Expect.IsTrue(manager.HostilityDisabled(enemy, pawn));
    }

    [Test]
    public void MoteBubbleIsCreatedAndDestroyedWithHediff()
    {
        GenSpawn.Spawn(pawn, new IntVec3(70, 0, 70), Find.CurrentMap);
        var condition = (HediffWithComps)pawn.health.AddHediff(HediffDef(new HediffCompProperties_MoteBubble { iconPath = "UI/Icons/PassionMajor" }));
        var comp = condition.TryGetComp<HediffComp_MoteBubble>();
        Expect.IsNotNull(comp.mote);
        Expect.IsFalse(comp.mote!.Destroyed);
        pawn.health.RemoveHediff(condition);
        Expect.IsTrue(comp.mote.Destroyed);
    }

    [Test]
    public void RandomInspirationCompCanStartInspiration()
    {
        GenSpawn.Spawn(pawn, new IntVec3(70, 0, 70), Find.CurrentMap);
        var condition = (HediffWithComps)pawn.health.AddHediff(HediffDef(new HediffCompProperties_RandomInspiration { mtbDays = 0.000001f, letter = "Test inspiration" }));
        Expect.IsFalse(pawn.Inspired);
        Expect.IsNotNull(pawn.mindState.inspirationHandler.GetRandomAvailableInspirationDef());
        float adjustment = 0;
        condition.TryGetComp<HediffComp_RandomInspiration>().CompPostTickInterval(ref adjustment, 150);
        Expect.IsTrue(pawn.Inspired);
    }

    [Test]
    public void PsylinkScenarioHelperAddsLevelsWithoutGrantingAbilities()
    {
        if (!ModsConfig.RoyaltyActive) { Test.Skip("Requires Royalty."); return; }
        int before = pawn.abilities.AllAbilitiesForReading.Count;
        ScenPart_PsylinkLevels.ChangePsylinkLevelWithoutAbility(pawn, 1, sendLetter: false);
        Expect.AreEqual(1, pawn.GetPsylinkLevel());
        Expect.AreEqual(before, pawn.abilities.AllAbilitiesForReading.Count);
        ScenPart_PsylinkLevels.ChangePsylinkLevelWithoutAbility(pawn, 1, sendLetter: false);
        Expect.AreEqual(2, pawn.GetPsylinkLevel());
        Expect.AreEqual(before, pawn.abilities.AllAbilitiesForReading.Count);
    }

    [Test]
    public void RandomXenotypeScenarioPreservesExplicitCustomXenotype()
    {
        var custom = new CustomXenotype { name = "Test custom", genes = [] };
        var data = new PawnGenerationData(new PawnGenerationRequest(PawnKindDefOf.Colonist, forcedCustomXenotype: custom), XenotypeDefOf.Baseliner);
        var part = new ScenPart_RandomXenotype();
        part.Notify_PawnGenerationEarly(pawn, data);
        Expect.ReferencesAreEqual(custom, data.request.ForcedCustomXenotype);
        Expect.ReferencesAreEqual(XenotypeDefOf.Baseliner, data.xenotype);
    }

    [Test]
    public void PetrificationSurgeryReplacesConditionWithTendedCut()
    {
        HediffDef def = HediffDef();
        BodyPartRecord part = pawn.RaceProps.body.corePart;
        Hediff condition = pawn.health.AddHediff(def, part);
        condition.Severity = 2;
        var recipe = new RecipeDef { removesHediff = def, targetsBodyPart = true, defaultIngredientFilter = new ThingFilter() };
        var worker = new Recipe_RemoveHediff_Petrified { recipe = recipe };
        Expect.IsTrue(worker.GetPartsToApplyOn(pawn, recipe).Contains(part));
        worker.ApplyOnPawn(pawn, part, null, [], new Bill_Medical(recipe, []));
        Expect.IsFalse(pawn.health.hediffSet.HasHediff(def));
        Hediff cut = pawn.health.hediffSet.hediffs.Single(h => h.def == HediffDefOf.SurgicalCut && h.Part == part);
        Expect.AreApproximatelyEqual(2, cut.Severity);
        Expect.IsTrue(cut.IsTended());
    }

    [Test]
    public void NameRuleCopiesGenderAndSlotAndGeneratesName()
    {
        var rule = new Rule_NamePersonExt { gender = Gender.Female, slot = PawnNameSlot.First };
        var copy = (Rule_NamePersonExt)rule.DeepCopy();
        Expect.AreEqual(rule.gender, copy.gender);
        Expect.AreEqual(rule.slot, copy.slot);
        Expect.IsFalse(string.IsNullOrWhiteSpace(copy.Generate()));
    }

    [Test]
    public void AttachmentShaderUsesComplexCutoutForOverrideAndDeadPawns()
    {
        Expect.ReferencesAreEqual(ShaderDatabase.CutoutComplex, PawnRenderNode_AttachmentComplex.GetSkinShaderAbstract(true, false));
        Expect.ReferencesAreEqual(ShaderDatabase.CutoutComplex, PawnRenderNode_AttachmentComplex.GetSkinShaderAbstract(false, true));
        Expect.ReferencesAreEqual(XShaderDatabase.CutoutComplexSkin, PawnRenderNode_AttachmentComplex.GetSkinShaderAbstract(false, false));
    }
}
