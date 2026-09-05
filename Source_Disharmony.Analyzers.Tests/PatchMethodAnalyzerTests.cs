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

public partial class PatchMethodAnalyzerTests
{
    // Minimal metadata contract: no game or patch execution is needed to analyze source.
    private const string Attributes = """
        namespace Disharmony
        {
            public enum MemberType { Any, Method, Getter, Setter, Constructor }
            public enum PatchType { Prefix, Postfix }
            [System.AttributeUsage(System.AttributeTargets.Class)]
            public class PatchAttribute : System.Attribute
            {
                public PatchAttribute(System.Type type = null) { }
            }
            [System.AttributeUsage(System.AttributeTargets.Class)]
            public class CategoryAttribute : System.Attribute
            {
                public CategoryAttribute(string category) { }
            }
            [System.AttributeUsage(System.AttributeTargets.Class | System.AttributeTargets.Method, AllowMultiple = true)]
            public class TargetAttribute : System.Attribute
            {
                public TargetAttribute(System.Type type, string methodName = null, MemberType memberType = MemberType.Any) { }
                public TargetAttribute(string methodName = null, MemberType memberType = MemberType.Any) { }
            }
            public class TargetsAttribute : TargetAttribute
            {
                public TargetsAttribute(System.Type type, string methodName = null, MemberType memberType = MemberType.Any) { }
                public TargetsAttribute(string methodName = null, MemberType memberType = MemberType.Any) { }
            }
            public abstract class InnerAttributeBase : System.Attribute { }
            [System.AttributeUsage(System.AttributeTargets.Method)]
            public class InnerAttribute : InnerAttributeBase
            {
                public InnerAttribute(System.Type type, string memberName = null, MemberType memberType = MemberType.Any) { }
            }
            [System.AttributeUsage(System.AttributeTargets.Method)]
            public class InnerConstantAttribute : InnerAttributeBase
            {
                public InnerConstantAttribute(int value) { }
                public InnerConstantAttribute(long value) { }
                public InnerConstantAttribute(float value) { }
                public InnerConstantAttribute(double value) { }
                public InnerConstantAttribute(string value) { }
            }
            [System.AttributeUsage(System.AttributeTargets.Class | System.AttributeTargets.Method)]
            public class PriorityAttribute : System.Attribute { }
            public enum Scope { Any, Inner, Outer }
            public abstract class ParameterBindingAttribute : System.Attribute
            {
                protected ParameterBindingAttribute(Scope scope) { }
            }
            [System.AttributeUsage(System.AttributeTargets.Parameter)]
            public class ParameterAttribute : ParameterBindingAttribute
            {
                public ParameterAttribute(Scope scope = Scope.Any) : base(scope) { }
                public ParameterAttribute(string name, Scope scope = Scope.Any) : base(scope) { }
                public ParameterAttribute(int index, Scope scope = Scope.Any) : base(scope) { }
            }
            [System.AttributeUsage(System.AttributeTargets.Parameter)]
            public class InstanceAttribute : ParameterBindingAttribute
            {
                public InstanceAttribute(Scope scope = Scope.Any) : base(scope) { }
            }
            [System.AttributeUsage(System.AttributeTargets.Parameter)]
            public class ReturnValueAttribute : ParameterBindingAttribute
            {
                public ReturnValueAttribute() : base(Scope.Any) { }
            }
            [System.AttributeUsage(System.AttributeTargets.Parameter)]
            public class StateAttribute : ParameterBindingAttribute
            {
                public StateAttribute(string key = null) : base(Scope.Outer) { }
            }
            [System.AttributeUsage(System.AttributeTargets.Parameter)]
            public class FieldAttribute : ParameterBindingAttribute
            {
                public FieldAttribute(Scope scope = Scope.Any) : base(scope) { }
                public FieldAttribute(string name, Scope scope = Scope.Any) : base(scope) { }
            }
            [System.AttributeUsage(System.AttributeTargets.Parameter)]
            public class BaseMethodAttribute : ParameterBindingAttribute
            {
                public BaseMethodAttribute() : base(Scope.Outer) { }
            }
            [System.AttributeUsage(System.AttributeTargets.Parameter)]
            public class MethodAttribute : ParameterBindingAttribute
            {
                public MethodAttribute(Scope scope = Scope.Any) : base(scope) { }
                public MethodAttribute(string name, Scope scope = Scope.Any) : base(scope) { }
            }
            [System.AttributeUsage(System.AttributeTargets.Parameter)]
            public class ExceptionAttribute : ParameterBindingAttribute
            {
                public ExceptionAttribute() : base(Scope.Any) { }
            }
            [System.Flags]
            public enum PatchOptions { Default = 0, Inline = 1, AlwaysRun = 4, AllowUnsafe = 8 }
            public abstract class PatchTypeAttribute : System.Attribute { }
            [System.AttributeUsage(System.AttributeTargets.Method)]
            public class PrefixAttribute : PatchTypeAttribute { }
            [System.AttributeUsage(System.AttributeTargets.Method)]
            public class PostfixAttribute : PatchTypeAttribute { }
            [System.AttributeUsage(System.AttributeTargets.Class | System.AttributeTargets.Method)]
            public class PatchOptionsAttribute : System.Attribute
            {
                public PatchOptionsAttribute(PatchOptions options) { }
            }
        }
        namespace HarmonyLib
        {
            public enum MethodType { Normal, Getter, Setter, Constructor }
            [System.AttributeUsage(System.AttributeTargets.Class | System.AttributeTargets.Method, AllowMultiple = true)]
            public class HarmonyPatch : System.Attribute
            {
                public HarmonyPatch() { }
                public HarmonyPatch(System.Type declaringType) { }
                public HarmonyPatch(string methodName) { }
                public HarmonyPatch(string typeName, string methodName, MethodType methodType) { }
            }
            [System.AttributeUsage(System.AttributeTargets.Class)]
            public class HarmonyPatchCategory : System.Attribute
            {
                public HarmonyPatchCategory(string category) { }
            }
        }
        """;

