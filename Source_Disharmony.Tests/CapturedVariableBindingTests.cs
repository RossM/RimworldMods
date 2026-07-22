namespace Disharmony.Tests;

[TestFixture]
public sealed class CapturedVariableBindingTests : PatchTestBase
{
    [Test]
    public void PrefixOnLocalFunctionCanReadCapturedVariable()
    {
        InnerParameterBindingPatchMethods.CapturedVariableObserved = 0;
        ApplyPatch(typeof(InnerParameterBindingPatchMethods), nameof(InnerParameterBindingPatchMethods.ReadCapturedVariablePrefix));

        int result = LocalFunctionTargets.CapturedVariableMethod(42);

        Assert.That(result, Is.EqualTo(42));
        Assert.That(InnerParameterBindingPatchMethods.CapturedVariableObserved, Is.EqualTo(42));
    }

    [Test]
    public void PostfixOnLocalFunctionCanReadCapturedVariable()
    {
        InnerParameterBindingPatchMethods.CapturedVariableObserved = 0;
        ApplyPatch(typeof(InnerParameterBindingPatchMethods), nameof(InnerParameterBindingPatchMethods.ReadCapturedVariablePostfix));

        int result = LocalFunctionTargets.CapturedVariableMethod(42);

        Assert.That(result, Is.EqualTo(42));
        Assert.That(InnerParameterBindingPatchMethods.CapturedVariableObserved, Is.EqualTo(42));
    }

    [Test]
    public void InnerPrefixAtLocalFunctionCallCanReadCapturedVariable()
    {
        InnerParameterBindingPatchMethods.CapturedVariableObserved = 0;
        ApplyPatch(typeof(InnerParameterBindingPatchMethods), nameof(InnerParameterBindingPatchMethods.ReadCapturedVariableInnerPrefix));

        int result = LocalFunctionTargets.CapturedVariableMethod(42);

        Assert.That(result, Is.EqualTo(42));
        Assert.That(InnerParameterBindingPatchMethods.CapturedVariableObserved, Is.EqualTo(42));
    }

    [Test]
    public void InnerPostfixAtLocalFunctionCallCanReadCapturedVariable()
    {
        InnerParameterBindingPatchMethods.CapturedVariableObserved = 0;
        ApplyPatch(typeof(InnerParameterBindingPatchMethods), nameof(InnerParameterBindingPatchMethods.ReadCapturedVariableInnerPostfix));

        int result = LocalFunctionTargets.CapturedVariableMethod(42);

        Assert.That(result, Is.EqualTo(42));
        Assert.That(InnerParameterBindingPatchMethods.CapturedVariableObserved, Is.EqualTo(42));
    }

    [Test]
    public void PrefixOnLocalFunctionCanWriteCapturedVariableByReference()
    {
        ApplyPatch(typeof(InnerParameterBindingPatchMethods), nameof(InnerParameterBindingPatchMethods.WriteCapturedVariablePrefix));

        int result = LocalFunctionTargets.CapturedVariableMethod(1);

        Assert.That(result, Is.EqualTo(42));
    }

    [Test]
    public void PostfixOnLocalFunctionCanWriteCapturedVariableByReference()
    {
        ApplyPatch(typeof(InnerParameterBindingPatchMethods), nameof(InnerParameterBindingPatchMethods.WriteCapturedVariablePostfix));

        int result = LocalFunctionTargets.CapturedVariableMethod(1);

        Assert.That(result, Is.EqualTo(42));
    }

    [Test]
    public void InnerPrefixAtLocalFunctionCallCanWriteCapturedVariableByReference()
    {
        ApplyPatch(typeof(InnerParameterBindingPatchMethods), nameof(InnerParameterBindingPatchMethods.WriteCapturedVariableInnerPrefix));

        int result = LocalFunctionTargets.CapturedVariableMethod(1);

        Assert.That(result, Is.EqualTo(42));
    }

    [Test]
    public void InnerPostfixAtLocalFunctionCallCanWriteCapturedVariableByReference()
    {
        ApplyPatch(typeof(InnerParameterBindingPatchMethods), nameof(InnerParameterBindingPatchMethods.WriteCapturedVariableInnerPostfix));

        int result = LocalFunctionTargets.CapturedVariableMethod(1);

        Assert.That(result, Is.EqualTo(42));
    }
}
