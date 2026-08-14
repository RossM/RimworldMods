namespace Disharmony.Tests.EndToEnd.ParameterBinding;

public static partial class ResultBindingPatches
{
    public static int ValueObserved;
    public static string? ReferenceObserved;
    public static int InnerObserved;
    public static BindingStruct StructObserved;

    [Prefix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.IntResult))]
    public static void Prefix_Result_Primitive_ReadByValue(int __result) => ValueObserved = __result;

    [Postfix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.IntResult))]
    public static void Postfix_Result_Primitive_ReadByValue(int __result) => ValueObserved = __result;

    [Prefix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.StringResult))]
    public static void Prefix_Result_ReferenceType_ReadByValue(string? __result) => ReferenceObserved = __result;

    [Prefix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.StructResult))]
    public static void Prefix_Result_Struct_ReadByValue(BindingStruct __result) => StructObserved = __result;

    [Postfix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.StringResult))]
    public static void Postfix_Result_ReferenceType_ReadByValue(string __result) => ReferenceObserved = __result;

    [Postfix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.StructResult))]
    public static void Postfix_Result_Struct_ReadByValue(BindingStruct __result) => StructObserved = __result;

    [Prefix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.ThrowingIntResult))]
    public static bool Prefix_Result_Primitive_WriteByReference(ref int __result)
    {
        __result = 42;
        return false;
    }

    [Postfix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.IntResult))]
    public static void Postfix_Result_Primitive_WriteByReference(ref int __result) => __result = 42;

    [Prefix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.ThrowingStringResult))]
    public static bool Prefix_Result_ReferenceType_WriteByReference(ref string? __result)
    {
        __result = "patched";
        return false;
    }

    [Prefix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.ThrowingStructResult))]
    public static bool Prefix_Result_Struct_WriteByReference(ref BindingStruct __result)
    {
        __result = new BindingStruct { Value = 42 };
        return false;
    }

    [Postfix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.StringResult))]
    public static void Postfix_Result_ReferenceType_WriteByReference(ref string __result) => __result = "patched";

    [Postfix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.StructResult))]
    public static void Postfix_Result_Struct_WriteByReference(ref BindingStruct __result) =>
        __result = new BindingStruct { Value = 42 };

    [InnerPrefix(typeof(InnerStaticMethodTargets), nameof(InnerStaticMethodTargets.IntResult))]
    [Target(typeof(OuterStaticMethodTargets), nameof(OuterStaticMethodTargets.IntResult))]
    public static void InnerPrefix_Result_Primitive_ReadByValue(int __result) => InnerObserved = __result;

    [InnerPrefix(typeof(InnerStaticMethodTargets), nameof(InnerStaticMethodTargets.StringResult))]
    [Target(typeof(OuterStaticMethodTargets), nameof(OuterStaticMethodTargets.StringResult))]
    public static void InnerPrefix_Result_ReferenceType_ReadByValue(string? __result) => ReferenceObserved = __result;

    [InnerPrefix(typeof(InnerStaticMethodTargets), nameof(InnerStaticMethodTargets.StructResult))]
    [Target(typeof(OuterStaticMethodTargets), nameof(OuterStaticMethodTargets.StructResult))]
    public static void InnerPrefix_Result_Struct_ReadByValue(BindingStruct __result) => StructObserved = __result;

    [InnerPostfix(typeof(InnerStaticMethodTargets), nameof(InnerStaticMethodTargets.IntResult))]
    [Target(typeof(OuterStaticMethodTargets), nameof(OuterStaticMethodTargets.IntResult))]
    public static void InnerPostfix_Result_Primitive_ReadByValue(int __result) => InnerObserved = __result;

    [InnerPostfix(typeof(InnerStaticMethodTargets), nameof(InnerStaticMethodTargets.StringResult))]
    [Target(typeof(OuterStaticMethodTargets), nameof(OuterStaticMethodTargets.StringResult))]
    public static void InnerPostfix_Result_ReferenceType_ReadByValue(string __result) => ReferenceObserved = __result;

    [InnerPostfix(typeof(InnerStaticMethodTargets), nameof(InnerStaticMethodTargets.StructResult))]
    [Target(typeof(OuterStaticMethodTargets), nameof(OuterStaticMethodTargets.StructResult))]
    public static void InnerPostfix_Result_Struct_ReadByValue(BindingStruct __result) => StructObserved = __result;

    [InnerPrefix(typeof(InnerStaticMethodTargets), nameof(InnerStaticMethodTargets.IntResult))]
    [Target(typeof(OuterStaticMethodTargets), nameof(OuterStaticMethodTargets.IntResult))]
    public static bool InnerPrefix_Result_Primitive_WriteByReference(ref int __result)
    {
        __result = 42;
        return false;
    }

    [InnerPrefix(typeof(InnerStaticMethodTargets), nameof(InnerStaticMethodTargets.StringResult))]
    [Target(typeof(OuterStaticMethodTargets), nameof(OuterStaticMethodTargets.StringResult))]
    public static bool InnerPrefix_Result_ReferenceType_WriteByReference(ref string? __result)
    {
        __result = "patched";
        return false;
    }

    [InnerPrefix(typeof(InnerStaticMethodTargets), nameof(InnerStaticMethodTargets.StructResult))]
    [Target(typeof(OuterStaticMethodTargets), nameof(OuterStaticMethodTargets.StructResult))]
    public static bool InnerPrefix_Result_Struct_WriteByReference(ref BindingStruct __result)
    {
        __result = new BindingStruct { Value = 42 };
        return false;
    }

    [InnerPostfix(typeof(InnerStaticMethodTargets), nameof(InnerStaticMethodTargets.IntResult))]
    [Target(typeof(OuterStaticMethodTargets), nameof(OuterStaticMethodTargets.IntResult))]
    public static void InnerPostfix_Result_Primitive_WriteByReference(ref int __result) => __result = 42;

    [InnerPostfix(typeof(InnerStaticMethodTargets), nameof(InnerStaticMethodTargets.StringResult))]
    [Target(typeof(OuterStaticMethodTargets), nameof(OuterStaticMethodTargets.StringResult))]
    public static void InnerPostfix_Result_ReferenceType_WriteByReference(ref string __result) => __result = "patched";

    [InnerPostfix(typeof(InnerStaticMethodTargets), nameof(InnerStaticMethodTargets.StructResult))]
    [Target(typeof(OuterStaticMethodTargets), nameof(OuterStaticMethodTargets.StructResult))]
    public static void InnerPostfix_Result_Struct_WriteByReference(ref BindingStruct __result) =>
        __result = new BindingStruct { Value = 42 };

    [Postfix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.IntResult))]
    public static void Postfix_ReturnValueAttribute_Primitive_WriteByReference([ReturnValue] ref int value) => value = 42;

    [Prefix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.IntResult))]
    public static void Prefix_ReturnValueAttribute_Primitive_ReadByValue([ReturnValue] int value) => ValueObserved = value;

    [Prefix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.StringResult))]
    public static void Prefix_ReturnValueAttribute_ReferenceType_ReadByValue([ReturnValue] string? value) =>
        ReferenceObserved = value;

    [Prefix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.ThrowingStringResult))]
    public static bool Prefix_ReturnValueAttribute_ReferenceType_WriteByReference([ReturnValue] ref string? value)
    {
        value = "patched";
        return false;
    }

    [Prefix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.StructResult))]
    public static void Prefix_ReturnValueAttribute_Struct_ReadByValue([ReturnValue] BindingStruct value) => StructObserved = value;

    [Prefix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.ThrowingStructResult))]
    public static bool Prefix_ReturnValueAttribute_Struct_WriteByReference([ReturnValue] ref BindingStruct value)
    {
        value = new BindingStruct { Value = 42 };
        return false;
    }

    [Postfix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.IntResult))]
    public static void Postfix_ReturnValueAttribute_Primitive_ReadByValue([ReturnValue] int value) => ValueObserved = value;

    [Postfix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.StringResult))]
    public static void Postfix_ReturnValueAttribute_ReferenceType_ReadByValue([ReturnValue] string value) =>
        ReferenceObserved = value;

    [Postfix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.StringResult))]
    public static void Postfix_ReturnValueAttribute_ReferenceType_WriteByReference([ReturnValue] ref string value) =>
        value = "patched";

    [Postfix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.StructResult))]
    public static void Postfix_ReturnValueAttribute_Struct_ReadByValue([ReturnValue] BindingStruct value) => StructObserved = value;

    [Postfix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.StructResult))]
    public static void Postfix_ReturnValueAttribute_Struct_WriteByReference([ReturnValue] ref BindingStruct value) =>
        value = new BindingStruct { Value = 42 };
}

