namespace Disharmony.Tests;

public static class StateBindingPatches
{
    public static int Observed;
    public static string? ReferenceObserved;
    public static BindingStruct StructObserved;

    [Prefix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.Void))]
    public static void PostfixCanReadStateWrittenByPrefix_Prefix(out int __state) => __state = 42;

    [Postfix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.Void))]
    public static void PostfixCanReadStateWrittenByPrefix_Postfix(int __state) => Observed = __state;

    [Prefix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.Void))]
    public static void PostfixCanWriteStateByReferenceForLaterPostfix_Prefix(out int __state) => __state = 42;

    [Postfix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.Void))]
    public static void PostfixCanWriteStateByReferenceForLaterPostfix_FirstPostfix(ref int __state) => __state = 43;

    [Postfix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.Void))]
    public static void PostfixCanWriteStateByReferenceForLaterPostfix_SecondPostfix(int __state) => Observed = __state;

    [Prefix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.Void))]
    public static void PostfixCanReadReferenceTypeStateWrittenByPrefix_Prefix(out string __state) => __state = "original";

    [Postfix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.Void))]
    public static void PostfixCanReadReferenceTypeStateWrittenByPrefix_Postfix(string __state) => ReferenceObserved = __state;

    [Prefix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.Void))]
    public static void PostfixCanWriteReferenceTypeStateByReferenceForLaterPostfix_Prefix(out string __state) =>
        __state = "original";

    [Postfix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.Void))]
    public static void PostfixCanWriteReferenceTypeStateByReferenceForLaterPostfix_FirstPostfix(ref string __state) =>
        __state = "patched";

    [Postfix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.Void))]
    public static void PostfixCanWriteReferenceTypeStateByReferenceForLaterPostfix_SecondPostfix(string __state) =>
        ReferenceObserved = __state;

    [Prefix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.Void))]
    public static void PostfixCanReadStructStateWrittenByPrefix_Prefix(out BindingStruct __state) =>
        __state = new BindingStruct { Value = 42 };

    [Postfix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.Void))]
    public static void PostfixCanReadStructStateWrittenByPrefix_Postfix(BindingStruct __state) => StructObserved = __state;

    [Prefix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.Void))]
    public static void PostfixCanWriteStructStateByReferenceForLaterPostfix_Prefix(out BindingStruct __state) =>
        __state = new BindingStruct { Value = 1 };

    [Postfix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.Void))]
    public static void PostfixCanWriteStructStateByReferenceForLaterPostfix_FirstPostfix(ref BindingStruct __state) =>
        __state = new BindingStruct { Value = 42 };

    [Postfix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.Void))]
    public static void PostfixCanWriteStructStateByReferenceForLaterPostfix_SecondPostfix(BindingStruct __state) =>
        StructObserved = __state;

    [Prefix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.Void))]
    public static void StateAttributeSharesStateBetweenPrefixAndPostfix_Prefix([State] out int state) => state = 42;

    [Postfix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.Void))]
    public static void StateAttributeSharesStateBetweenPrefixAndPostfix_Postfix([State] int state) => Observed = state;

    [Prefix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.Void))]
    public static void StateAttributeAllowsPrimitiveStateToBeWrittenByReference_Prefix([State] out int state) => state = 1;

    [Postfix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.Void))]
    public static void StateAttributeAllowsPrimitiveStateToBeWrittenByReference_FirstPostfix([State] ref int state) => state = 42;

    [Postfix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.Void))]
    public static void StateAttributeAllowsPrimitiveStateToBeWrittenByReference_SecondPostfix([State] int state) => Observed = state;

    [Prefix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.Void))]
    public static void StateAttributeSharesReferenceTypeStateByValue_Prefix([State] out string state) => state = "original";

    [Postfix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.Void))]
    public static void StateAttributeSharesReferenceTypeStateByValue_Postfix([State] string state) => ReferenceObserved = state;

    [Prefix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.Void))]
    public static void StateAttributeAllowsReferenceTypeStateToBeWrittenByReference_Prefix([State] out string state) =>
        state = "original";

    [Postfix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.Void))]
    public static void StateAttributeAllowsReferenceTypeStateToBeWrittenByReference_FirstPostfix([State] ref string state) =>
        state = "patched";

    [Postfix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.Void))]
    public static void StateAttributeAllowsReferenceTypeStateToBeWrittenByReference_SecondPostfix([State] string state) =>
        ReferenceObserved = state;

    [Prefix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.Void))]
    public static void StateAttributeSharesStructStateByValue_Prefix([State] out BindingStruct state) =>
        state = new BindingStruct { Value = 42 };

    [Postfix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.Void))]
    public static void StateAttributeSharesStructStateByValue_Postfix([State] BindingStruct state) => StructObserved = state;

    [Prefix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.Void))]
    public static void StateAttributeAllowsStructStateToBeWrittenByReference_Prefix([State] out BindingStruct state) =>
        state = new BindingStruct { Value = 1 };

    [Postfix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.Void))]
    public static void StateAttributeAllowsStructStateToBeWrittenByReference_FirstPostfix([State] ref BindingStruct state) =>
        state = new BindingStruct { Value = 42 };

    [Postfix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.Void))]
    public static void StateAttributeAllowsStructStateToBeWrittenByReference_SecondPostfix([State] BindingStruct state) =>
        StructObserved = state;
}

