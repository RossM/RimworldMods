using System;
using NUnit.Framework;

namespace Disharmony.Tests;

public static class InnerPatchMethods
{
    public static int ArgumentObserved;
    public static int ResultObserved;

    [InnerPrefix(typeof(InnerPatchTargets), nameof(InnerPatchTargets.RunTarget))]
    [Target(typeof(OuterPatchTargets), nameof(OuterPatchTargets.RunTarget))]
    public static bool RunTargetPrefix() => true;

    [InnerPrefix(typeof(InnerPatchTargets), nameof(InnerPatchTargets.SkipTarget))]
    [Target(typeof(OuterPatchTargets), nameof(OuterPatchTargets.SkipTarget))]
    public static bool SkipTargetPrefix() => false;

    [InnerPrefix(typeof(InnerPatchTargets), nameof(InnerPatchTargets.ReadArgumentInPrefix))]
    [Target(typeof(OuterPatchTargets), nameof(OuterPatchTargets.ReadArgumentInPrefix))]
    public static void ReadArgumentPrefix(int value) => ArgumentObserved = value;

    [InnerPostfix(typeof(InnerPatchTargets), nameof(InnerPatchTargets.ReadArgumentInPostfix))]
    [Target(typeof(OuterPatchTargets), nameof(OuterPatchTargets.ReadArgumentInPostfix))]
    public static void ReadArgumentPostfix(int value) => ArgumentObserved = value;

    [InnerPrefix(typeof(InnerPatchTargets), nameof(InnerPatchTargets.WriteArgumentInPrefix))]
    [Target(typeof(OuterPatchTargets), nameof(OuterPatchTargets.WriteArgumentInPrefix))]
    public static void WriteArgumentPrefix(ref int value) => value = 42;

    [InnerPostfix(typeof(InnerPatchTargets), nameof(InnerPatchTargets.WriteArgumentInPostfix))]
    [Target(typeof(OuterPatchTargets), nameof(OuterPatchTargets.WriteArgumentInPostfix))]
    public static void WriteArgumentPostfix(ref int value) => value = 42;

    [InnerPrefix(typeof(InnerPatchTargets), nameof(InnerPatchTargets.ReadResultInPrefix))]
    [Target(typeof(OuterPatchTargets), nameof(OuterPatchTargets.ReadResultInPrefix))]
    public static void ReadResultPrefix(int __result) => ResultObserved = __result;

    [InnerPostfix(typeof(InnerPatchTargets), nameof(InnerPatchTargets.ReadResultInPostfix))]
    [Target(typeof(OuterPatchTargets), nameof(OuterPatchTargets.ReadResultInPostfix))]
    public static void ReadResultPostfix(int __result) => ResultObserved = __result;

    [InnerPrefix(typeof(InnerPatchTargets), nameof(InnerPatchTargets.WriteResultInPrefix))]
    [Target(typeof(OuterPatchTargets), nameof(OuterPatchTargets.WriteResultInPrefix))]
    public static bool WriteResultPrefix(ref int __result)
    {
        __result = 42;
        return false;
    }

    [InnerPostfix(typeof(InnerPatchTargets), nameof(InnerPatchTargets.WriteResultInPostfix))]
    [Target(typeof(OuterPatchTargets), nameof(OuterPatchTargets.WriteResultInPostfix))]
    public static void WriteResultPostfix(ref int __result) => __result = 42;

    [InnerPrefix(typeof(InnerPatchTargets), nameof(InnerPatchTargets.ReadOuterArgumentInPrefix))]
    [Target(typeof(OuterPatchTargets), nameof(OuterPatchTargets.ReadOuterArgumentInPrefix))]
    public static void ReadOuterArgumentPrefix(int outerValue) => ArgumentObserved = outerValue;

