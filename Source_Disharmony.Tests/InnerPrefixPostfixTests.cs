using System;
using NUnit.Framework;

namespace Disharmony.Tests;

public static class InnerPatchMethods
{
    public static int ArgumentObserved;
    public static int ResultObserved;

    [InnerPrefix(typeof(InnerStaticMethodTargets), nameof(InnerStaticMethodTargets.IntResult))]
    [Target(typeof(OuterStaticMethodTargets), nameof(OuterStaticMethodTargets.IntResult))]
    public static bool RunTargetPrefix() => true;

    [InnerPrefix(typeof(InnerStaticMethodTargets), nameof(InnerStaticMethodTargets.IntResult))]
    [Target(typeof(OuterStaticMethodTargets), nameof(OuterStaticMethodTargets.IntResult))]
    public static bool SkipTargetPrefix() => false;

    [InnerPrefix(typeof(InnerStaticMethodTargets), nameof(InnerStaticMethodTargets.IntArgument))]
    [Target(typeof(OuterStaticMethodTargets), nameof(OuterStaticMethodTargets.IntArgument))]
    public static void ReadArgumentPrefix(int value) => ArgumentObserved = value;

    [InnerPostfix(typeof(InnerStaticMethodTargets), nameof(InnerStaticMethodTargets.IntArgument))]
    [Target(typeof(OuterStaticMethodTargets), nameof(OuterStaticMethodTargets.IntArgument))]
    public static void ReadArgumentPostfix(int value) => ArgumentObserved = value;

    [InnerPrefix(typeof(InnerStaticMethodTargets), nameof(InnerStaticMethodTargets.IntIdentity))]
    [Target(typeof(OuterStaticMethodTargets), nameof(OuterStaticMethodTargets.IntIdentity))]
    public static void WriteArgumentPrefix(ref int value) => value = 42;

    [InnerPostfix(typeof(InnerStaticMethodTargets), nameof(InnerStaticMethodTargets.RefIntArgument))]
    [Target(typeof(OuterStaticMethodTargets), nameof(OuterStaticMethodTargets.RefIntArgument))]
    public static void WriteArgumentPostfix(ref int value) => value = 42;

    [InnerPrefix(typeof(InnerStaticMethodTargets), nameof(InnerStaticMethodTargets.IntResult))]
    [Target(typeof(OuterStaticMethodTargets), nameof(OuterStaticMethodTargets.IntResult))]
    public static void ReadResultPrefix(int __result) => ResultObserved = __result;

    [InnerPostfix(typeof(InnerStaticMethodTargets), nameof(InnerStaticMethodTargets.IntResult))]
    [Target(typeof(OuterStaticMethodTargets), nameof(OuterStaticMethodTargets.IntResult))]
    public static void ReadResultPostfix(int __result) => ResultObserved = __result;

    [InnerPrefix(typeof(InnerStaticMethodTargets), nameof(InnerStaticMethodTargets.IntResult))]
    [Target(typeof(OuterStaticMethodTargets), nameof(OuterStaticMethodTargets.IntResult))]
    public static bool WriteResultPrefix(ref int __result)
    {
        __result = 42;
        return false;
    }

    [InnerPostfix(typeof(InnerStaticMethodTargets), nameof(InnerStaticMethodTargets.IntResult))]
    [Target(typeof(OuterStaticMethodTargets), nameof(OuterStaticMethodTargets.IntResult))]
    public static void WriteResultPostfix(ref int __result) => __result = 42;

    [InnerPrefix(typeof(InnerStaticMethodTargets), nameof(InnerStaticMethodTargets.Void))]
    [Target(typeof(OuterStaticMethodTargets), nameof(OuterStaticMethodTargets.OuterArgument))]
    public static void ReadOuterArgumentPrefix(int outerValue) => ArgumentObserved = outerValue;

    [InnerPostfix(typeof(InnerStaticMethodTargets), nameof(InnerStaticMethodTargets.Void))]
    [Target(typeof(OuterStaticMethodTargets), nameof(OuterStaticMethodTargets.OuterArgument))]
    public static void ReadOuterArgumentPostfix(int outerValue) => ArgumentObserved = outerValue;

    [InnerPrefix(typeof(InnerStaticMethodTargets), nameof(InnerStaticMethodTargets.Void))]
    [Target(typeof(OuterStaticMethodTargets), nameof(OuterStaticMethodTargets.OuterArgument))]
    public static void WriteOuterArgumentPrefix(ref int outerValue) => outerValue = 42;

    [InnerPostfix(typeof(InnerStaticMethodTargets), nameof(InnerStaticMethodTargets.Void))]
    [Target(typeof(OuterStaticMethodTargets), nameof(OuterStaticMethodTargets.OuterArgument))]
    public static void WriteOuterArgumentPostfix(ref int outerValue) => outerValue = 42;

