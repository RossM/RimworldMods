namespace Disharmony.Tests;

public static class BaseMethodBindingPatches
{
    public static string? ResultObserved;

    [Prefix]
    [Target(typeof(DerivedMethodTargets), nameof(DerivedMethodTargets.Describe))]
    public static bool Prefix_BaseMethod_Parameter_Primitive_Result_ReferenceType_Invokes(
        int value,
        Func<int, string> __base,
        ref string __result)
    {
        __result = __base(value);
        return false;
    }

    [Postfix]
    [Target(typeof(DerivedMethodTargets), nameof(DerivedMethodTargets.Describe))]
    public static void Postfix_BaseMethod_Parameter_Primitive_Result_ReferenceType_Invokes(
        int value,
        Func<int, string> __base,
        ref string __result) =>
        __result = $"{__result}|{__base(value)}";

    [InnerPrefix(typeof(InnerStaticMethodTargets), nameof(InnerStaticMethodTargets.Void))]
    [Target(typeof(DerivedMethodTargets), nameof(DerivedMethodTargets.DescribeWithInnerCall))]
    public static void InnerPrefix_BaseMethod_Parameter_Primitive_Result_ReferenceType_Invokes(
        int value,
        Func<int, string> __base) =>
        ResultObserved = __base(value);

    [InnerPostfix(typeof(InnerStaticMethodTargets), nameof(InnerStaticMethodTargets.Void))]
    [Target(typeof(DerivedMethodTargets), nameof(DerivedMethodTargets.DescribeWithInnerCall))]
    public static void InnerPostfix_BaseMethod_Parameter_Primitive_Result_ReferenceType_Invokes(
        int value,
        Func<int, string> __base) =>
        ResultObserved = __base(value);
}

[TestFixture]
public sealed class BaseMethodBindingTests : PatchTestBase
{
    [Test]
    public void Prefix_BaseMethod_Parameter_Primitive_Result_ReferenceType_Invokes()
    {
        ApplyPatch(
            typeof(BaseMethodBindingPatches),
            nameof(BaseMethodBindingPatches.Prefix_BaseMethod_Parameter_Primitive_Result_ReferenceType_Invokes));
        var target = new DerivedMethodTargets { InstanceValue = 1 };

        string result = target.Describe(41);

        Assert.That(result, Is.EqualTo("base:41:1"));
    }

    [Test]
    public void Postfix_BaseMethod_Parameter_Primitive_Result_ReferenceType_Invokes()
    {
        ApplyPatch(
            typeof(BaseMethodBindingPatches),
            nameof(BaseMethodBindingPatches.Postfix_BaseMethod_Parameter_Primitive_Result_ReferenceType_Invokes));
        var target = new DerivedMethodTargets { InstanceValue = 1 };

        string result = target.Describe(41);

        Assert.That(result, Is.EqualTo("derived:41:1|base:41:1"));
    }

    [Test]
    public void InnerPrefix_BaseMethod_Parameter_Primitive_Result_ReferenceType_Invokes()
    {
        BaseMethodBindingPatches.ResultObserved = null;
        ApplyPatch(
            typeof(BaseMethodBindingPatches),
            nameof(BaseMethodBindingPatches.InnerPrefix_BaseMethod_Parameter_Primitive_Result_ReferenceType_Invokes));
        var target = new DerivedMethodTargets { InstanceValue = 1 };

        string result = target.DescribeWithInnerCall(41);

        Assert.That(result, Is.EqualTo("derived:41:1"));
        Assert.That(BaseMethodBindingPatches.ResultObserved, Is.EqualTo("base:41:1"));
    }

    [Test]
    public void InnerPostfix_BaseMethod_Parameter_Primitive_Result_ReferenceType_Invokes()
    {
        BaseMethodBindingPatches.ResultObserved = null;
        ApplyPatch(
            typeof(BaseMethodBindingPatches),
            nameof(BaseMethodBindingPatches.InnerPostfix_BaseMethod_Parameter_Primitive_Result_ReferenceType_Invokes));
        var target = new DerivedMethodTargets { InstanceValue = 1 };

        string result = target.DescribeWithInnerCall(41);

        Assert.That(result, Is.EqualTo("derived:41:1"));
        Assert.That(BaseMethodBindingPatches.ResultObserved, Is.EqualTo("base:41:1"));
    }
}