[TestFixture]
public sealed partial class StateBindingTests : PatchTestBase
{
    [Test]
    public void PostfixCanReadStateWrittenByPrefix()
    {
        StateBindingPatches.Observed = 0;
        ApplyPatch(typeof(StateBindingPatches), nameof(StateBindingPatches.PostfixCanReadStateWrittenByPrefix_Prefix));
        ApplyPatch(typeof(StateBindingPatches), nameof(StateBindingPatches.PostfixCanReadStateWrittenByPrefix_Postfix));

        StaticMethodTargets.Void();

        Assert.That(StateBindingPatches.Observed, Is.EqualTo(42));
    }

    [Test]
    public void PostfixCanWriteStateByReferenceForLaterPostfix()
    {
        StateBindingPatches.Observed = 0;
        ApplyPatch(typeof(StateBindingPatches), nameof(StateBindingPatches.PostfixCanWriteStateByReferenceForLaterPostfix_Prefix));
        ApplyPatch(typeof(StateBindingPatches), nameof(StateBindingPatches.PostfixCanWriteStateByReferenceForLaterPostfix_FirstPostfix));
        ApplyPatch(typeof(StateBindingPatches), nameof(StateBindingPatches.PostfixCanWriteStateByReferenceForLaterPostfix_SecondPostfix));

        StaticMethodTargets.Void();

        Assert.That(StateBindingPatches.Observed, Is.EqualTo(43));
    }

    [Test]
    public void PostfixCanReadReferenceTypeStateWrittenByPrefix()
    {
        StateBindingPatches.ReferenceObserved = null;
        ApplyPatch(typeof(StateBindingPatches), nameof(StateBindingPatches.PostfixCanReadReferenceTypeStateWrittenByPrefix_Prefix));
        ApplyPatch(typeof(StateBindingPatches), nameof(StateBindingPatches.PostfixCanReadReferenceTypeStateWrittenByPrefix_Postfix));

        StaticMethodTargets.Void();

        Assert.That(StateBindingPatches.ReferenceObserved, Is.EqualTo("original"));
    }

    [Test]
    public void PostfixCanWriteReferenceTypeStateByReferenceForLaterPostfix()
    {
        StateBindingPatches.ReferenceObserved = null;
        ApplyPatch(typeof(StateBindingPatches), nameof(StateBindingPatches.PostfixCanWriteReferenceTypeStateByReferenceForLaterPostfix_Prefix));
        ApplyPatch(typeof(StateBindingPatches), nameof(StateBindingPatches.PostfixCanWriteReferenceTypeStateByReferenceForLaterPostfix_FirstPostfix));
        ApplyPatch(typeof(StateBindingPatches), nameof(StateBindingPatches.PostfixCanWriteReferenceTypeStateByReferenceForLaterPostfix_SecondPostfix));

        StaticMethodTargets.Void();

        Assert.That(StateBindingPatches.ReferenceObserved, Is.EqualTo("patched"));
    }

    [Test]
    public void PostfixCanReadStructStateWrittenByPrefix()
    {
        StateBindingPatches.StructObserved = default;
        ApplyPatch(typeof(StateBindingPatches), nameof(StateBindingPatches.PostfixCanReadStructStateWrittenByPrefix_Prefix));
        ApplyPatch(typeof(StateBindingPatches), nameof(StateBindingPatches.PostfixCanReadStructStateWrittenByPrefix_Postfix));

        StaticMethodTargets.Void();

        Assert.That(StateBindingPatches.StructObserved.Value, Is.EqualTo(42));
    }

