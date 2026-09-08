namespace Disharmony.Tests.EndToEnd.Patching;

public static class RefReturnPatchingPatches
{
    public static int ExecutionCount;
    public static int Observed;

    [Prefix]
    [Target(typeof(BoundaryTargets), nameof(BoundaryTargets.RefReturnMethod))]
    public static void Prefix_StaticField_PreservesAliasOnFirstAndSubsequentCalls() => ExecutionCount++;

    [Postfix]
    [Target(typeof(RefReturnTargets), nameof(RefReturnTargets.PrimitiveReference))]
    public static void Postfix_InstancePrimitive_PreservesAlias() => ExecutionCount++;

    [Prefix]
    [Target(typeof(RefReturnTargets), nameof(RefReturnTargets.ObjectReference))]
    [PatchOptions(PatchOptions.Debug)]
    public static void Prefix_InstanceReferenceType_PreservesAlias() => ExecutionCount++;

    [Postfix]
    [Target(typeof(RefReturnTargets), nameof(RefReturnTargets.StructReference))]
    public static void Postfix_InstanceStruct_PreservesAlias() => ExecutionCount++;

    [Prefix]
    [Target(typeof(RefReturnTargets), nameof(RefReturnTargets.ArgumentReference))]
    public static void Prefix_RefArgument_PreservesCallerStorage() => ExecutionCount++;

    [Postfix]
    [Target(typeof(RefReturnTargets), nameof(RefReturnTargets.ArrayElement))]
    public static void Postfix_ArrayElement_PreservesSelectedElement() => ExecutionCount++;

    [Postfix]
    [Target(typeof(RefReturnTargets), nameof(RefReturnTargets.ReadonlyReference))]
    public static void Postfix_ReadonlyReference_PreservesLiveAlias() => ExecutionCount++;

    [Prefix]
    [Inner(typeof(RefReturnTargets), nameof(RefReturnTargets.ArrayElement))]
    [Target(typeof(RefReturnTargets), nameof(RefReturnTargets.CallArrayElement))]
    public static void InnerPrefix_ArrayElement_PreservesAlias() => ExecutionCount++;

    [Postfix]
    [Inner(typeof(RefReturnTargets), nameof(RefReturnTargets.ArrayElement))]
    [Target(typeof(RefReturnTargets), nameof(RefReturnTargets.CallArrayElement))]
    public static void InnerPostfix_ArrayElement_PreservesAlias() => ExecutionCount++;

    [Postfix]
    [Target(typeof(RefReturnTargets), nameof(RefReturnTargets.PrimitiveReference))]
    [PatchOptions(PatchOptions.Debug)]
    public static void Postfix_Result_Primitive_ReadByValue(int __result) => Observed = __result;

    [Postfix]
    [Target(typeof(RefReturnTargets), nameof(RefReturnTargets.PrimitiveReference))]
    [PatchOptions(PatchOptions.Debug)]
    public static void Postfix_Result_Primitive_ReadByReference(ref int __result) => Observed = __result;

}

[TestFixture]
public sealed class RefReturnPatchingTests : PatchTestBase
{
    [Test, Timeout(10000)]
    public void Prefix_StaticField_PreservesAliasOnFirstAndSubsequentCalls()
    {
        RefReturnPatchingPatches.ExecutionCount = 0;
        BoundaryTargets.RefReturnStorage = 11;
        ApplyPatch(typeof(RefReturnPatchingPatches), nameof(RefReturnPatchingPatches.Prefix_StaticField_PreservesAliasOnFirstAndSubsequentCalls));

        ref int first = ref BoundaryTargets.RefReturnMethod();
        Assert.That(first, Is.EqualTo(11));
        first = 23;
        Assert.That(BoundaryTargets.RefReturnStorage, Is.EqualTo(23));

        ref int second = ref BoundaryTargets.RefReturnMethod();
        second = 37;
        Assert.That(first, Is.EqualTo(37));
        Assert.That(BoundaryTargets.RefReturnStorage, Is.EqualTo(37));
        Assert.That(RefReturnPatchingPatches.ExecutionCount, Is.EqualTo(2));
    }

    [Test, Timeout(10000)]
    public void Postfix_InstancePrimitive_PreservesAlias()
    {
        RefReturnPatchingPatches.ExecutionCount = 0;
        var target = new RefReturnTargets { Primitive = 11 };
        ApplyPatch(typeof(RefReturnPatchingPatches), nameof(RefReturnPatchingPatches.Postfix_InstancePrimitive_PreservesAlias));

        ref int result = ref target.PrimitiveReference();
        Assert.That(result, Is.EqualTo(11));
        result = 23;
        Assert.That(target.Primitive, Is.EqualTo(23));
        target.Primitive = 37;
        Assert.That(result, Is.EqualTo(37));
        Assert.That(RefReturnPatchingPatches.ExecutionCount, Is.EqualTo(1));
    }

    [Test, Timeout(10000)]
    public void Prefix_InstanceReferenceType_PreservesAlias()
    {
        RefReturnPatchingPatches.ExecutionCount = 0;
        var original = new BindingReference { Value = 11 };
        var target = new RefReturnTargets { Reference = original };
        ApplyPatch(typeof(RefReturnPatchingPatches), nameof(RefReturnPatchingPatches.Prefix_InstanceReferenceType_PreservesAlias));

        ref BindingReference result = ref target.ObjectReference();
        Assert.That(result, Is.SameAs(original));
        var replacement = new BindingReference { Value = 23 };
        result = replacement;
        Assert.That(target.Reference, Is.SameAs(replacement));
        Assert.That(RefReturnPatchingPatches.ExecutionCount, Is.EqualTo(1));
    }

