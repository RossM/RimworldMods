namespace Disharmony.Tests;

[TestFixture]
public sealed class PatchInteractionTests : PatchTestBase
{
    [Test]
    public void PostfixObservesArgumentWrittenByPrefixWhenTargetRuns()
    {
        PatchMethods.CombinedPatchObserved = 0;
        ApplyPatch(typeof(PatchMethods), nameof(PatchMethods.WriteArgumentAndRunTargetPrefix));
        ApplyPatch(typeof(PatchMethods), nameof(PatchMethods.ObserveArgumentAfterTargetRunsPostfix));

        StaticMethodTargets.IntArgument(1);

        Assert.That(PatchMethods.CombinedPatchObserved, Is.EqualTo(42));
    }

    [Test]
    public void PostfixObservesArgumentWrittenByPrefixWhenTargetIsSkipped()
    {
        PatchMethods.CombinedPatchObserved = 0;
        ApplyPatch(typeof(PatchMethods), nameof(PatchMethods.WriteArgumentAndSkipTargetPrefix));
        ApplyPatch(typeof(PatchMethods), nameof(PatchMethods.ObserveArgumentAfterTargetIsSkippedPostfix));

        StaticMethodTargets.ThrowingIntArgument(1);

        Assert.That(PatchMethods.CombinedPatchObserved, Is.EqualTo(42));
    }

    [Test]
    public void PostfixObservesTargetResultWhenPrefixWritesResultAndTargetRuns()
    {
        PatchMethods.CombinedPatchObserved = 0;
        ApplyPatch(typeof(PatchMethods), nameof(PatchMethods.WriteResultAndRunTargetPrefix));
        ApplyPatch(typeof(PatchMethods), nameof(PatchMethods.ObserveResultAfterTargetRunsPostfix));

        StaticMethodTargets.IntResult();

        Assert.That(PatchMethods.CombinedPatchObserved, Is.EqualTo(1));
    }

    [Test]
    public void PostfixObservesPrefixResultWhenPrefixWritesResultAndTargetIsSkipped()
    {
        PatchMethods.CombinedPatchObserved = 0;
        ApplyPatch(typeof(PatchMethods), nameof(PatchMethods.WriteResultAndSkipTargetPrefix));
        ApplyPatch(typeof(PatchMethods), nameof(PatchMethods.ObserveResultAfterTargetIsSkippedPostfix));

        StaticMethodTargets.ThrowingIntResult();

        Assert.That(PatchMethods.CombinedPatchObserved, Is.EqualTo(42));
    }
}
