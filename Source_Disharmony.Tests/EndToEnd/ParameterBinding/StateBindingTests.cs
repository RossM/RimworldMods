namespace Disharmony.Tests.EndToEnd.ParameterBinding;

public static partial class StateBindingPatches
{
    public static int Observed;
    public static int SecondaryObserved;
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

    [Prefix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.Void))]
    public static void StateSharing_SameClass_SameType_ImplicitAndAttribute_Shares_Prefix(out int __state) =>
        __state = 42;

    [Postfix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.Void))]
    public static void StateSharing_SameClass_SameType_ImplicitAndAttribute_Shares_Postfix([State("__state")] int state) =>
        Observed = state;

    [Prefix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.Void))]
    public static void StateSharing_SameClass_SameType_SameExplicitKey_Shares_Prefix(
        [State("shared")] out int state) =>
        state = 42;

    [Postfix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.Void))]
    public static void StateSharing_SameClass_SameType_SameExplicitKey_Shares_Postfix(
        [State("shared")] int state) =>
        Observed = state;

    [Prefix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.Void))]
    public static void StateSharing_SameClass_SameType_DifferentExplicitKeys_Separate_Prefix(
        [State("first")] out int first,
        [State("second")] out int second)
    {
        first = 41;
        second = 42;
    }

    [Postfix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.Void))]
    public static void StateSharing_SameClass_SameType_DifferentExplicitKeys_Separate_Postfix(
        [State("first")] int first,
        [State("second")] int second)
    {
        Observed = first;
        SecondaryObserved = second;
    }

    [Prefix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.Void))]
    public static void StateSharing_SameClass_SameType_KeyedAndUnkeyed_Separate_Prefix(
        [State] out int unkeyed,
        [State("keyed")] out int keyed)
    {
        unkeyed = 41;
        keyed = 42;
    }

    [Postfix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.Void))]
    public static void StateSharing_SameClass_SameType_KeyedAndUnkeyed_Separate_Postfix(
        [State] int unkeyed,
        [State("keyed")] int keyed)
    {
        Observed = unkeyed;
        SecondaryObserved = keyed;
    }

    [Prefix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.Void))]
    public static void StateSharing_SameClass_DifferentTypes_Unkeyed_Separate_Prefix(
        [State] out int primitive,
        [State] out string reference)
    {
        primitive = 42;
        reference = "state";
    }

    [Postfix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.Void))]
    public static void StateSharing_SameClass_DifferentTypes_Unkeyed_Separate_Postfix(
        [State] int primitive,
        [State] string reference)
    {
        Observed = primitive;
        ReferenceObserved = reference;
    }

    [Prefix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.Void))]
    public static void StateSharing_DifferentClasses_SameType_Unkeyed_Separate_Prefix(out int __state) =>
        __state = 42;
}