    [InnerPostfix(typeof(InnerPatchTargets), nameof(InnerPatchTargets.ReadOuterArgumentInPostfix))]
    [Target(typeof(OuterPatchTargets), nameof(OuterPatchTargets.ReadOuterArgumentInPostfix))]
    public static void ReadOuterArgumentPostfix(int outerValue) => ArgumentObserved = outerValue;

    [InnerPrefix(typeof(InnerPatchTargets), nameof(InnerPatchTargets.WriteOuterArgumentInPrefix))]
    [Target(typeof(OuterPatchTargets), nameof(OuterPatchTargets.WriteOuterArgumentInPrefix))]
    public static void WriteOuterArgumentPrefix(ref int outerValue) => outerValue = 42;

    [InnerPostfix(typeof(InnerPatchTargets), nameof(InnerPatchTargets.WriteOuterArgumentInPostfix))]
    [Target(typeof(OuterPatchTargets), nameof(OuterPatchTargets.WriteOuterArgumentInPostfix))]
    public static void WriteOuterArgumentPostfix(ref int outerValue) => outerValue = 42;

    [InnerPrefix(typeof(InnerPatchTargets), nameof(InnerPatchTargets.SameNamedArgumentInPrefix))]
    [Target(typeof(OuterPatchTargets), nameof(OuterPatchTargets.SameNamedArgumentInPrefix))]
    public static void ReadSameNamedArgumentPrefix(int value) => ArgumentObserved = value;

    [InnerPostfix(typeof(InnerPatchTargets), nameof(InnerPatchTargets.SameNamedArgumentInPostfix))]
    [Target(typeof(OuterPatchTargets), nameof(OuterPatchTargets.SameNamedArgumentInPostfix))]
    public static void ReadSameNamedArgumentPostfix(int value) => ArgumentObserved = value;

    [InnerPostfix(typeof(InnerPatchTargets), nameof(InnerPatchTargets.NonVoidPostfixTarget))]
    [Target(typeof(OuterPatchTargets), nameof(OuterPatchTargets.NonVoidPostfixTarget))]
    public static int NonVoidPostfix() => 42;
}

public static class InnerPatchTargets
{
    public static int RunTarget() => 1;

    public static int SkipTarget()
    {
        Assert.Fail("The inner target should have been skipped.");
        return 1;
    }

    public static void ReadArgumentInPrefix(int value) { }
    public static void ReadArgumentInPostfix(int value) { }
    public static int WriteArgumentInPrefix(int value) => value;
    public static void WriteArgumentInPostfix(ref int value) { }
    public static int ReadResultInPrefix() => 42;
    public static int ReadResultInPostfix() => 42;

    public static int WriteResultInPrefix()
    {
        Assert.Fail("The inner target should have been skipped.");
        return 1;
    }

    public static int WriteResultInPostfix() => 1;
    public static void ReadOuterArgumentInPrefix() { }
    public static void ReadOuterArgumentInPostfix() { }
    public static void WriteOuterArgumentInPrefix() { }
    public static void WriteOuterArgumentInPostfix() { }
    public static void SameNamedArgumentInPrefix(int value) { }
    public static void SameNamedArgumentInPostfix(int value) { }
    public static int NonVoidPostfixTarget() => 1;
}

public static class OuterPatchTargets
{
    public static int RunTarget() => InnerPatchTargets.RunTarget();
    public static int SkipTarget() => InnerPatchTargets.SkipTarget();
    public static void ReadArgumentInPrefix(int value) => InnerPatchTargets.ReadArgumentInPrefix(value);
    public static void ReadArgumentInPostfix(int value) => InnerPatchTargets.ReadArgumentInPostfix(value);
    public static int WriteArgumentInPrefix(int value) => InnerPatchTargets.WriteArgumentInPrefix(value);
    public static void WriteArgumentInPostfix(ref int value) => InnerPatchTargets.WriteArgumentInPostfix(ref value);
    public static int ReadResultInPrefix() => InnerPatchTargets.ReadResultInPrefix();
    public static int ReadResultInPostfix() => InnerPatchTargets.ReadResultInPostfix();
    public static int WriteResultInPrefix() => InnerPatchTargets.WriteResultInPrefix();
    public static int WriteResultInPostfix() => InnerPatchTargets.WriteResultInPostfix();
    public static void ReadOuterArgumentInPrefix(int outerValue) => InnerPatchTargets.ReadOuterArgumentInPrefix();
    public static void ReadOuterArgumentInPostfix(int outerValue) => InnerPatchTargets.ReadOuterArgumentInPostfix();

