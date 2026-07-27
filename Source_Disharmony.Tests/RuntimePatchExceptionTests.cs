namespace Disharmony.Tests;

public class RuntimePatchExceptionPatches
{
    public static void RuleBuilder_IncompatibleParameterConversion_IsRejectedBeforeUpdateMethod(string value) { }

    public static void RuleBuilder_OutOfRangeParameterIndex_IsRejectedBeforeUpdateMethod(
        [Parameter(10)] int value) { }

    public static void Circumfix_ResultBindingForVoidTarget_IsRejectedBeforeUpdateMethod(int __result) { }

    public static void RuleBuilder_BaseMethodBindingForStaticTarget_IsRejectedBeforeUpdateMethod(Action __base) { }

    public static void RuleBuilder_BaseMethodBindingWithoutBaseImplementation_IsRejectedBeforeUpdateMethod(Action __base) { }

    public static void PatchWorker_InvalidPatchType_IsRejectedBeforeUpdateMethod() { }

    public void Circumfix_InstancePatchMethod_IsRejectedBeforeUpdateMethod() { }

    public static void Circumfix_GenericPatchMethod_IsRejectedBeforeUpdateMethod<T>() { }

    public static BindingStruct Circumfix_PrefixReturningStruct_IsRejectedBeforeUpdateMethod() => default;

    public static BindingStruct Infix_InnerPrefixReturningStruct_IsRejectedBeforeUpdateMethod() => default;

    public static void Infix_InnerTargetAbsentFromOuterTarget_IsRejectedBeforeUpdateMethod() { }

    public static void StateBuilder_SameKeyWithDifferentTypes_DoesNotConflict(
        [State("shared")] out int primitive,
        [State("shared")] out string reference)
    {
        primitive = 42;
        reference = "state";
    }
}

[TestFixture]
public sealed class RuntimePatchExceptionTests : PatchTestBase
{
    [Test]
    public void RuleBuilder_IncompatibleParameterConversion_IsRejectedBeforeUpdateMethod()
    {
        MethodInfo patch = typeof(RuntimePatchExceptionPatches)
            .GetMethod(nameof(RuntimePatchExceptionPatches.RuleBuilder_IncompatibleParameterConversion_IsRejectedBeforeUpdateMethod))!;
        MethodInfo target = typeof(StaticMethodTargets)
            .GetMethod(nameof(StaticMethodTargets.IntArgument))!;

        Assert.Throws<InvalidOperationException>(() =>
        {
            Autopatcher.Register(patch, PatchType.Prefix, targets: [target]);
            Autopatcher.ForceApply();
        });
    }

    [Test]
    public void RuleBuilder_OutOfRangeParameterIndex_IsRejectedBeforeUpdateMethod()
    {
        MethodInfo patch = typeof(RuntimePatchExceptionPatches)
            .GetMethod(nameof(RuntimePatchExceptionPatches.RuleBuilder_OutOfRangeParameterIndex_IsRejectedBeforeUpdateMethod))!;
        MethodInfo target = typeof(StaticMethodTargets)
            .GetMethod(nameof(StaticMethodTargets.IntArgument))!;

        Assert.Throws<InvalidOperationException>(() =>
        {
            Autopatcher.Register(patch, PatchType.Prefix, targets: [target]);
            Autopatcher.ForceApply();
        });
    }

    [Test]
    public void Circumfix_ResultBindingForVoidTarget_IsRejectedBeforeUpdateMethod()
    {
        MethodInfo patch = typeof(RuntimePatchExceptionPatches)
            .GetMethod(nameof(RuntimePatchExceptionPatches.Circumfix_ResultBindingForVoidTarget_IsRejectedBeforeUpdateMethod))!;
        MethodInfo target = typeof(StaticMethodTargets)
            .GetMethod(nameof(StaticMethodTargets.Void))!;

        Assert.Throws<InvalidOperationException>(() =>
        {
            Autopatcher.Register(patch, PatchType.Postfix, targets: [target]);
            Autopatcher.ForceApply();
        });
    }

    [Test]
    public void RuleBuilder_BaseMethodBindingForStaticTarget_IsRejectedBeforeUpdateMethod()
    {
        MethodInfo patch = typeof(RuntimePatchExceptionPatches)
            .GetMethod(nameof(RuntimePatchExceptionPatches.RuleBuilder_BaseMethodBindingForStaticTarget_IsRejectedBeforeUpdateMethod))!;
        MethodInfo target = typeof(StaticMethodTargets)
            .GetMethod(nameof(StaticMethodTargets.Void))!;

        Assert.Throws<InvalidOperationException>(() =>
        {
            Autopatcher.Register(patch, PatchType.Prefix, targets: [target]);
            Autopatcher.ForceApply();
        });
    }

    [Test]
    public void RuleBuilder_BaseMethodBindingWithoutBaseImplementation_IsRejectedBeforeUpdateMethod()
    {
        MethodInfo patch = typeof(RuntimePatchExceptionPatches)
            .GetMethod(nameof(RuntimePatchExceptionPatches.RuleBuilder_BaseMethodBindingWithoutBaseImplementation_IsRejectedBeforeUpdateMethod))!;
        MethodInfo target = typeof(InstanceMethodTargetsWithoutFields)
            .GetMethod(nameof(InstanceMethodTargetsWithoutFields.Void))!;

        Assert.Throws<InvalidOperationException>(() =>
        {
            Autopatcher.Register(patch, PatchType.Prefix, targets: [target]);
            Autopatcher.ForceApply();
        });
    }

