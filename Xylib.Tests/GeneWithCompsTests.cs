namespace Xylib.Tests;

[TestFixture(TestType.Playing)]
public sealed class GeneWithCompsTests : PawnTestFixture
{
    [Test]
    public void CreationInitializesCompParentPropertiesAndPostMake()
    {
        var properties = new ProbeProperties();
        GeneDef def = GeneDef(properties);
        var gene = (GeneWithComps)pawn.genes.AddGene(def, false);
        ProbeComp comp = gene.GetComp<ProbeComp>()!;
        Expect.ReferencesAreEqual(gene, comp.parent);
        Expect.ReferencesAreEqual(properties, comp.props);
        Expect.ReferencesAreEqual(pawn, comp.Pawn);
        Expect.AreEqual(1, comp.made);
        Expect.AreEqual(1, comp.added);
    }

    [Test]
    public void GeneWithoutCompsSupportsLifecycleAndTicks()
    {
        var gene = (GeneWithComps)pawn.genes.AddGene(GeneDef(), false);
        Expect.IsTrue(gene.Active);
        gene.Tick();
        gene.TickInterval(15);
        gene.Reset();
        Expect.IsEmpty(gene.GetGizmos());
        Expect.IsNull(gene.GetComp<ProbeComp>());
        pawn.genes.RemoveGene(gene);
        Expect.IsFalse(pawn.genes.GenesListForReading.Contains(gene));
    }

    [Test]
    public void TickAndIntervalCallbacksRunOnlyWhileActive()
    {
        var gene = (GeneWithComps)pawn.genes.AddGene(GeneDef(new ProbeProperties()), false);
        var comp = gene.GetComp<ProbeComp>()!;
        gene.Tick();
        gene.TickInterval(15);
        Expect.AreEqual(1, comp.ticks);
        Expect.AreEqual(15, comp.delta);
        comp.allow = false;
        gene.SetActiveStateNeedsUpdating();
        gene.Tick();
        gene.TickInterval(30);
        Expect.AreEqual(1, comp.ticks);
        Expect.AreEqual(15, comp.delta);
    }

    [Test]
    public void InheritedTickOverridesAreDispatched()
    {
        var gene = (GeneWithComps)pawn.genes.AddGene(GeneDef(new ProbeProperties { compClass = typeof(DerivedProbeComp) }), false);
        gene.Tick();
        gene.TickInterval(7);
        var comp = gene.GetComp<ProbeComp>()!;
        Expect.AreEqual(1, comp.ticks);
        Expect.AreEqual(7, comp.delta);
    }

    [Test]
    public void GeneChangeEventInvalidatesCachedActiveState()
    {
        var gene = (GeneWithComps)pawn.genes.AddGene(GeneDef(new ProbeProperties()), false);
        var comp = gene.GetComp<ProbeComp>()!;
        Expect.IsTrue(gene.Active);
        comp.allow = false;
        Expect.IsTrue(gene.Active, "Active is cached until a relevant event.");
        pawn.genes.AddGene(GeneDef(), false);
        Expect.IsFalse(gene.Active);
    }

    [Test]
    public void GenderAndGeneTypeRestrictionsControlActivation()
    {
        GeneDef def = GeneDef();
        var extension = def.GetModExtension<DefModExtension_GeneWithComps>();
        extension.gender = pawn.gender == Gender.Female ? Gender.Male : Gender.Female;
        var gene = (GeneWithComps)pawn.genes.AddGene(def, true);
        Expect.IsFalse(gene.Active);
        extension.gender = pawn.gender;
        extension.geneType = GeneType.Endogene;
        gene.SetActiveStateNeedsUpdating();
        Expect.IsFalse(gene.Active);
        extension.geneType = GeneType.Xenogene;
        gene.SetActiveStateNeedsUpdating();
        Expect.IsTrue(gene.Active);
        Expect.AreEqual(GeneType.Xenogene, gene.GeneType);
    }

