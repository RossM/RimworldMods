namespace Disharmony.Tests;

public static class InlinePatchMethods
{
    [Inline]
    [Prefix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.IntIdentity))]
    public static void MakeArgumentPositive(ref int value)
    {
        if (value < 0)
            value = -value;
    }
}

[TestFixture]
public sealed class InlinePatchTests : PatchTestBase
{
    [Test]
    public void InlinePrefixExecutesInlinedBranchAndRefWrite()
    {
        ApplyPatch(typeof(InlinePatchMethods), nameof(InlinePatchMethods.MakeArgumentPositive));

        int negativeResult = StaticMethodTargets.IntIdentity(-42);
        int positiveResult = StaticMethodTargets.IntIdentity(7);

        Assert.That(negativeResult, Is.EqualTo(42));
        Assert.That(positiveResult, Is.EqualTo(7));
    }
}