public static class OtherStateBindingPatches
{
    [Postfix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.Void))]
    public static void StateSharing_DifferentClasses_SameType_Unkeyed_Separate_Postfix(int __state) =>
        StateBindingPatches.Observed = __state;
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
        ApplyPatch(typeof(StateBindingPatches),
            nameof(StateBindingPatches.Postfix_StateParameter_Primitive_WriteByReference_SecondPostfix));

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
        ApplyPatch(typeof(StateBindingPatches),
            nameof(StateBindingPatches.Postfix_StateParameter_ReferenceType_WriteByReference_FirstPostfix));
        ApplyPatch(typeof(StateBindingPatches),
            nameof(StateBindingPatches.Postfix_StateParameter_ReferenceType_WriteByReference_SecondPostfix));

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
        ApplyPatch(typeof(StateBindingPatches),
            nameof(StateBindingPatches.Postfix_StateAttribute_Primitive_WriteByReference_SecondPostfix));

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
        ApplyPatch(typeof(StateBindingPatches),
            nameof(StateBindingPatches.Postfix_StateAttribute_ReferenceType_WriteByReference_FirstPostfix));
        ApplyPatch(typeof(StateBindingPatches),
            nameof(StateBindingPatches.Postfix_StateAttribute_ReferenceType_WriteByReference_SecondPostfix));

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

    [Test]
    public void StateSharing_SameClass_SameType_ImplicitAndAttribute_Shares()
    {
        StateBindingPatches.Observed = 0;
        ApplyPatch(typeof(StateBindingPatches),
            nameof(StateBindingPatches.StateSharing_SameClass_SameType_ImplicitAndAttribute_Shares_Prefix));
        ApplyPatch(typeof(StateBindingPatches),
            nameof(StateBindingPatches.StateSharing_SameClass_SameType_ImplicitAndAttribute_Shares_Postfix));

        StaticMethodTargets.Void();

        Assert.That(StateBindingPatches.Observed, Is.EqualTo(42));
    }

    [Test]
    public void StateSharing_SameClass_SameType_SameExplicitKey_Shares()
    {
        StateBindingPatches.Observed = 0;
        ApplyPatch(typeof(StateBindingPatches),
            nameof(StateBindingPatches.StateSharing_SameClass_SameType_SameExplicitKey_Shares_Prefix));
        ApplyPatch(typeof(StateBindingPatches),
            nameof(StateBindingPatches.StateSharing_SameClass_SameType_SameExplicitKey_Shares_Postfix));

        StaticMethodTargets.Void();

        Assert.That(StateBindingPatches.Observed, Is.EqualTo(42));
    }

    [Test]
    public void StateSharing_SameClass_SameType_DifferentExplicitKeys_Separate()
    {
        StateBindingPatches.Observed = 0;
        StateBindingPatches.SecondaryObserved = 0;
        ApplyPatch(typeof(StateBindingPatches),
            nameof(StateBindingPatches.StateSharing_SameClass_SameType_DifferentExplicitKeys_Separate_Prefix));
        ApplyPatch(typeof(StateBindingPatches),
            nameof(StateBindingPatches.StateSharing_SameClass_SameType_DifferentExplicitKeys_Separate_Postfix));

        StaticMethodTargets.Void();

        Assert.That(StateBindingPatches.Observed, Is.EqualTo(41));
        Assert.That(StateBindingPatches.SecondaryObserved, Is.EqualTo(42));
    }

    [Test]
    public void StateSharing_SameClass_SameType_KeyedAndUnkeyed_Separate()
    {
        StateBindingPatches.Observed = 0;
        StateBindingPatches.SecondaryObserved = 0;
        ApplyPatch(typeof(StateBindingPatches),
            nameof(StateBindingPatches.StateSharing_SameClass_SameType_KeyedAndUnkeyed_Separate_Prefix));
        ApplyPatch(typeof(StateBindingPatches),
            nameof(StateBindingPatches.StateSharing_SameClass_SameType_KeyedAndUnkeyed_Separate_Postfix));

        StaticMethodTargets.Void();

        Assert.That(StateBindingPatches.Observed, Is.EqualTo(41));
        Assert.That(StateBindingPatches.SecondaryObserved, Is.EqualTo(42));
    }

    [Test]
    public void StateSharing_SameClass_DifferentTypes_Unkeyed_Separate()
    {
        StateBindingPatches.Observed = 0;
        StateBindingPatches.ReferenceObserved = null;
        ApplyPatch(typeof(StateBindingPatches),
            nameof(StateBindingPatches.StateSharing_SameClass_DifferentTypes_Unkeyed_Separate_Prefix));
        ApplyPatch(typeof(StateBindingPatches),
            nameof(StateBindingPatches.StateSharing_SameClass_DifferentTypes_Unkeyed_Separate_Postfix));

        StaticMethodTargets.Void();

        Assert.That(StateBindingPatches.Observed, Is.EqualTo(42));
        Assert.That(StateBindingPatches.ReferenceObserved, Is.EqualTo("state"));
    }

    [Test]
    public void StateSharing_DifferentClasses_SameType_Unkeyed_Separate()
    {
        StateBindingPatches.Observed = -1;
        ApplyPatch(typeof(StateBindingPatches),
            nameof(StateBindingPatches.StateSharing_DifferentClasses_SameType_Unkeyed_Separate_Prefix));
        ApplyPatch(typeof(OtherStateBindingPatches),
            nameof(OtherStateBindingPatches.StateSharing_DifferentClasses_SameType_Unkeyed_Separate_Postfix));

        StaticMethodTargets.Void();

        Assert.That(StateBindingPatches.Observed, Is.Zero);
    }
}
