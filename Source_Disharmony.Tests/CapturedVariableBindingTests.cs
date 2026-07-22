namespace Disharmony.Tests;

public static class CapturedVariableBindingPatches
{
    public static int Observed;
    public static BindingReference? ReferenceObserved;
    public static BindingStruct StructObserved;

    [Prefix]
    [Target(typeof(LocalFunctionTargets), "CapturedVariableMethod.LocalMethod")]
    public static void PrefixOnLocalFunctionCanReadCapturedVariable(int captured) => Observed = captured;

    [Prefix]
    [Target(typeof(LocalFunctionTargets), "CapturedReferenceVariableMethod.LocalMethod")]
    public static void PrefixOnLocalFunctionCanReadCapturedReferenceTypeVariable(BindingReference captured) =>
        ReferenceObserved = captured;

    [Prefix]
    [Target(typeof(LocalFunctionTargets), "CapturedStructVariableMethod.LocalMethod")]
    public static void PrefixOnLocalFunctionCanReadCapturedStructVariable(BindingStruct captured) => StructObserved = captured;

    [Postfix]
    [Target(typeof(LocalFunctionTargets), "CapturedVariableMethod.LocalMethod")]
    public static void PostfixOnLocalFunctionCanReadCapturedVariable(int captured) => Observed = captured;

    [InnerPrefix(typeof(LocalFunctionTargets), "CapturedVariableMethod.LocalMethod")]
    [Target(typeof(LocalFunctionTargets), nameof(LocalFunctionTargets.CapturedVariableMethod))]
    public static void InnerPrefixAtLocalFunctionCallCanReadCapturedVariable(int captured) => Observed = captured;

    [InnerPrefix(typeof(LocalFunctionTargets), "CapturedReferenceVariableMethod.LocalMethod")]
    [Target(typeof(LocalFunctionTargets), nameof(LocalFunctionTargets.CapturedReferenceVariableMethod))]
    public static void InnerPrefixAtLocalFunctionCallCanReadCapturedReferenceTypeVariable(BindingReference captured) =>
        ReferenceObserved = captured;

    [InnerPrefix(typeof(LocalFunctionTargets), "CapturedStructVariableMethod.LocalMethod")]
    [Target(typeof(LocalFunctionTargets), nameof(LocalFunctionTargets.CapturedStructVariableMethod))]
    public static void InnerPrefixAtLocalFunctionCallCanReadCapturedStructVariable(BindingStruct captured) => StructObserved = captured;

    [InnerPostfix(typeof(LocalFunctionTargets), "CapturedVariableMethod.LocalMethod")]
    [Target(typeof(LocalFunctionTargets), nameof(LocalFunctionTargets.CapturedVariableMethod))]
    public static void InnerPostfixAtLocalFunctionCallCanReadCapturedVariable(int captured) => Observed = captured;

    [Prefix]
    [Target(typeof(LocalFunctionTargets), "CapturedVariableMethod.LocalMethod")]
    public static void PrefixOnLocalFunctionCanWriteCapturedVariableByReference(ref int captured) => captured = 42;

    [Prefix]
    [Target(typeof(LocalFunctionTargets), "CapturedReferenceVariableMethod.LocalMethod")]
    public static void PrefixOnLocalFunctionCanWriteCapturedReferenceTypeVariableByReference(ref BindingReference captured) =>
        captured = new BindingReference { Value = 42 };

    [Prefix]
    [Target(typeof(LocalFunctionTargets), "CapturedStructVariableMethod.LocalMethod")]
    public static void PrefixOnLocalFunctionCanWriteCapturedStructVariableByReference(ref BindingStruct captured) =>
        captured = new BindingStruct { Value = 42 };

    [Postfix]
    [Target(typeof(LocalFunctionTargets), "CapturedVariableMethod.LocalMethod")]
    public static void PostfixOnLocalFunctionCanWriteCapturedVariableByReference(ref int captured) => captured = 42;

    [InnerPrefix(typeof(LocalFunctionTargets), "CapturedVariableMethod.LocalMethod")]
    [Target(typeof(LocalFunctionTargets), nameof(LocalFunctionTargets.CapturedVariableMethod))]
    public static void InnerPrefixAtLocalFunctionCallCanWriteCapturedVariableByReference(ref int captured) => captured = 42;

    [InnerPrefix(typeof(LocalFunctionTargets), "CapturedReferenceVariableMethod.LocalMethod")]
    [Target(typeof(LocalFunctionTargets), nameof(LocalFunctionTargets.CapturedReferenceVariableMethod))]
    public static void InnerPrefixAtLocalFunctionCallCanWriteCapturedReferenceTypeVariableByReference(ref BindingReference captured) =>
        captured = new BindingReference { Value = 42 };

