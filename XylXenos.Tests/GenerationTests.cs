using System.Reflection;
using RimWorld.Planet;

namespace XylXenos.Tests;

[TestFixture(TestType.Playing)]
public sealed class GenerationTests : SeededTestFixture
{
    [Test]
    public void GeneTemplateCopiesAbilityBiostatsAndClonesExtensions()
    {
        var extension = new DefModExtension_GeneWithComps();
        var template = new GeneTemplateDef
        {
            defName = "TestTemplate", label = "Gene {0}", description = "Ability {0}", iconPath = "Icon/{0}",
            displayCategory = new GeneCategoryDef(), displayOrderOffset = 17, selectionWeight = 0.25f,
            biostats = [new GeneTemplateDef.AbilityBiostatInfo { levels = new IntRange(2), biostatArc = 1, biostatCpx = 3, biostatMet = -2 }],
            modExtensions = [extension],
        };
        var ability = new AbilityDef { defName = "TestAbility", label = "test ability", description = "test description", level = 2, iconPath = "Test" };
        var gene = (GeneDef)typeof(GeneDefGenerator).GetMethod("GetFromTemplate", BindingFlags.NonPublic | BindingFlags.Static)!
            .Invoke(null, [template, ability, 1000, false])!;
        Expect.AreEqual("TestTemplate_TestAbility", gene.defName);
        Expect.AreEqual("Gene test ability", gene.label);
        Expect.AreEqual("Icon/Test", gene.iconPath);
        Expect.AreEqual(1017f, gene.displayOrderInCategory);
        Expect.AreEqual(1, gene.biostatArc);
        Expect.AreEqual(3, gene.biostatCpx);
        Expect.AreEqual(-2, gene.biostatMet);
        Expect.ReferencesAreEqual(ability, gene.abilities.Single());
        Expect.ReferencesAreNotEqual(extension, gene.GetModExtension<DefModExtension_GeneWithComps>());
    }

    [Test]
    public void StockReferenceDelegatesHandlingAndGeneration()
    {
        var item = new ThingDef();
        var expected = new Thing { def = item };
        var generator = new StockGenerator_Ref
        {
            traderKind = new TraderKindDef { stockGenerators = [new TestStockGenerator { item = item, result = expected }] },
        };
        Expect.IsTrue(generator.HandlesThingDef(item));
        Expect.IsFalse(generator.HandlesThingDef(new ThingDef()));
        Expect.ReferencesAreEqual(expected, generator.GenerateThings(PlanetTile.Invalid).Single());
    }
}

public sealed class TestStockGenerator : StockGenerator
{
    public ThingDef item = null!;
    public Thing result = null!;
    public override IEnumerable<Thing> GenerateThings(PlanetTile tile, Faction? faction = null) => [result];
    public override bool HandlesThingDef(ThingDef def) => ReferenceEquals(item, def);
}
