namespace Disharmony.Tests;

public static class ResultBindingPatches
{
    public static int ValueObserved;
    public static string? ReferenceObserved;
    public static int InnerObserved;

    [Prefix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.IntResult))]
    public static void PrefixReadsDefaultValueTypeResult(int __result) => ValueObserved = __result;

    [Postfix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.IntResult))]
    public static void PostfixReadsValueTypeResult(int __result) => ValueObserved = __result;

    [Prefix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.StringResult))]
    public static void PrefixReadsDefaultReferenceTypeResult(string? __result) => ReferenceObserved = __result;

    [Postfix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.StringResult))]
    public static void PostfixReadsReferenceTypeResult(string __result) => ReferenceObserved = __result;

    [Prefix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.ThrowingIntResult))]
    public static bool PrefixCanWriteValueTypeResultByReference(ref int __result)
    {
        __result = 42;
        return false;
    }

    [Postfix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.IntResult))]
    public static void PostfixCanWriteValueTypeResultByReference(ref int __result) => __result = 42;

    [Prefix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.ThrowingStringResult))]
    public static bool PrefixCanWriteReferenceTypeResultByReference(ref string? __result)
    {
        __result = "patched";
        return false;
    }

    [Postfix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.StringResult))]
    public static void PostfixCanWriteReferenceTypeResultByReference(ref string __result) => __result = "patched";

    [InnerPrefix(typeof(InnerStaticMethodTargets), nameof(InnerStaticMethodTargets.IntResult))]
    [Target(typeof(OuterStaticMethodTargets), nameof(OuterStaticMethodTargets.IntResult))]
    public static void InnerPrefixReadsDefaultInnerResult(int __result) => InnerObserved = __result;

    [InnerPostfix(typeof(InnerStaticMethodTargets), nameof(InnerStaticMethodTargets.IntResult))]
    [Target(typeof(OuterStaticMethodTargets), nameof(OuterStaticMethodTargets.IntResult))]
    public static void InnerPostfixReadsInnerResult(int __result) => InnerObserved = __result;

    [InnerPrefix(typeof(InnerStaticMethodTargets), nameof(InnerStaticMethodTargets.IntResult))]
    [Target(typeof(OuterStaticMethodTargets), nameof(OuterStaticMethodTargets.IntResult))]
    public static bool InnerPrefixCanWriteInnerResultByReference(ref int __result)
    {
        __result = 42;
        return false;
    }

    [InnerPostfix(typeof(InnerStaticMethodTargets), nameof(InnerStaticMethodTargets.IntResult))]
    [Target(typeof(OuterStaticMethodTargets), nameof(OuterStaticMethodTargets.IntResult))]
    public static void InnerPostfixCanWriteInnerResultByReference(ref int __result) => __result = 42;

    [Postfix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.IntResult))]
    public static void ReturnValueAttributeBindsWritableResult([ReturnValue] ref int value) => value = 42;
}

[TestFixture]
public sealed partial class ResultBindingTests : PatchTestBase
{
    [Test]
    public void PrefixReadsDefaultValueTypeResult()
    {
        ResultBindingPatches.ValueObserved = -1;
        ApplyPatch(typeof(ResultBindingPatches), nameof(ResultBindingPatches.PrefixReadsDefaultValueTypeResult));
        StaticMethodTargets.IntResult();
        Assert.That(ResultBindingPatches.ValueObserved, Is.Zero);
    }

    [Test]
    public void PostfixReadsValueTypeResult()
    {
        ResultBindingPatches.ValueObserved = 0;
        ApplyPatch(typeof(ResultBindingPatches), nameof(ResultBindingPatches.PostfixReadsValueTypeResult));
        StaticMethodTargets.IntResult();
        Assert.That(ResultBindingPatches.ValueObserved, Is.EqualTo(1));
    }

