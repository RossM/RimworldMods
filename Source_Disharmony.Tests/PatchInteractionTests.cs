using NUnit.Framework;

namespace Disharmony.Tests;

[TestFixture]
public sealed class PatchInteractionTests : PatchTestBase
{
    [Test]
    public void PostfixObservesArgumentWrittenByPrefixWhenTargetRuns()
    {
        PatchMethods.CombinedPatchObserved = 0;
        ApplyPatches(
            nameof(PatchMethods.WriteArgumentAndRunTargetPrefix),
            nameof(PatchMethods.ObserveArgumentAfterTargetRunsPostfix));

        StaticMethodTargets.IntArgument(1);

        Assert.That(PatchMethods.CombinedPatchObserved, Is.EqualTo(42));
    }

    [Test]
    public void PostfixObservesArgumentWrittenByPrefixWhenTargetIsSkipped()
    {
        PatchMethods.CombinedPatchObserved = 0;
        ApplyPatches(
            nameof(PatchMethods.WriteArgumentAndSkipTargetPrefix),
            nameof(PatchMethods.ObserveArgumentAfterTargetIsSkippedPostfix));

        StaticMethodTargets.ThrowingIntArgument(1);

        Assert.That(PatchMethods.CombinedPatchObserved, Is.EqualTo(42));
    }

    [Test]
    public void PostfixObservesTargetResultWhenPrefixWritesResultAndTargetRuns()
    {
        PatchMethods.CombinedPatchObserved = 0;
        ApplyPatches(
            nameof(PatchMethods.WriteResultAndRunTargetPrefix),
            nameof(PatchMethods.ObserveResultAfterTargetRunsPostfix));

        StaticMethodTargets.IntResult();

        Assert.That(PatchMethods.CombinedPatchObserved, Is.EqualTo(1));
    }

    [Test]
    public void PostfixObservesPrefixResultWhenPrefixWritesResultAndTargetIsSkipped()
    {
        PatchMethods.CombinedPatchObserved = 0;
        ApplyPatches(
            nameof(PatchMethods.WriteResultAndSkipTargetPrefix),
            nameof(PatchMethods.ObserveResultAfterTargetIsSkippedPostfix));

        StaticMethodTargets.ThrowingIntResult();

        Assert.That(PatchMethods.CombinedPatchObserved, Is.EqualTo(42));
    }
}
