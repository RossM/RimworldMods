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
            [System.AttributeUsage(System.AttributeTargets.Class)]
            public class PatchAttribute : System.Attribute { }
            [System.AttributeUsage(System.AttributeTargets.Class | System.AttributeTargets.Method, AllowMultiple = true)]
            public class TargetAttribute : System.Attribute
            {
                public TargetAttribute(System.Type type = null, string methodName = null) { }
            }
            public class TargetsAttribute : TargetAttribute { }
            [System.AttributeUsage(System.AttributeTargets.Method)]
            public class InnerAttribute : System.Attribute { }
            [System.AttributeUsage(System.AttributeTargets.Method)]
            public class InnerConstantAttribute : System.Attribute { }
            [System.AttributeUsage(System.AttributeTargets.Class | System.AttributeTargets.Method)]
            public class PriorityAttribute : System.Attribute { }
            [System.AttributeUsage(System.AttributeTargets.Parameter)]
            public class ParameterAttribute : System.Attribute { }
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
        namespace HarmonyLib
        {
            [System.AttributeUsage(System.AttributeTargets.Class | System.AttributeTargets.Method, AllowMultiple = true)]
            public class HarmonyPatch : System.Attribute { }
        }
        """;

    [TestCase("[Patch, Target] class C { [Prefix] static void M<T>() {} }", "DH0001")]
    [TestCase("[Patch, Target] class C<T> { [Prefix] static void M() {} }", "DH0001")]
    [TestCase("[Patch, Target] class C<T> { [Patch, Target] class Nested { [Postfix] static void M() {} } }", "DH0001")]
    [TestCase("[Patch, Target] class C { [Prefix] void M() {} }", "DH0002")]
    [TestCase("[Patch, Target] class C { [Postfix] void M() {} }", "DH0002")]
    [TestCase("[Patch, Target] class C { [Prefix] static int M() => 0; }", "DH0003")]
    [TestCase("[Patch, Target] class C { [Prefix] static bool? M() => null; }", "DH0003")]
    [TestCase("[Patch, Target] class C { static bool b; [Prefix] static ref bool M() => ref b; }", "DH0003")]
    [TestCase("[Patch, Target] class C { static bool b; [Prefix] static ref readonly bool M() => ref b; }", "DH0003")]
    [TestCase("[Patch, Target] class C { [Postfix] static bool M() => true; }", "DH0004")]
    [TestCase("[Patch, Target] class C { [Prefix, PatchOptions(PatchOptions.AlwaysRun)] static bool M() => true; }", "DH0005")]
    [TestCase("[Patch, Target] class C { [Prefix, PatchOptions(PatchOptions.AlwaysRun)] static int M() => 0; }", "DH0005")]
    [TestCase("[PatchOptions(PatchOptions.AlwaysRun)] [Patch, Target] class C { [Prefix] static bool M() => true; }", "DH0005")]
    [TestCase("[Patch, Target] class C { [Prefix, PatchOptions(PatchOptions.Inline | PatchOptions.AlwaysRun)] static bool M() => true; }", "DH0005")]
    [TestCase("[PatchOptions(PatchOptions.Default)] [Patch, Target] class C { [Prefix, PatchOptions(PatchOptions.AlwaysRun)] static bool M() => true; }", "DH0005")]
    [TestCase("[Patch, Target] partial class C { [Prefix] static bool M() => true; } [PatchOptions(PatchOptions.AlwaysRun)] partial class C {}", "DH0005")]
    [TestCase("class CustomAttribute : PrefixAttribute {} [Patch, Target] class C { [Custom] static int M() => 0; }", "DH0003")]
    [TestCase("[Patch, Target] class C { [global::Disharmony.PrefixAttribute] static int M() => 0; }", "DH0003")]
    [TestCase("using P = Disharmony.PrefixAttribute; [Patch, Target] class C { [P] static int M() => 0; }", "DH0003")]
    [TestCase("[PatchOptions(PatchOptions.AlwaysRun)] [Patch, Target] class B {} [Patch, Target] class C : B { [Prefix] static bool M() => true; }", "DH0005")]
    public async Task InvalidPatchReportsWarningAtMethodName(string source, string expectedId)
    {
        var diagnostics = await Analyze(source);
        Assert.That(diagnostics, Has.Length.EqualTo(1));
        Assert.That(diagnostics[0].Id, Is.EqualTo(expectedId));
        Assert.That(diagnostics[0].Severity, Is.EqualTo(DiagnosticSeverity.Warning));
        var text = await diagnostics[0].Location.SourceTree!.GetTextAsync();
        Assert.That(text.ToString(diagnostics[0].Location.SourceSpan), Is.EqualTo("M"));
    }

    [TestCase("[Patch, Target] class C { [Prefix] static void M() {} }")]
    [TestCase("[Patch, Target] class C { [Prefix] static bool M() => true; }")]
    [TestCase("[Patch, Target] class C { [Postfix] static void M() {} }")]
    [TestCase("[Patch, Target] class C { [Prefix, PatchOptions(PatchOptions.AlwaysRun)] static void M() {} }")]
    [TestCase("[Patch, Target] class C { [Postfix, PatchOptions(PatchOptions.AlwaysRun)] static void M() {} }")]
    [TestCase("[PatchOptions(PatchOptions.AlwaysRun)] [Patch, Target] class C { [Prefix, PatchOptions(PatchOptions.Default)] static bool M() => true; }")]
    [TestCase("[PatchOptions(PatchOptions.AlwaysRun)] [Patch, Target] class C { [Prefix, PatchOptions(PatchOptions.Inline)] static bool M() => true; }")]
    [TestCase("[Patch, Target] class C { [Prefix, PatchOptions(PatchOptions.Inline)] static bool M() => true; }")]
    [TestCase("[Patch, Target] class C<T> { int M<U>() => 0; }")]
    [TestCase("namespace Other { class PrefixAttribute : System.Attribute {} [Patch, Target] class C { [Prefix] int M() => 0; } }")]
    [TestCase("[PatchOptions(PatchOptions.AlwaysRun)] [Patch, Target] class C { [Patch, Target] class Nested { [Prefix] static bool M() => true; } }")]

    [TestCase("[Patch, Target] class C { [Prefix] static void M(int __result) {} }")]
    [TestCase("class CustomAttribute : PatchOptionsAttribute { public CustomAttribute() : base(PatchOptions.Default) {} } [PatchOptions(PatchOptions.AlwaysRun)] [Patch, Target] class C { [Prefix, Custom] static bool M() => true; }")]
    [TestCase("[PatchOptions(PatchOptions.AlwaysRun)] [Patch, Target] class B {} [PatchOptions(PatchOptions.Default)] [Patch, Target] class C : B { [Prefix] static bool M() => true; }")]
    [TestCase("[System.AttributeUsage(System.AttributeTargets.Class, Inherited = false)] class CustomAttribute : PatchOptionsAttribute { public CustomAttribute() : base(PatchOptions.AlwaysRun) {} } [Custom] [Patch, Target] class B {} [Patch, Target] class C : B { [Prefix] static bool M() => true; }")]
    [TestCase("namespace HarmonyLib { class HarmonyPostfix : System.Attribute {} [Patch, Target] class C { [HarmonyPostfix] static int M() => 0; } }")]
    public async Task ValidOrUnrelatedCodeDoesNotWarn(string source)
    {
        Assert.That(await Analyze(source), Is.Empty);
    }

    [Test]
    public async Task IndependentViolationsAreAllReported()
    {
        var diagnostics = await Analyze("[Patch, Target] class C<T> { [Postfix] int M() => 0; }");
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
        var diagnostics = await Analyze("[Patch, Target] class B { [Prefix] public virtual void M() {} } [Patch, Target] class C : B { public override void M() {} }");
        Assert.That(diagnostics.Select(d => d.Id), Is.EquivalentTo(new[] { "DH0002", "DH0002" }));
    }

    [Test]
    public async Task NonInheritedPatchAttributeDoesNotApplyToOverride()
    {
        var diagnostics = await Analyze("[System.AttributeUsage(System.AttributeTargets.Method, Inherited = false)] class CustomAttribute : PrefixAttribute {} [Patch, Target] class B { [Custom] public virtual void M() {} } [Patch, Target] class C : B { public override void M() {} }");
        Assert.That(diagnostics.Select(d => d.Id), Is.EquivalentTo(new[] { "DH0002" }));
    }

    [TestCase("class C { [Prefix, Target] static void M() {} }", "DH0006")]
    [TestCase("class C { [Postfix, Targets] static void M() {} }", "DH0006")]
    [TestCase("[Patch] class Outer { class C { [Prefix, Target] static void M() {} } }", "DH0006")]
    [TestCase("[HarmonyLib.HarmonyPatch] class Outer { class C { [Prefix, Target] static void M() {} } }", "DH0006")]
    [TestCase("class C { [HarmonyLib.HarmonyPatch, Prefix, Target] static void M() {} }", "DH0006")]
    [TestCase("[Patch] class C { [Prefix] static void M() {} }", "DH0007")]
    [TestCase("[HarmonyLib.HarmonyPatch] class C { [Postfix] static void M() {} }", "DH0007")]
    [TestCase("[Patch, Target] class Outer { [Patch] class C { [Prefix] static void M() {} } }", "DH0007")]
    [TestCase("[Patch] class C { [Prefix, Inner] static void M() {} }", "DH0007")]
    [TestCase("class C { [Target] static void M() {} }", "DH0008")]
    [TestCase("[Patch] class C { [Targets] static void M() {} }", "DH0008")]
    [TestCase("class C { [Inner] static void M() {} }", "DH0008")]
    [TestCase("class C { [InnerConstant] static void M() {} }", "DH0008")]
    [TestCase("class C { [Priority] static void M() {} }", "DH0008")]
    [TestCase("class C { [PatchOptions(PatchOptions.Default)] static void M() {} }", "DH0008")]
    [TestCase("class C { [Target, Inner, Priority] static void M() {} }", "DH0008")]
    [TestCase("class CustomAttribute : TargetAttribute {} class C { [Custom] static void M() {} }", "DH0008")]
    [TestCase("using T = Disharmony.TargetAttribute; class C { [T] static void M() {} }", "DH0008")]
    [TestCase("class C { [global::Disharmony.TargetsAttribute] static void M() {} }", "DH0008")]
    [TestCase("[System.AttributeUsage(System.AttributeTargets.Class, Inherited = false)] class CustomAttribute : PatchAttribute {} [Custom] class B {} class C : B { [Prefix, Target] static void M() {} }", "DH0006")]
    [TestCase("[System.AttributeUsage(System.AttributeTargets.Class, Inherited = false)] class CustomAttribute : TargetAttribute {} [Custom] class B {} [Patch] class C : B { [Prefix] static void M() {} }", "DH0007")]
    public async Task DiscoveryViolationReportsWarningAtMethodName(string source, string expectedId)
    {
        var diagnostics = await Analyze(source);
        Assert.That(diagnostics, Has.Length.EqualTo(1));
        Assert.That(diagnostics[0].Id, Is.EqualTo(expectedId));
        Assert.That(diagnostics[0].Severity, Is.EqualTo(DiagnosticSeverity.Warning));
        var text = await diagnostics[0].Location.SourceTree!.GetTextAsync();
        Assert.That(text.ToString(diagnostics[0].Location.SourceSpan), Is.EqualTo("M"));
    }

    [TestCase("[Patch] class C { [Prefix, Target] static void M() {} }")]
    [TestCase("[HarmonyLib.HarmonyPatch] class C { [Postfix, Targets] static void M() {} }")]
    [TestCase("[Patch, Target] class C { [Prefix] static void M() {} }")]
    [TestCase("[Patch, Targets] class C { [Postfix] static void M() {} }")]
    [TestCase("[Patch, Targets] class B {} class C : B { [Prefix] static void M() {} }")]
    [TestCase("[HarmonyLib.HarmonyPatch] class B {} class C : B { [Prefix, Target] static void M() {} }")]
    [TestCase("[Patch, Target] partial class C {} partial class C { [Prefix] static void M() {} }")]
    [TestCase("[Patch, Target, Priority, PatchOptions(PatchOptions.AlwaysRun)] class C { static void M() {} }")]
    [TestCase("class C { static void M([Parameter] int value) {} }")]
    [TestCase("class C { [System.Obsolete] static void M() {} }")]
    [TestCase("class C { [HarmonyLib.HarmonyPatch] static void M() {} }")]
    [TestCase("class TargetAttribute : System.Attribute {} class C { [Target] static void M() {} }")]
    [TestCase("class PatchAttribute : Disharmony.PatchAttribute {} class C : B { [Prefix, Target] static void M() {} } [Patch] class B {}")]
    [TestCase("class CustomAttribute : TargetsAttribute {} [Patch, Custom] class C { [Prefix] static void M() {} }")]
    [TestCase("[Patch] class C { [Prefix, Target(typeof(C), \"DoesNotExist\")] static void M() {} }")]
    [TestCase("[Patch] class C { [Prefix, Target, Targets] static void M() {} }")]
    public async Task DiscoverablePatchesAndUnattributedHelpersDoNotWarn(string source)
    {
        Assert.That(await Analyze(source), Is.Empty);
    }

    [Test]
    public async Task MissingClassAndTargetAreBothReported()
    {
        var diagnostics = await Analyze("class C { [Prefix] static void M() {} }");
        Assert.That(diagnostics.Select(d => d.Id), Is.EquivalentTo(new[] { "DH0006", "DH0007" }));
    }

    [Test]
    public async Task InheritedMethodAttributeDoesNotWarnOnUnattributedOverride()
    {
        var diagnostics = await Analyze("class B { [Target] public virtual void M() {} } class C : B { public override void M() {} }");
        Assert.That(diagnostics.Select(d => d.Id), Is.EquivalentTo(new[] { "DH0008" }));
        Assert.That(diagnostics[0].Location.SourceSpan.Start, Is.LessThan(
            diagnostics[0].Location.SourceTree!.ToString().IndexOf("class C", StringComparison.Ordinal)));
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
