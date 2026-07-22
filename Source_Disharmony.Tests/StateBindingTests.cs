namespace Disharmony.Tests;

public static partial class StateBindingPatches
{
    public static int Observed;
    public static string? ReferenceObserved;
    public static BindingStruct StructObserved;

    [Prefix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.Void))]
    public static void Postfix_StateParameter_Primitive_ReadByValue_Prefix(out int __state) => __state = 42;

    [Postfix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.Void))]
    public static void Postfix_StateParameter_Primitive_ReadByValue_Postfix(int __state) => Observed = __state;

    [Prefix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.Void))]
    public static void Postfix_StateParameter_Primitive_WriteByReference_Prefix(out int __state) => __state = 42;

    [Postfix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.Void))]
    public static void Postfix_StateParameter_Primitive_WriteByReference_FirstPostfix(ref int __state) => __state = 43;

    [Postfix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.Void))]
    public static void Postfix_StateParameter_Primitive_WriteByReference_SecondPostfix(int __state) => Observed = __state;

    [Prefix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.Void))]
    public static void Postfix_StateParameter_ReferenceType_ReadByValue_Prefix(out string __state) => __state = "original";

    [Postfix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.Void))]
    public static void Postfix_StateParameter_ReferenceType_ReadByValue_Postfix(string __state) => ReferenceObserved = __state;

    [Prefix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.Void))]
    public static void Postfix_StateParameter_ReferenceType_WriteByReference_Prefix(out string __state) =>
        __state = "original";

    [Postfix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.Void))]
    public static void Postfix_StateParameter_ReferenceType_WriteByReference_FirstPostfix(ref string __state) =>
        __state = "patched";

    [Postfix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.Void))]
    public static void Postfix_StateParameter_ReferenceType_WriteByReference_SecondPostfix(string __state) =>
        ReferenceObserved = __state;

    [Prefix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.Void))]
    public static void Postfix_StateParameter_Struct_ReadByValue_Prefix(out BindingStruct __state) =>
        __state = new BindingStruct { Value = 42 };

    [Postfix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.Void))]
    public static void Postfix_StateParameter_Struct_ReadByValue_Postfix(BindingStruct __state) => StructObserved = __state;

    [Prefix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.Void))]
    public static void Postfix_StateParameter_Struct_WriteByReference_Prefix(out BindingStruct __state) =>
        __state = new BindingStruct { Value = 1 };

    [Postfix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.Void))]
    public static void Postfix_StateParameter_Struct_WriteByReference_FirstPostfix(ref BindingStruct __state) =>
        __state = new BindingStruct { Value = 42 };

    [Postfix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.Void))]
    public static void Postfix_StateParameter_Struct_WriteByReference_SecondPostfix(BindingStruct __state) =>
        StructObserved = __state;

    [Prefix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.Void))]
    public static void Postfix_StateAttribute_Primitive_ReadByValue_Prefix([State] out int state) => state = 42;

    [Postfix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.Void))]
    public static void Postfix_StateAttribute_Primitive_ReadByValue_Postfix([State] int state) => Observed = state;

    [Prefix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.Void))]
    public static void Postfix_StateAttribute_Primitive_WriteByReference_Prefix([State] out int state) => state = 1;

    [Postfix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.Void))]
    public static void Postfix_StateAttribute_Primitive_WriteByReference_FirstPostfix([State] ref int state) => state = 42;

    [Postfix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.Void))]
    public static void Postfix_StateAttribute_Primitive_WriteByReference_SecondPostfix([State] int state) => Observed = state;

    [Prefix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.Void))]
    public static void Postfix_StateAttribute_ReferenceType_ReadByValue_Prefix([State] out string state) => state = "original";

    [Postfix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.Void))]
    public static void Postfix_StateAttribute_ReferenceType_ReadByValue_Postfix([State] string state) => ReferenceObserved = state;

    [Prefix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.Void))]
    public static void Postfix_StateAttribute_ReferenceType_WriteByReference_Prefix([State] out string state) =>
        state = "original";

    [Postfix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.Void))]
    public static void Postfix_StateAttribute_ReferenceType_WriteByReference_FirstPostfix([State] ref string state) =>
        state = "patched";

    [Postfix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.Void))]
    public static void Postfix_StateAttribute_ReferenceType_WriteByReference_SecondPostfix([State] string state) =>
        ReferenceObserved = state;

    [Prefix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.Void))]
    public static void Postfix_StateAttribute_Struct_ReadByValue_Prefix([State] out BindingStruct state) =>
        state = new BindingStruct { Value = 42 };

    [Postfix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.Void))]
    public static void Postfix_StateAttribute_Struct_ReadByValue_Postfix([State] BindingStruct state) => StructObserved = state;

    [Prefix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.Void))]
    public static void Postfix_StateAttribute_Struct_WriteByReference_Prefix([State] out BindingStruct state) =>
        state = new BindingStruct { Value = 1 };

    [Postfix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.Void))]
    public static void Postfix_StateAttribute_Struct_WriteByReference_FirstPostfix([State] ref BindingStruct state) =>
        state = new BindingStruct { Value = 42 };

    [Postfix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.Void))]
    public static void Postfix_StateAttribute_Struct_WriteByReference_SecondPostfix([State] BindingStruct state) =>
        StructObserved = state;
}

