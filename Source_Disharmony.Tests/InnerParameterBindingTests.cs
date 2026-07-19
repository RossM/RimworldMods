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

    [Prefix]
    [Target(typeof(LocalVariableBindingTarget), "CapturedVariablePrefixTarget.LocalMethod")]
    public static void ReadCapturedVariablePrefix(int captured) => CapturedVariableObserved = captured;

    [Postfix]
    [Target(typeof(LocalVariableBindingTarget), "CapturedVariablePostfixTarget.LocalMethod")]
    public static void ReadCapturedVariablePostfix(int captured) => CapturedVariableObserved = captured;

    [InnerPrefix(typeof(LocalVariableBindingTarget), "CapturedVariableInnerPrefixTarget.LocalMethod")]
    [Target(typeof(LocalVariableBindingTarget), nameof(LocalVariableBindingTarget.CapturedVariableInnerPrefixTarget))]
    public static void ReadCapturedVariableInnerPrefix(int captured) => CapturedVariableObserved = captured;

    [InnerPostfix(typeof(LocalVariableBindingTarget), "CapturedVariableInnerPostfixTarget.LocalMethod")]
    [Target(typeof(LocalVariableBindingTarget), nameof(LocalVariableBindingTarget.CapturedVariableInnerPostfixTarget))]
    public static void ReadCapturedVariableInnerPostfix(int captured) => CapturedVariableObserved = captured;
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
    public static int CapturedVariablePrefixTarget(int value)
    {
        int captured = value;
        return LocalMethod();

        int LocalMethod() => captured;
    }

    public static int CapturedVariablePostfixTarget(int value)
    {
        int captured = value;
        return LocalMethod();

        int LocalMethod() => captured;
    }

    public static int CapturedVariableInnerPrefixTarget(int value)
    {
        int captured = value;
        return LocalMethod();

        int LocalMethod() => captured;
    }

    public static int CapturedVariableInnerPostfixTarget(int value)
    {
        int captured = value;
        return LocalMethod();

        int LocalMethod() => captured;
    }
}

[TestFixture]
public sealed partial class InstanceBindingTests
{
    [Test]
    public void InnerPrefixCanCaptureOuterMethodInstanceAsCaller()
    {
        InnerParameterBindingPatchMethods.CallerObserved = null;
        ApplyInnerParameterBindingPatch(nameof(InnerParameterBindingPatchMethods.CaptureCallerPrefix));
        var outer = new OuterParameterBindingTarget();

        outer.CallerPrefixTarget();

        Assert.That(InnerParameterBindingPatchMethods.CallerObserved, Is.SameAs(outer));
    }

    [Test]
    public void InnerPostfixCanCaptureOuterMethodInstanceAsCaller()
    {
        InnerParameterBindingPatchMethods.CallerObserved = null;
        ApplyInnerParameterBindingPatch(nameof(InnerParameterBindingPatchMethods.CaptureCallerPostfix));
        var outer = new OuterParameterBindingTarget();

        outer.CallerPostfixTarget();

        Assert.That(InnerParameterBindingPatchMethods.CallerObserved, Is.SameAs(outer));
    }
}

[TestFixture]
public sealed class FieldBindingTests : PatchTestBase
{
    [Test]
    public void TripleUnderscoreParameterCanReadOuterInstanceField()
    {
        InnerParameterBindingPatchMethods.FieldObserved = 0;
        ApplyInnerParameterBindingPatch(nameof(InnerParameterBindingPatchMethods.ReadOuterFieldPrefix));
        var outer = new OuterParameterBindingTarget { foo = 42 };

        outer.OuterFieldTarget(new InnerWithoutFieldTarget());

        Assert.That(InnerParameterBindingPatchMethods.FieldObserved, Is.EqualTo(42));
    }

    [Test]
    public void TripleUnderscoreParameterPrefersInnerInstanceField()
    {
        InnerParameterBindingPatchMethods.FieldObserved = 0;
        ApplyInnerParameterBindingPatch(nameof(InnerParameterBindingPatchMethods.ReadInnerFieldPostfix));
        var outer = new OuterParameterBindingTarget { foo = 1 };
        var inner = new InnerParameterBindingTarget { foo = 42 };

        outer.InnerFieldTarget(inner);

        Assert.That(InnerParameterBindingPatchMethods.FieldObserved, Is.EqualTo(42));
    }
}

[TestFixture]
public sealed class CapturedVariableBindingTests : PatchTestBase
{
    [Test]
    public void PrefixOnLocalFunctionCanReadCapturedVariable()
    {
        InnerParameterBindingPatchMethods.CapturedVariableObserved = 0;
        ApplyInnerParameterBindingPatch(nameof(InnerParameterBindingPatchMethods.ReadCapturedVariablePrefix));

        int result = LocalVariableBindingTarget.CapturedVariablePrefixTarget(42);

        Assert.That(result, Is.EqualTo(42));
        Assert.That(InnerParameterBindingPatchMethods.CapturedVariableObserved, Is.EqualTo(42));
    }

    [Test]
    public void PostfixOnLocalFunctionCanReadCapturedVariable()
    {
        InnerParameterBindingPatchMethods.CapturedVariableObserved = 0;
        ApplyInnerParameterBindingPatch(nameof(InnerParameterBindingPatchMethods.ReadCapturedVariablePostfix));

        int result = LocalVariableBindingTarget.CapturedVariablePostfixTarget(42);

        Assert.That(result, Is.EqualTo(42));
        Assert.That(InnerParameterBindingPatchMethods.CapturedVariableObserved, Is.EqualTo(42));
    }

    [Test]
    public void InnerPrefixAtLocalFunctionCallCanReadCapturedVariable()
    {
        InnerParameterBindingPatchMethods.CapturedVariableObserved = 0;
        ApplyInnerParameterBindingPatch(nameof(InnerParameterBindingPatchMethods.ReadCapturedVariableInnerPrefix));

        int result = LocalVariableBindingTarget.CapturedVariableInnerPrefixTarget(42);

        Assert.That(result, Is.EqualTo(42));
        Assert.That(InnerParameterBindingPatchMethods.CapturedVariableObserved, Is.EqualTo(42));
    }

    [Test]
    public void InnerPostfixAtLocalFunctionCallCanReadCapturedVariable()
    {
        InnerParameterBindingPatchMethods.CapturedVariableObserved = 0;
        ApplyInnerParameterBindingPatch(nameof(InnerParameterBindingPatchMethods.ReadCapturedVariableInnerPostfix));

        int result = LocalVariableBindingTarget.CapturedVariableInnerPostfixTarget(42);

        Assert.That(result, Is.EqualTo(42));
        Assert.That(InnerParameterBindingPatchMethods.CapturedVariableObserved, Is.EqualTo(42));
    }
}