    [Test]
    public void PatchWorker_InvalidPatchType_IsRejectedBeforeUpdateMethod()
    {
        MethodInfo patch = typeof(RuntimePatchExceptionPatches)
            .GetMethod(nameof(RuntimePatchExceptionPatches.PatchWorker_InvalidPatchType_IsRejectedBeforeUpdateMethod))!;
        MethodInfo target = typeof(StaticMethodTargets)
            .GetMethod(nameof(StaticMethodTargets.Void))!;

        Assert.Throws<InvalidOperationException>(() =>
        {
            Autopatcher.Register(patch, (PatchType)int.MaxValue, targets: [target]);
            Autopatcher.ForceApply();
        });
    }

    [Test]
    public void Circumfix_InstancePatchMethod_IsRejectedBeforeUpdateMethod()
    {
        MethodInfo patch = typeof(RuntimePatchExceptionPatches)
            .GetMethod(nameof(RuntimePatchExceptionPatches.Circumfix_InstancePatchMethod_IsRejectedBeforeUpdateMethod))!;
        MethodInfo target = typeof(StaticMethodTargets)
            .GetMethod(nameof(StaticMethodTargets.Void))!;

        Assert.Throws<InvalidOperationException>(() =>
        {
            Autopatcher.Register(patch, PatchType.Prefix, targets: [target]);
            Autopatcher.ForceApply();
        });
    }

    [Test]
    public void Circumfix_GenericPatchMethod_IsRejectedBeforeUpdateMethod()
    {
        MethodInfo patch = typeof(RuntimePatchExceptionPatches)
            .GetMethod(nameof(RuntimePatchExceptionPatches.Circumfix_GenericPatchMethod_IsRejectedBeforeUpdateMethod))!;
        MethodInfo target = typeof(StaticMethodTargets)
            .GetMethod(nameof(StaticMethodTargets.Void))!;

        Assert.Throws<InvalidOperationException>(() =>
        {
            Autopatcher.Register(patch, PatchType.Prefix, targets: [target]);
            Autopatcher.ForceApply();
        });
    }

    [Test]
    public void Circumfix_PrefixReturningStruct_IsRejectedBeforeUpdateMethod()
    {
        MethodInfo patch = typeof(RuntimePatchExceptionPatches)
            .GetMethod(nameof(RuntimePatchExceptionPatches.Circumfix_PrefixReturningStruct_IsRejectedBeforeUpdateMethod))!;
        MethodInfo target = typeof(StaticMethodTargets)
            .GetMethod(nameof(StaticMethodTargets.Void))!;

        Assert.Throws<InvalidOperationException>(() =>
        {
            Autopatcher.Register(patch, PatchType.Prefix, targets: [target]);
            Autopatcher.ForceApply();
        });
    }

    [Test]
    public void Infix_InnerPrefixReturningStruct_IsRejectedBeforeUpdateMethod()
    {
        MethodInfo patch = typeof(RuntimePatchExceptionPatches)
            .GetMethod(nameof(RuntimePatchExceptionPatches.Infix_InnerPrefixReturningStruct_IsRejectedBeforeUpdateMethod))!;
        MethodInfo innerTarget = typeof(InnerStaticMethodTargets)
            .GetMethod(nameof(InnerStaticMethodTargets.IntResult))!;
        MethodInfo outerTarget = typeof(OuterStaticMethodTargets)
            .GetMethod(nameof(OuterStaticMethodTargets.IntResult))!;

        Assert.Throws<InvalidOperationException>(() =>
        {
            Autopatcher.Register(
                patch,
                PatchType.InnerPrefix,
                innerTarget: innerTarget,
                targets: [outerTarget]);
            Autopatcher.ForceApply();
        });
    }

    [Test]
    [Ignore("Maybe WONTFIX")]
    public void Infix_InnerTargetAbsentFromOuterTarget_IsRejectedBeforeUpdateMethod()
    {
        MethodInfo patch = typeof(RuntimePatchExceptionPatches)
            .GetMethod(nameof(RuntimePatchExceptionPatches.Infix_InnerTargetAbsentFromOuterTarget_IsRejectedBeforeUpdateMethod))!;
        MethodInfo innerTarget = typeof(InnerStaticMethodTargets)
            .GetMethod(nameof(InnerStaticMethodTargets.IntResult))!;
        MethodInfo outerTarget = typeof(StaticMethodTargets)
            .GetMethod(nameof(StaticMethodTargets.Void))!;

        Assert.Throws<InvalidOperationException>(() =>
        {
            Autopatcher.Register(
                patch,
                PatchType.InnerPrefix,
                innerTarget: innerTarget,
                targets: [outerTarget]);
            Autopatcher.ForceApply();
        });
    }

    [Test]
    public void StateBuilder_SameKeyWithDifferentTypes_DoesNotConflict()
    {
        MethodInfo patch = typeof(RuntimePatchExceptionPatches)
            .GetMethod(nameof(RuntimePatchExceptionPatches.StateBuilder_SameKeyWithDifferentTypes_DoesNotConflict))!;
        MethodInfo target = typeof(StaticMethodTargets)
            .GetMethod(nameof(StaticMethodTargets.Void))!;

        Assert.DoesNotThrow(() =>
        {
            Autopatcher.Register(patch, PatchType.Prefix, targets: [target]);
            Autopatcher.ForceApply();
        });
    }
}