public static partial class StateBindingPatches
{
    [Prefix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.Void))]
    public static void Postfix_StateParameter_Primitive_ReadByReference_Prefix(out int __state) => __state = 42;

    [Postfix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.Void))]
    public static void Postfix_StateParameter_Primitive_ReadByReference_Postfix(ref int __state) => Observed = __state;

    [Prefix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.Void))]
    public static void Postfix_StateParameter_ReferenceType_ReadByReference_Prefix(out string __state) => __state = "original";

    [Postfix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.Void))]
    public static void Postfix_StateParameter_ReferenceType_ReadByReference_Postfix(ref string __state) =>
        ReferenceObserved = __state;

    [Prefix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.Void))]
    public static void Postfix_StateParameter_Struct_ReadByReference_Prefix(out BindingStruct __state) =>
        __state = new BindingStruct { Value = 42 };

    [Postfix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.Void))]
    public static void Postfix_StateParameter_Struct_ReadByReference_Postfix(ref BindingStruct __state) => StructObserved = __state;

    [Prefix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.Void))]
    public static void Postfix_StateAttribute_Primitive_ReadByReference_Prefix([State] out int state) => state = 42;

    [Postfix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.Void))]
    public static void Postfix_StateAttribute_Primitive_ReadByReference_Postfix([State] ref int state) => Observed = state;

    [Prefix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.Void))]
    public static void Postfix_StateAttribute_ReferenceType_ReadByReference_Prefix([State] out string state) =>
        state = "original";

    [Postfix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.Void))]
    public static void Postfix_StateAttribute_ReferenceType_ReadByReference_Postfix([State] ref string state) =>
        ReferenceObserved = state;

    [Prefix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.Void))]
    public static void Postfix_StateAttribute_Struct_ReadByReference_Prefix([State] out BindingStruct state) =>
        state = new BindingStruct { Value = 42 };

    [Postfix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.Void))]
    public static void Postfix_StateAttribute_Struct_ReadByReference_Postfix([State] ref BindingStruct state) =>
        StructObserved = state;
}

[TestFixture]
public sealed partial class StateBindingTests
{
    [Test]
    public void Postfix_StateParameter_Primitive_ReadByReference()
    {
        StateBindingPatches.Observed = 0;
        ApplyPatch(typeof(StateBindingPatches), nameof(StateBindingPatches.Postfix_StateParameter_Primitive_ReadByReference_Prefix));
        ApplyPatch(typeof(StateBindingPatches), nameof(StateBindingPatches.Postfix_StateParameter_Primitive_ReadByReference_Postfix));
        StaticMethodTargets.Void();
        Assert.That(StateBindingPatches.Observed, Is.EqualTo(42));
    }