    [Test]
    public void ResetRemovalAndChildListenerLifetimeAreForwarded()
    {
        var gene = (GeneWithComps)pawn.genes.AddGene(GeneDef(new ProbeProperties()), false);
        var comp = gene.GetComp<ProbeComp>()!;
        EventManager.Instance.Notify(EventDefOf.PostBirthday, pawn);
        Expect.AreEqual(1, comp.events);
        gene.Reset();
        Expect.AreEqual(1, comp.resets);
        pawn.genes.RemoveGene(gene);
        Expect.AreEqual(1, comp.removed);
        EventManager.Instance.Notify(EventDefOf.PostBirthday, pawn);
        Expect.AreEqual(1, comp.events);
    }

    [Test]
    public void ScribeRoundTripRecreatesCompsAndRestoresTheirState()
    {
        var gene = (GeneWithComps)pawn.genes.AddGene(GeneDef(new ProbeProperties()), false);
        gene.GetComp<ProbeComp>()!.savedValue = 42;
        string path = Path.Combine(GenFilePaths.SaveDataFolderPath, "gene-comps-test.xml");
        try
        {
            Scribe.saver.InitSaving(path, "GeneTest");
            Scribe_Deep.Look(ref gene, "gene");
            Scribe.saver.FinalizeSaving();
            GeneWithComps restored = null!;
            Scribe.loader.InitLoading(path);
            Scribe_Deep.Look(ref restored, "gene");
            var directory = (LoadedObjectDirectory)typeof(CrossRefHandler)
                .GetField("loadedObjectDirectory", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
                .GetValue(Scribe.loader.crossRefs)!;
            directory.RegisterLoaded(pawn);
            Scribe.loader.FinalizeLoading();
            var comp = restored.GetComp<ProbeComp>()!;
            Expect.AreEqual(42, comp.savedValue);
            Expect.ReferencesAreEqual(restored, comp.parent);
            Expect.ReferencesAreEqual(pawn, comp.Pawn);
            restored.Tick();
            Expect.AreEqual(1, comp.ticks);
        }
        finally
        {
            Scribe.ForceStop();
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Test]
    public void CompsContributeGizmosStatsAndVacuumImmunity()
    {
        var gene = (GeneWithComps)pawn.genes.AddGene(GeneDef(new ProbeProperties()), false);
        var comp = gene.GetComp<ProbeComp>()!;
        Expect.IsTrue(gene.GetGizmos().Contains(comp.gizmo));
        Expect.IsTrue(gene.SpecialDisplayStats().Contains(comp.stat));
        Expect.IsFalse(gene.ImmuneToVacuumBurns);
        comp.immune = true;
        Expect.IsTrue(gene.ImmuneToVacuumBurns);
    }
}

public sealed class ProbeProperties : GeneCompProperties
{
    public ProbeProperties() => compClass = typeof(ProbeComp);
}

public class ProbeComp : GeneComp, IEventListener
{
    public int made, added, removed, resets, ticks, delta, events;
    public int savedValue;
    public bool allow = true, immune;
    public readonly Gizmo gizmo = new Command_Action();
    public readonly StatDrawEntry stat = new(StatCategoryDefOf.PawnMisc, "test", "value", "description", 0);
    public override bool CompAllowActive => allow;
    public override bool CompImmuneToVacuumBurns => immune;
    public override void CompPostMake() => made++;
    public override void CompPostPostAdd() => added++;
    public override void CompPostPostRemove() => removed++;
    public override void CompReset() => resets++;
    public override void CompExposeData() => Scribe_Values.Look(ref savedValue, nameof(savedValue));
    public override void CompTick() => ticks++;
    public override void CompTickInterval(int value) => delta += value;
    public override IEnumerable<Gizmo> CompGetGizmos() => [gizmo];
    public override IEnumerable<StatDrawEntry> SpecialDisplayStats() => [stat];
    public void RegisterWith(EventManager manager) => manager.Register(EventDefOf.PostBirthday, Pawn, Birthday);
    public void PreUnregister(EventManager manager) { }
    private void Birthday() => events++;
}

public sealed class DerivedProbeComp : ProbeComp;
