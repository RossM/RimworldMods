namespace Disharmony.Tests;

[TestFixture]
public sealed partial class PostfixReturnValueTests : PatchTestBase
{
    [Test]
    public void PostfixReturnValueIsDiscarded()
    {
        ApplyPatch(typeof(PatchMethods), nameof(PatchMethods.NonVoidPostfix));
        Assert.That(StaticMethodTargets.IntResult(), Is.EqualTo(1));
    }
}

[TestFixture]
public sealed partial class PostfixReturnValueTests
{
    [Test]
    public void InnerPostfixReturnValueIsDiscarded()
    {
        ApplyPatch(typeof(InnerPatchMethods), nameof(InnerPatchMethods.NonVoidPostfix));
        Assert.That(OuterStaticMethodTargets.IntResult(), Is.EqualTo(1));
    }
}
