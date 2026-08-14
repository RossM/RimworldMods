namespace Disharmony.Tests.EndToEnd.ParameterBinding;

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

    [Prefix]
    [Target(typeof(DerivedMethodTargets), nameof(DerivedMethodTargets.Describe))]
    public static void Prefix_BaseMethod_Delegate_ParameterTypeMismatch_RejectedByPatch(Func<string, string> __base) { }

    [Prefix]
    [Target(typeof(DerivedMethodTargets), nameof(DerivedMethodTargets.Describe))]
    public static void Prefix_BaseMethod_Delegate_ParameterCountMismatch_RejectedByPatch(Func<string> __base) { }

    [Prefix]
    [Target(typeof(DerivedMethodTargets), nameof(DerivedMethodTargets.Describe))]
    public static void Prefix_BaseMethod_Delegate_ReturnTypeMismatch_RejectedByPatch(Func<int, int> __base) { }

    [Prefix]
    [Target(typeof(DerivedMethodTargets), nameof(DerivedMethodTargets.Describe))]
    public static bool Prefix_BaseMethodAttribute_ReturnValueAttribute_NonReservedNames_Invokes(
        int value,
        [BaseMethod] Func<int, string> baseMethod,
        [ReturnValue] ref string result)
    {
        result = baseMethod(value);
        return false;
    }
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

    [Test]
    public void Prefix_BaseMethod_Delegate_ParameterTypeMismatch_RejectedByPatch()
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            ApplyPatch(
                typeof(BaseMethodBindingPatches),
                nameof(BaseMethodBindingPatches.Prefix_BaseMethod_Delegate_ParameterTypeMismatch_RejectedByPatch)));

        Assert.That(exception!.InnerException, Is.TypeOf<ParameterBindingException>());
        Assert.That(exception.InnerException!.Message, Is.EqualTo("__base: Parameter type mismatch"));
    }

    [Test]
    public void Prefix_BaseMethod_Delegate_ParameterCountMismatch_RejectedByPatch()
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            ApplyPatch(
                typeof(BaseMethodBindingPatches),
                nameof(BaseMethodBindingPatches.Prefix_BaseMethod_Delegate_ParameterCountMismatch_RejectedByPatch)));

        Assert.That(exception!.InnerException, Is.TypeOf<ParameterBindingException>());
        Assert.That(exception.InnerException!.Message, Is.EqualTo("__base: Parameter type mismatch"));
    }

    [Test]
    public void Prefix_BaseMethod_Delegate_ReturnTypeMismatch_RejectedByPatch()
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            ApplyPatch(
                typeof(BaseMethodBindingPatches),
                nameof(BaseMethodBindingPatches.Prefix_BaseMethod_Delegate_ReturnTypeMismatch_RejectedByPatch)));

        Assert.That(exception!.InnerException, Is.TypeOf<ParameterBindingException>());
        Assert.That(exception.InnerException!.Message, Is.EqualTo("__base: Return type mismatch"));
    }

    [Test]
    public void Prefix_BaseMethodAttribute_ReturnValueAttribute_NonReservedNames_Invokes()
    {
        ApplyPatch(
            typeof(BaseMethodBindingPatches),
            nameof(BaseMethodBindingPatches.Prefix_BaseMethodAttribute_ReturnValueAttribute_NonReservedNames_Invokes));
        var target = new DerivedMethodTargets { InstanceValue = 1 };

        string result = target.Describe(41);

        Assert.That(result, Is.EqualTo("base:41:1"));
    }
}