    [Test]
    public void Postfix_StateParameter_ReferenceType_ReadByReference()
    {
        StateBindingPatches.ReferenceObserved = null;
        ApplyPatch(typeof(StateBindingPatches), nameof(StateBindingPatches.Postfix_StateParameter_ReferenceType_ReadByReference_Prefix));
        ApplyPatch(typeof(StateBindingPatches), nameof(StateBindingPatches.Postfix_StateParameter_ReferenceType_ReadByReference_Postfix));
        StaticMethodTargets.Void();
        Assert.That(StateBindingPatches.ReferenceObserved, Is.EqualTo("original"));
    }

    [Test]
    public void Postfix_StateParameter_Struct_ReadByReference()
    {
        StateBindingPatches.StructObserved = default;
        ApplyPatch(typeof(StateBindingPatches), nameof(StateBindingPatches.Postfix_StateParameter_Struct_ReadByReference_Prefix));
        ApplyPatch(typeof(StateBindingPatches), nameof(StateBindingPatches.Postfix_StateParameter_Struct_ReadByReference_Postfix));
        StaticMethodTargets.Void();
        Assert.That(StateBindingPatches.StructObserved.Value, Is.EqualTo(42));
    }

    [Test]
    public void Postfix_StateAttribute_Primitive_ReadByReference()
    {
        StateBindingPatches.Observed = 0;
        ApplyPatch(typeof(StateBindingPatches), nameof(StateBindingPatches.Postfix_StateAttribute_Primitive_ReadByReference_Prefix));
        ApplyPatch(typeof(StateBindingPatches), nameof(StateBindingPatches.Postfix_StateAttribute_Primitive_ReadByReference_Postfix));
        StaticMethodTargets.Void();
        Assert.That(StateBindingPatches.Observed, Is.EqualTo(42));
    }

    [Test]
    public void Postfix_StateAttribute_ReferenceType_ReadByReference()
    {
        StateBindingPatches.ReferenceObserved = null;
        ApplyPatch(typeof(StateBindingPatches), nameof(StateBindingPatches.Postfix_StateAttribute_ReferenceType_ReadByReference_Prefix));
        ApplyPatch(typeof(StateBindingPatches), nameof(StateBindingPatches.Postfix_StateAttribute_ReferenceType_ReadByReference_Postfix));
        StaticMethodTargets.Void();
        Assert.That(StateBindingPatches.ReferenceObserved, Is.EqualTo("original"));
    }

    [Test]
    public void Postfix_StateAttribute_Struct_ReadByReference()
    {
        StateBindingPatches.StructObserved = default;
        ApplyPatch(typeof(StateBindingPatches), nameof(StateBindingPatches.Postfix_StateAttribute_Struct_ReadByReference_Prefix));
        ApplyPatch(typeof(StateBindingPatches), nameof(StateBindingPatches.Postfix_StateAttribute_Struct_ReadByReference_Postfix));
        StaticMethodTargets.Void();
        Assert.That(StateBindingPatches.StructObserved.Value, Is.EqualTo(42));
    }
}

[TestFixture]
public sealed partial class StateBindingTests : PatchTestBase
{
    [Test]
    public void Postfix_StateParameter_Primitive_ReadByValue()
    {
        StateBindingPatches.Observed = 0;
        ApplyPatch(typeof(StateBindingPatches), nameof(StateBindingPatches.Postfix_StateParameter_Primitive_ReadByValue_Prefix));
        ApplyPatch(typeof(StateBindingPatches), nameof(StateBindingPatches.Postfix_StateParameter_Primitive_ReadByValue_Postfix));

        StaticMethodTargets.Void();

        Assert.That(StateBindingPatches.Observed, Is.EqualTo(42));
    }

