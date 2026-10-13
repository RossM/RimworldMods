using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Xylib.Analyzer.Tests;

public sealed class PersistenceChecksTests
{
    [Test]
    public async Task ReportsMissingConfigErrorsBaseCall()
    {
        var diagnostics = await AnalyzerTestHelper.Analyze("""
            [Xylib.UsedFromXml] class Child : Verse.Def
            {
                public override System.Collections.Generic.IEnumerable<string> ConfigErrors() { yield break; }
            }
            """);
        Assert.That(diagnostics.Select(d => (d.Id, d.Descriptor.Category)), Is.EqualTo(new[] { ("XYLIB0007", "Correctness") }));
    }

    [Test]
    public async Task RecognizesIteratorBaseCalls()
    {
        var diagnostics = await AnalyzerTestHelper.Analyze("""
            [Xylib.UsedFromXml] class Child : Verse.Def
            {
                public override System.Collections.Generic.IEnumerable<string> ConfigErrors()
                {
                    foreach (var error in base.ConfigErrors()) yield return error;
                }
            }
            """);
        Assert.That(diagnostics, Is.Empty);
    }

    [Test]
    public async Task RecognizesExpressionBodyBaseCalls()
    {
        var diagnostics = await AnalyzerTestHelper.Analyze("""
            [Xylib.UsedFromXml] class Child : Verse.Def
            {
                public override System.Collections.Generic.IEnumerable<string> ConfigErrors() => base.ConfigErrors();
            }
            """);
        Assert.That(diagnostics, Is.Empty);
    }

    [Test]
    public async Task CallingAnotherInstanceDoesNotCallBase()
    {
        var diagnostics = await AnalyzerTestHelper.Analyze("""
            [Xylib.UsedFromXml] class Child : Verse.Def
            {
                public override System.Collections.Generic.IEnumerable<string> ConfigErrors() => new Verse.Def().ConfigErrors();
            }
            """);
        Assert.That(diagnostics.Select(d => d.Id), Is.EqualTo(new[] { "XYLIB0007" }));
    }

    [Test]
    public async Task OverloadsDoNotCreateBaseCallRequirements()
    {
        var diagnostics = await AnalyzerTestHelper.Analyze("""
            class Parent { public virtual void ConfigErrors(int value) { } }
            class Child : Parent { public void ConfigErrors(string value) { } }
            """);
        Assert.That(diagnostics, Is.Empty);
    }

    [Test]
    public async Task HiddenMatchingConfigErrorsAlsoNeedsBaseCall()
    {
        var diagnostics = await AnalyzerTestHelper.Analyze("""
            class Parent { public void ConfigErrors(int value) { } }
            class Child : Parent { public new void ConfigErrors(int value) { } }
            """);
        Assert.That(diagnostics.Select(d => d.Id), Is.EqualTo(new[] { "XYLIB0007" }));
    }

    [TestCase("Verse.IExposable", "ExposeData", "")]
    [TestCase("Verse.ThingComp", "PostExposeData", "override")]
    [TestCase("ParentComp", "CompExposeData", "override")]
    public async Task ChecksPublicAndPrivateUnsavedFields(string baseType, string method, string modifier)
    {
        var diagnostics = await AnalyzerTestHelper.Analyze($$"""
            class ParentComp { public virtual void CompExposeData() { } }
            class Child : {{baseType}}
            {
                public int visible;
                private int hidden;
                public {{modifier}} void {{method}}() { }
            }
            """);
        Assert.That(diagnostics.Select(d => d.Id), Is.EqualTo(new[] { "XYLIB0008", "XYLIB0008" }));
        Assert.That(diagnostics.Select(d => d.Location.SourceTree!.GetText().ToString(d.Location.SourceSpan)),
            Is.EquivalentTo(new[] { "visible", "hidden" }));
    }

    [Test]
    public async Task IgnoresExcludedAndImplicitFields()
    {
        var diagnostics = await AnalyzerTestHelper.Analyze("""
            class Child : Verse.IExposable
            {
                public const int Constant = 1;
                public static int shared;
                public readonly int fixedValue;
                [Verse.Unsaved] private int temporary;
                public int Property { get; set; }
                public void ExposeData() { }
            }
            """);
        Assert.That(diagnostics, Is.Empty);
    }

    [Test]
    public async Task RecognizesFieldReferencesInExplicitInterfaceImplementation()
    {
        var diagnostics = await AnalyzerTestHelper.Analyze("""
            class Child : Verse.IExposable
            {
                private int value;
                void Verse.IExposable.ExposeData() { Save(ref this.value); }
                private static void Save(ref int field) { }
            }
            """);
        Assert.That(diagnostics, Is.Empty);
    }

