namespace Disharmony.Tests.EndToEnd.ParameterBinding;

public static class MethodBindingPatches
{
    public static int ResultObserved;

    [Prefix]
    [Target(typeof(MethodBindingInstanceTargets), nameof(MethodBindingInstanceTargets.TargetInstanceMethod))]
    public static void Prefix_MethodAttribute_InstanceMethodOnOuterInstance_Invokes(
        [Method(nameof(MethodBindingInstanceTargets.BoundInstanceMethod))] Func<int, int> method) =>
        ResultObserved = method(5);

    [Prefix]
    [Inner(typeof(MethodBindingInnerTargets), nameof(MethodBindingInnerTargets.TargetInstanceMethod))]
    [Target(typeof(MethodBindingInstanceTargets), nameof(MethodBindingInstanceTargets.CallInnerInstanceMethod))]
    public static void InnerPrefix_MethodAttribute_StaticMethodOnInnerInstanceType_Invokes(
        [Method(nameof(MethodBindingInnerTargets.BoundStaticMethod))] Func<int, int> method) =>
        ResultObserved = method(5);

    [Prefix]
    [Target(typeof(MethodBindingStaticTargets), nameof(MethodBindingStaticTargets.TargetStaticMethod))]
    public static void Prefix_MethodAttribute_StaticMethodOnStaticType_Invokes(
        [Method(nameof(MethodBindingStaticTargets.BoundStaticMethod))] Func<int, int> method) =>
        ResultObserved = method(5);

    [Prefix]
    [Inner(typeof(MethodBindingInnerTargets), nameof(MethodBindingInnerTargets.TargetInstanceMethod))]
    [Target(typeof(MethodBindingInstanceTargets), nameof(MethodBindingInstanceTargets.CallInnerInstanceMethod))]
    public static void InnerPrefix_MethodAttribute_OuterScope_InstanceMethodOnOuterInstance_Invokes(
        [Method(nameof(MethodBindingInstanceTargets.BoundInstanceMethod), Scope.Outer)] Func<int, int> method) =>
        ResultObserved = method(5);
}

[TestFixture]
public sealed class MethodBindingTests : PatchTestBase
{
    [Test]
    public void Prefix_MethodAttribute_InstanceMethodOnOuterInstance_Invokes()
    {
        MethodBindingPatches.ResultObserved = 0;
        ApplyPatch(
            typeof(MethodBindingPatches),
            nameof(MethodBindingPatches.Prefix_MethodAttribute_InstanceMethodOnOuterInstance_Invokes));
        MethodBindingInstanceTargets target = new() { InstanceValue = 7 };

        int result = target.TargetInstanceMethod();

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.EqualTo(10));
            Assert.That(MethodBindingPatches.ResultObserved, Is.EqualTo(12));
        });
    }

    [Test]
    public void InnerPrefix_MethodAttribute_StaticMethodOnInnerInstanceType_Invokes()
    {
        MethodBindingPatches.ResultObserved = 0;
        ApplyPatch(
            typeof(MethodBindingPatches),
            nameof(MethodBindingPatches.InnerPrefix_MethodAttribute_StaticMethodOnInnerInstanceType_Invokes));
        MethodBindingInstanceTargets target = new();
        MethodBindingInnerTargets inner = new();

        int result = target.CallInnerInstanceMethod(inner);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.EqualTo(20));
            Assert.That(MethodBindingPatches.ResultObserved, Is.EqualTo(205));
        });
    }

    [Test]
    public void Prefix_MethodAttribute_StaticMethodOnStaticType_Invokes()
    {
        MethodBindingPatches.ResultObserved = 0;
        ApplyPatch(
            typeof(MethodBindingPatches),
            nameof(MethodBindingPatches.Prefix_MethodAttribute_StaticMethodOnStaticType_Invokes));

        int result = MethodBindingStaticTargets.TargetStaticMethod();

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.EqualTo(30));
            Assert.That(MethodBindingPatches.ResultObserved, Is.EqualTo(305));
        });
    }

    [Test]
    public void InnerPrefix_MethodAttribute_OuterScope_InstanceMethodOnOuterInstance_Invokes()
    {
        MethodBindingPatches.ResultObserved = 0;
        ApplyPatch(
            typeof(MethodBindingPatches),
            nameof(MethodBindingPatches.InnerPrefix_MethodAttribute_OuterScope_InstanceMethodOnOuterInstance_Invokes));
        MethodBindingInstanceTargets target = new() { InstanceValue = 7 };
        MethodBindingInnerTargets inner = new();

        int result = target.CallInnerInstanceMethod(inner);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.EqualTo(20));
            Assert.That(MethodBindingPatches.ResultObserved, Is.EqualTo(12));
        });
    }
}