    [Test]
    public void Postfix_StateParameter_Primitive_WriteByReference()
    {
        StateBindingPatches.Observed = 0;
        ApplyPatch(typeof(StateBindingPatches), nameof(StateBindingPatches.Postfix_StateParameter_Primitive_WriteByReference_Prefix));
        ApplyPatch(typeof(StateBindingPatches), nameof(StateBindingPatches.Postfix_StateParameter_Primitive_WriteByReference_FirstPostfix));
        ApplyPatch(typeof(StateBindingPatches), nameof(StateBindingPatches.Postfix_StateParameter_Primitive_WriteByReference_SecondPostfix));

        StaticMethodTargets.Void();

        Assert.That(StateBindingPatches.Observed, Is.EqualTo(43));
    }

    [Test]
    public void Postfix_StateParameter_ReferenceType_ReadByValue()
    {
        StateBindingPatches.ReferenceObserved = null;
        ApplyPatch(typeof(StateBindingPatches), nameof(StateBindingPatches.Postfix_StateParameter_ReferenceType_ReadByValue_Prefix));
        ApplyPatch(typeof(StateBindingPatches), nameof(StateBindingPatches.Postfix_StateParameter_ReferenceType_ReadByValue_Postfix));

        StaticMethodTargets.Void();

        Assert.That(StateBindingPatches.ReferenceObserved, Is.EqualTo("original"));
    }

    [Test]
    public void Postfix_StateParameter_ReferenceType_WriteByReference()
    {
        StateBindingPatches.ReferenceObserved = null;
        ApplyPatch(typeof(StateBindingPatches), nameof(StateBindingPatches.Postfix_StateParameter_ReferenceType_WriteByReference_Prefix));
        ApplyPatch(typeof(StateBindingPatches), nameof(StateBindingPatches.Postfix_StateParameter_ReferenceType_WriteByReference_FirstPostfix));
        ApplyPatch(typeof(StateBindingPatches), nameof(StateBindingPatches.Postfix_StateParameter_ReferenceType_WriteByReference_SecondPostfix));

        StaticMethodTargets.Void();

        Assert.That(StateBindingPatches.ReferenceObserved, Is.EqualTo("patched"));
    }

    [Test]
    public void Postfix_StateParameter_Struct_ReadByValue()
    {
        StateBindingPatches.StructObserved = default;
        ApplyPatch(typeof(StateBindingPatches), nameof(StateBindingPatches.Postfix_StateParameter_Struct_ReadByValue_Prefix));
        ApplyPatch(typeof(StateBindingPatches), nameof(StateBindingPatches.Postfix_StateParameter_Struct_ReadByValue_Postfix));

        StaticMethodTargets.Void();

        Assert.That(StateBindingPatches.StructObserved.Value, Is.EqualTo(42));
    }

    [Test]
    public void Postfix_StateParameter_Struct_WriteByReference()
    {
        StateBindingPatches.StructObserved = default;
        ApplyPatch(typeof(StateBindingPatches), nameof(StateBindingPatches.Postfix_StateParameter_Struct_WriteByReference_Prefix));
        ApplyPatch(typeof(StateBindingPatches), nameof(StateBindingPatches.Postfix_StateParameter_Struct_WriteByReference_FirstPostfix));
        ApplyPatch(typeof(StateBindingPatches), nameof(StateBindingPatches.Postfix_StateParameter_Struct_WriteByReference_SecondPostfix));

        StaticMethodTargets.Void();

        Assert.That(StateBindingPatches.StructObserved.Value, Is.EqualTo(42));
    }

    [Test]
    public void Postfix_StateAttribute_Primitive_ReadByValue()
    {
        StateBindingPatches.Observed = 0;
        ApplyPatch(typeof(StateBindingPatches), nameof(StateBindingPatches.Postfix_StateAttribute_Primitive_ReadByValue_Prefix));
        ApplyPatch(typeof(StateBindingPatches), nameof(StateBindingPatches.Postfix_StateAttribute_Primitive_ReadByValue_Postfix));

        StaticMethodTargets.Void();

        Assert.That(StateBindingPatches.Observed, Is.EqualTo(42));
    }