    [Test, Timeout(10000)]
    public void Postfix_InstanceStruct_PreservesAlias()
    {
        RefReturnPatchingPatches.ExecutionCount = 0;
        var target = new RefReturnTargets { Structure = new BindingStruct { Value = 11 } };
        ApplyPatch(typeof(RefReturnPatchingPatches), nameof(RefReturnPatchingPatches.Postfix_InstanceStruct_PreservesAlias));

        ref BindingStruct result = ref target.StructReference();
        Assert.That(result.Value, Is.EqualTo(11));
        result.Value = 23;
        Assert.That(target.Structure.Value, Is.EqualTo(23));
        Assert.That(RefReturnPatchingPatches.ExecutionCount, Is.EqualTo(1));
    }

    [Test, Timeout(10000)]
    public void Prefix_RefArgument_PreservesCallerStorage()
    {
        RefReturnPatchingPatches.ExecutionCount = 0;
        int value = 11;
        ApplyPatch(typeof(RefReturnPatchingPatches), nameof(RefReturnPatchingPatches.Prefix_RefArgument_PreservesCallerStorage));

        ref int result = ref RefReturnTargets.ArgumentReference(ref value);
        Assert.That(result, Is.EqualTo(11));
        result = 23;
        Assert.That(value, Is.EqualTo(23));
        Assert.That(RefReturnPatchingPatches.ExecutionCount, Is.EqualTo(1));
    }

    [Test, Timeout(10000)]
    public void Postfix_ArrayElement_PreservesSelectedElement()
    {
        RefReturnPatchingPatches.ExecutionCount = 0;
        int[] values = [11, 23, 37];
        ApplyPatch(typeof(RefReturnPatchingPatches), nameof(RefReturnPatchingPatches.Postfix_ArrayElement_PreservesSelectedElement));

        ref int result = ref RefReturnTargets.ArrayElement(values, 1);
        Assert.That(result, Is.EqualTo(23));
        result = 42;
        Assert.That(values, Is.EqualTo(new[] { 11, 42, 37 }));
        Assert.That(RefReturnPatchingPatches.ExecutionCount, Is.EqualTo(1));
    }

    [Test, Timeout(10000)]
    public void Postfix_ReadonlyReference_PreservesLiveAlias()
    {
        RefReturnPatchingPatches.ExecutionCount = 0;
        var target = new RefReturnTargets { Primitive = 11 };
        ApplyPatch(typeof(RefReturnPatchingPatches), nameof(RefReturnPatchingPatches.Postfix_ReadonlyReference_PreservesLiveAlias));

        ref readonly int result = ref target.ReadonlyReference();
        Assert.That(result, Is.EqualTo(11));
        target.Primitive = 23;
        Assert.That(result, Is.EqualTo(23));
        Assert.That(RefReturnPatchingPatches.ExecutionCount, Is.EqualTo(1));
    }

    [Test, Timeout(10000)]
    public void InnerPrefix_ArrayElement_PreservesAlias()
    {
        RefReturnPatchingPatches.ExecutionCount = 0;
        int[] values = [11, 23, 37];
        ApplyPatch(typeof(RefReturnPatchingPatches), nameof(RefReturnPatchingPatches.InnerPrefix_ArrayElement_PreservesAlias));

        ref int result = ref RefReturnTargets.CallArrayElement(values, 1);
        Assert.That(result, Is.EqualTo(23));
        result = 42;
        Assert.That(values, Is.EqualTo(new[] { 11, 42, 37 }));
        Assert.That(RefReturnPatchingPatches.ExecutionCount, Is.EqualTo(1));
    }

    [Test, Timeout(10000)]
    public void InnerPostfix_ArrayElement_PreservesAlias()
    {
        RefReturnPatchingPatches.ExecutionCount = 0;
        int[] values = [11, 23, 37];
        ApplyPatch(typeof(RefReturnPatchingPatches), nameof(RefReturnPatchingPatches.InnerPostfix_ArrayElement_PreservesAlias));

        ref int result = ref RefReturnTargets.CallArrayElement(values, 1);
        Assert.That(result, Is.EqualTo(23));
        result = 42;
        Assert.That(values, Is.EqualTo(new[] { 11, 42, 37 }));
        Assert.That(RefReturnPatchingPatches.ExecutionCount, Is.EqualTo(1));
    }

    [Test, Timeout(10000)]
    public void Postfix_Result_Primitive_ReadByValue()
    {
        RefReturnPatchingPatches.Observed = 0;
        var target = new RefReturnTargets { Primitive = 42 };
        ApplyPatch(typeof(RefReturnPatchingPatches), nameof(RefReturnPatchingPatches.Postfix_Result_Primitive_ReadByValue));

        ref int result = ref target.PrimitiveReference();

        Assert.That(RefReturnPatchingPatches.Observed, Is.EqualTo(42));
        Assert.That(result, Is.EqualTo(42));
    }

    [Test, Timeout(10000)]
    public void Postfix_Result_Primitive_ReadByReference()
    {
        RefReturnPatchingPatches.Observed = 0;
        var target = new RefReturnTargets { Primitive = 42 };
        ApplyPatch(typeof(RefReturnPatchingPatches), nameof(RefReturnPatchingPatches.Postfix_Result_Primitive_ReadByReference));

        ref int result = ref target.PrimitiveReference();

        Assert.That(RefReturnPatchingPatches.Observed, Is.EqualTo(42));
        Assert.That(result, Is.EqualTo(42));
    }

}
