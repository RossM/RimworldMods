namespace InGameTests;

public abstract class PawnTestFixture : SeededTestFixture
{
    protected Pawn pawn = null!;
    protected readonly List<Thing> things = [];
    private readonly List<GeneDef> mutableGeneDefs = [];

    [SetUp]
    public void SetUpPawn()
    {
        pawn = PawnGenerator.GeneratePawn(new PawnGenerationRequest(PawnKindDefOf.Colonist,
            Faction.OfPlayer, fixedBiologicalAge: 25, fixedChronologicalAge: 25,
            canGeneratePawnRelations: false, forceNoGear: true, forcedXenotype: XenotypeDefOf.Baseliner));
        things.Add(pawn);
        pawn.health.RemoveAllHediffs();
        foreach (Gene gene in pawn.genes.GenesListForReading.ToArray())
            pawn.genes.RemoveGene(gene);
    }

    // Tests that change a live gene's configuration need a private def.
    protected GeneDef CreateMutableGeneDef(params GeneCompProperties[] comps)
    {
        var extension = new DefModExtension_GeneWithComps { comps = [.. comps] };
        var def = new GeneDef
        {
            defName = "TestGene_" + Guid.NewGuid().ToString("N"), label = "test gene",
            geneClass = typeof(GeneWithComps), modExtensions = [extension],
            displayCategory = new GeneCategoryDef { defName = "TestGenes", label = "test genes" },
        };
        DefDatabase<GeneDef>.Add(def);
        mutableGeneDefs.Add(def);
        extension.ResolveReferences(def);
        return def;
    }

    [TearDown]
    public void TearDownPawn()
    {
        // Remove listeners while definitions and pawns still exist.
        foreach (Pawn p in things.OfType<Pawn>())
        {
            foreach (Gene gene in p.genes?.GenesListForReading.ToArray() ?? [])
                p.genes!.RemoveGene(gene);
            p.health.RemoveAllHediffs();
        }
        foreach (Thing thing in things.AsEnumerable().Reverse())
        {
            if (!thing.Destroyed)
                thing.Destroy();
            if (thing is Pawn p)
            {
                if (Find.WorldPawns.Contains(p))
                    Find.WorldPawns.RemovePawn(p);
                p.Discard(silentlyRemoveReferences: true);
            }
        }
        things.Clear();
        // The private defs were appended, so removing them leaves the loaded def indices intact.
        foreach (GeneDef def in mutableGeneDefs)
            typeof(DefDatabase<GeneDef>).GetMethod("Remove", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)!.Invoke(null, [def]);
        mutableGeneDefs.Clear();
    }
}
