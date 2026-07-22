namespace Disharmony.Tests;

public static class CapturedVariableBindingPatches
{
    public static int Observed;

    [Prefix]
    [Target(typeof(LocalFunctionTargets), "CapturedVariableMethod.LocalMethod")]
    public static void PrefixOnLocalFunctionCanReadCapturedVariable(int captured) => Observed = captured;

    [Postfix]
    [Target(typeof(LocalFunctionTargets), "CapturedVariableMethod.LocalMethod")]
    public static void PostfixOnLocalFunctionCanReadCapturedVariable(int captured) => Observed = captured;

    [InnerPrefix(typeof(LocalFunctionTargets), "CapturedVariableMethod.LocalMethod")]
    [Target(typeof(LocalFunctionTargets), nameof(LocalFunctionTargets.CapturedVariableMethod))]
    public static void InnerPrefixAtLocalFunctionCallCanReadCapturedVariable(int captured) => Observed = captured;

    [InnerPostfix(typeof(LocalFunctionTargets), "CapturedVariableMethod.LocalMethod")]
    [Target(typeof(LocalFunctionTargets), nameof(LocalFunctionTargets.CapturedVariableMethod))]
    public static void InnerPostfixAtLocalFunctionCallCanReadCapturedVariable(int captured) => Observed = captured;

    [Prefix]
    [Target(typeof(LocalFunctionTargets), "CapturedVariableMethod.LocalMethod")]
    public static void PrefixOnLocalFunctionCanWriteCapturedVariableByReference(ref int captured) => captured = 42;

    [Postfix]
    [Target(typeof(LocalFunctionTargets), "CapturedVariableMethod.LocalMethod")]
    public static void PostfixOnLocalFunctionCanWriteCapturedVariableByReference(ref int captured) => captured = 42;

    [InnerPrefix(typeof(LocalFunctionTargets), "CapturedVariableMethod.LocalMethod")]
    [Target(typeof(LocalFunctionTargets), nameof(LocalFunctionTargets.CapturedVariableMethod))]
    public static void InnerPrefixAtLocalFunctionCallCanWriteCapturedVariableByReference(ref int captured) => captured = 42;

    [InnerPostfix(typeof(LocalFunctionTargets), "CapturedVariableMethod.LocalMethod")]
    [Target(typeof(LocalFunctionTargets), nameof(LocalFunctionTargets.CapturedVariableMethod))]
    public static void InnerPostfixAtLocalFunctionCallCanWriteCapturedVariableByReference(ref int captured) => captured = 42;
}

[TestFixture]
public sealed class CapturedVariableBindingTests : PatchTestBase
{
    [Test]
    public void PrefixOnLocalFunctionCanReadCapturedVariable()
    {
        CapturedVariableBindingPatches.Observed = 0;
        ApplyPatch(typeof(CapturedVariableBindingPatches), nameof(CapturedVariableBindingPatches.PrefixOnLocalFunctionCanReadCapturedVariable));

        int result = LocalFunctionTargets.CapturedVariableMethod(42);

        Assert.That(result, Is.EqualTo(42));
        Assert.That(CapturedVariableBindingPatches.Observed, Is.EqualTo(42));
    }

    [Test]
    public void PostfixOnLocalFunctionCanReadCapturedVariable()
    {
        CapturedVariableBindingPatches.Observed = 0;
        ApplyPatch(typeof(CapturedVariableBindingPatches), nameof(CapturedVariableBindingPatches.PostfixOnLocalFunctionCanReadCapturedVariable));

        int result = LocalFunctionTargets.CapturedVariableMethod(42);

        Assert.That(result, Is.EqualTo(42));
        Assert.That(CapturedVariableBindingPatches.Observed, Is.EqualTo(42));
    }

    [Test]
    public void InnerPrefixAtLocalFunctionCallCanReadCapturedVariable()
    {
        CapturedVariableBindingPatches.Observed = 0;
        ApplyPatch(typeof(CapturedVariableBindingPatches), nameof(CapturedVariableBindingPatches.InnerPrefixAtLocalFunctionCallCanReadCapturedVariable));

        int result = LocalFunctionTargets.CapturedVariableMethod(42);

        Assert.That(result, Is.EqualTo(42));
        Assert.That(CapturedVariableBindingPatches.Observed, Is.EqualTo(42));
    }

    [Test]
    public void InnerPostfixAtLocalFunctionCallCanReadCapturedVariable()
    {
        CapturedVariableBindingPatches.Observed = 0;
        ApplyPatch(typeof(CapturedVariableBindingPatches), nameof(CapturedVariableBindingPatches.InnerPostfixAtLocalFunctionCallCanReadCapturedVariable));

        int result = LocalFunctionTargets.CapturedVariableMethod(42);

        Assert.That(result, Is.EqualTo(42));
        Assert.That(CapturedVariableBindingPatches.Observed, Is.EqualTo(42));
    }

    [Test]
    public void PrefixOnLocalFunctionCanWriteCapturedVariableByReference()
    {
        ApplyPatch(typeof(CapturedVariableBindingPatches), nameof(CapturedVariableBindingPatches.PrefixOnLocalFunctionCanWriteCapturedVariableByReference));

        int result = LocalFunctionTargets.CapturedVariableMethod(1);

        Assert.That(result, Is.EqualTo(42));
    }

    [Test]
    public void PostfixOnLocalFunctionCanWriteCapturedVariableByReference()
    {
        ApplyPatch(typeof(CapturedVariableBindingPatches), nameof(CapturedVariableBindingPatches.PostfixOnLocalFunctionCanWriteCapturedVariableByReference));

        int result = LocalFunctionTargets.CapturedVariableMethod(1);

        Assert.That(result, Is.EqualTo(42));
    }

    [Test]
    public void InnerPrefixAtLocalFunctionCallCanWriteCapturedVariableByReference()
    {
        ApplyPatch(typeof(CapturedVariableBindingPatches), nameof(CapturedVariableBindingPatches.InnerPrefixAtLocalFunctionCallCanWriteCapturedVariableByReference));

        int result = LocalFunctionTargets.CapturedVariableMethod(1);

        Assert.That(result, Is.EqualTo(42));
    }

    [Test]
    public void InnerPostfixAtLocalFunctionCallCanWriteCapturedVariableByReference()
    {
        ApplyPatch(typeof(CapturedVariableBindingPatches), nameof(CapturedVariableBindingPatches.InnerPostfixAtLocalFunctionCallCanWriteCapturedVariableByReference));

        int result = LocalFunctionTargets.CapturedVariableMethod(1);

        Assert.That(result, Is.EqualTo(42));
    }
}