    [Test]
    public void PostfixCanWriteStructStateByReferenceForLaterPostfix()
    {
        StateBindingPatches.StructObserved = default;
        ApplyPatch(typeof(StateBindingPatches), nameof(StateBindingPatches.PostfixCanWriteStructStateByReferenceForLaterPostfix_Prefix));
        ApplyPatch(typeof(StateBindingPatches), nameof(StateBindingPatches.PostfixCanWriteStructStateByReferenceForLaterPostfix_FirstPostfix));
        ApplyPatch(typeof(StateBindingPatches), nameof(StateBindingPatches.PostfixCanWriteStructStateByReferenceForLaterPostfix_SecondPostfix));

        StaticMethodTargets.Void();

        Assert.That(StateBindingPatches.StructObserved.Value, Is.EqualTo(42));
    }

    [Test]
    public void StateAttributeSharesStateBetweenPrefixAndPostfix()
    {
        StateBindingPatches.Observed = 0;
        ApplyPatch(typeof(StateBindingPatches), nameof(StateBindingPatches.StateAttributeSharesStateBetweenPrefixAndPostfix_Prefix));
        ApplyPatch(typeof(StateBindingPatches), nameof(StateBindingPatches.StateAttributeSharesStateBetweenPrefixAndPostfix_Postfix));

        StaticMethodTargets.Void();

        Assert.That(StateBindingPatches.Observed, Is.EqualTo(42));
    }

    [Test]
    public void StateAttributeAllowsPrimitiveStateToBeWrittenByReference()
    {
        StateBindingPatches.Observed = 0;
        ApplyPatch(typeof(StateBindingPatches), nameof(StateBindingPatches.StateAttributeAllowsPrimitiveStateToBeWrittenByReference_Prefix));
        ApplyPatch(typeof(StateBindingPatches), nameof(StateBindingPatches.StateAttributeAllowsPrimitiveStateToBeWrittenByReference_FirstPostfix));
        ApplyPatch(typeof(StateBindingPatches), nameof(StateBindingPatches.StateAttributeAllowsPrimitiveStateToBeWrittenByReference_SecondPostfix));

        StaticMethodTargets.Void();

        Assert.That(StateBindingPatches.Observed, Is.EqualTo(42));
    }

    [Test]
    public void StateAttributeSharesReferenceTypeStateByValue()
    {
        StateBindingPatches.ReferenceObserved = null;
        ApplyPatch(typeof(StateBindingPatches), nameof(StateBindingPatches.StateAttributeSharesReferenceTypeStateByValue_Prefix));
        ApplyPatch(typeof(StateBindingPatches), nameof(StateBindingPatches.StateAttributeSharesReferenceTypeStateByValue_Postfix));

        StaticMethodTargets.Void();

        Assert.That(StateBindingPatches.ReferenceObserved, Is.EqualTo("original"));
    }

    [Test]
    public void StateAttributeAllowsReferenceTypeStateToBeWrittenByReference()
    {
        StateBindingPatches.ReferenceObserved = null;
        ApplyPatch(typeof(StateBindingPatches), nameof(StateBindingPatches.StateAttributeAllowsReferenceTypeStateToBeWrittenByReference_Prefix));
        ApplyPatch(typeof(StateBindingPatches), nameof(StateBindingPatches.StateAttributeAllowsReferenceTypeStateToBeWrittenByReference_FirstPostfix));
        ApplyPatch(typeof(StateBindingPatches), nameof(StateBindingPatches.StateAttributeAllowsReferenceTypeStateToBeWrittenByReference_SecondPostfix));

        StaticMethodTargets.Void();

        Assert.That(StateBindingPatches.ReferenceObserved, Is.EqualTo("patched"));
    }

    [Test]
    public void StateAttributeSharesStructStateByValue()
    {
        StateBindingPatches.StructObserved = default;
        ApplyPatch(typeof(StateBindingPatches), nameof(StateBindingPatches.StateAttributeSharesStructStateByValue_Prefix));
        ApplyPatch(typeof(StateBindingPatches), nameof(StateBindingPatches.StateAttributeSharesStructStateByValue_Postfix));

        StaticMethodTargets.Void();

        Assert.That(StateBindingPatches.StructObserved.Value, Is.EqualTo(42));
    }

    [Test]
    public void StateAttributeAllowsStructStateToBeWrittenByReference()
    {
        StateBindingPatches.StructObserved = default;
        ApplyPatch(typeof(StateBindingPatches), nameof(StateBindingPatches.StateAttributeAllowsStructStateToBeWrittenByReference_Prefix));
        ApplyPatch(typeof(StateBindingPatches), nameof(StateBindingPatches.StateAttributeAllowsStructStateToBeWrittenByReference_FirstPostfix));
        ApplyPatch(typeof(StateBindingPatches), nameof(StateBindingPatches.StateAttributeAllowsStructStateToBeWrittenByReference_SecondPostfix));

        StaticMethodTargets.Void();

        Assert.That(StateBindingPatches.StructObserved.Value, Is.EqualTo(42));
    }
}