    [Test]
    public void PrefixReadsDefaultReferenceTypeResult()
    {
        ResultBindingPatches.ReferenceObserved = "sentinel";
        ApplyPatch(typeof(ResultBindingPatches), nameof(ResultBindingPatches.PrefixReadsDefaultReferenceTypeResult));
        StaticMethodTargets.StringResult();
        Assert.That(ResultBindingPatches.ReferenceObserved, Is.Null);
    }

    [Test]
    public void PostfixReadsReferenceTypeResult()
    {
        ResultBindingPatches.ReferenceObserved = null;
        ApplyPatch(typeof(ResultBindingPatches), nameof(ResultBindingPatches.PostfixReadsReferenceTypeResult));
        StaticMethodTargets.StringResult();
        Assert.That(ResultBindingPatches.ReferenceObserved, Is.EqualTo("original"));
    }

    [Test]
    public void PrefixCanWriteValueTypeResultByReference()
    {
        ApplyPatch(typeof(ResultBindingPatches), nameof(ResultBindingPatches.PrefixCanWriteValueTypeResultByReference));
        Assert.That(StaticMethodTargets.ThrowingIntResult(), Is.EqualTo(42));
    }

    [Test]
    public void PostfixCanWriteValueTypeResultByReference()
    {
        ApplyPatch(typeof(ResultBindingPatches), nameof(ResultBindingPatches.PostfixCanWriteValueTypeResultByReference));
        Assert.That(StaticMethodTargets.IntResult(), Is.EqualTo(42));
    }

    [Test]
    public void PrefixCanWriteReferenceTypeResultByReference()
    {
        ApplyPatch(typeof(ResultBindingPatches), nameof(ResultBindingPatches.PrefixCanWriteReferenceTypeResultByReference));
        Assert.That(StaticMethodTargets.ThrowingStringResult(), Is.EqualTo("patched"));
    }

    [Test]
    public void PostfixCanWriteReferenceTypeResultByReference()
    {
        ApplyPatch(typeof(ResultBindingPatches), nameof(ResultBindingPatches.PostfixCanWriteReferenceTypeResultByReference));
        Assert.That(StaticMethodTargets.StringResult(), Is.EqualTo("patched"));
    }
}

[TestFixture]
public sealed partial class ResultBindingTests
{
    [Test]
    public void InnerPrefixReadsDefaultInnerResult()
    {
        ResultBindingPatches.InnerObserved = -1;
        ApplyPatch(typeof(ResultBindingPatches), nameof(ResultBindingPatches.InnerPrefixReadsDefaultInnerResult));
        OuterStaticMethodTargets.IntResult();
        Assert.That(ResultBindingPatches.InnerObserved, Is.Zero);
    }

    [Test]
    public void InnerPostfixReadsInnerResult()
    {
        ResultBindingPatches.InnerObserved = 0;
        ApplyPatch(typeof(ResultBindingPatches), nameof(ResultBindingPatches.InnerPostfixReadsInnerResult));
        OuterStaticMethodTargets.IntResult();
        Assert.That(ResultBindingPatches.InnerObserved, Is.EqualTo(1));
    }

    [Test]
    public void InnerPrefixCanWriteInnerResultByReference()
    {
        ApplyPatch(typeof(ResultBindingPatches), nameof(ResultBindingPatches.InnerPrefixCanWriteInnerResultByReference));
        Assert.That(OuterStaticMethodTargets.IntResult(), Is.EqualTo(42));
    }

    [Test]
    public void InnerPostfixCanWriteInnerResultByReference()
    {
        ApplyPatch(typeof(ResultBindingPatches), nameof(ResultBindingPatches.InnerPostfixCanWriteInnerResultByReference));
        Assert.That(OuterStaticMethodTargets.IntResult(), Is.EqualTo(42));
    }

    [Test]
    public void ReturnValueAttributeBindsWritableResult()
    {
        ApplyPatch(typeof(ResultBindingPatches), nameof(ResultBindingPatches.ReturnValueAttributeBindsWritableResult));

        Assert.That(StaticMethodTargets.IntResult(), Is.EqualTo(42));
    }
}