public static partial class ResultBindingPatches
{
    [Prefix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.IntResult))]
    public static void Prefix_Result_Primitive_ReadByReference(ref int __result) => ValueObserved = __result;

    [Prefix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.StringResult))]
    public static void Prefix_Result_ReferenceType_ReadByReference(ref string? __result) => ReferenceObserved = __result;

    [Prefix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.StructResult))]
    public static void Prefix_Result_Struct_ReadByReference(ref BindingStruct __result) => StructObserved = __result;

    [Postfix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.IntResult))]
    public static void Postfix_Result_Primitive_ReadByReference(ref int __result) => ValueObserved = __result;

    [Postfix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.StringResult))]
    public static void Postfix_Result_ReferenceType_ReadByReference(ref string __result) => ReferenceObserved = __result;

    [Postfix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.StructResult))]
    public static void Postfix_Result_Struct_ReadByReference(ref BindingStruct __result) => StructObserved = __result;

    [InnerPrefix(typeof(InnerStaticMethodTargets), nameof(InnerStaticMethodTargets.IntResult))]
    [Target(typeof(OuterStaticMethodTargets), nameof(OuterStaticMethodTargets.IntResult))]
    public static void InnerPrefix_Result_Primitive_ReadByReference(ref int __result) => InnerObserved = __result;

    [InnerPrefix(typeof(InnerStaticMethodTargets), nameof(InnerStaticMethodTargets.StringResult))]
    [Target(typeof(OuterStaticMethodTargets), nameof(OuterStaticMethodTargets.StringResult))]
    public static void InnerPrefix_Result_ReferenceType_ReadByReference(ref string? __result) =>
        ReferenceObserved = __result;

    [InnerPrefix(typeof(InnerStaticMethodTargets), nameof(InnerStaticMethodTargets.StructResult))]
    [Target(typeof(OuterStaticMethodTargets), nameof(OuterStaticMethodTargets.StructResult))]
    public static void InnerPrefix_Result_Struct_ReadByReference(ref BindingStruct __result) => StructObserved = __result;

    [InnerPostfix(typeof(InnerStaticMethodTargets), nameof(InnerStaticMethodTargets.IntResult))]
    [Target(typeof(OuterStaticMethodTargets), nameof(OuterStaticMethodTargets.IntResult))]
    public static void InnerPostfix_Result_Primitive_ReadByReference(ref int __result) => InnerObserved = __result;

    [InnerPostfix(typeof(InnerStaticMethodTargets), nameof(InnerStaticMethodTargets.StringResult))]
    [Target(typeof(OuterStaticMethodTargets), nameof(OuterStaticMethodTargets.StringResult))]
    public static void InnerPostfix_Result_ReferenceType_ReadByReference(ref string __result) =>
        ReferenceObserved = __result;

    [InnerPostfix(typeof(InnerStaticMethodTargets), nameof(InnerStaticMethodTargets.StructResult))]
    [Target(typeof(OuterStaticMethodTargets), nameof(OuterStaticMethodTargets.StructResult))]
    public static void InnerPostfix_Result_Struct_ReadByReference(ref BindingStruct __result) => StructObserved = __result;

    [Postfix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.IntResult))]
    public static void Postfix_ReturnValueAttribute_Primitive_ReadByReference([ReturnValue] ref int value) =>
        ValueObserved = value;

    [Prefix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.StringResult))]
    public static void Prefix_ReturnValueAttribute_ReferenceType_ReadByReference(
        [ReturnValue] ref string? value) => ReferenceObserved = value;

    [Prefix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.StructResult))]
    public static void Prefix_ReturnValueAttribute_Struct_ReadByReference(
        [ReturnValue] ref BindingStruct value) => StructObserved = value;

    [Postfix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.StringResult))]
    public static void Postfix_ReturnValueAttribute_ReferenceType_ReadByReference(
        [ReturnValue] ref string value) => ReferenceObserved = value;

    [Postfix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.StructResult))]
    public static void Postfix_ReturnValueAttribute_Struct_ReadByReference(
        [ReturnValue] ref BindingStruct value) => StructObserved = value;
}

