using NUnit.Framework;

namespace Disharmony.Tests;

[TestFixture]
public sealed partial class InstanceBindingTests : PatchTestBase
{
    [Test]
    public void PrefixCanCapturePatchedMethodInstance()
    {
        PatchMethods.InstanceObserved = null;
        ApplyPatch(nameof(PatchMethods.CaptureInstancePrefix));
        var instance = new ClassMethodTargets();

        instance.Void();

        Assert.That(PatchMethods.InstanceObserved, Is.SameAs(instance));
    }

    [Test]
    public void PostfixCanCapturePatchedMethodInstance()
    {
        PatchMethods.InstanceObserved = null;
        ApplyPatch(nameof(PatchMethods.CaptureInstancePostfix));
        var instance = new ClassMethodTargets();

        instance.Void();

        Assert.That(PatchMethods.InstanceObserved, Is.SameAs(instance));
    }

    [Test]
    public void PrefixCanWritePatchedMethodInstanceByReference()
    {
        var original = new ClassMethodTargets();
        var replacement = new ClassMethodTargets();
        PatchMethods.ReplacementInstance = replacement;
        ApplyPatch(nameof(PatchMethods.WriteInstancePrefix));

        ClassMethodTargets result = original.Self();

        Assert.That(result, Is.SameAs(replacement));
    }

    [Test]
    public void PostfixCanWritePatchedMethodInstanceByReference()
    {
        var original = new ClassMethodTargets();
        var replacement = new ClassMethodTargets();
        PatchMethods.ReplacementInstance = replacement;
        ApplyPatch(nameof(PatchMethods.WriteInstancePostfix));

        ClassMethodTargets result = original.Self();

        Assert.That(result, Is.SameAs(replacement));
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

    [Test]
    public void InnerPrefixCanWriteOuterMethodInstanceByReference()
    {
        var original = new ClassMethodTargets();
        var replacement = new ClassMethodTargets();
        replacement.IntIdentity(42);
        InnerParameterBindingPatchMethods.ReplacementCaller = replacement;
        ApplyInnerParameterBindingPatch(nameof(InnerParameterBindingPatchMethods.WriteCallerPrefix));

        int result = original.CallStaticVoidAndReturnValue();

        Assert.That(result, Is.EqualTo(42));
    }

    [Test]
    public void InnerPostfixCanWriteOuterMethodInstanceByReference()
    {
        var original = new ClassMethodTargets();
        var replacement = new ClassMethodTargets();
        replacement.IntIdentity(42);
        InnerParameterBindingPatchMethods.ReplacementCaller = replacement;
        ApplyInnerParameterBindingPatch(nameof(InnerParameterBindingPatchMethods.WriteCallerPostfix));

        int result = original.CallStaticVoidAndReturnValue();

        Assert.That(result, Is.EqualTo(42));
    }
}
