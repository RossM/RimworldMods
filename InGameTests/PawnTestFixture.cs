namespace InGameTests;

// Linked into both suites. Only the frequently used pawn/def construction lives here;
// each test supplies its own behavior and values.
public abstract class PawnTestFixture : SeededTestFixture
{
    protected Pawn pawn = null!;
    protected readonly List<Thing> things = [];
    private readonly List<GeneDef> geneDefs = [];
    private readonly List<HediffDef> hediffDefs = [];

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

    protected GeneDef GeneDef(params GeneCompProperties[] comps)
    {
        var extension = new DefModExtension_GeneWithComps { comps = [.. comps] };
        var def = new GeneDef
        {
            defName = "TestGene_" + Guid.NewGuid().ToString("N"), label = "test gene",
            geneClass = typeof(GeneWithComps), modExtensions = [extension],
            displayCategory = new GeneCategoryDef { defName = "TestGenes", label = "test genes" },
        };
        DefDatabase<GeneDef>.Add(def);
        geneDefs.Add(def);
        extension.ResolveReferences(def);
        return def;
    }

    protected HediffDef HediffDef(params HediffCompProperties[] comps)
    {
        var def = new HediffDef
        {
            defName = "TestHediff_" + Guid.NewGuid().ToString("N"), label = "test hediff",
            hediffClass = typeof(HediffWithComps), initialSeverity = 0.4f,
            maxSeverity = 10f, isBad = false, comps = [.. comps],
        };
        DefDatabase<HediffDef>.Add(def);
        hediffDefs.Add(def);
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
        // RimWorld has no public def-removal API. These defs were appended, so
        // removing them leaves the original indices intact; clear our index-based cache.
        foreach (GeneDef def in geneDefs)
            typeof(DefDatabase<GeneDef>).GetMethod("Remove", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)!.Invoke(null, [def]);
        foreach (HediffDef def in hediffDefs)
            typeof(DefDatabase<HediffDef>).GetMethod("Remove", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)!.Invoke(null, [def]);
        Xylib.Extensions.defExtCache.Clear();
        geneDefs.Clear();
        hediffDefs.Clear();
    }
}