[TestFixture]
public sealed partial class ResultBindingTests
{
    [Test]
    public void Prefix_Result_Primitive_ReadByReference()
    {
        ResultBindingPatches.ValueObserved = -1;
        ApplyPatch(typeof(ResultBindingPatches), nameof(ResultBindingPatches.Prefix_Result_Primitive_ReadByReference));
        StaticMethodTargets.IntResult();
        Assert.That(ResultBindingPatches.ValueObserved, Is.Zero);
    }

    [Test]
    public void Prefix_Result_ReferenceType_ReadByReference()
    {
        ResultBindingPatches.ReferenceObserved = "sentinel";
        ApplyPatch(typeof(ResultBindingPatches), nameof(ResultBindingPatches.Prefix_Result_ReferenceType_ReadByReference));
        StaticMethodTargets.StringResult();
        Assert.That(ResultBindingPatches.ReferenceObserved, Is.Null);
    }

    [Test]
    public void Prefix_Result_Struct_ReadByReference()
    {
        ResultBindingPatches.StructObserved = new BindingStruct { Value = -1 };
        ApplyPatch(typeof(ResultBindingPatches), nameof(ResultBindingPatches.Prefix_Result_Struct_ReadByReference));
        StaticMethodTargets.StructResult();
        Assert.That(ResultBindingPatches.StructObserved.Value, Is.Zero);
    }

