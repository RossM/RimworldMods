namespace Disharmony.Tests.EndToEnd.PatchLifecycle;

public class RuntimePatchExceptionPatches
{
    public static int PatchCalls;

    public static void RuleBuilder_IncompatibleParameterConversion_IsRejectedBeforeUpdateMethod(string value) { }

    public static void RuleBuilder_OutOfRangeParameterIndex_IsRejectedBeforeUpdateMethod(
        [Parameter(10)] int value) { }

    public static void Circumfix_ResultBindingForVoidTarget_IsRejectedBeforeUpdateMethod(int __result) { }

    public static void RuleBuilder_BaseMethodBindingForStaticTarget_IsRejectedBeforeUpdateMethod(Action __base) { }

    public static void RuleBuilder_BaseMethodBindingWithoutBaseImplementation_IsRejectedBeforeUpdateMethod(Action __base) { }

    public static void PatchWorker_InvalidPatchType_IsRejectedBeforeUpdateMethod() { }

    public void Circumfix_InstancePatchMethod_IsRejectedBeforeUpdateMethod() { }

    public static void Circumfix_GenericPatchMethod_IsRejectedBeforeUpdateMethod<T>() { }

    public static void Circumfix_ConstructedGenericPatchMethod_IsAllowed<T>() => PatchCalls++;

    public static BindingStruct Circumfix_PrefixReturningStruct_IsRejectedBeforeUpdateMethod() => default;

    public static BindingStruct Infix_InnerPrefixReturningStruct_IsRejectedBeforeUpdateMethod() => default;

    public static void ForceApply_ApplicationFailure_ReportsAndPreservesOriginalBehavior() { }

    public static void ApplyThenForceApply_ApplicationFailure_ReportsAndPreservesOriginalBehavior() { }

    public static void TrampolineResolution_ApplicationFailure_ReportsAndRestoresOriginalMethod() { }

    public static void StateBuilder_SameKeyWithDifferentTypes_DoesNotConflict(
        [State("shared")] out int primitive,
        [State("shared")] out string reference)
    {
        primitive = 42;
        reference = "state";
    }
}

public static class RuntimePatchExceptionGenericPatches<T>
{
    public static void Circumfix_OpenGenericDeclaringType_IsRejectedBeforeUpdateMethod() { }

