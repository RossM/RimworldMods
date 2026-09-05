using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Disharmony.Analyzers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using NUnit.Framework;

namespace Disharmony.Analyzers.Tests;

public class PatchMethodAnalyzerTests
{
    // Minimal metadata contract: no game or patch execution is needed to analyze source.
    private const string Attributes = """
        namespace Disharmony
        {
            [System.Flags]
            public enum PatchOptions { Default = 0, Inline = 1, AlwaysRun = 4 }
            [System.AttributeUsage(System.AttributeTargets.Method)]
            public class PrefixAttribute : System.Attribute { }
            [System.AttributeUsage(System.AttributeTargets.Method)]
            public class PostfixAttribute : System.Attribute { }
            [System.AttributeUsage(System.AttributeTargets.Class | System.AttributeTargets.Method)]
            public class PatchOptionsAttribute : System.Attribute
            {
                public PatchOptionsAttribute(PatchOptions options) { }
            }
        }
        """;

    [TestCase("class C { [Prefix] static void M<T>() {} }", "DH0001")]
    [TestCase("class C<T> { [Prefix] static void M() {} }", "DH0001")]
    [TestCase("class C<T> { class Nested { [Postfix] static void M() {} } }", "DH0001")]
    [TestCase("class C { [Prefix] void M() {} }", "DH0002")]
    [TestCase("class C { [Postfix] void M() {} }", "DH0002")]
    [TestCase("class C { [Prefix] static int M() => 0; }", "DH0003")]
    [TestCase("class C { [Prefix] static bool? M() => null; }", "DH0003")]
    [TestCase("class C { static bool b; [Prefix] static ref bool M() => ref b; }", "DH0003")]
    [TestCase("class C { static bool b; [Prefix] static ref readonly bool M() => ref b; }", "DH0003")]
    [TestCase("class C { [Postfix] static bool M() => true; }", "DH0004")]
    [TestCase("class C { [Prefix, PatchOptions(PatchOptions.AlwaysRun)] static bool M() => true; }", "DH0005")]
    [TestCase("class C { [Prefix, PatchOptions(PatchOptions.AlwaysRun)] static int M() => 0; }", "DH0005")]
    [TestCase("[PatchOptions(PatchOptions.AlwaysRun)] class C { [Prefix] static bool M() => true; }", "DH0005")]
    [TestCase("class C { [Prefix, PatchOptions(PatchOptions.Inline | PatchOptions.AlwaysRun)] static bool M() => true; }", "DH0005")]
    [TestCase("[PatchOptions(PatchOptions.Default)] class C { [Prefix, PatchOptions(PatchOptions.AlwaysRun)] static bool M() => true; }", "DH0005")]
    [TestCase("partial class C { [Prefix] static bool M() => true; } [PatchOptions(PatchOptions.AlwaysRun)] partial class C {}", "DH0005")]
    [TestCase("class CustomAttribute : PrefixAttribute {} class C { [Custom] static int M() => 0; }", "DH0003")]
    [TestCase("class C { [global::Disharmony.PrefixAttribute] static int M() => 0; }", "DH0003")]
    [TestCase("using P = Disharmony.PrefixAttribute; class C { [P] static int M() => 0; }", "DH0003")]
    [TestCase("[PatchOptions(PatchOptions.AlwaysRun)] class B {} class C : B { [Prefix] static bool M() => true; }", "DH0005")]
    public async Task InvalidPatchReportsWarningAtMethodName(string source, string expectedId)
    {
        var diagnostics = await Analyze(source);
        Assert.That(diagnostics, Has.Length.EqualTo(1));
        Assert.That(diagnostics[0].Id, Is.EqualTo(expectedId));
        Assert.That(diagnostics[0].Severity, Is.EqualTo(DiagnosticSeverity.Warning));
        var text = await diagnostics[0].Location.SourceTree!.GetTextAsync();
        Assert.That(text.ToString(diagnostics[0].Location.SourceSpan), Is.EqualTo("M"));
    }