    public static int WriteOuterArgumentInPrefix(int outerValue)
    {
        InnerPatchTargets.WriteOuterArgumentInPrefix();
        return outerValue;
    }

    public static int WriteOuterArgumentInPostfix(int outerValue)
    {
        InnerPatchTargets.WriteOuterArgumentInPostfix();
        return outerValue;
    }

    public static void SameNamedArgumentInPrefix(int value) =>
        InnerPatchTargets.SameNamedArgumentInPrefix(value + 41);

    public static void SameNamedArgumentInPostfix(int value) =>
        InnerPatchTargets.SameNamedArgumentInPostfix(value + 41);

    public static int NonVoidPostfixTarget() => InnerPatchTargets.NonVoidPostfixTarget();
}

[TestFixture]
public sealed class InnerPatchTests
{
    private static void ApplyPatch(string patchMethodName) =>
        Autopatcher.Patch(typeof(InnerPatchMethods).GetMethod(patchMethodName));

    [Test]
    public void InnerPrefixReturningTrueRunsInnerTarget()
    {
        ApplyPatch(nameof(InnerPatchMethods.RunTargetPrefix));
        Assert.That(OuterPatchTargets.RunTarget(), Is.EqualTo(1));
    }

    [Test]
    public void InnerPrefixReturningFalseSkipsInnerTarget()
    {
        ApplyPatch(nameof(InnerPatchMethods.SkipTargetPrefix));
        Assert.That(OuterPatchTargets.SkipTarget(), Is.Zero);
    }

    [Test]
    public void InnerPrefixCanReadInnerArgument()
    {
        InnerPatchMethods.ArgumentObserved = 0;
        ApplyPatch(nameof(InnerPatchMethods.ReadArgumentPrefix));
        OuterPatchTargets.ReadArgumentInPrefix(42);
        Assert.That(InnerPatchMethods.ArgumentObserved, Is.EqualTo(42));
    }

    [Test]
    public void InnerPostfixCanReadInnerArgument()
    {
        InnerPatchMethods.ArgumentObserved = 0;
        ApplyPatch(nameof(InnerPatchMethods.ReadArgumentPostfix));
        OuterPatchTargets.ReadArgumentInPostfix(42);
        Assert.That(InnerPatchMethods.ArgumentObserved, Is.EqualTo(42));
    }

    [Test]
    public void InnerPrefixCanWriteInnerArgumentByReference()
    {
        ApplyPatch(nameof(InnerPatchMethods.WriteArgumentPrefix));
        Assert.That(OuterPatchTargets.WriteArgumentInPrefix(1), Is.EqualTo(42));
    }

    [Test]
    public void InnerPostfixCanWriteInnerArgumentByReference()
    {
        ApplyPatch(nameof(InnerPatchMethods.WriteArgumentPostfix));
        int value = 1;
        OuterPatchTargets.WriteArgumentInPostfix(ref value);
        Assert.That(value, Is.EqualTo(42));
    }

    [Test]
    public void InnerPrefixReadsDefaultInnerResult()
    {
        InnerPatchMethods.ResultObserved = -1;
        ApplyPatch(nameof(InnerPatchMethods.ReadResultPrefix));
        OuterPatchTargets.ReadResultInPrefix();
        Assert.That(InnerPatchMethods.ResultObserved, Is.Zero);
    }

