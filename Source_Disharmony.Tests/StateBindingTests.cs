namespace Disharmony.Tests;

public static class StateBindingPatches
{
    public static int Observed;

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
    public static void StateAttributeSharesStateBetweenPrefixAndPostfix_Prefix([State] out int state) => state = 42;

    [Postfix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.Void))]
    public static void StateAttributeSharesStateBetweenPrefixAndPostfix_Postfix([State] int state) => Observed = state;
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
    public void StateAttributeSharesStateBetweenPrefixAndPostfix()
    {
        StateBindingPatches.Observed = 0;
        ApplyPatch(typeof(StateBindingPatches), nameof(StateBindingPatches.StateAttributeSharesStateBetweenPrefixAndPostfix_Prefix));
        ApplyPatch(typeof(StateBindingPatches), nameof(StateBindingPatches.StateAttributeSharesStateBetweenPrefixAndPostfix_Postfix));

        StaticMethodTargets.Void();

        Assert.That(StateBindingPatches.Observed, Is.EqualTo(42));
    }
}