    [TestCase("[Patch, Target(typeof(object), \"M\")] class C { [Prefix] static void M<T>() {} }", "DH0001")]
    [TestCase("[Patch, Target(typeof(object), \"M\")] class C<T> { [Prefix] static void M() {} }", "DH0001")]
    [TestCase("[Patch, Target(typeof(object), \"M\")] class C<T> { [Patch, Target(typeof(object), \"M\")] class Nested { [Postfix] static void M() {} } }", "DH0001")]
    [TestCase("[Patch, Target(typeof(object), \"M\")] class C { [Prefix] void M() {} }", "DH0002")]
    [TestCase("[Patch, Target(typeof(object), \"M\")] class C { [Postfix] void M() {} }", "DH0002")]
    [TestCase("[Patch, Target(typeof(object), \"M\")] class C { [Prefix] static int M() => 0; }", "DH0003")]
    [TestCase("[Patch, Target(typeof(object), \"M\")] class C { [Prefix] static bool? M() => null; }", "DH0003")]
    [TestCase("[Patch, Target(typeof(object), \"M\")] class C { static bool b; [Prefix] static ref bool M() => ref b; }", "DH0003")]
    [TestCase("[Patch, Target(typeof(object), \"M\")] class C { static bool b; [Prefix] static ref readonly bool M() => ref b; }", "DH0003")]
    [TestCase("[Patch, Target(typeof(object), \"M\")] class C { [Postfix] static bool M() => true; }", "DH0004")]
    [TestCase("[Patch, Target(typeof(object), \"M\")] class C { [Prefix, PatchOptions(PatchOptions.AlwaysRun)] static bool M() => true; }", "DH0005")]
    [TestCase("[Patch, Target(typeof(object), \"M\")] class C { [Prefix, PatchOptions(PatchOptions.AlwaysRun)] static int M() => 0; }", "DH0005")]
    [TestCase("[PatchOptions(PatchOptions.AlwaysRun)] [Patch, Target(typeof(object), \"M\")] class C { [Prefix] static bool M() => true; }", "DH0005")]
    [TestCase("[Patch, Target(typeof(object), \"M\")] class C { [Prefix, PatchOptions(PatchOptions.Inline | PatchOptions.AlwaysRun)] static bool M() => true; }", "DH0005")]
    [TestCase("[PatchOptions(PatchOptions.Default)] [Patch, Target(typeof(object), \"M\")] class C { [Prefix, PatchOptions(PatchOptions.AlwaysRun)] static bool M() => true; }", "DH0005")]
    [TestCase("[Patch, Target(typeof(object), \"M\")] partial class C { [Prefix] static bool M() => true; } [PatchOptions(PatchOptions.AlwaysRun)] partial class C {}", "DH0005")]
    [TestCase("[Patch, Target(typeof(object), \"M\")] class C { [global::Disharmony.PrefixAttribute] static int M() => 0; }", "DH0003")]
    [TestCase("using P = Disharmony.PrefixAttribute; [Patch, Target(typeof(object), \"M\")] class C { [P] static int M() => 0; }", "DH0003")]
    [TestCase("[PatchOptions(PatchOptions.AlwaysRun)] [Patch, Target(typeof(object), \"M\")] class B {} [Patch, Target(typeof(object), \"M\")] class C : B { [Prefix] static bool M() => true; }", "DH0005")]
    public async Task InvalidPatchReportsWarningAtMethodName(string source, string expectedId)
    {
        var diagnostics = await Analyze(source);
        Assert.That(diagnostics, Has.Length.EqualTo(1));
        Assert.That(diagnostics[0].Id, Is.EqualTo(expectedId));
        Assert.That(diagnostics[0].Severity, Is.EqualTo(DiagnosticSeverity.Warning));
        var text = await diagnostics[0].Location.SourceTree!.GetTextAsync();
        Assert.That(text.ToString(diagnostics[0].Location.SourceSpan), Is.EqualTo("M"));
    }

