namespace Xylib.Tests;

public abstract class XylibPawnTestFixture : PawnTestFixture
{
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
}
