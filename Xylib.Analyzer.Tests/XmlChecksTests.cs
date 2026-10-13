using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using NUnit.Framework;

namespace Xylib.Analyzer.Tests;

public sealed class XmlChecksTests
{
    [TestCase("Verse.AbilityCompProperties")]
    [TestCase("Verse.ColorGenerator")]
    [TestCase("Verse.CompProperties")]
    [TestCase("Verse.Def")]
    [TestCase("Verse.DefModExtension")]
    [TestCase("Verse.HediffCompProperties")]
    [TestCase("Verse.HediffGiver")]
    [TestCase("RimWorld.IngestionOutcomeDoer")]
    [TestCase("Verse.PatchOperation")]
    [TestCase("Verse.PawnRenderNodeProperties")]
    [TestCase("RimWorld.ScenPart")]
    public async Task ChecksKnownXmlTypes(string baseType)
    {
        var diagnostics = await AnalyzerTestHelper.Analyze($"class Child : {baseType} {{ }}");
        Assert.That(diagnostics.Select(d => (d.Id, d.Descriptor.Category)), Is.EqualTo(new[] { ("XYLIB0006", "Style") }));
    }

    [Test]
    public async Task AbstractTypesAndAnnotatedConcreteTypesAreAccepted()
    {
        var diagnostics = await AnalyzerTestHelper.Analyze("""
            abstract class Parent : Verse.Def { }
            [Xylib.UsedFromXml] class Child : Parent { }
            class Ordinary { }
            """);
        Assert.That(diagnostics, Is.Empty);
    }

    [Test]
    public async Task ConcreteSubclassMustDeclareItsOwnAttribute()
    {
        var diagnostics = await AnalyzerTestHelper.Analyze("""
            [Xylib.UsedFromXml] abstract class Parent { }
            abstract class Middle : Parent { }
            class Child : Middle { }
            """);
        Assert.That(diagnostics.Select(d => d.Id), Is.EqualTo(new[] { "XYLIB0006" }));
    }

    [Test]
    public async Task ChecksAbstractXmlBasesInReferencedAssemblies()
    {
        var parent = CSharpCompilation.Create("Parent",
            [CSharpSyntaxTree.ParseText(AnalyzerTestHelper.Contracts), CSharpSyntaxTree.ParseText("[Xylib.UsedFromXml] public abstract class Parent { }")],
            [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)],
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        using var stream = new MemoryStream();
        var emit = parent.Emit(stream);
        Assert.That(emit.Success, Is.True, string.Join("\n", emit.Diagnostics));
        var diagnostics = await AnalyzerTestHelper.Analyze("class Child : Parent { }", contracts: "",
            additionalReference: MetadataReference.CreateFromImage(stream.ToArray()));
        Assert.That(diagnostics.Select(d => d.Id), Is.EqualTo(new[] { "XYLIB0006" }));
    }

    [Test]
    public async Task PartialClassProducesOneWarning()
    {
        var diagnostics = await AnalyzerTestHelper.Analyze("partial class Child : Verse.Def { } partial class Child { }");
        Assert.That(diagnostics.Select(d => d.Id), Is.EqualTo(new[] { "XYLIB0006" }));
    }
}
