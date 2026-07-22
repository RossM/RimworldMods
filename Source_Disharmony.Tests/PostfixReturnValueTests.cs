namespace Disharmony.Tests;

public static class PostfixReturnValuePatches
{
    [Postfix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.IntResult))]
    public static int PostfixReturnValueIsDiscarded() => 42;

    [InnerPostfix(typeof(InnerStaticMethodTargets), nameof(InnerStaticMethodTargets.IntResult))]
    [Target(typeof(OuterStaticMethodTargets), nameof(OuterStaticMethodTargets.IntResult))]
    public static int InnerPostfixReturnValueIsDiscarded() => 42;
}

[TestFixture]
public sealed partial class PostfixReturnValueTests : PatchTestBase
{
    [Test]
    public void PostfixReturnValueIsDiscarded()
    {
        ApplyPatch(typeof(PostfixReturnValuePatches), nameof(PostfixReturnValuePatches.PostfixReturnValueIsDiscarded));
        Assert.That(StaticMethodTargets.IntResult(), Is.EqualTo(1));
    }
}

[TestFixture]
public sealed partial class PostfixReturnValueTests
{
    [Test]
    public void InnerPostfixReturnValueIsDiscarded()
    {
        ApplyPatch(typeof(PostfixReturnValuePatches), nameof(PostfixReturnValuePatches.InnerPostfixReturnValueIsDiscarded));
        Assert.That(OuterStaticMethodTargets.IntResult(), Is.EqualTo(1));
    }
}