    [TestCase("[Patch, Target(typeof(object), \"M\")] class C { [Prefix] static void M() {} }")]
    [TestCase("[Patch, Target(typeof(object), \"M\")] class C { [Prefix] static bool M() => true; }")]
    [TestCase("[Patch, Target(typeof(object), \"M\")] class C { [Postfix] static void M() {} }")]
    [TestCase("[Patch, Target(typeof(object), \"M\")] class C { [Prefix, PatchOptions(PatchOptions.AlwaysRun)] static void M() {} }")]
    [TestCase("[Patch, Target(typeof(object), \"M\")] class C { [Postfix, PatchOptions(PatchOptions.AlwaysRun)] static void M() {} }")]
    [TestCase("[PatchOptions(PatchOptions.AlwaysRun)] [Patch, Target(typeof(object), \"M\")] class C { [Prefix, PatchOptions(PatchOptions.Default)] static bool M() => true; }")]
    [TestCase("[PatchOptions(PatchOptions.AlwaysRun)] [Patch, Target(typeof(object), \"M\")] class C { [Prefix, PatchOptions(PatchOptions.Inline)] static bool M() => true; }")]
    [TestCase("[Patch, Target(typeof(object), \"M\")] class C { [Prefix, PatchOptions(PatchOptions.Inline)] static bool M() => true; }")]
    [TestCase("[Patch, Target(typeof(object), \"M\")] class C<T> { int M<U>() => 0; }")]
    [TestCase("namespace Other { class PrefixAttribute : System.Attribute {} [Patch, Target(typeof(object), \"M\")] class C { [Prefix] int M() => 0; } }")]
    [TestCase("[PatchOptions(PatchOptions.AlwaysRun)] [Patch, Target(typeof(object), \"M\")] class C { [Patch, Target(typeof(object), \"M\")] class Nested { [Prefix] static bool M() => true; } }")]

    [TestCase("[Patch, Target(typeof(object), \"M\")] class C { [Prefix] static void M(int __result) {} }")]
    [TestCase("[PatchOptions(PatchOptions.AlwaysRun)] [Patch, Target(typeof(object), \"M\")] class B {} [PatchOptions(PatchOptions.Default)] [Patch, Target(typeof(object), \"M\")] class C : B { [Prefix] static bool M() => true; }")]
    [TestCase("namespace HarmonyLib { class HarmonyPostfix : System.Attribute {} [Patch, Target(typeof(object), \"M\")] class C { [HarmonyPostfix] static int M() => 0; } }")]
    public async Task ValidOrUnrelatedCodeDoesNotWarn(string source)
    {
        Assert.That(await Analyze(source), Is.Empty);
    }