    public static void Circumfix_ClosedGenericDeclaringType_IsAllowed() { }
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
            Patcher.Register(patch, PatchType.Prefix, targets: [target]);
            Patcher.ForceApply();
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
            Patcher.Register(patch, PatchType.Prefix, targets: [target]);
            Patcher.ForceApply();
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
            Patcher.Register(patch, PatchType.Postfix, targets: [target]);
            Patcher.ForceApply();
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
            Patcher.Register(patch, PatchType.Prefix, targets: [target]);
            Patcher.ForceApply();
        });
    }

    [Test]
    public void RuleBuilder_BaseMethodBindingWithoutBaseImplementation_IsRejectedBeforeUpdateMethod()
    {
        MethodInfo patch = typeof(RuntimePatchExceptionPatches)
            .GetMethod(nameof(RuntimePatchExceptionPatches
                .RuleBuilder_BaseMethodBindingWithoutBaseImplementation_IsRejectedBeforeUpdateMethod))!;
        MethodInfo target = typeof(InstanceMethodTargetsWithoutFields)
            .GetMethod(nameof(InstanceMethodTargetsWithoutFields.Void))!;

        Assert.Throws<InvalidOperationException>(() =>
        {
            Patcher.Register(patch, PatchType.Prefix, targets: [target]);
            Patcher.ForceApply();
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
            Patcher.Register(patch, (PatchType)int.MaxValue, targets: [target]);
            Patcher.ForceApply();
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
            Patcher.Register(patch, PatchType.Prefix, targets: [target]);
            Patcher.ForceApply();
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
            Patcher.Register(patch, PatchType.Prefix, targets: [target]);
            Patcher.ForceApply();
        });
    }

    [Test]
    public void Circumfix_ConstructedGenericPatchMethod_IsAllowed()
    {
        RuntimePatchExceptionPatches.PatchCalls = 0;
        MethodInfo patch = typeof(RuntimePatchExceptionPatches)
            .GetMethod(nameof(RuntimePatchExceptionPatches.Circumfix_ConstructedGenericPatchMethod_IsAllowed))!
            .MakeGenericMethod(typeof(int));
        MethodInfo target = typeof(StaticMethodTargets)
            .GetMethod(nameof(StaticMethodTargets.Void))!;

        Patcher.Register(patch, PatchType.Prefix, targets: [target]);
        Patcher.ForceApply();

        StaticMethodTargets.Void();

        Assert.That(RuntimePatchExceptionPatches.PatchCalls, Is.EqualTo(1));
    }

    [Test]
    public void Circumfix_OpenGenericDeclaringType_IsRejectedBeforeUpdateMethod()
    {
        MethodInfo patch = typeof(RuntimePatchExceptionGenericPatches<>)
            .GetMethod(nameof(RuntimePatchExceptionGenericPatches<int>.Circumfix_OpenGenericDeclaringType_IsRejectedBeforeUpdateMethod))!;
        MethodInfo target = typeof(StaticMethodTargets)
            .GetMethod(nameof(StaticMethodTargets.Void))!;

        Assert.Throws<InvalidOperationException>(() =>
        {
            Patcher.Register(patch, PatchType.Prefix, targets: [target]);
            Patcher.ForceApply();
        });
    }

    [Test]
    public void Circumfix_ClosedGenericDeclaringType_IsAllowed()
    {
        MethodInfo patch = typeof(RuntimePatchExceptionGenericPatches<int>)
            .GetMethod(nameof(RuntimePatchExceptionGenericPatches<int>.Circumfix_ClosedGenericDeclaringType_IsAllowed))!;
        MethodInfo target = typeof(StaticMethodTargets)
            .GetMethod(nameof(StaticMethodTargets.Void))!;

        Assert.DoesNotThrow(() =>
        {
            Patcher.Register(patch, PatchType.Prefix, targets: [target]);
            Patcher.ForceApply();
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
            Patcher.Register(patch, PatchType.Prefix, targets: [target]);
            Patcher.ForceApply();
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
            Patcher.Register(
                patch,
                PatchType.InnerPrefix,
                innerTarget: innerTarget,
                targets: [outerTarget]);
            Patcher.ForceApply();
        });
    }

    [Test]
    public void ForceApply_ApplicationFailure_ReportsAndPreservesOriginalBehavior()
    {
        MethodInfo patch = typeof(RuntimePatchExceptionPatches)
            .GetMethod(nameof(RuntimePatchExceptionPatches.ForceApply_ApplicationFailure_ReportsAndPreservesOriginalBehavior))!;
        MethodInfo innerTarget = typeof(InnerStaticMethodTargets)
            .GetMethod(nameof(InnerStaticMethodTargets.IntResult))!;
        MethodInfo outerTarget = typeof(StaticMethodTargets)
            .GetMethod(nameof(StaticMethodTargets.IntResult))!;

        List<Exception> reportedExceptions = [];
        Action<Exception> handler = reportedExceptions.Add;
        Patcher.RuntimeExceptionHandler -= ThrowRuntimeException;
        Patcher.RuntimeExceptionHandler += handler;

        try
        {
            Patcher.Register(
                patch,
                PatchType.InnerPrefix,
                innerTarget: innerTarget,
                targets: [outerTarget]);

            Assert.That(() => Patcher.ForceApply(), Throws.Nothing);
            Assert.That(reportedExceptions, Has.Count.EqualTo(1));
            Assert.That(reportedExceptions[0], Is.TypeOf<InvalidOperationException>());
            Assert.That(StaticMethodTargets.IntResult(), Is.EqualTo(1));
        }
        finally
        {
            Patcher.RuntimeExceptionHandler -= handler;
            Patcher.RuntimeExceptionHandler += ThrowRuntimeException;
        }
    }

    [Test]
    public void ApplyThenForceApply_ApplicationFailure_ReportsAndPreservesOriginalBehavior()
    {
        MethodInfo patch = typeof(RuntimePatchExceptionPatches)
            .GetMethod(nameof(RuntimePatchExceptionPatches.ApplyThenForceApply_ApplicationFailure_ReportsAndPreservesOriginalBehavior))!;
        MethodInfo innerTarget = typeof(InnerStaticMethodTargets)
            .GetMethod(nameof(InnerStaticMethodTargets.IntResult))!;
        MethodInfo outerTarget = typeof(StaticMethodTargets)
            .GetMethod(nameof(StaticMethodTargets.IntResult))!;

        List<Exception> reportedExceptions = [];
        Action<Exception> handler = reportedExceptions.Add;
        Patcher.RuntimeExceptionHandler -= ThrowRuntimeException;
        Patcher.RuntimeExceptionHandler += handler;

        try
        {
            Patcher.Register(
                patch,
                PatchType.InnerPrefix,
                innerTarget: innerTarget,
                targets: [outerTarget]);
            Patcher.Apply();

            Assert.That(reportedExceptions, Is.Empty);
            Assert.That(() => Patcher.ForceApply(), Throws.Nothing);
            Assert.That(reportedExceptions, Has.Count.EqualTo(1));
            Assert.That(reportedExceptions[0], Is.TypeOf<InvalidOperationException>());
            Assert.That(StaticMethodTargets.IntResult(), Is.EqualTo(1));
        }
        finally
        {
            Patcher.RuntimeExceptionHandler -= handler;
            Patcher.RuntimeExceptionHandler += ThrowRuntimeException;
        }
    }

    [Test]
    public void TrampolineResolution_ApplicationFailure_ReportsAndRestoresOriginalMethod()
    {
        MethodInfo patch = typeof(RuntimePatchExceptionPatches)
            .GetMethod(nameof(RuntimePatchExceptionPatches.TrampolineResolution_ApplicationFailure_ReportsAndRestoresOriginalMethod))!;
        MethodInfo innerTarget = typeof(InnerStaticMethodTargets)
            .GetMethod(nameof(InnerStaticMethodTargets.IntResult))!;
        MethodInfo outerTarget = typeof(StaticMethodTargets)
            .GetMethod(nameof(StaticMethodTargets.IntResult))!;
        List<Exception> reportedExceptions = [];
        Action<Exception> handler = reportedExceptions.Add;
        Patcher.RuntimeExceptionHandler -= ThrowRuntimeException;
        Patcher.RuntimeExceptionHandler += handler;

        try
        {
            Patcher.Register(
                patch,
                PatchType.InnerPrefix,
                innerTarget: innerTarget,
                targets: [outerTarget]);
            Patcher.Apply();

            Assert.That(StaticMethodTargets.IntResult(), Is.EqualTo(1));
            Assert.That(reportedExceptions, Has.Count.EqualTo(1));

            Assert.That(StaticMethodTargets.IntResult(), Is.EqualTo(1));
            Assert.That(reportedExceptions, Has.Count.EqualTo(1));
        }
        finally
        {
            Patcher.RuntimeExceptionHandler -= handler;
            Patcher.RuntimeExceptionHandler += ThrowRuntimeException;
        }
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
            Patcher.Register(patch, PatchType.Prefix, targets: [target]);
            Patcher.ForceApply();
        });
    }
}
