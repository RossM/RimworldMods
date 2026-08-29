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
    [Target(typeof(MethodBindingInstanceTargets), nameof(MethodBindingInstanceTargets.TargetInstanceMethod))]
    public static void Prefix_MethodAttribute_NullName_UsesParameterName(
        [Method(null)] Func<int, int> BoundInstanceMethod) =>
        ResultObserved = BoundInstanceMethod(5);

    [Prefix]
    [Target(typeof(MethodBindingInstanceTargets), nameof(MethodBindingInstanceTargets.TargetInstanceMethod))]
    public static void Prefix_MethodAttribute_PrivateInstanceMethod_Invokes(
        [Method("BoundPrivateInstanceMethod")] Func<int, int> method) =>
        ResultObserved = method(5);

    [Prefix]
    [Target(typeof(MethodBindingInstanceTargets), nameof(MethodBindingInstanceTargets.TargetInstanceMethod))]
    public static void Prefix_MethodAttribute_PrivateStaticMethod_Invokes(
        [Method("BoundPrivateStaticMethod")] Func<int, int> method) =>
        ResultObserved = method(5);

    [Prefix]
    [Target(typeof(MethodBindingVirtualBaseTargets), nameof(MethodBindingVirtualBaseTargets.TargetInstanceMethod))]
    public static void Prefix_MethodAttribute_VirtualMethod_DispatchesOnRuntimeInstance(
        [Method(nameof(MethodBindingVirtualBaseTargets.BoundVirtualMethod))] Func<int, int> method) =>
        ResultObserved = method(5);

    [Prefix]
    [Target(typeof(MethodBindingStructTargets), nameof(MethodBindingStructTargets.TargetInstanceMethod))]
    public static void Prefix_MethodAttribute_StructInstanceMethod_IsRejected(
        [Method(nameof(MethodBindingStructTargets.BoundInstanceMethod))] Func<int, int> method) =>
        ResultObserved = method(5);

    [Prefix]
    [Target(typeof(MethodBindingStructTargets), nameof(MethodBindingStructTargets.TargetInstanceMethod))]
    public static void Prefix_MethodAttribute_StaticMethodOnStruct_Invokes(
        [Method(nameof(MethodBindingStructTargets.BoundStaticMethod))] Func<int, int> method) =>
        ResultObserved = method(5);

    [Prefix]
    [Target(typeof(MethodBindingStructTargets), nameof(MethodBindingStructTargets.TargetInstanceMethod))]
    public static void Prefix_MethodAttribute_ReadonlyInstanceMethodOnMutableStruct_Invokes(
        [Method(nameof(MethodBindingStructTargets.BoundReadonlyInstanceMethod))] Func<int, int> method) =>
        ResultObserved = method(5);

    [Prefix]
    [Target(typeof(MethodBindingReadonlyStructTargets), nameof(MethodBindingReadonlyStructTargets.TargetInstanceMethod))]
    public static void Prefix_MethodAttribute_InstanceMethodOnReadonlyStruct_Invokes(
        [Method(nameof(MethodBindingReadonlyStructTargets.BoundInstanceMethod))] Func<int, int> method) =>
        ResultObserved = method(5);

    [Prefix]
    [Inner(typeof(MethodBindingReadonlyStructTargets), nameof(MethodBindingReadonlyStructTargets.TargetInstanceMethod))]
    [Target(typeof(MethodBindingStaticTargets), nameof(MethodBindingStaticTargets.CallReadonlyStructInstanceMethod))]
    public static void InnerPrefix_MethodAttribute_InstanceMethodOnReadonlyInnerStruct_Invokes(
        [Method(nameof(MethodBindingReadonlyStructTargets.BoundInstanceMethod))] Func<int, int> method) =>
        ResultObserved = method(5);

    [Prefix]
    [Inner(typeof(MethodBindingStructTargets), nameof(MethodBindingStructTargets.TargetInstanceMethod))]
    [Target(typeof(MethodBindingReadonlyStructTargets),
        nameof(MethodBindingReadonlyStructTargets.CallMutableStructInstanceMethod))]
    public static void InnerPrefix_MethodAttribute_ReadonlyOuterDoesNotAllowMutableInnerStructMethod(
        [Method(nameof(MethodBindingStructTargets.BoundMutatingInstanceMethod))] Func<int, int> method) =>
        ResultObserved = method(5);

    [Prefix]
    [Target(typeof(MethodBindingInstanceTargets), nameof(MethodBindingInstanceTargets.TargetInstanceMethod))]
    public static void Prefix_MethodAttribute_OverloadedMethod_ThrowsAmbiguousMatchException(
        [Method(nameof(MethodBindingInstanceTargets.BoundOverloadedMethod))] Func<int, int> method) { }

    [Prefix]
    [Target(typeof(MethodBindingInstanceTargets), nameof(MethodBindingInstanceTargets.TargetInstanceMethod))]
    public static void Prefix_MethodAttribute_MissingMethod_IsRejected(
        [Method("MissingMethod")] Func<int, int> method) { }

    [Prefix]
    [Target(typeof(MethodBindingInstanceTargets), nameof(MethodBindingInstanceTargets.TargetStaticMethod))]
    public static void Prefix_MethodAttribute_InstanceMethodOnStaticTarget_IsRejected(
        [Method(nameof(MethodBindingInstanceTargets.BoundInstanceMethod))] Func<int, int> method) { }

    [Prefix]
    [Target(typeof(MethodBindingInstanceTargets), nameof(MethodBindingInstanceTargets.TargetInstanceMethod))]
    public static void Prefix_MethodAttribute_InnerScopeWithoutInnerPatch_IsRejected(
        [Method(nameof(MethodBindingInnerTargets.BoundInstanceMethod), Scope.Inner)] Func<int, int> method) { }

    [Prefix]
    [Inner(typeof(MethodBindingInnerTargets), nameof(MethodBindingInnerTargets.TargetInstanceMethod))]
    [Target(typeof(MethodBindingInstanceTargets), nameof(MethodBindingInstanceTargets.CallInnerInstanceMethod))]
    public static void InnerPrefix_MethodAttribute_StaticMethodOnInnerInstanceType_Invokes(
        [Method(nameof(MethodBindingInnerTargets.BoundStaticMethod))] Func<int, int> method) =>
        ResultObserved = method(5);

    [Prefix]
    [Inner(typeof(MethodBindingInnerTargets), nameof(MethodBindingInnerTargets.TargetInstanceMethod))]
    [Target(typeof(MethodBindingInstanceTargets), nameof(MethodBindingInstanceTargets.CallInnerInstanceMethod))]
    public static void InnerPrefix_MethodAttribute_InstanceMethodOnInnerInstance_BindsInnerInstance(
        [Method(nameof(MethodBindingInnerTargets.BoundInstanceMethod))] Func<int, int> method) =>
        ResultObserved = method(5);

    [Prefix]
    [Inner(typeof(MethodBindingInnerTargets), nameof(MethodBindingInnerTargets.TargetInstanceMethod))]
    [Target(typeof(MethodBindingInstanceTargets), nameof(MethodBindingInstanceTargets.CallInnerInstanceMethod))]
    public static void InnerPrefix_MethodAttribute_InnerScope_InstanceMethodOnInnerInstance_BindsInnerInstance(
        [Method(nameof(MethodBindingInnerTargets.BoundInstanceMethod), Scope.Inner)] Func<int, int> method) =>
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

    [Prefix]
    [Inner(typeof(MethodBindingInnerTargets), nameof(MethodBindingInnerTargets.TargetInstanceMethod))]
    [Target(typeof(MethodBindingIteratorTargets), nameof(MethodBindingIteratorTargets.EnumerateInnerInstanceMethod))]
    public static void IteratorInnerPrefix_MethodAttribute_InnerScope_BindsInnerInstance(
        [Method(nameof(MethodBindingInnerTargets.BoundInstanceMethod), Scope.Inner)] Func<int, int> method) =>
        ResultObserved = method(5);

    [Prefix]
    [Inner(typeof(MethodBindingInnerTargets), nameof(MethodBindingInnerTargets.TargetInstanceMethod))]
    [Target(typeof(MethodBindingIteratorTargets), nameof(MethodBindingIteratorTargets.EnumerateInnerInstanceMethod))]
    public static void IteratorInnerPrefix_MethodAttribute_OuterScopeInstanceMethod_IsRejected(
        [Method(nameof(MethodBindingIteratorTargets.BoundInstanceMethod), Scope.Outer)] Func<int, int> method) =>
        ResultObserved = method(5);

    [Prefix]
    [Inner(typeof(MethodBindingInnerTargets), nameof(MethodBindingInnerTargets.TargetInstanceMethod))]
    [Target(typeof(MethodBindingIteratorTargets), nameof(MethodBindingIteratorTargets.EnumerateInnerInstanceMethod))]
    public static void IteratorInnerPrefix_MethodAttribute_OuterScopeStaticMethod_Invokes(
        [Method(nameof(MethodBindingIteratorTargets.BoundStaticMethod), Scope.Outer)] Func<int, int> method) =>
        ResultObserved = method(5);

    [Prefix]
    [Inner(typeof(MethodBindingInstanceTargets), "CallLocalFunction.LocalFunction")]
    [Target(typeof(MethodBindingInstanceTargets), nameof(MethodBindingInstanceTargets.CallLocalFunction))]
    public static void LocalFunctionInnerPrefix_MethodAttribute_OuterScope_BindsDeclaringInstance(
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
    public void Prefix_MethodAttribute_NullName_UsesParameterName()
    {
        MethodBindingPatches.ResultObserved = 0;
        ApplyPatch(
            typeof(MethodBindingPatches),
            nameof(MethodBindingPatches.Prefix_MethodAttribute_NullName_UsesParameterName));
        MethodBindingInstanceTargets target = new() { InstanceValue = 7 };

        int result = target.TargetInstanceMethod();

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.EqualTo(10));
            Assert.That(MethodBindingPatches.ResultObserved, Is.EqualTo(12));
        });
    }

    [Test]
    public void Prefix_MethodAttribute_PrivateInstanceMethod_Invokes()
    {
        MethodBindingPatches.ResultObserved = 0;
        ApplyPatch(
            typeof(MethodBindingPatches),
            nameof(MethodBindingPatches.Prefix_MethodAttribute_PrivateInstanceMethod_Invokes));
        MethodBindingInstanceTargets target = new();

        int result = target.TargetInstanceMethod();

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.EqualTo(10));
            Assert.That(MethodBindingPatches.ResultObserved, Is.EqualTo(105));
        });
    }

    [Test]
    public void Prefix_MethodAttribute_PrivateStaticMethod_Invokes()
    {
        MethodBindingPatches.ResultObserved = 0;
        ApplyPatch(
            typeof(MethodBindingPatches),
            nameof(MethodBindingPatches.Prefix_MethodAttribute_PrivateStaticMethod_Invokes));
        MethodBindingInstanceTargets target = new();

        int result = target.TargetInstanceMethod();

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.EqualTo(10));
            Assert.That(MethodBindingPatches.ResultObserved, Is.EqualTo(205));
        });
    }

    [Test]
    public void Prefix_MethodAttribute_VirtualMethod_DispatchesOnRuntimeInstance()
    {
        MethodBindingPatches.ResultObserved = 0;
        ApplyPatch(
            typeof(MethodBindingPatches),
            nameof(MethodBindingPatches.Prefix_MethodAttribute_VirtualMethod_DispatchesOnRuntimeInstance));
        MethodBindingVirtualBaseTargets target = new MethodBindingVirtualDerivedTargets();

        int result = target.TargetInstanceMethod();

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.EqualTo(50));
            Assert.That(MethodBindingPatches.ResultObserved, Is.EqualTo(605));
        });
    }

    [Test]
    public void Prefix_MethodAttribute_StructInstanceMethod_IsRejected()
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            ApplyPatch(
                typeof(MethodBindingPatches),
                nameof(MethodBindingPatches.Prefix_MethodAttribute_StructInstanceMethod_IsRejected)));

        Assert.That(exception!.InnerException, Is.TypeOf<ParameterBindingException>()
            .With.Message.EqualTo("method: [Method] is not supported for non-static methods on structs"));
    }

    [Test]
    public void Prefix_MethodAttribute_StaticMethodOnStruct_Invokes()
    {
        MethodBindingPatches.ResultObserved = 0;
        ApplyPatch(
            typeof(MethodBindingPatches),
            nameof(MethodBindingPatches.Prefix_MethodAttribute_StaticMethodOnStruct_Invokes));
        MethodBindingStructTargets target = new() { InstanceValue = 40 };

        int result = target.TargetInstanceMethod();

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.EqualTo(40));
            Assert.That(MethodBindingPatches.ResultObserved, Is.EqualTo(405));
        });
    }

    [Test]
    public void Prefix_MethodAttribute_ReadonlyInstanceMethodOnMutableStruct_Invokes()
    {
        MethodBindingPatches.ResultObserved = 0;
        ApplyPatch(
            typeof(MethodBindingPatches),
            nameof(MethodBindingPatches.Prefix_MethodAttribute_ReadonlyInstanceMethodOnMutableStruct_Invokes));
        MethodBindingStructTargets target = new() { InstanceValue = 40 };

        int result = target.TargetInstanceMethod();

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.EqualTo(40));
            Assert.That(MethodBindingPatches.ResultObserved, Is.EqualTo(45));
        });
    }

    [Test]
    public void Prefix_MethodAttribute_InstanceMethodOnReadonlyStruct_Invokes()
    {
        MethodBindingPatches.ResultObserved = 0;
        ApplyPatch(
            typeof(MethodBindingPatches),
            nameof(MethodBindingPatches.Prefix_MethodAttribute_InstanceMethodOnReadonlyStruct_Invokes));
        MethodBindingReadonlyStructTargets target = new(40);

        int result = target.TargetInstanceMethod();

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.EqualTo(60));
            Assert.That(MethodBindingPatches.ResultObserved, Is.EqualTo(45));
        });
    }

    [Test]
    public void InnerPrefix_MethodAttribute_InstanceMethodOnReadonlyInnerStruct_Invokes()
    {
        MethodBindingPatches.ResultObserved = 0;
        ApplyPatch(
            typeof(MethodBindingPatches),
            nameof(MethodBindingPatches.InnerPrefix_MethodAttribute_InstanceMethodOnReadonlyInnerStruct_Invokes));
        MethodBindingReadonlyStructTargets inner = new(40);

        int result = MethodBindingStaticTargets.CallReadonlyStructInstanceMethod(inner);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.EqualTo(60));
            Assert.That(MethodBindingPatches.ResultObserved, Is.EqualTo(45));
        });
    }

    [Test]
    public void InnerPrefix_MethodAttribute_ReadonlyOuterDoesNotAllowMutableInnerStructMethod()
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            ApplyPatch(
                typeof(MethodBindingPatches),
                nameof(MethodBindingPatches.InnerPrefix_MethodAttribute_ReadonlyOuterDoesNotAllowMutableInnerStructMethod)));

        Assert.That(exception!.InnerException, Is.TypeOf<ParameterBindingException>()
            .With.Message.EqualTo("method: [Method] is not supported for non-static methods on structs"));
    }

    [Test]
    public void Prefix_MethodAttribute_OverloadedMethod_ThrowsAmbiguousMatchException()
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            ApplyPatch(
                typeof(MethodBindingPatches),
                nameof(MethodBindingPatches.Prefix_MethodAttribute_OverloadedMethod_ThrowsAmbiguousMatchException)));

        Assert.That(exception!.InnerException, Is.TypeOf<AmbiguousMatchException>());
    }

    [Test]
    public void Prefix_MethodAttribute_MissingMethod_IsRejected()
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            ApplyPatch(
                typeof(MethodBindingPatches),
                nameof(MethodBindingPatches.Prefix_MethodAttribute_MissingMethod_IsRejected)));

        Assert.That(exception!.InnerException, Is.TypeOf<ParameterBindingException>()
            .With.Message.EqualTo("method: Method not found"));
    }

    [Test]
    public void Prefix_MethodAttribute_InstanceMethodOnStaticTarget_IsRejected()
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            ApplyPatch(
                typeof(MethodBindingPatches),
                nameof(MethodBindingPatches.Prefix_MethodAttribute_InstanceMethodOnStaticTarget_IsRejected)));

        Assert.That(exception!.InnerException, Is.TypeOf<ParameterBindingException>()
            .With.Message.EqualTo("method: Instance required"));
    }

    [Test]
    public void Prefix_MethodAttribute_InnerScopeWithoutInnerPatch_IsRejected()
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            ApplyPatch(
                typeof(MethodBindingPatches),
                nameof(MethodBindingPatches.Prefix_MethodAttribute_InnerScopeWithoutInnerPatch_IsRejected)));

        Assert.That(exception!.InnerException, Is.TypeOf<ParameterBindingException>()
            .With.Message.EqualTo("method: Invalid scope"));
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
    public void InnerPrefix_MethodAttribute_InstanceMethodOnInnerInstance_BindsInnerInstance()
    {
        MethodBindingPatches.ResultObserved = 0;
        ApplyPatch(
            typeof(MethodBindingPatches),
            nameof(MethodBindingPatches.InnerPrefix_MethodAttribute_InstanceMethodOnInnerInstance_BindsInnerInstance));
        MethodBindingInstanceTargets target = new() { InstanceValue = 7 };
        MethodBindingInnerTargets inner = new() { InstanceValue = 40 };

        int result = target.CallInnerInstanceMethod(inner);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.EqualTo(20));
            Assert.That(MethodBindingPatches.ResultObserved, Is.EqualTo(45));
        });
    }

    [Test]
    public void InnerPrefix_MethodAttribute_InnerScope_InstanceMethodOnInnerInstance_BindsInnerInstance()
    {
        MethodBindingPatches.ResultObserved = 0;
        ApplyPatch(
            typeof(MethodBindingPatches),
            nameof(MethodBindingPatches.InnerPrefix_MethodAttribute_InnerScope_InstanceMethodOnInnerInstance_BindsInnerInstance));
        MethodBindingInstanceTargets target = new() { InstanceValue = 7 };
        MethodBindingInnerTargets inner = new() { InstanceValue = 40 };

        int result = target.CallInnerInstanceMethod(inner);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.EqualTo(20));
            Assert.That(MethodBindingPatches.ResultObserved, Is.EqualTo(45));
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

    [Test]
    public void IteratorInnerPrefix_MethodAttribute_InnerScope_BindsInnerInstance()
    {
        MethodBindingPatches.ResultObserved = 0;
        ApplyPatch(
            typeof(MethodBindingPatches),
            nameof(MethodBindingPatches.IteratorInnerPrefix_MethodAttribute_InnerScope_BindsInnerInstance));
        MethodBindingIteratorTargets target = new() { InstanceValue = 7 };
        MethodBindingInnerTargets inner = new() { InstanceValue = 40 };

        int result = target.EnumerateInnerInstanceMethod(inner).Single();

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.EqualTo(20));
            Assert.That(MethodBindingPatches.ResultObserved, Is.EqualTo(45));
        });
    }

    [Test]
    public void IteratorInnerPrefix_MethodAttribute_OuterScopeInstanceMethod_IsRejected()
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            ApplyPatch(
                typeof(MethodBindingPatches),
                nameof(MethodBindingPatches.IteratorInnerPrefix_MethodAttribute_OuterScopeInstanceMethod_IsRejected)));

        Assert.That(exception!.InnerException, Is.TypeOf<ParameterBindingException>()
            .With.Message.EqualTo(
                "method: [Method] is not supported for iterator state machines"));
    }

    [Test]
    public void IteratorInnerPrefix_MethodAttribute_OuterScopeStaticMethod_Invokes()
    {
        MethodBindingPatches.ResultObserved = 0;
        ApplyPatch(
            typeof(MethodBindingPatches),
            nameof(MethodBindingPatches.IteratorInnerPrefix_MethodAttribute_OuterScopeStaticMethod_Invokes));
        MethodBindingIteratorTargets target = new();
        MethodBindingInnerTargets inner = new();

        int result = target.EnumerateInnerInstanceMethod(inner).Single();

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.EqualTo(20));
            Assert.That(MethodBindingPatches.ResultObserved, Is.EqualTo(705));
        });
    }

    [Test]
    public void LocalFunctionInnerPrefix_MethodAttribute_OuterScope_BindsDeclaringInstance()
    {
        MethodBindingPatches.ResultObserved = 0;
        ApplyPatch(
            typeof(MethodBindingPatches),
            nameof(MethodBindingPatches.LocalFunctionInnerPrefix_MethodAttribute_OuterScope_BindsDeclaringInstance));
        MethodBindingInstanceTargets target = new() { InstanceValue = 7 };

        int result = target.CallLocalFunction();

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.EqualTo(8));
            Assert.That(MethodBindingPatches.ResultObserved, Is.EqualTo(12));
        });
    }
}
