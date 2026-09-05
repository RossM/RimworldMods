using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using NUnit.Framework;

namespace Disharmony.Analyzers.Tests;

public partial class PatchMethodAnalyzerTests
{
    [TestCase("[Patch, Target(typeof(object), \"M\")] class C { [Postfix, PatchOptions(PatchOptions.AlwaysRun)] static void M(System.Exception __exception, [Exception] System.Exception other) {} }")]
    [TestCase("[Patch, Target(typeof(object), \"M\")] class C { [Prefix] static void M(object __instance, [Instance(Scope.Outer)] object other) {} }")]
    [TestCase("[Patch, Target(typeof(object), \"M\")] class C { [Prefix] static void M(int value, [Parameter(\"value\")] int other) {} }")]
    [TestCase("[Patch, Target(typeof(object), \"M\")] class C { [Prefix] static void M([Parameter(0)] int a, [Parameter(0, Scope.Outer)] int b) {} }")]
    [TestCase("[Patch, Target(typeof(object), \"M\")] class C { [Prefix] static void M(int ___field, [Field(\"field\")] int other) {} }")]
    [TestCase("[Patch, Target(typeof(object), \"M\")] class C { [Prefix] static void M(System.Action __base, [BaseMethod] System.Action other) {} }")]
    [TestCase("[Patch, Target(typeof(object), \"M\")] class C { [Prefix] static void M([Method] System.Action action, [Method(\"action\")] System.Action other) {} }")]
    [TestCase("[Patch, Target(typeof(object), \"M\")] class C { [Prefix] static void M(ref int __state, [State(\"__state\")] int other) {} }")]
    [TestCase("[Patch, Target(typeof(object), \"M\")] class C { [Prefix, Inner(typeof(object), \"I\")] static void M([Parameter(1)] int a, [Parameter(1, Scope.Inner)] int b) {} }")]
    [TestCase("[Patch, Target(typeof(object), \"M\")] class C { [Prefix, Inner(typeof(object), \"I\")] static void M(int value, [Parameter(\"value\")] int other) {} }")]
    [TestCase("[Patch, Target(typeof(object), \"M\")] class C { [Postfix] static void M(int __result, [ReturnValue] in int value) {} }")]
    [TestCase("[Patch, Target(typeof(object), \"M\")] class C { [Prefix, InnerConstant(1)] static void M(object __caller, [Instance(Scope.Outer)] object outer, int value, [Field] int field, [Parameter(0, Scope.Outer)] int x) {} }")]
    public async Task DuplicateBindingsWarnOnEachParameter(string source)
    {
        var diagnostics = await Analyze(source);
        Assert.That(diagnostics.Select(d => d.Id), Is.EquivalentTo(new[] { "DH0028", "DH0028" }));
        Assert.That(diagnostics.All(d => d.Severity == DiagnosticSeverity.Warning), Is.True);
        Assert.That(diagnostics.Select(d => d.Location.SourceSpan).Distinct().Count(), Is.EqualTo(2));
    }

    [TestCase("[Patch, Target(typeof(object), \"M\")] class C { [Prefix] static void M(int value, [Parameter(0)] int other) {} }")]
    [TestCase("[Patch, Target(typeof(object), \"M\")] class C { [Prefix, Inner(typeof(object), \"I\")] static void M([Parameter(\"x\", Scope.Inner)] int a, [Parameter(\"x\", Scope.Outer)] int b, [Parameter(\"x\")] int c) {} }")]
    [TestCase("[Patch, Target(typeof(object), \"M\")] class C { [Prefix, Inner(typeof(object), \"I\")] static void M(object __instance, object __caller) {} }")]
    [TestCase("[Patch, Target(typeof(object), \"M\")] class C { [Prefix] static void M(int value, [Field(\"value\")] int field) {} }")]
    [TestCase("[Patch] partial class C { [Prefix, Target(typeof(object), \"A\")] static void A([State(\"key\")] out int a) { a = 1; } } partial class C { [Postfix, Target(typeof(object), \"B\")] static void B([State(\"key\")] in int b) {} }")]
    [TestCase("[Patch, Target(typeof(object), \"M\")] class C { [Postfix] static void A(ref int __state) {} [Prefix] static void B(int __state) {} }")]
    public async Task DistinctBindingsAndWrittenStateDoNotWarn(string source)
    {
        Assert.That(await Analyze(source), Is.Empty);
    }

    [TestCase("[Patch, Target(typeof(object), \"M\")] class C { [Prefix] static void A(in int __state) {} static void Helper(ref int __state) {} }")]
    [TestCase("[Patch, Target(typeof(object), \"M\")] class B { [Prefix] static void A(ref int __state) {} } class C : B { [Postfix] static void B(int __state) {} }")]
    [TestCase("[Patch, Target(typeof(object), \"M\")] class C { [Prefix] static void A([State(\"written\")] ref int a) {} [Postfix] static void B([State(\"unwritten\")] int b) {} }")]
    public async Task StateWithoutMatchingPatchWriterWarns(string source)
    {
        var diagnostics = await Analyze(source);
        Assert.That(diagnostics.Select(d => d.Id), Is.EqualTo(new[] { "DH0029" }));
        Assert.That(diagnostics[0].Severity, Is.EqualTo(DiagnosticSeverity.Warning));
    }
}