    [Test]
    public void Postfix_Result_Primitive_ReadByReference()
    {
        ResultBindingPatches.ValueObserved = 0;
        ApplyPatch(typeof(ResultBindingPatches), nameof(ResultBindingPatches.Postfix_Result_Primitive_ReadByReference));
        StaticMethodTargets.IntResult();
        Assert.That(ResultBindingPatches.ValueObserved, Is.EqualTo(1));
    }

    [Test]
    public void Postfix_Result_ReferenceType_ReadByReference()
    {
        ResultBindingPatches.ReferenceObserved = null;
        ApplyPatch(typeof(ResultBindingPatches), nameof(ResultBindingPatches.Postfix_Result_ReferenceType_ReadByReference));
        StaticMethodTargets.StringResult();
        Assert.That(ResultBindingPatches.ReferenceObserved, Is.EqualTo("original"));
    }

    [Test]
    public void Postfix_Result_Struct_ReadByReference()
    {
        ResultBindingPatches.StructObserved = default;
        ApplyPatch(typeof(ResultBindingPatches), nameof(ResultBindingPatches.Postfix_Result_Struct_ReadByReference));
        StaticMethodTargets.StructResult();
        Assert.That(ResultBindingPatches.StructObserved.Value, Is.EqualTo(1));
    }

    [Test]
    public void InnerPrefix_Result_Primitive_ReadByReference()
    {
        ResultBindingPatches.InnerObserved = -1;
        ApplyPatch(typeof(ResultBindingPatches), nameof(ResultBindingPatches.InnerPrefix_Result_Primitive_ReadByReference));
        OuterStaticMethodTargets.IntResult();
        Assert.That(ResultBindingPatches.InnerObserved, Is.Zero);
    }

    [Test]
    public void InnerPrefix_Result_ReferenceType_ReadByReference()
    {
        ResultBindingPatches.ReferenceObserved = "sentinel";
        ApplyPatch(typeof(ResultBindingPatches), nameof(ResultBindingPatches.InnerPrefix_Result_ReferenceType_ReadByReference));
        OuterStaticMethodTargets.StringResult();
        Assert.That(ResultBindingPatches.ReferenceObserved, Is.Null);
    }

    [Test]
    public void InnerPrefix_Result_Struct_ReadByReference()
    {
        ResultBindingPatches.StructObserved = new BindingStruct { Value = -1 };
        ApplyPatch(typeof(ResultBindingPatches), nameof(ResultBindingPatches.InnerPrefix_Result_Struct_ReadByReference));
        OuterStaticMethodTargets.StructResult();
        Assert.That(ResultBindingPatches.StructObserved.Value, Is.Zero);
    }

    [Test]
    public void InnerPostfix_Result_Primitive_ReadByReference()
    {
        ResultBindingPatches.InnerObserved = 0;
        ApplyPatch(typeof(ResultBindingPatches), nameof(ResultBindingPatches.InnerPostfix_Result_Primitive_ReadByReference));
        OuterStaticMethodTargets.IntResult();
        Assert.That(ResultBindingPatches.InnerObserved, Is.EqualTo(1));
    }