    [Test]
    public async Task IndependentViolationsAreAllReported()
    {
        var diagnostics = await Analyze("[Patch, Target(typeof(object), \"M\")] class C<T> { [Postfix] int M() => 0; }");
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
        var diagnostics = await Analyze("[Patch, Target(typeof(object), \"M\")] class B { [Prefix] public virtual void M() {} } [Patch, Target(typeof(object), \"M\")] class C : B { public override void M() {} }");
        Assert.That(diagnostics.Select(d => d.Id), Is.EquivalentTo(new[] { "DH0002", "DH0002" }));
    }

    [Test]
    public async Task CustomPatchAttributeIsIgnored()
    {
        var diagnostics = await Analyze("[System.AttributeUsage(System.AttributeTargets.Method, Inherited = false)] class CustomAttribute : PrefixAttribute {} [Patch, Target(typeof(object), \"M\")] class B { [Custom] public virtual void M() {} } [Patch, Target(typeof(object), \"M\")] class C : B { public override void M() {} }");
        Assert.That(diagnostics, Is.Empty);
    }

    [TestCase("class C { [Prefix, Target(typeof(object), \"M\")] static void M() {} }", "DH0006")]
    [TestCase("class C { [Postfix, Targets(typeof(object), \"M\")] static void M() {} }", "DH0006")]
    [TestCase("[Patch] class Outer { class C { [Prefix, Target(typeof(object), \"M\")] static void M() {} } }", "DH0006")]
    [TestCase("[HarmonyLib.HarmonyPatch] class Outer { class C { [Prefix, Target(typeof(object), \"M\")] static void M() {} } }", "DH0006")]
    [TestCase("class C { [HarmonyLib.HarmonyPatch, Prefix, Target(typeof(object), \"M\")] static void M() {} }", "DH0006")]
    [TestCase("[Patch] class C { [Prefix] static void M() {} }", "DH0007")]
    [TestCase("[HarmonyLib.HarmonyPatch] class C { [Postfix] static void M() {} }", "DH0007")]
    [TestCase("[Patch, Target(typeof(object), \"M\")] class Outer { [Patch] class C { [Prefix] static void M() {} } }", "DH0007")]
    [TestCase("[Patch] class C { [Prefix, Inner(typeof(object), \"M\")] static void M() {} }", "DH0007")]
    [TestCase("class C { [Target(typeof(object), \"M\")] static void M() {} }", "DH0008")]
    [TestCase("[Patch] class C { [Targets(typeof(object), \"M\")] static void M() {} }", "DH0008")]
    [TestCase("class C { [Inner(typeof(object), \"M\")] static void M() {} }", "DH0008")]
    [TestCase("class C { [InnerConstant(1)] static void M() {} }", "DH0008")]
    [TestCase("class C { [Priority] static void M() {} }", "DH0008")]
    [TestCase("class C { [PatchOptions(PatchOptions.Default)] static void M() {} }", "DH0008")]
    [TestCase("class C { [Target(typeof(object), \"M\"), Inner(typeof(object), \"M\"), Priority] static void M() {} }", "DH0008")]
    [TestCase("using T = Disharmony.TargetAttribute; class C { [T] static void M() {} }", "DH0008")]
    [TestCase("class C { [global::Disharmony.TargetsAttribute] static void M() {} }", "DH0008")]
    public async Task DiscoveryViolationReportsWarningAtMethodName(string source, string expectedId)
    {
        var diagnostics = await Analyze(source);
        Assert.That(diagnostics, Has.Length.EqualTo(1));
        Assert.That(diagnostics[0].Id, Is.EqualTo(expectedId));
        Assert.That(diagnostics[0].Severity, Is.EqualTo(DiagnosticSeverity.Warning));
        var text = await diagnostics[0].Location.SourceTree!.GetTextAsync();
        Assert.That(text.ToString(diagnostics[0].Location.SourceSpan), Is.EqualTo("M"));
    }

