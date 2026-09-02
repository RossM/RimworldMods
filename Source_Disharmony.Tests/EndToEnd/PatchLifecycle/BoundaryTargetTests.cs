namespace Disharmony.Tests.EndToEnd.PatchLifecycle;

public static class BoundaryTargetPatches
{
    public static bool PatchExecuted;

    public static void AbstractMethod_IsRejectedBeforeRuntimePatching() => PatchExecuted = true;

    public static void InterfaceMethod_IsRejectedBeforeRuntimePatching() => PatchExecuted = true;

    public static void PInvokeMethod_IsRejectedBeforeRuntimePatching() => PatchExecuted = true;

    public static void OpenGenericDeclaringType_IsRejectedBeforeRuntimePatching() => PatchExecuted = true;

    public static void GenericMethodDefinition_IsRejectedBeforeRuntimePatching() => PatchExecuted = true;

    public static void ConstructedGenericMethod_IsRejectedBeforeRuntimePatching() => PatchExecuted = true;

    public static void ClosedGenericDeclaringType_ExecutesPatch() => PatchExecuted = true;

    public static void ExplicitInterfaceImplementation_ExecutesPatch() => PatchExecuted = true;

    public static void RefReturnMethod_ExecutesPatchAndPreservesReference() => PatchExecuted = true;

    public static void PointerParameterMethod_ExecutesPatchAndPreservesPointer() => PatchExecuted = true;

    public static void VarArgsMethod_ExecutesPatchAndPreservesArguments() => PatchExecuted = true;
}

[TestFixture]
public sealed class BoundaryTargetTests : PatchTestBase
{
    [Test]
    public void AbstractMethod_IsRejectedBeforeRuntimePatching()
    {
        MethodInfo target = typeof(BoundaryAbstractTargets)
            .GetMethod(nameof(BoundaryAbstractTargets.AbstractMethod))!;
        MethodInfo patch = typeof(BoundaryTargetPatches)
            .GetMethod(nameof(BoundaryTargetPatches.AbstractMethod_IsRejectedBeforeRuntimePatching))!;

        Assert.Throws<PatchException>(() => Patcher.Patch(Patch.Prefix.With(patch).Of(target)));
    }

    [Test]
    public void InterfaceMethod_IsRejectedBeforeRuntimePatching()
    {
        MethodInfo target = typeof(IBoundaryInterfaceTargets)
            .GetMethod(nameof(IBoundaryInterfaceTargets.InterfaceMethod))!;
        MethodInfo patch = typeof(BoundaryTargetPatches)
            .GetMethod(nameof(BoundaryTargetPatches.InterfaceMethod_IsRejectedBeforeRuntimePatching))!;

        Assert.Throws<PatchException>(() => Patcher.Patch(Patch.Prefix.With(patch).Of(target)));
    }

    [Test]
    public void PInvokeMethod_IsRejectedBeforeRuntimePatching()
    {
        MethodInfo target = typeof(BoundaryTargets)
            .GetMethod(nameof(BoundaryTargets.PInvokeMethod))!;
        MethodInfo patch = typeof(BoundaryTargetPatches)
            .GetMethod(nameof(BoundaryTargetPatches.PInvokeMethod_IsRejectedBeforeRuntimePatching))!;

        Assert.Throws<PatchException>(() => Patcher.Patch(Patch.Prefix.With(patch).Of(target)));
    }

    [Test]
    public void OpenGenericDeclaringType_IsRejectedBeforeRuntimePatching()
    {
        MethodInfo target = typeof(BoundaryGenericTargets<>)
            .GetMethod(nameof(BoundaryGenericTargets<int>.NonGenericMethod))!;
        MethodInfo patch = typeof(BoundaryTargetPatches)
            .GetMethod(nameof(BoundaryTargetPatches.OpenGenericDeclaringType_IsRejectedBeforeRuntimePatching))!;

        Assert.Throws<PatchException>(() => Patcher.Patch(Patch.Prefix.With(patch).Of(target)));
    }

    [Test]
    public void GenericMethodDefinition_IsRejectedBeforeRuntimePatching()
    {
        MethodInfo target = typeof(BoundaryTargets)
            .GetMethod(nameof(BoundaryTargets.GenericMethod))!;
        MethodInfo patch = typeof(BoundaryTargetPatches)
            .GetMethod(nameof(BoundaryTargetPatches.GenericMethodDefinition_IsRejectedBeforeRuntimePatching))!;

        Assert.Throws<PatchException>(() => Patcher.Patch(Patch.Prefix.With(patch).Of(target)));
    }