    [TestCase("class C { [Prefix] static void M() {} }")]
    [TestCase("class C { [Prefix] static bool M() => true; }")]
    [TestCase("class C { [Postfix] static void M() {} }")]
    [TestCase("class C { [Prefix, PatchOptions(PatchOptions.AlwaysRun)] static void M() {} }")]
    [TestCase("class C { [Postfix, PatchOptions(PatchOptions.AlwaysRun)] static void M() {} }")]
    [TestCase("[PatchOptions(PatchOptions.AlwaysRun)] class C { [Prefix, PatchOptions(PatchOptions.Default)] static bool M() => true; }")]
    [TestCase("[PatchOptions(PatchOptions.AlwaysRun)] class C { [Prefix, PatchOptions(PatchOptions.Inline)] static bool M() => true; }")]
    [TestCase("class C { [Prefix, PatchOptions(PatchOptions.Inline)] static bool M() => true; }")]
    [TestCase("class C<T> { int M<U>() => 0; }")]
    [TestCase("namespace Other { class PrefixAttribute : System.Attribute {} class C { [Prefix] int M() => 0; } }")]
    [TestCase("[PatchOptions(PatchOptions.AlwaysRun)] class C { class Nested { [Prefix] static bool M() => true; } }")]

    [TestCase("class C { [Prefix] static void M(int __result) {} }")]
    [TestCase("class CustomAttribute : PatchOptionsAttribute { public CustomAttribute() : base(PatchOptions.Default) {} } [PatchOptions(PatchOptions.AlwaysRun)] class C { [Prefix, Custom] static bool M() => true; }")]
    [TestCase("[PatchOptions(PatchOptions.AlwaysRun)] class B {} [PatchOptions(PatchOptions.Default)] class C : B { [Prefix] static bool M() => true; }")]
    [TestCase("[System.AttributeUsage(System.AttributeTargets.Class, Inherited = false)] class CustomAttribute : PatchOptionsAttribute { public CustomAttribute() : base(PatchOptions.AlwaysRun) {} } [Custom] class B {} class C : B { [Prefix] static bool M() => true; }")]
    [TestCase("namespace HarmonyLib { class HarmonyPostfix : System.Attribute {} class C { [HarmonyPostfix] static int M() => 0; } }")]
    public async Task ValidOrUnrelatedCodeDoesNotWarn(string source)
    {
        Assert.That(await Analyze(source), Is.Empty);
    }

    [Test]
    public async Task IndependentViolationsAreAllReported()
    {
        var diagnostics = await Analyze("class C<T> { [Postfix] int M() => 0; }");
        Assert.That(diagnostics.Select(d => d.Id), Is.EquivalentTo(new[] { "DH0001", "DH0002", "DH0004" }));
    }

    [Test]
    public async Task MissingDisharmonyReferenceDoesNotWarn()
    {
        var compilation = CSharpCompilation.Create("Test",
            new[] { CSharpSyntaxTree.ParseText("class C { int M<T>() => 0; }") },
            new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location) },
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        var diagnostics = await compilation.WithAnalyzers(
            ImmutableArray.Create<DiagnosticAnalyzer>(new PatchMethodAnalyzer())).GetAnalyzerDiagnosticsAsync();
        Assert.That(diagnostics, Is.Empty);
    }

    [Test]
    public async Task OverriddenMethodInheritsPatchAttribute()
    {
        var diagnostics = await Analyze("class B { [Prefix] public virtual void M() {} } class C : B { public override void M() {} }");
        Assert.That(diagnostics.Select(d => d.Id), Is.EquivalentTo(new[] { "DH0002", "DH0002" }));
    }

    [Test]
    public async Task NonInheritedPatchAttributeDoesNotApplyToOverride()
    {
        var diagnostics = await Analyze("[System.AttributeUsage(System.AttributeTargets.Method, Inherited = false)] class CustomAttribute : PrefixAttribute {} class B { [Custom] public virtual void M() {} } class C : B { public override void M() {} }");
        Assert.That(diagnostics.Select(d => d.Id), Is.EquivalentTo(new[] { "DH0002" }));
    }

    private static async Task<ImmutableArray<Diagnostic>> Analyze(string source)
    {
        var compilation = CSharpCompilation.Create("Test",
            new[] { CSharpSyntaxTree.ParseText(Attributes), CSharpSyntaxTree.ParseText("using Disharmony;\n" + source) },
            new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location) },
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        Assert.That(compilation.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error), Is.Empty,
            "The fixture must compile before analyzer diagnostics are checked.");
        return await compilation.WithAnalyzers(
            ImmutableArray.Create<DiagnosticAnalyzer>(new PatchMethodAnalyzer())).GetAnalyzerDiagnosticsAsync();
    }
}