    [Test]
    public void InnerPostfix_Result_ReferenceType_ReadByReference()
    {
        ResultBindingPatches.ReferenceObserved = null;
        ApplyPatch(typeof(ResultBindingPatches), nameof(ResultBindingPatches.InnerPostfix_Result_ReferenceType_ReadByReference));
        OuterStaticMethodTargets.StringResult();
        Assert.That(ResultBindingPatches.ReferenceObserved, Is.EqualTo("original"));
    }

    [Test]
    public void InnerPostfix_Result_Struct_ReadByReference()
    {
        ResultBindingPatches.StructObserved = default;
        ApplyPatch(typeof(ResultBindingPatches), nameof(ResultBindingPatches.InnerPostfix_Result_Struct_ReadByReference));
        OuterStaticMethodTargets.StructResult();
        Assert.That(ResultBindingPatches.StructObserved.Value, Is.EqualTo(1));
    }

    [Test]
    public void Postfix_ReturnValueAttribute_Primitive_ReadByReference()
    {
        ResultBindingPatches.ValueObserved = 0;
        ApplyPatch(typeof(ResultBindingPatches), nameof(ResultBindingPatches.Postfix_ReturnValueAttribute_Primitive_ReadByReference));
        StaticMethodTargets.IntResult();
        Assert.That(ResultBindingPatches.ValueObserved, Is.EqualTo(1));
    }

    [Test]
    public void Prefix_ReturnValueAttribute_ReferenceType_ReadByReference()
    {
        ResultBindingPatches.ReferenceObserved = "sentinel";
        ApplyPatch(typeof(ResultBindingPatches), nameof(ResultBindingPatches.Prefix_ReturnValueAttribute_ReferenceType_ReadByReference));
        StaticMethodTargets.StringResult();
        Assert.That(ResultBindingPatches.ReferenceObserved, Is.Null);
    }

    [Test]
    public void Prefix_ReturnValueAttribute_Struct_ReadByReference()
    {
        ResultBindingPatches.StructObserved = new BindingStruct { Value = -1 };
        ApplyPatch(typeof(ResultBindingPatches), nameof(ResultBindingPatches.Prefix_ReturnValueAttribute_Struct_ReadByReference));
        StaticMethodTargets.StructResult();
        Assert.That(ResultBindingPatches.StructObserved.Value, Is.Zero);
    }

    [Test]
    public void Postfix_ReturnValueAttribute_ReferenceType_ReadByReference()
    {
        ResultBindingPatches.ReferenceObserved = null;
        ApplyPatch(typeof(ResultBindingPatches), nameof(ResultBindingPatches.Postfix_ReturnValueAttribute_ReferenceType_ReadByReference));
        StaticMethodTargets.StringResult();
        Assert.That(ResultBindingPatches.ReferenceObserved, Is.EqualTo("original"));
    }

    [Test]
    public void Postfix_ReturnValueAttribute_Struct_ReadByReference()
    {
        ResultBindingPatches.StructObserved = default;
        ApplyPatch(typeof(ResultBindingPatches), nameof(ResultBindingPatches.Postfix_ReturnValueAttribute_Struct_ReadByReference));
        StaticMethodTargets.StructResult();
        Assert.That(ResultBindingPatches.StructObserved.Value, Is.EqualTo(1));
    }
}

[TestFixture]
public sealed partial class ResultBindingTests : PatchTestBase
{
    [Test]
    public void Prefix_Result_Primitive_ReadByValue()
    {
        ResultBindingPatches.ValueObserved = -1;
        ApplyPatch(typeof(ResultBindingPatches), nameof(ResultBindingPatches.Prefix_Result_Primitive_ReadByValue));
        StaticMethodTargets.IntResult();
        Assert.That(ResultBindingPatches.ValueObserved, Is.Zero);
    }

    [Test]
    public void Postfix_Result_Primitive_ReadByValue()
    {
        ResultBindingPatches.ValueObserved = 0;
        ApplyPatch(typeof(ResultBindingPatches), nameof(ResultBindingPatches.Postfix_Result_Primitive_ReadByValue));
        StaticMethodTargets.IntResult();
        Assert.That(ResultBindingPatches.ValueObserved, Is.EqualTo(1));
    }

    [Test]
    public void Prefix_Result_ReferenceType_ReadByValue()
    {
        ResultBindingPatches.ReferenceObserved = "sentinel";
        ApplyPatch(typeof(ResultBindingPatches), nameof(ResultBindingPatches.Prefix_Result_ReferenceType_ReadByValue));
        StaticMethodTargets.StringResult();
        Assert.That(ResultBindingPatches.ReferenceObserved, Is.Null);
    }