    [InnerPrefix(typeof(LocalFunctionTargets), "CapturedStructVariableMethod.LocalMethod")]
    [Target(typeof(LocalFunctionTargets), nameof(LocalFunctionTargets.CapturedStructVariableMethod))]
    public static void InnerPrefixAtLocalFunctionCallCanWriteCapturedStructVariableByReference(ref BindingStruct captured) =>
        captured = new BindingStruct { Value = 42 };

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
    public void PrefixOnLocalFunctionCanReadCapturedReferenceTypeVariable()
    {
        CapturedVariableBindingPatches.ReferenceObserved = null;
        var value = new BindingReference { Value = 42 };
        ApplyPatch(typeof(CapturedVariableBindingPatches), nameof(CapturedVariableBindingPatches.PrefixOnLocalFunctionCanReadCapturedReferenceTypeVariable));

        LocalFunctionTargets.CapturedReferenceVariableMethod(value);

        Assert.That(CapturedVariableBindingPatches.ReferenceObserved, Is.SameAs(value));
    }

    [Test]
    public void PrefixOnLocalFunctionCanReadCapturedStructVariable()
    {
        CapturedVariableBindingPatches.StructObserved = default;
        ApplyPatch(typeof(CapturedVariableBindingPatches), nameof(CapturedVariableBindingPatches.PrefixOnLocalFunctionCanReadCapturedStructVariable));

        LocalFunctionTargets.CapturedStructVariableMethod(new BindingStruct { Value = 42 });

        Assert.That(CapturedVariableBindingPatches.StructObserved.Value, Is.EqualTo(42));
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
    public void InnerPrefixAtLocalFunctionCallCanReadCapturedReferenceTypeVariable()
    {
        CapturedVariableBindingPatches.ReferenceObserved = null;
        var value = new BindingReference { Value = 42 };
        ApplyPatch(typeof(CapturedVariableBindingPatches), nameof(CapturedVariableBindingPatches.InnerPrefixAtLocalFunctionCallCanReadCapturedReferenceTypeVariable));

        LocalFunctionTargets.CapturedReferenceVariableMethod(value);

        Assert.That(CapturedVariableBindingPatches.ReferenceObserved, Is.SameAs(value));
    }

    [Test]
    public void InnerPrefixAtLocalFunctionCallCanReadCapturedStructVariable()
    {
        CapturedVariableBindingPatches.StructObserved = default;
        ApplyPatch(typeof(CapturedVariableBindingPatches), nameof(CapturedVariableBindingPatches.InnerPrefixAtLocalFunctionCallCanReadCapturedStructVariable));

        LocalFunctionTargets.CapturedStructVariableMethod(new BindingStruct { Value = 42 });

        Assert.That(CapturedVariableBindingPatches.StructObserved.Value, Is.EqualTo(42));
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
    public void PrefixOnLocalFunctionCanWriteCapturedReferenceTypeVariableByReference()
    {
        ApplyPatch(typeof(CapturedVariableBindingPatches), nameof(CapturedVariableBindingPatches.PrefixOnLocalFunctionCanWriteCapturedReferenceTypeVariableByReference));

        BindingReference result = LocalFunctionTargets.CapturedReferenceVariableMethod(new BindingReference { Value = 1 });

        Assert.That(result.Value, Is.EqualTo(42));
    }

    [Test]
    public void PrefixOnLocalFunctionCanWriteCapturedStructVariableByReference()
    {
        ApplyPatch(typeof(CapturedVariableBindingPatches), nameof(CapturedVariableBindingPatches.PrefixOnLocalFunctionCanWriteCapturedStructVariableByReference));

        BindingStruct result = LocalFunctionTargets.CapturedStructVariableMethod(new BindingStruct { Value = 1 });

        Assert.That(result.Value, Is.EqualTo(42));
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
    public void InnerPrefixAtLocalFunctionCallCanWriteCapturedReferenceTypeVariableByReference()
    {
        ApplyPatch(typeof(CapturedVariableBindingPatches), nameof(CapturedVariableBindingPatches.InnerPrefixAtLocalFunctionCallCanWriteCapturedReferenceTypeVariableByReference));

        BindingReference result = LocalFunctionTargets.CapturedReferenceVariableMethod(new BindingReference { Value = 1 });

        Assert.That(result.Value, Is.EqualTo(42));
    }

    [Test]
    public void InnerPrefixAtLocalFunctionCallCanWriteCapturedStructVariableByReference()
    {
        ApplyPatch(typeof(CapturedVariableBindingPatches), nameof(CapturedVariableBindingPatches.InnerPrefixAtLocalFunctionCallCanWriteCapturedStructVariableByReference));

        BindingStruct result = LocalFunctionTargets.CapturedStructVariableMethod(new BindingStruct { Value = 1 });

        Assert.That(result.Value, Is.EqualTo(42));
    }

    [Test]
    public void InnerPostfixAtLocalFunctionCallCanWriteCapturedVariableByReference()
    {
        ApplyPatch(typeof(CapturedVariableBindingPatches), nameof(CapturedVariableBindingPatches.InnerPostfixAtLocalFunctionCallCanWriteCapturedVariableByReference));

        int result = LocalFunctionTargets.CapturedVariableMethod(1);

        Assert.That(result, Is.EqualTo(42));
    }
}