    [Test]
    public void ConstructedGenericMethod_IsRejectedBeforeRuntimePatching()
    {
        MethodInfo target = typeof(BoundaryTargets)
            .GetMethod(nameof(BoundaryTargets.GenericMethod))!
            .MakeGenericMethod(typeof(int));
        MethodInfo patch = typeof(BoundaryTargetPatches)
            .GetMethod(nameof(BoundaryTargetPatches.ConstructedGenericMethod_IsRejectedBeforeRuntimePatching))!;

        Assert.Throws<PatchException>(() => Patcher.Patch(Patch.Prefix.With(patch).Of(target)));
    }

    [Test]
    public void ClosedGenericDeclaringType_ExecutesPatch()
    {
        BoundaryTargetPatches.PatchExecuted = false;
        MethodInfo target = typeof(BoundaryGenericTargets<int>)
            .GetMethod(nameof(BoundaryGenericTargets<int>.NonGenericMethod))!;
        MethodInfo patch = typeof(BoundaryTargetPatches)
            .GetMethod(nameof(BoundaryTargetPatches.ClosedGenericDeclaringType_ExecutesPatch))!;

        Patcher.Patch(Patch.Prefix.With(patch).Of(target));
        int result = BoundaryGenericTargets<int>.NonGenericMethod(42);

        Assert.That(result, Is.EqualTo(42));
        Assert.That(BoundaryTargetPatches.PatchExecuted, Is.True);
    }

    [Test]
    public void ExplicitInterfaceImplementation_ExecutesPatch()
    {
        BoundaryTargetPatches.PatchExecuted = false;
        InterfaceMapping mapping = typeof(BoundaryExplicitInterfaceTargets)
            .GetInterfaceMap(typeof(IBoundaryInterfaceTargets));
        MethodInfo target = mapping.TargetMethods.Single();
        MethodInfo patch = typeof(BoundaryTargetPatches)
            .GetMethod(nameof(BoundaryTargetPatches.ExplicitInterfaceImplementation_ExecutesPatch))!;
        var instance = new BoundaryExplicitInterfaceTargets();

        Patcher.Patch(Patch.Prefix.With(patch).Of(target));
        int result = instance.CallInterfaceMethod(42);

        Assert.That(result, Is.EqualTo(42));
        Assert.That(BoundaryTargetPatches.PatchExecuted, Is.True);
    }

    [Test]
    public void RefReturnMethod_ExecutesPatchAndPreservesReference()
    {
        BoundaryTargetPatches.PatchExecuted = false;
        BoundaryTargets.RefReturnStorage = 1;
        MethodInfo target = typeof(BoundaryTargets)
            .GetMethod(nameof(BoundaryTargets.RefReturnMethod))!;
        MethodInfo patch = typeof(BoundaryTargetPatches)
            .GetMethod(nameof(BoundaryTargetPatches.RefReturnMethod_ExecutesPatchAndPreservesReference))!;

        Patcher.Patch(Patch.Prefix.With(patch).Of(target));
        ref int result = ref BoundaryTargets.RefReturnMethod();
        result = 42;

        Assert.That(BoundaryTargetPatches.PatchExecuted, Is.True);
        Assert.That(BoundaryTargets.RefReturnStorage, Is.EqualTo(42));
    }

    [Test]
    public void PointerParameterMethod_ExecutesPatchAndPreservesPointer()
    {
        BoundaryTargetPatches.PatchExecuted = false;
        MethodInfo target = typeof(BoundaryTargets)
            .GetMethod(nameof(BoundaryTargets.PointerParameterMethod))!;
        MethodInfo patch = typeof(BoundaryTargetPatches)
            .GetMethod(nameof(BoundaryTargetPatches.PointerParameterMethod_ExecutesPatchAndPreservesPointer))!;

        Patcher.Patch(Patch.Prefix.With(patch).Of(target));
        int result = BoundaryTargets.CallPointerParameterMethod(42);

        Assert.That(result, Is.EqualTo(42));
        Assert.That(BoundaryTargetPatches.PatchExecuted, Is.True);
    }

    [Test]
    public void VarArgsMethod_ExecutesPatchAndPreservesArguments()
    {
        BoundaryTargetPatches.PatchExecuted = false;
        MethodInfo target = typeof(BoundaryTargets)
            .GetMethod(nameof(BoundaryTargets.VarArgsMethod))!;
        MethodInfo patch = typeof(BoundaryTargetPatches)
            .GetMethod(nameof(BoundaryTargetPatches.VarArgsMethod_ExecutesPatchAndPreservesArguments))!;

        Patcher.Patch(Patch.Prefix.With(patch).Of(target));
        int result = BoundaryTargets.CallVarArgsMethod();

        Assert.That(result, Is.EqualTo(3));
        Assert.That(BoundaryTargetPatches.PatchExecuted, Is.True);
    }
}