    [Test]
    public void Prefix_Result_Struct_ReadByValue()
    {
        ResultBindingPatches.StructObserved = new BindingStruct { Value = -1 };
        ApplyPatch(typeof(ResultBindingPatches), nameof(ResultBindingPatches.Prefix_Result_Struct_ReadByValue));

        StaticMethodTargets.StructResult();

        Assert.That(ResultBindingPatches.StructObserved.Value, Is.Zero);
    }

    [Test]
    public void Postfix_Result_ReferenceType_ReadByValue()
    {
        ResultBindingPatches.ReferenceObserved = null;
        ApplyPatch(typeof(ResultBindingPatches), nameof(ResultBindingPatches.Postfix_Result_ReferenceType_ReadByValue));
        StaticMethodTargets.StringResult();
        Assert.That(ResultBindingPatches.ReferenceObserved, Is.EqualTo("original"));
    }

    [Test]
    public void Postfix_Result_Struct_ReadByValue()
    {
        ResultBindingPatches.StructObserved = default;
        ApplyPatch(typeof(ResultBindingPatches), nameof(ResultBindingPatches.Postfix_Result_Struct_ReadByValue));

        StaticMethodTargets.StructResult();

        Assert.That(ResultBindingPatches.StructObserved.Value, Is.EqualTo(1));
    }

    [Test]
    public void Prefix_Result_Primitive_WriteByReference()
    {
        ApplyPatch(typeof(ResultBindingPatches), nameof(ResultBindingPatches.Prefix_Result_Primitive_WriteByReference));
        Assert.That(StaticMethodTargets.ThrowingIntResult(), Is.EqualTo(42));
    }

    [Test]
    public void Postfix_Result_Primitive_WriteByReference()
    {
        ApplyPatch(typeof(ResultBindingPatches), nameof(ResultBindingPatches.Postfix_Result_Primitive_WriteByReference));
        Assert.That(StaticMethodTargets.IntResult(), Is.EqualTo(42));
    }

    [Test]
    public void Prefix_Result_ReferenceType_WriteByReference()
    {
        ApplyPatch(typeof(ResultBindingPatches), nameof(ResultBindingPatches.Prefix_Result_ReferenceType_WriteByReference));
        Assert.That(StaticMethodTargets.ThrowingStringResult(), Is.EqualTo("patched"));
    }

    [Test]
    public void Prefix_Result_Struct_WriteByReference()
    {
        ApplyPatch(typeof(ResultBindingPatches), nameof(ResultBindingPatches.Prefix_Result_Struct_WriteByReference));

        BindingStruct result = StaticMethodTargets.ThrowingStructResult();

        Assert.That(result.Value, Is.EqualTo(42));
    }

    [Test]
    public void Postfix_Result_ReferenceType_WriteByReference()
    {
        ApplyPatch(typeof(ResultBindingPatches), nameof(ResultBindingPatches.Postfix_Result_ReferenceType_WriteByReference));
        Assert.That(StaticMethodTargets.StringResult(), Is.EqualTo("patched"));
    }

    [Test]
    public void Postfix_Result_Struct_WriteByReference()
    {
        ApplyPatch(typeof(ResultBindingPatches), nameof(ResultBindingPatches.Postfix_Result_Struct_WriteByReference));

        BindingStruct result = StaticMethodTargets.StructResult();

        Assert.That(result.Value, Is.EqualTo(42));
    }
}

[TestFixture]
public sealed partial class ResultBindingTests
{
    [Test]
    public void InnerPrefix_Result_Primitive_ReadByValue()
    {
        ResultBindingPatches.InnerObserved = -1;
        ApplyPatch(typeof(ResultBindingPatches), nameof(ResultBindingPatches.InnerPrefix_Result_Primitive_ReadByValue));
        OuterStaticMethodTargets.IntResult();
        Assert.That(ResultBindingPatches.InnerObserved, Is.Zero);
    }

    [Test]
    public void InnerPrefix_Result_ReferenceType_ReadByValue()
    {
        ResultBindingPatches.ReferenceObserved = "sentinel";
        ApplyPatch(typeof(ResultBindingPatches), nameof(ResultBindingPatches.InnerPrefix_Result_ReferenceType_ReadByValue));

        OuterStaticMethodTargets.StringResult();

        Assert.That(ResultBindingPatches.ReferenceObserved, Is.Null);
    }

