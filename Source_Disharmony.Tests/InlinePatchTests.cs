namespace Disharmony.Tests;

[TestFixture]
public sealed class InlinePatchTests : PatchTestBase
{
    [Test]
    public void InlinePrefixExecutesInlinedBranchAndRefWrite()
    {
        MethodInfo patch = typeof(InlinePatchPatches)
            .GetMethod(nameof(InlinePatchPatches.InlinePrefixExecutesInlinedBranchAndRefWrite))!;
        MethodInfo target = typeof(StaticMethodTargets)
            .GetMethod(nameof(StaticMethodTargets.IntIdentity))!;
        Autopatcher.Patch(
            patch,
            PatchType.Prefix,
            options: PatchOptions.Inline,
            targets: [target]);

        int negativeResult = StaticMethodTargets.IntIdentity(-42);
        int positiveResult = StaticMethodTargets.IntIdentity(7);

        Assert.That(negativeResult, Is.EqualTo(42));
        Assert.That(positiveResult, Is.EqualTo(7));
    }
}
