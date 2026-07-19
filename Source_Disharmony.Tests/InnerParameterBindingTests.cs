using NUnit.Framework;

namespace Disharmony.Tests;

public static class InnerParameterBindingPatchMethods
{
    public static OuterParameterBindingTarget? CallerObserved;
    public static int FieldObserved;
    public static int CapturedVariableObserved;

    [InnerPrefix(typeof(InnerParameterBindingTarget), nameof(InnerParameterBindingTarget.CallerPrefixTarget))]
    [Target(typeof(OuterParameterBindingTarget), nameof(OuterParameterBindingTarget.CallerPrefixTarget))]
    public static void CaptureCallerPrefix(OuterParameterBindingTarget __caller) => CallerObserved = __caller;

    [InnerPostfix(typeof(InnerParameterBindingTarget), nameof(InnerParameterBindingTarget.CallerPostfixTarget))]
    [Target(typeof(OuterParameterBindingTarget), nameof(OuterParameterBindingTarget.CallerPostfixTarget))]
    public static void CaptureCallerPostfix(OuterParameterBindingTarget __caller) => CallerObserved = __caller;

    [InnerPrefix(typeof(InnerWithoutFieldTarget), nameof(InnerWithoutFieldTarget.OuterFieldTarget))]
    [Target(typeof(OuterParameterBindingTarget), nameof(OuterParameterBindingTarget.OuterFieldTarget))]
    public static void ReadOuterFieldPrefix(int ___foo) => FieldObserved = ___foo;

    [InnerPostfix(typeof(InnerParameterBindingTarget), nameof(InnerParameterBindingTarget.InnerFieldTarget))]
    [Target(typeof(OuterParameterBindingTarget), nameof(OuterParameterBindingTarget.InnerFieldTarget))]
    public static void ReadInnerFieldPostfix(int ___foo) => FieldObserved = ___foo;

    [InnerPrefix(typeof(LocalVariableBindingTarget), "CapturedVariablePrefixTarget.LocalMethod")]
    [Target(typeof(LocalVariableBindingTarget), nameof(LocalVariableBindingTarget.CapturedVariablePrefixTarget))]
    public static void ReadCapturedVariablePrefix(int captured) => CapturedVariableObserved = captured;

    [InnerPostfix(typeof(LocalVariableBindingTarget), "CapturedVariablePostfixTarget.LocalMethod")]
    [Target(typeof(LocalVariableBindingTarget), nameof(LocalVariableBindingTarget.CapturedVariablePostfixTarget))]
    public static void ReadCapturedVariablePostfix(int captured) => CapturedVariableObserved = captured;
}

public sealed class OuterParameterBindingTarget
{
    public int foo;

    public void CallerPrefixTarget() => InnerParameterBindingTarget.CallerPrefixTarget();
    public void CallerPostfixTarget() => InnerParameterBindingTarget.CallerPostfixTarget();
    public void OuterFieldTarget(InnerWithoutFieldTarget inner) => inner.OuterFieldTarget();
    public void InnerFieldTarget(InnerParameterBindingTarget inner) => inner.InnerFieldTarget();
}

public sealed class InnerParameterBindingTarget
{
    public int foo;

    public static void CallerPrefixTarget() { }
    public static void CallerPostfixTarget() { }
    public void InnerFieldTarget() { }
}

public sealed class InnerWithoutFieldTarget
{
    public void OuterFieldTarget() { }
}

public static class LocalVariableBindingTarget
{
    public static void CapturedVariablePrefixTarget(int value)
    {
        int captured = value;
        LocalMethod();
        return;

        void LocalMethod() => _ = captured;
    }

    public static void CapturedVariablePostfixTarget(int value)
    {
        int captured = value;
        LocalMethod();
        return;

        void LocalMethod() => _ = captured;
    }
}

[TestFixture]
public sealed class InnerParameterBindingTests
{
    private static void ApplyPatch(string patchMethodName) =>
        Autopatcher.Patch(typeof(InnerParameterBindingPatchMethods).GetMethod(patchMethodName));

    [Test]
    public void InnerPrefixCanCaptureOuterMethodInstanceAsCaller()
    {
        InnerParameterBindingPatchMethods.CallerObserved = null;
        ApplyPatch(nameof(InnerParameterBindingPatchMethods.CaptureCallerPrefix));
        var outer = new OuterParameterBindingTarget();

        outer.CallerPrefixTarget();

        Assert.That(InnerParameterBindingPatchMethods.CallerObserved, Is.SameAs(outer));
    }

    [Test]
    public void InnerPostfixCanCaptureOuterMethodInstanceAsCaller()
    {
        InnerParameterBindingPatchMethods.CallerObserved = null;
        ApplyPatch(nameof(InnerParameterBindingPatchMethods.CaptureCallerPostfix));
        var outer = new OuterParameterBindingTarget();

        outer.CallerPostfixTarget();

        Assert.That(InnerParameterBindingPatchMethods.CallerObserved, Is.SameAs(outer));
    }

    [Test]
    public void TripleUnderscoreParameterCanReadOuterInstanceField()
    {
        InnerParameterBindingPatchMethods.FieldObserved = 0;
        ApplyPatch(nameof(InnerParameterBindingPatchMethods.ReadOuterFieldPrefix));
        var outer = new OuterParameterBindingTarget { foo = 42 };

        outer.OuterFieldTarget(new InnerWithoutFieldTarget());

        Assert.That(InnerParameterBindingPatchMethods.FieldObserved, Is.EqualTo(42));
    }

    [Test]
    public void TripleUnderscoreParameterPrefersInnerInstanceField()
    {
        InnerParameterBindingPatchMethods.FieldObserved = 0;
        ApplyPatch(nameof(InnerParameterBindingPatchMethods.ReadInnerFieldPostfix));
        var outer = new OuterParameterBindingTarget { foo = 1 };
        var inner = new InnerParameterBindingTarget { foo = 42 };

        outer.InnerFieldTarget(inner);

        Assert.That(InnerParameterBindingPatchMethods.FieldObserved, Is.EqualTo(42));
    }

    [Test]
    public void InnerPrefixLocalFunctionCanReadCapturedVariable()
    {
        InnerParameterBindingPatchMethods.CapturedVariableObserved = 0;
        ApplyPatch(nameof(InnerParameterBindingPatchMethods.ReadCapturedVariablePrefix));

        LocalVariableBindingTarget.CapturedVariablePrefixTarget(42);

        Assert.That(InnerParameterBindingPatchMethods.CapturedVariableObserved, Is.EqualTo(42));
    }

    [Test]
    public void InnerPostfixLocalFunctionCanReadCapturedVariable()
    {
        InnerParameterBindingPatchMethods.CapturedVariableObserved = 0;
        ApplyPatch(nameof(InnerParameterBindingPatchMethods.ReadCapturedVariablePostfix));

        LocalVariableBindingTarget.CapturedVariablePostfixTarget(42);

        Assert.That(InnerParameterBindingPatchMethods.CapturedVariableObserved, Is.EqualTo(42));
    }
}