    [Test]
    public void InnerPrefix_Result_Struct_ReadByValue()
    {
        ResultBindingPatches.StructObserved = new BindingStruct { Value = -1 };
        ApplyPatch(typeof(ResultBindingPatches), nameof(ResultBindingPatches.InnerPrefix_Result_Struct_ReadByValue));

        OuterStaticMethodTargets.StructResult();

        Assert.That(ResultBindingPatches.StructObserved.Value, Is.Zero);
    }

    [Test]
    public void InnerPostfix_Result_Primitive_ReadByValue()
    {
        ResultBindingPatches.InnerObserved = 0;
        ApplyPatch(typeof(ResultBindingPatches), nameof(ResultBindingPatches.InnerPostfix_Result_Primitive_ReadByValue));
        OuterStaticMethodTargets.IntResult();
        Assert.That(ResultBindingPatches.InnerObserved, Is.EqualTo(1));
    }

    [Test]
    public void InnerPostfix_Result_ReferenceType_ReadByValue()
    {
        ResultBindingPatches.ReferenceObserved = null;
        ApplyPatch(typeof(ResultBindingPatches), nameof(ResultBindingPatches.InnerPostfix_Result_ReferenceType_ReadByValue));

        OuterStaticMethodTargets.StringResult();

        Assert.That(ResultBindingPatches.ReferenceObserved, Is.EqualTo("original"));
    }

    [Test]
    public void InnerPostfix_Result_Struct_ReadByValue()
    {
        ResultBindingPatches.StructObserved = default;
        ApplyPatch(typeof(ResultBindingPatches), nameof(ResultBindingPatches.InnerPostfix_Result_Struct_ReadByValue));

        OuterStaticMethodTargets.StructResult();

        Assert.That(ResultBindingPatches.StructObserved.Value, Is.EqualTo(1));
    }

    [Test]
    public void InnerPrefix_Result_Primitive_WriteByReference()
    {
        ApplyPatch(typeof(ResultBindingPatches), nameof(ResultBindingPatches.InnerPrefix_Result_Primitive_WriteByReference));
        Assert.That(OuterStaticMethodTargets.IntResult(), Is.EqualTo(42));
    }

    [Test]
    public void InnerPrefix_Result_ReferenceType_WriteByReference()
    {
        ApplyPatch(typeof(ResultBindingPatches), nameof(ResultBindingPatches.InnerPrefix_Result_ReferenceType_WriteByReference));

        Assert.That(OuterStaticMethodTargets.StringResult(), Is.EqualTo("patched"));
    }

    [Test]
    public void InnerPrefix_Result_Struct_WriteByReference()
    {
        ApplyPatch(typeof(ResultBindingPatches), nameof(ResultBindingPatches.InnerPrefix_Result_Struct_WriteByReference));

        BindingStruct result = OuterStaticMethodTargets.StructResult();

        Assert.That(result.Value, Is.EqualTo(42));
    }

    [Test]
    public void InnerPostfix_Result_Primitive_WriteByReference()
    {
        ApplyPatch(typeof(ResultBindingPatches), nameof(ResultBindingPatches.InnerPostfix_Result_Primitive_WriteByReference));
        Assert.That(OuterStaticMethodTargets.IntResult(), Is.EqualTo(42));
    }

    [Test]
    public void InnerPostfix_Result_ReferenceType_WriteByReference()
    {
        ApplyPatch(typeof(ResultBindingPatches), nameof(ResultBindingPatches.InnerPostfix_Result_ReferenceType_WriteByReference));

        Assert.That(OuterStaticMethodTargets.StringResult(), Is.EqualTo("patched"));
    }

    [Test]
    public void InnerPostfix_Result_Struct_WriteByReference()
    {
        ApplyPatch(typeof(ResultBindingPatches), nameof(ResultBindingPatches.InnerPostfix_Result_Struct_WriteByReference));

        BindingStruct result = OuterStaticMethodTargets.StructResult();

        Assert.That(result.Value, Is.EqualTo(42));
    }

    [Test]
    public void Postfix_ReturnValueAttribute_Primitive_WriteByReference()
    {
        ApplyPatch(typeof(ResultBindingPatches), nameof(ResultBindingPatches.Postfix_ReturnValueAttribute_Primitive_WriteByReference));

        Assert.That(StaticMethodTargets.IntResult(), Is.EqualTo(42));
    }