    [InnerPrefix(typeof(InnerStaticMethodTargets), nameof(InnerStaticMethodTargets.IntArgument))]
    [Target(typeof(OuterStaticMethodTargets), nameof(OuterStaticMethodTargets.SameNamedArgument))]
    public static void ReadSameNamedArgumentPrefix(int value) => ArgumentObserved = value;

    [InnerPostfix(typeof(InnerStaticMethodTargets), nameof(InnerStaticMethodTargets.IntArgument))]
    [Target(typeof(OuterStaticMethodTargets), nameof(OuterStaticMethodTargets.SameNamedArgument))]
    public static void ReadSameNamedArgumentPostfix(int value) => ArgumentObserved = value;

    [InnerPrefix(typeof(InnerStaticMethodTargets), nameof(InnerStaticMethodTargets.RefIntArgument))]
    [Target(typeof(OuterStaticMethodTargets), nameof(OuterStaticMethodTargets.SameNamedRefArgument))]
    public static void WriteSameNamedArgumentPrefix(ref int value) => value = 42;

    [InnerPostfix(typeof(InnerStaticMethodTargets), nameof(InnerStaticMethodTargets.RefIntArgument))]
    [Target(typeof(OuterStaticMethodTargets), nameof(OuterStaticMethodTargets.SameNamedRefArgument))]
    public static void WriteSameNamedArgumentPostfix(ref int value) => value = 42;

    [InnerPostfix(typeof(InnerStaticMethodTargets), nameof(InnerStaticMethodTargets.IntResult))]
    [Target(typeof(OuterStaticMethodTargets), nameof(OuterStaticMethodTargets.IntResult))]
    public static int NonVoidPostfix() => 42;
}

[TestFixture]
public sealed partial class ExecutionControlTests
{
    [Test]
    public void InnerPrefixReturningTrueRunsInnerTarget()
    {
        ApplyInnerPatch(nameof(InnerPatchMethods.RunTargetPrefix));
        Assert.That(OuterStaticMethodTargets.IntResult(), Is.EqualTo(1));
    }

    [Test]
    public void InnerPrefixReturningFalseSkipsInnerTarget()
    {
        ApplyInnerPatch(nameof(InnerPatchMethods.SkipTargetPrefix));
        Assert.That(OuterStaticMethodTargets.IntResult(), Is.Zero);
    }
}

[TestFixture]
public sealed partial class ArgumentBindingTests
{
    [Test]
    public void InnerPrefixCanReadInnerArgument()
    {
        InnerPatchMethods.ArgumentObserved = 0;
        ApplyInnerPatch(nameof(InnerPatchMethods.ReadArgumentPrefix));
        OuterStaticMethodTargets.IntArgument(42);
        Assert.That(InnerPatchMethods.ArgumentObserved, Is.EqualTo(42));
    }

    [Test]
    public void InnerPostfixCanReadInnerArgument()
    {
        InnerPatchMethods.ArgumentObserved = 0;
        ApplyInnerPatch(nameof(InnerPatchMethods.ReadArgumentPostfix));
        OuterStaticMethodTargets.IntArgument(42);
        Assert.That(InnerPatchMethods.ArgumentObserved, Is.EqualTo(42));
    }

    [Test]
    public void InnerPrefixCanWriteInnerArgumentByReference()
    {
        ApplyInnerPatch(nameof(InnerPatchMethods.WriteArgumentPrefix));
        Assert.That(OuterStaticMethodTargets.IntIdentity(1), Is.EqualTo(42));
    }

    [Test]
    public void InnerPostfixCanWriteInnerArgumentByReference()
    {
        ApplyInnerPatch(nameof(InnerPatchMethods.WriteArgumentPostfix));
        int value = 1;
        OuterStaticMethodTargets.RefIntArgument(ref value);
        Assert.That(value, Is.EqualTo(42));
    }
}

[TestFixture]
public sealed partial class ResultBindingTests
{
    [Test]
    public void InnerPrefixReadsDefaultInnerResult()
    {
        InnerPatchMethods.ResultObserved = -1;
        ApplyInnerPatch(nameof(InnerPatchMethods.ReadResultPrefix));
        OuterStaticMethodTargets.IntResult();
        Assert.That(InnerPatchMethods.ResultObserved, Is.Zero);
    }

    [Test]
    public void InnerPostfixReadsInnerResult()
    {
        InnerPatchMethods.ResultObserved = 0;
        ApplyInnerPatch(nameof(InnerPatchMethods.ReadResultPostfix));
        OuterStaticMethodTargets.IntResult();
        Assert.That(InnerPatchMethods.ResultObserved, Is.EqualTo(1));
    }

