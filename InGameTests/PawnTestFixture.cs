using DevTools.Testing;
using RimWorld;
using System.Reflection;
using Verse;

namespace InGameTests;

public abstract class PawnTestFixture : SeededTestFixture
{
    protected Pawn pawn = null!;
    protected readonly List<Thing> things = [];
    protected readonly List<GeneDef> mutableGeneDefs = [];

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
            typeof(DefDatabase<GeneDef>).GetMethod("Remove", BindingFlags.Static | BindingFlags.NonPublic)!.Invoke(null, [def]);
        mutableGeneDefs.Clear();
    }
}