    [Test]
    public void Postfix_StateAttribute_Primitive_WriteByReference()
    {
        StateBindingPatches.Observed = 0;
        ApplyPatch(typeof(StateBindingPatches), nameof(StateBindingPatches.Postfix_StateAttribute_Primitive_WriteByReference_Prefix));
        ApplyPatch(typeof(StateBindingPatches), nameof(StateBindingPatches.Postfix_StateAttribute_Primitive_WriteByReference_FirstPostfix));
        ApplyPatch(typeof(StateBindingPatches), nameof(StateBindingPatches.Postfix_StateAttribute_Primitive_WriteByReference_SecondPostfix));

        StaticMethodTargets.Void();

        Assert.That(StateBindingPatches.Observed, Is.EqualTo(42));
    }

    [Test]
    public void Postfix_StateAttribute_ReferenceType_ReadByValue()
    {
        StateBindingPatches.ReferenceObserved = null;
        ApplyPatch(typeof(StateBindingPatches), nameof(StateBindingPatches.Postfix_StateAttribute_ReferenceType_ReadByValue_Prefix));
        ApplyPatch(typeof(StateBindingPatches), nameof(StateBindingPatches.Postfix_StateAttribute_ReferenceType_ReadByValue_Postfix));

        StaticMethodTargets.Void();

        Assert.That(StateBindingPatches.ReferenceObserved, Is.EqualTo("original"));
    }

    [Test]
    public void Postfix_StateAttribute_ReferenceType_WriteByReference()
    {
        StateBindingPatches.ReferenceObserved = null;
        ApplyPatch(typeof(StateBindingPatches), nameof(StateBindingPatches.Postfix_StateAttribute_ReferenceType_WriteByReference_Prefix));
        ApplyPatch(typeof(StateBindingPatches), nameof(StateBindingPatches.Postfix_StateAttribute_ReferenceType_WriteByReference_FirstPostfix));
        ApplyPatch(typeof(StateBindingPatches), nameof(StateBindingPatches.Postfix_StateAttribute_ReferenceType_WriteByReference_SecondPostfix));

        StaticMethodTargets.Void();

        Assert.That(StateBindingPatches.ReferenceObserved, Is.EqualTo("patched"));
    }

    [Test]
    public void Postfix_StateAttribute_Struct_ReadByValue()
    {
        StateBindingPatches.StructObserved = default;
        ApplyPatch(typeof(StateBindingPatches), nameof(StateBindingPatches.Postfix_StateAttribute_Struct_ReadByValue_Prefix));
        ApplyPatch(typeof(StateBindingPatches), nameof(StateBindingPatches.Postfix_StateAttribute_Struct_ReadByValue_Postfix));

        StaticMethodTargets.Void();

        Assert.That(StateBindingPatches.StructObserved.Value, Is.EqualTo(42));
    }

    [Test]
    public void Postfix_StateAttribute_Struct_WriteByReference()
    {
        StateBindingPatches.StructObserved = default;
        ApplyPatch(typeof(StateBindingPatches), nameof(StateBindingPatches.Postfix_StateAttribute_Struct_WriteByReference_Prefix));
        ApplyPatch(typeof(StateBindingPatches), nameof(StateBindingPatches.Postfix_StateAttribute_Struct_WriteByReference_FirstPostfix));
        ApplyPatch(typeof(StateBindingPatches), nameof(StateBindingPatches.Postfix_StateAttribute_Struct_WriteByReference_SecondPostfix));

        StaticMethodTargets.Void();

        Assert.That(StateBindingPatches.StructObserved.Value, Is.EqualTo(42));
    }
}