    [Test]
    public async Task NameofDoesNotCountAsSavingAField()
    {
        var diagnostics = await AnalyzerTestHelper.Analyze("""
            class Child : Verse.IExposable
            {
                public int value;
                public void ExposeData() { var label = nameof(value); }
            }
            """);
        Assert.That(diagnostics.Select(d => d.Id), Is.EqualTo(new[] { "XYLIB0008" }));
    }

    [Test]
    public async Task SavingAnotherInstancesFieldDoesNotSaveThisInstance()
    {
        var diagnostics = await AnalyzerTestHelper.Analyze("""
            class Child : Verse.IExposable
            {
                public int value;
                public void ExposeData() { var other = new Child(); var saved = other.value; }
            }
            """);
        Assert.That(diagnostics.Select(d => d.Id), Is.EqualTo(new[] { "XYLIB0008" }));
    }

    [Test]
    public async Task InheritedSaveMethodDoesNotSaveNewFields()
    {
        var diagnostics = await AnalyzerTestHelper.Analyze("""
            class Parent : Verse.IExposable { public virtual void ExposeData() { } }
            class Child : Parent { public int value; }
            """);
        Assert.That(diagnostics.Select(d => d.Id), Is.EqualTo(new[] { "XYLIB0008" }));
    }

    [Test]
    public async Task CompWithoutSaveMethodReportsFields()
    {
        var diagnostics = await AnalyzerTestHelper.Analyze("class ExampleComp { public int value; }");
        Assert.That(diagnostics.Select(d => d.Id), Is.EqualTo(new[] { "XYLIB0008" }));
    }

    [Test]
    public async Task ChecksBaseCallEvenWithoutNewFields()
    {
        var diagnostics = await AnalyzerTestHelper.Analyze("""
            class Parent : Verse.IExposable
            {
                private int value;
                public virtual void ExposeData() { var saved = value; }
            }
            class Child : Parent { public override void ExposeData() { } }
            """);
        Assert.That(diagnostics.Select(d => d.Id), Is.EqualTo(new[] { "XYLIB0009" }));
    }

    [Test]
    public async Task SaveBaseCallAndOwnFieldsAreAccepted()
    {
        var diagnostics = await AnalyzerTestHelper.Analyze("""
            class Parent : Verse.IExposable
            {
                public int inherited;
                public virtual void ExposeData() { var saved = inherited; }
            }
            class Child : Parent
            {
                public int own;
                public override void ExposeData() { base.ExposeData(); var saved = own; }
            }
            """);
        Assert.That(diagnostics, Is.Empty);
    }

    [Test]
    public async Task DoesNotRequireBaseCallWhenBaseHasNoFieldsToSave()
    {
        var diagnostics = await AnalyzerTestHelper.Analyze("""
            class Parent : Verse.IExposable
            {
                [Verse.Unsaved] public int temporary;
                public readonly int fixedValue;
                public virtual void ExposeData() { }
            }
            class Child : Parent { public override void ExposeData() { } }
            """);
        Assert.That(diagnostics, Is.Empty);
    }

    [Test]
    public async Task DoesNotRequireCallingAbstractConfigErrors()
    {
        var diagnostics = await AnalyzerTestHelper.Analyze("""
            abstract class Parent { public abstract void ConfigErrors(); }
            class Child : Parent { public override void ConfigErrors() { } }
            """);
        Assert.That(diagnostics, Is.Empty);
    }

    [Test]
    public async Task PartialSaveMethodImplementationCanSaveFields()
    {
        var diagnostics = await AnalyzerTestHelper.Analyze("""
            partial class Child : Verse.IExposable
            {
                private int value;
                public partial void ExposeData();
            }
            partial class Child
            {
                public partial void ExposeData() { var saved = value; }
            }
            """);
        Assert.That(diagnostics, Is.Empty);
    }

    [Test]
    public async Task HiddenSaveMethodDoesNotReplaceInterfaceImplementation()
    {
        var diagnostics = await AnalyzerTestHelper.Analyze("""
            class Parent : Verse.IExposable { public void ExposeData() { } }
            class Child : Parent
            {
                private int value;
                public new void ExposeData() { var saved = value; }
            }
            """);
        Assert.That(diagnostics.Select(d => d.Id), Is.EqualTo(new[] { "XYLIB0008" }));
    }

    [Test]
    public async Task ExposableContractTakesPrecedenceOverCompConvention()
    {
        var diagnostics = await AnalyzerTestHelper.Analyze("""
            class ExampleComp : Verse.IExposable
            {
                public int value;
                public void ExposeData() { var saved = value; }
                public void CompExposeData() { }
            }
            """);
        Assert.That(diagnostics, Is.Empty);
    }
}