    [Test]
    public void InnerPostfixReadsInnerResult()
    {
        InnerPatchMethods.ResultObserved = 0;
        ApplyPatch(nameof(InnerPatchMethods.ReadResultPostfix));
        OuterPatchTargets.ReadResultInPostfix();
        Assert.That(InnerPatchMethods.ResultObserved, Is.EqualTo(42));
    }

    [Test]
    public void InnerPrefixCanWriteInnerResultByReference()
    {
        ApplyPatch(nameof(InnerPatchMethods.WriteResultPrefix));
        Assert.That(OuterPatchTargets.WriteResultInPrefix(), Is.EqualTo(42));
    }

    [Test]
    public void InnerPostfixCanWriteInnerResultByReference()
    {
        ApplyPatch(nameof(InnerPatchMethods.WriteResultPostfix));
        Assert.That(OuterPatchTargets.WriteResultInPostfix(), Is.EqualTo(42));
    }

    [Test]
    public void InnerPrefixCanReadOuterArgumentWhenInnerHasNoMatchingArgument()
    {
        InnerPatchMethods.ArgumentObserved = 0;
        ApplyPatch(nameof(InnerPatchMethods.ReadOuterArgumentPrefix));
        OuterPatchTargets.ReadOuterArgumentInPrefix(42);
        Assert.That(InnerPatchMethods.ArgumentObserved, Is.EqualTo(42));
    }

    [Test]
    public void InnerPostfixCanReadOuterArgumentWhenInnerHasNoMatchingArgument()
    {
        InnerPatchMethods.ArgumentObserved = 0;
        ApplyPatch(nameof(InnerPatchMethods.ReadOuterArgumentPostfix));
        OuterPatchTargets.ReadOuterArgumentInPostfix(42);
        Assert.That(InnerPatchMethods.ArgumentObserved, Is.EqualTo(42));
    }

    [Test]
    public void InnerPrefixCannotWriteOuterArgumentByReference()
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            ApplyPatch(nameof(InnerPatchMethods.WriteOuterArgumentPrefix)));

        Assert.That(exception!.InnerException, Is.TypeOf<ArgumentException>());
        Assert.That(exception.InnerException!.Message, Is.EqualTo("Outer method parameters can't be accessed by ref"));
    }

    [Test]
    public void InnerPostfixCannotWriteOuterArgumentByReference()
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            ApplyPatch(nameof(InnerPatchMethods.WriteOuterArgumentPostfix)));

        Assert.That(exception!.InnerException, Is.TypeOf<ArgumentException>());
        Assert.That(exception.InnerException!.Message, Is.EqualTo("Outer method parameters can't be accessed by ref"));
    }

    [Test]
    public void InnerPrefixPrefersInnerArgumentWhenOuterArgumentHasSameName()
    {
        InnerPatchMethods.ArgumentObserved = 0;
        ApplyPatch(nameof(InnerPatchMethods.ReadSameNamedArgumentPrefix));
        OuterPatchTargets.SameNamedArgumentInPrefix(1);
        Assert.That(InnerPatchMethods.ArgumentObserved, Is.EqualTo(42));
    }

    [Test]
    public void InnerPostfixPrefersInnerArgumentWhenOuterArgumentHasSameName()
    {
        InnerPatchMethods.ArgumentObserved = 0;
        ApplyPatch(nameof(InnerPatchMethods.ReadSameNamedArgumentPostfix));
        OuterPatchTargets.SameNamedArgumentInPostfix(1);
        Assert.That(InnerPatchMethods.ArgumentObserved, Is.EqualTo(42));
    }

    [Test]
    public void InnerPostfixReturnValueIsDiscarded()
    {
        ApplyPatch(nameof(InnerPatchMethods.NonVoidPostfix));
        Assert.That(OuterPatchTargets.NonVoidPostfixTarget(), Is.EqualTo(1));
    }
}