    [TestCase("[Patch] class C { [Prefix, Target(typeof(object), \"M\")] static void M() {} }")]
    [TestCase("[HarmonyLib.HarmonyPatch] class C { [Postfix, Targets(typeof(object), \"M\")] static void M() {} }")]
    [TestCase("[Patch, Target(typeof(object), \"M\")] class C { [Prefix] static void M() {} }")]
    [TestCase("[Patch, Targets(typeof(object), \"M\")] class C { [Postfix] static void M() {} }")]
    [TestCase("[Patch, Targets(typeof(object), \"M\")] class B {} class C : B { [Prefix] static void M() {} }")]
    [TestCase("[HarmonyLib.HarmonyPatch] class B {} class C : B { [Prefix, Target(typeof(object), \"M\")] static void M() {} }")]
    [TestCase("[Patch, Target(typeof(object), \"M\")] partial class C {} partial class C { [Prefix] static void M() {} }")]
    [TestCase("[Patch, Target(typeof(object), \"M\"), Priority, PatchOptions(PatchOptions.AlwaysRun)] class C { static void M() {} }")]
    [TestCase("class C { static void M([Parameter] int value) {} }")]
    [TestCase("class C { [System.Obsolete] static void M() {} }")]
    [TestCase("class C { [HarmonyLib.HarmonyPatch] static void M() {} }")]
    [TestCase("class TargetAttribute : System.Attribute {} class C { [Target] static void M() {} }")]
    [TestCase("class C : B { [Prefix, Target(typeof(object), \"M\")] static void M() {} } [Patch] class B {}")]
    [TestCase("[Patch] class C { [Prefix, Target(typeof(C), \"DoesNotExist\")] static void M() {} }")]
    [TestCase("[Patch] class C { [Prefix, Target(typeof(object), \"M\"), Targets(typeof(object), \"M\")] static void M() {} }")]
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
        var diagnostics = await Analyze("class B { [Target(typeof(object), \"M\")] public virtual void M() {} } class C : B { public override void M() {} }");
        Assert.That(diagnostics.Select(d => d.Id), Is.EquivalentTo(new[] { "DH0008" }));
        Assert.That(diagnostics[0].Location.SourceSpan.Start, Is.LessThan(
            diagnostics[0].Location.SourceTree!.ToString().IndexOf("class C", StringComparison.Ordinal)));
    }

    [TestCase("[Patch, Target(typeof(object), \"M\")] class C { [Prefix, Postfix] static void M() {} }", "DH0009", "M")]
    [TestCase("[Patch, Target(typeof(object), \"M\")] class C { [Prefix, Inner(typeof(object), \"M\"), InnerConstant(1)] static void M() {} }", "DH0010", "M")]
    [TestCase("[Patch] class C { [Prefix, Target(\"M\")] static void M() {} }", "DH0011", "Target(\"M\")")]
    [TestCase("[Patch] class C { [Prefix, Targets(\"M\")] static void M() {} }", "DH0011", "Targets(\"M\")")]
    [TestCase("[HarmonyLib.HarmonyPatch(\"M\")] class C { [Prefix, Target(\"M\")] static void M() {} }", "DH0011", "Target(\"M\")")]
    [TestCase("[Patch(typeof(object)), Target(\"M\")] class C { [Prefix, Inner(null, \"M\")] static void M() {} }", "DH0011", "Inner(null, \"M\")")]
    [TestCase("[Target(\"M\")] class B {} [Patch] class C : B { [Prefix, Target(typeof(object), \"M\")] static void M() {} }", "DH0011", "Target(\"M\")")]
    [TestCase("[Patch, Target(typeof(object), \"M\")] class C { [Prefix, InnerConstant(null)] static void M() {} }", "DH0012", "InnerConstant(null)")]
    [TestCase("[Patch, Target(typeof(object), \"M\")] class C { const string Value = null; [Prefix, InnerConstant(Value)] static void M() {} }", "DH0012", "InnerConstant(Value)")]
    [TestCase("[HarmonyLib.HarmonyPatch(typeof(object)), HarmonyLib.HarmonyPatch(\"M\")] class C {}", "DH0014", "C")]
    [TestCase("[Patch(typeof(object))] class C { [Prefix, Target] static void M() {} }", "DH0015", "Target")]
    [TestCase("[Patch] class C { [Prefix, Targets(typeof(object))] static void M() {} }", "DH0015", "Targets(typeof(object))")]
    [TestCase("[Patch, Target(typeof(object), \"M\")] class C { [Prefix, Inner(typeof(object))] static void M() {} }", "DH0015", "Inner(typeof(object))")]
    public async Task RegistryViolationReportsWarningAtDeclaration(string source, string expectedId, string expectedSpan)
    {
        var diagnostics = await Analyze(source);
        Assert.That(diagnostics, Has.Length.EqualTo(1));
        Assert.That(diagnostics[0].Id, Is.EqualTo(expectedId));
        Assert.That(diagnostics[0].Severity, Is.EqualTo(DiagnosticSeverity.Warning));
        var text = await diagnostics[0].Location.SourceTree!.GetTextAsync();
        Assert.That(text.ToString(diagnostics[0].Location.SourceSpan), Is.EqualTo(expectedSpan));
    }

    [TestCase("[Patch] class C { [Prefix, Target(\"Namespace.Type:M\")] static void M() {} }")]
    [TestCase("[Patch] class C { [Prefix, Targets(\"Namespace.Type.M\")] static void M() {} }")]
    [TestCase("[Patch] class C { [Prefix, Target(\"Namespace.Type.M\")] static void M() {} }")]
    [TestCase("[Patch] class C { [Prefix, Targets(\"Namespace.Type:M\")] static void M() {} }")]
    [TestCase("[Patch(typeof(object))] class C { [Prefix, Target(\"M\")] static void M() {} }")]
    [TestCase("[HarmonyLib.HarmonyPatch(typeof(object))] class C { [Prefix, Target(\"M\")] static void M() {} }")]
    [TestCase("[HarmonyLib.HarmonyPatch(\"RuntimeType\", \"M\", HarmonyLib.MethodType.Normal)] class C { [Prefix, Target(\"M\")] static void M() {} }")]
    [TestCase("[Patch, Target(typeof(object), \"M\")] class C { [Prefix, Inner(null, \"Namespace.Type:M\")] static void M() {} }")]
    [TestCase("[Patch, Target(typeof(object), \"M\")] class C { [Prefix, Inner(null, \"Namespace.Type.M\")] static void M() {} }")]
    [TestCase("[Patch] class C { [Prefix, Target(typeof(object), memberType: MemberType.Constructor)] static void M() {} }")]
    [TestCase("[Patch, Target(typeof(object), \"M\")] class C { [Prefix, Inner(typeof(object), memberType: MemberType.Constructor)] static void M() {} }")]
    [TestCase("[Patch, Target(typeof(object), \"M\")] class C { [Prefix, InnerConstant(1)] static void M() {} }")]
    [TestCase("[Patch, Target(typeof(object), \"M\")] class C { [Prefix, InnerConstant(1L)] static void M() {} }")]
    [TestCase("[Patch, Target(typeof(object), \"M\")] class C { [Prefix, InnerConstant(1F)] static void M() {} }")]
    [TestCase("[Patch, Target(typeof(object), \"M\")] class C { [Prefix, InnerConstant(1D)] static void M() {} }")]
    [TestCase("[Patch, Target(typeof(object), \"M\")] class C { [Prefix, InnerConstant(\"\")] static void M() {} }")]
    [TestCase("[Patch(typeof(object))] class B {} [Patch] class C : B { [Prefix, Target(typeof(object), \"M\")] static void M() {} }")]
    [TestCase("[Patch, Target(typeof(object), \"A\"), Targets(typeof(object), \"B\")] class C { [Prefix, Target(typeof(object), \"C\")] static void M() {} }")]
    public async Task SupportedOrRuntimeDependentRegistryMetadataDoesNotWarn(string source)
    {
        Assert.That(await Analyze(source), Is.Empty);
    }

    [Test]
    public async Task NonMultipleInheritedPatchAttributeIsNotCountedTwice()
    {
        var diagnostics = await Analyze("[Patch, Target(typeof(object), \"M\")] class B { [Prefix] public virtual void M() {} } class C : B { [Prefix] public override void M() {} }");
        Assert.That(diagnostics.Select(d => d.Id), Is.EquivalentTo(new[] { "DH0002", "DH0002" }));
    }

    [Test]
    public async Task MultipleInnerAttributesAreCheckedEvenWithoutPatchType()
    {
        var diagnostics = await Analyze("[Patch] class C { [Inner(typeof(object), \"M\"), InnerConstant(1)] static void M() {} }");
        Assert.That(diagnostics.Select(d => d.Id), Is.EquivalentTo(new[] { "DH0008", "DH0010" }));
    }

    [TestCase("[Patch, HarmonyLib.HarmonyPatch] class C {}", "DH0014")]
    [TestCase("[Patch(typeof(object)), HarmonyLib.HarmonyPatch(typeof(object))] class C {}", "DH0014")]
    [TestCase("[Patch, HarmonyLib.HarmonyPatch(typeof(object)), HarmonyLib.HarmonyPatch(\"M\")] class C {}", "DH0014")]
    [TestCase("[Patch] class B {} [HarmonyLib.HarmonyPatch] class C : B {}", "DH0014")]
    [TestCase("[HarmonyLib.HarmonyPatch] class B {} [Patch] class C : B {}", "DH0014")]
    [TestCase("[Patch] partial class C {} [HarmonyLib.HarmonyPatch] partial class C {}", "DH0014")]
    [TestCase("[Category(\"test\"), HarmonyLib.HarmonyPatchCategory(\"test\")] class C {}", "DH0014")]
    [TestCase("[Patch, Category(\"test\"), HarmonyLib.HarmonyPatchCategory(\"other\")] class C {}", "DH0014")]
    [TestCase("[Patch, Category(null), HarmonyLib.HarmonyPatchCategory(\"test\")] class C {}", "DH0014")]
    [TestCase("[Category(\"test\")] class B {} [HarmonyLib.HarmonyPatchCategory(\"test\")] class C : B {}", "DH0014")]
    [TestCase("[Category(\"test\")] partial class C {} [HarmonyLib.HarmonyPatchCategory(\"test\")] partial class C {}", "DH0014")]
    [TestCase("[Patch, HarmonyLib.HarmonyPatch, Category(\"test\"), HarmonyLib.HarmonyPatchCategory(\"test\")] class C {}", "DH0014,DH0014")]
    public async Task MixedDiscoveryMetadataReportsWarningsOnClass(string source, string expectedIds)
    {
        var diagnostics = await Analyze(source);
        Assert.That(diagnostics.Select(d => d.Id), Is.EquivalentTo(expectedIds.Split(',')));
        foreach (var diagnostic in diagnostics)
        {
            Assert.That(diagnostic.Severity, Is.EqualTo(DiagnosticSeverity.Warning));
            var text = await diagnostic.Location.SourceTree!.GetTextAsync();
            Assert.That(text.ToString(diagnostic.Location.SourceSpan), Is.EqualTo("C"));
        }
    }

    [TestCase("[Patch, HarmonyLib.HarmonyPatchCategory(\"test\")] class C {}")]
    [TestCase("[HarmonyLib.HarmonyPatch, Category(\"test\")] class C {}")]
    [TestCase("[Patch] class Outer { [HarmonyLib.HarmonyPatch] class C {} }")]
    [TestCase("[Category(\"test\")] class Outer { [HarmonyLib.HarmonyPatchCategory(\"test\")] class C {} }")]
    [TestCase("class PatchAttribute : System.Attribute {} [Patch, HarmonyLib.HarmonyPatch] class C {}")]
    [TestCase("class CategoryAttribute : System.Attribute {} [Category, HarmonyLib.HarmonyPatchCategory(\"test\")] class C {}")]
    public async Task SeparateDiscoveryMetadataDoesNotWarn(string source)
    {
        Assert.That(await Analyze(source), Is.Empty);
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
