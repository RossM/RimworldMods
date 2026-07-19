using NUnit.Framework;

namespace Disharmony.Tests;

public static class InnerParameterBindingPatchMethods
{
    public static ClassMethodTargets? CallerObserved;
    public static int FieldObserved;
    public static int CapturedVariableObserved;

    [InnerPrefix(typeof(InnerStaticMethodTargets), nameof(InnerStaticMethodTargets.Void))]
    [Target(typeof(ClassMethodTargets), nameof(ClassMethodTargets.CallStaticVoid))]
    public static void CaptureCallerPrefix(ClassMethodTargets __caller) => CallerObserved = __caller;

    [InnerPostfix(typeof(InnerStaticMethodTargets), nameof(InnerStaticMethodTargets.Void))]
    [Target(typeof(ClassMethodTargets), nameof(ClassMethodTargets.CallStaticVoid))]
    public static void CaptureCallerPostfix(ClassMethodTargets __caller) => CallerObserved = __caller;

    [InnerPrefix(typeof(InstanceMethodTargetsWithoutFields), nameof(InstanceMethodTargetsWithoutFields.Void))]
    [Target(typeof(ClassMethodTargets), nameof(ClassMethodTargets.CallInnerWithoutField))]
    public static void ReadOuterFieldPrefix(int ___foo) => FieldObserved = ___foo;

    [InnerPostfix(typeof(InnerInstanceMethodTargets), nameof(InnerInstanceMethodTargets.Void))]
    [Target(typeof(ClassMethodTargets), nameof(ClassMethodTargets.CallInnerWithField))]
    public static void ReadInnerFieldPostfix(int ___foo) => FieldObserved = ___foo;

    [Prefix]
    [Target(typeof(LocalFunctionTargets), "CapturedVariableMethod.LocalMethod")]
    public static void ReadCapturedVariablePrefix(int captured) => CapturedVariableObserved = captured;

    [Postfix]
    [Target(typeof(LocalFunctionTargets), "CapturedVariableMethod.LocalMethod")]
    public static void ReadCapturedVariablePostfix(int captured) => CapturedVariableObserved = captured;

    [InnerPrefix(typeof(LocalFunctionTargets), "CapturedVariableMethod.LocalMethod")]
    [Target(typeof(LocalFunctionTargets), nameof(LocalFunctionTargets.CapturedVariableMethod))]
    public static void ReadCapturedVariableInnerPrefix(int captured) => CapturedVariableObserved = captured;

    [InnerPostfix(typeof(LocalFunctionTargets), "CapturedVariableMethod.LocalMethod")]
    [Target(typeof(LocalFunctionTargets), nameof(LocalFunctionTargets.CapturedVariableMethod))]
    public static void ReadCapturedVariableInnerPostfix(int captured) => CapturedVariableObserved = captured;
}

[TestFixture]
public sealed partial class InstanceBindingTests
{
    [Test]
    public void InnerPrefixCanCaptureOuterMethodInstanceAsCaller()
    {
        InnerParameterBindingPatchMethods.CallerObserved = null;
        ApplyInnerParameterBindingPatch(nameof(InnerParameterBindingPatchMethods.CaptureCallerPrefix));
        var outer = new ClassMethodTargets();

        outer.CallStaticVoid();

        Assert.That(InnerParameterBindingPatchMethods.CallerObserved, Is.SameAs(outer));
    }

    [Test]
    public void InnerPostfixCanCaptureOuterMethodInstanceAsCaller()
    {
        InnerParameterBindingPatchMethods.CallerObserved = null;
        ApplyInnerParameterBindingPatch(nameof(InnerParameterBindingPatchMethods.CaptureCallerPostfix));
        var outer = new ClassMethodTargets();

        outer.CallStaticVoid();

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
        var outer = new ClassMethodTargets { foo = 42 };

        outer.CallInnerWithoutField(new InstanceMethodTargetsWithoutFields());

        Assert.That(InnerParameterBindingPatchMethods.FieldObserved, Is.EqualTo(42));
    }

    [Test]
    public void TripleUnderscoreParameterPrefersInnerInstanceField()
    {
        InnerParameterBindingPatchMethods.FieldObserved = 0;
        ApplyInnerParameterBindingPatch(nameof(InnerParameterBindingPatchMethods.ReadInnerFieldPostfix));
        var outer = new ClassMethodTargets { foo = 1 };
        var inner = new InnerInstanceMethodTargets { foo = 42 };

        outer.CallInnerWithField(inner);

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

        int result = LocalFunctionTargets.CapturedVariableMethod(42);

        Assert.That(result, Is.EqualTo(42));
        Assert.That(InnerParameterBindingPatchMethods.CapturedVariableObserved, Is.EqualTo(42));
    }

    [Test]
    public void PostfixOnLocalFunctionCanReadCapturedVariable()
    {
        InnerParameterBindingPatchMethods.CapturedVariableObserved = 0;
        ApplyInnerParameterBindingPatch(nameof(InnerParameterBindingPatchMethods.ReadCapturedVariablePostfix));

        int result = LocalFunctionTargets.CapturedVariableMethod(42);

        Assert.That(result, Is.EqualTo(42));
        Assert.That(InnerParameterBindingPatchMethods.CapturedVariableObserved, Is.EqualTo(42));
    }

    [Test]
    public void InnerPrefixAtLocalFunctionCallCanReadCapturedVariable()
    {
        InnerParameterBindingPatchMethods.CapturedVariableObserved = 0;
        ApplyInnerParameterBindingPatch(nameof(InnerParameterBindingPatchMethods.ReadCapturedVariableInnerPrefix));

        int result = LocalFunctionTargets.CapturedVariableMethod(42);

        Assert.That(result, Is.EqualTo(42));
        Assert.That(InnerParameterBindingPatchMethods.CapturedVariableObserved, Is.EqualTo(42));
    }

    [Test]
    public void InnerPostfixAtLocalFunctionCallCanReadCapturedVariable()
    {
        InnerParameterBindingPatchMethods.CapturedVariableObserved = 0;
        ApplyInnerParameterBindingPatch(nameof(InnerParameterBindingPatchMethods.ReadCapturedVariableInnerPostfix));

        int result = LocalFunctionTargets.CapturedVariableMethod(42);

        Assert.That(result, Is.EqualTo(42));
        Assert.That(InnerParameterBindingPatchMethods.CapturedVariableObserved, Is.EqualTo(42));
    }
}