    [Test]
    public void InnerPrefixCanWriteInnerResultByReference()
    {
        ApplyInnerPatch(nameof(InnerPatchMethods.WriteResultPrefix));
        Assert.That(OuterStaticMethodTargets.IntResult(), Is.EqualTo(42));
    }

    [Test]
    public void InnerPostfixCanWriteInnerResultByReference()
    {
        ApplyInnerPatch(nameof(InnerPatchMethods.WriteResultPostfix));
        Assert.That(OuterStaticMethodTargets.IntResult(), Is.EqualTo(42));
    }
}

[TestFixture]
public sealed partial class ArgumentBindingTests
{
    [Test]
    public void InnerPrefixCanReadOuterArgumentWhenInnerHasNoMatchingArgument()
    {
        InnerPatchMethods.ArgumentObserved = 0;
        ApplyInnerPatch(nameof(InnerPatchMethods.ReadOuterArgumentPrefix));
        OuterStaticMethodTargets.OuterArgument(42);
        Assert.That(InnerPatchMethods.ArgumentObserved, Is.EqualTo(42));
    }

    [Test]
    public void InnerPostfixCanReadOuterArgumentWhenInnerHasNoMatchingArgument()
    {
        InnerPatchMethods.ArgumentObserved = 0;
        ApplyInnerPatch(nameof(InnerPatchMethods.ReadOuterArgumentPostfix));
        OuterStaticMethodTargets.OuterArgument(42);
        Assert.That(InnerPatchMethods.ArgumentObserved, Is.EqualTo(42));
    }

    [Test]
    public void InnerPrefixCannotWriteOuterArgumentByReference()
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            ApplyInnerPatch(nameof(InnerPatchMethods.WriteOuterArgumentPrefix)));

        Assert.That(exception!.InnerException, Is.TypeOf<ParameterBindingException>());
        Assert.That(exception.InnerException!.Message, Is.EqualTo("outerValue: Outer method parameter can't be accessed by ref"));
    }

    [Test]
    public void InnerPostfixCannotWriteOuterArgumentByReference()
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            ApplyInnerPatch(nameof(InnerPatchMethods.WriteOuterArgumentPostfix)));

        Assert.That(exception!.InnerException, Is.TypeOf<ParameterBindingException>());
        Assert.That(exception.InnerException!.Message, Is.EqualTo("outerValue: Outer method parameter can't be accessed by ref"));
    }

    [Test]
    public void InnerPrefixPrefersInnerArgumentWhenOuterArgumentHasSameName()
    {
        InnerPatchMethods.ArgumentObserved = 0;
        ApplyInnerPatch(nameof(InnerPatchMethods.ReadSameNamedArgumentPrefix));
        OuterStaticMethodTargets.SameNamedArgument(1);
        Assert.That(InnerPatchMethods.ArgumentObserved, Is.EqualTo(42));
    }

    [Test]
    public void InnerPostfixPrefersInnerArgumentWhenOuterArgumentHasSameName()
    {
        InnerPatchMethods.ArgumentObserved = 0;
        ApplyInnerPatch(nameof(InnerPatchMethods.ReadSameNamedArgumentPostfix));
        OuterStaticMethodTargets.SameNamedArgument(1);
        Assert.That(InnerPatchMethods.ArgumentObserved, Is.EqualTo(42));
    }

    [Test]
    public void InnerPrefixCanWriteInnerArgumentWhenOuterArgumentHasSameName()
    {
        ApplyInnerPatch(nameof(InnerPatchMethods.WriteSameNamedArgumentPrefix));
        int outerValue = 7;

        int result = OuterStaticMethodTargets.SameNamedRefArgument(ref outerValue);

        Assert.That(result, Is.EqualTo(42));
        Assert.That(outerValue, Is.EqualTo(7));
    }

    [Test]
    public void InnerPostfixCanWriteInnerArgumentWhenOuterArgumentHasSameName()
    {
        ApplyInnerPatch(nameof(InnerPatchMethods.WriteSameNamedArgumentPostfix));
        int outerValue = 7;

        int result = OuterStaticMethodTargets.SameNamedRefArgument(ref outerValue);

        Assert.That(result, Is.EqualTo(42));
        Assert.That(outerValue, Is.EqualTo(7));
    }
}

[TestFixture]
public sealed partial class PostfixReturnValueTests
{
    [Test]
    public void InnerPostfixReturnValueIsDiscarded()
    {
        ApplyInnerPatch(nameof(InnerPatchMethods.NonVoidPostfix));
        Assert.That(OuterStaticMethodTargets.IntResult(), Is.EqualTo(1));
    }
}