    [Test]
    public void Prefix_ReturnValueAttribute_Primitive_ReadByValue()
    {
        ResultBindingPatches.ValueObserved = -1;
        ApplyPatch(typeof(ResultBindingPatches), nameof(ResultBindingPatches.Prefix_ReturnValueAttribute_Primitive_ReadByValue));

        StaticMethodTargets.IntResult();

        Assert.That(ResultBindingPatches.ValueObserved, Is.Zero);
    }

    [Test]
    public void Prefix_ReturnValueAttribute_ReferenceType_ReadByValue()
    {
        ResultBindingPatches.ReferenceObserved = "sentinel";
        ApplyPatch(typeof(ResultBindingPatches), nameof(ResultBindingPatches.Prefix_ReturnValueAttribute_ReferenceType_ReadByValue));

        StaticMethodTargets.StringResult();

        Assert.That(ResultBindingPatches.ReferenceObserved, Is.Null);
    }

    [Test]
    public void Prefix_ReturnValueAttribute_ReferenceType_WriteByReference()
    {
        ApplyPatch(typeof(ResultBindingPatches), nameof(ResultBindingPatches.Prefix_ReturnValueAttribute_ReferenceType_WriteByReference));

        Assert.That(StaticMethodTargets.ThrowingStringResult(), Is.EqualTo("patched"));
    }

    [Test]
    public void Prefix_ReturnValueAttribute_Struct_ReadByValue()
    {
        ResultBindingPatches.StructObserved = new BindingStruct { Value = -1 };
        ApplyPatch(typeof(ResultBindingPatches), nameof(ResultBindingPatches.Prefix_ReturnValueAttribute_Struct_ReadByValue));

        StaticMethodTargets.StructResult();

        Assert.That(ResultBindingPatches.StructObserved.Value, Is.Zero);
    }

    [Test]
    public void Prefix_ReturnValueAttribute_Struct_WriteByReference()
    {
        ApplyPatch(typeof(ResultBindingPatches), nameof(ResultBindingPatches.Prefix_ReturnValueAttribute_Struct_WriteByReference));

        BindingStruct result = StaticMethodTargets.ThrowingStructResult();

        Assert.That(result.Value, Is.EqualTo(42));
    }

    [Test]
    public void Postfix_ReturnValueAttribute_Primitive_ReadByValue()
    {
        ResultBindingPatches.ValueObserved = 0;
        ApplyPatch(typeof(ResultBindingPatches), nameof(ResultBindingPatches.Postfix_ReturnValueAttribute_Primitive_ReadByValue));

        StaticMethodTargets.IntResult();

        Assert.That(ResultBindingPatches.ValueObserved, Is.EqualTo(1));
    }

    [Test]
    public void Postfix_ReturnValueAttribute_ReferenceType_ReadByValue()
    {
        ResultBindingPatches.ReferenceObserved = null;
        ApplyPatch(typeof(ResultBindingPatches), nameof(ResultBindingPatches.Postfix_ReturnValueAttribute_ReferenceType_ReadByValue));

        StaticMethodTargets.StringResult();

        Assert.That(ResultBindingPatches.ReferenceObserved, Is.EqualTo("original"));
    }

    [Test]
    public void Postfix_ReturnValueAttribute_ReferenceType_WriteByReference()
    {
        ApplyPatch(typeof(ResultBindingPatches), nameof(ResultBindingPatches.Postfix_ReturnValueAttribute_ReferenceType_WriteByReference));

        Assert.That(StaticMethodTargets.StringResult(), Is.EqualTo("patched"));
    }

    [Test]
    public void Postfix_ReturnValueAttribute_Struct_ReadByValue()
    {
        ResultBindingPatches.StructObserved = default;
        ApplyPatch(typeof(ResultBindingPatches), nameof(ResultBindingPatches.Postfix_ReturnValueAttribute_Struct_ReadByValue));

        StaticMethodTargets.StructResult();

        Assert.That(ResultBindingPatches.StructObserved.Value, Is.EqualTo(1));
    }

    [Test]
    public void Postfix_ReturnValueAttribute_Struct_WriteByReference()
    {
        ApplyPatch(typeof(ResultBindingPatches), nameof(ResultBindingPatches.Postfix_ReturnValueAttribute_Struct_WriteByReference));

        BindingStruct result = StaticMethodTargets.StructResult();

        Assert.That(result.Value, Is.EqualTo(42));
    }
}
