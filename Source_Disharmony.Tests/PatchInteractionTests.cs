namespace Disharmony.Tests;

public static class PatchInteractionPatches
{
    public static int Observed;

    [Prefix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.IntArgument))]
    public static bool PostfixObservesArgumentWrittenByPrefixWhenTargetRuns_Prefix(ref int value)
    {
        value = 42;
        return true;
    }

    [Postfix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.IntArgument))]
    public static void PostfixObservesArgumentWrittenByPrefixWhenTargetRuns_Postfix(int value) => Observed = value;

    [Prefix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.ThrowingIntArgument))]
    public static bool PostfixObservesArgumentWrittenByPrefixWhenTargetIsSkipped_Prefix(ref int value)
    {
        value = 42;
        return false;
    }

    [Postfix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.ThrowingIntArgument))]
    public static void PostfixObservesArgumentWrittenByPrefixWhenTargetIsSkipped_Postfix(int value) => Observed = value;

    [Prefix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.IntResult))]
    public static bool PostfixObservesTargetResultWhenPrefixWritesResultAndTargetRuns_Prefix(ref int __result)
    {
        __result = 42;
        return true;
    }

    [Postfix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.IntResult))]
    public static void PostfixObservesTargetResultWhenPrefixWritesResultAndTargetRuns_Postfix(int __result) => Observed = __result;

    [Prefix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.ThrowingIntResult))]
    public static bool PostfixObservesPrefixResultWhenPrefixWritesResultAndTargetIsSkipped_Prefix(ref int __result)
    {
        __result = 42;
        return false;
    }

    [Postfix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.ThrowingIntResult))]
    public static void PostfixObservesPrefixResultWhenPrefixWritesResultAndTargetIsSkipped_Postfix(int __result) => Observed = __result;
}

[TestFixture]
public sealed class PatchInteractionTests : PatchTestBase
{
    [Test]
    public void PostfixObservesArgumentWrittenByPrefixWhenTargetRuns()
    {
        PatchInteractionPatches.Observed = 0;
        ApplyPatch(typeof(PatchInteractionPatches),
            nameof(PatchInteractionPatches.PostfixObservesArgumentWrittenByPrefixWhenTargetRuns_Prefix));
        ApplyPatch(typeof(PatchInteractionPatches),
            nameof(PatchInteractionPatches.PostfixObservesArgumentWrittenByPrefixWhenTargetRuns_Postfix));

        StaticMethodTargets.IntArgument(1);

        Assert.That(PatchInteractionPatches.Observed, Is.EqualTo(42));
    }

    [Test]
    public void PostfixObservesArgumentWrittenByPrefixWhenTargetIsSkipped()
    {
        PatchInteractionPatches.Observed = 0;
        ApplyPatch(typeof(PatchInteractionPatches),
            nameof(PatchInteractionPatches.PostfixObservesArgumentWrittenByPrefixWhenTargetIsSkipped_Prefix));
        ApplyPatch(typeof(PatchInteractionPatches),
            nameof(PatchInteractionPatches.PostfixObservesArgumentWrittenByPrefixWhenTargetIsSkipped_Postfix));

        StaticMethodTargets.ThrowingIntArgument(1);

        Assert.That(PatchInteractionPatches.Observed, Is.EqualTo(42));
    }

    [Test]
    public void PostfixObservesTargetResultWhenPrefixWritesResultAndTargetRuns()
    {
        PatchInteractionPatches.Observed = 0;
        ApplyPatch(typeof(PatchInteractionPatches),
            nameof(PatchInteractionPatches.PostfixObservesTargetResultWhenPrefixWritesResultAndTargetRuns_Prefix));
        ApplyPatch(typeof(PatchInteractionPatches),
            nameof(PatchInteractionPatches.PostfixObservesTargetResultWhenPrefixWritesResultAndTargetRuns_Postfix));

        StaticMethodTargets.IntResult();

        Assert.That(PatchInteractionPatches.Observed, Is.EqualTo(1));
    }

    [Test]
    public void PostfixObservesPrefixResultWhenPrefixWritesResultAndTargetIsSkipped()
    {
        PatchInteractionPatches.Observed = 0;
        ApplyPatch(typeof(PatchInteractionPatches),
            nameof(PatchInteractionPatches.PostfixObservesPrefixResultWhenPrefixWritesResultAndTargetIsSkipped_Prefix));
        ApplyPatch(typeof(PatchInteractionPatches),
            nameof(PatchInteractionPatches.PostfixObservesPrefixResultWhenPrefixWritesResultAndTargetIsSkipped_Postfix));

        StaticMethodTargets.ThrowingIntResult();

        Assert.That(PatchInteractionPatches.Observed, Is.EqualTo(42));
    }
}
