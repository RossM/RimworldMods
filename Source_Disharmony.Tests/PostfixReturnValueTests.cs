namespace Disharmony.Tests;

public static class PostfixReturnValuePatches
{
    [Postfix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.IntResult))]
    public static int PostfixReturningNonVoidIsRejected() => 42;

    [InnerPostfix(typeof(InnerStaticMethodTargets), nameof(InnerStaticMethodTargets.IntResult))]
    [Target(typeof(OuterStaticMethodTargets), nameof(OuterStaticMethodTargets.IntResult))]
    public static int InnerPostfixReturningNonVoidIsRejected() => 42;
}

[TestFixture]
public sealed partial class PostfixReturnValueTests : PatchTestBase
{
    [Test]
    public void PostfixReturningNonVoidIsRejected()
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            ApplyPatch(
                typeof(PostfixReturnValuePatches),
                nameof(PostfixReturnValuePatches.PostfixReturningNonVoidIsRejected)));

        Assert.That(exception!.InnerException, Is.TypeOf<InvalidOperationException>());
        Assert.That(exception.InnerException!.Message, Does.EndWith("Postfix must return 'void'"));
    }
}

[TestFixture]
public sealed partial class PostfixReturnValueTests
{
    [Test]
    public void InnerPostfixReturningNonVoidIsRejected()
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            ApplyPatch(
                typeof(PostfixReturnValuePatches),
                nameof(PostfixReturnValuePatches.InnerPostfixReturningNonVoidIsRejected)));

        Assert.That(exception!.InnerException, Is.TypeOf<InvalidOperationException>());
        Assert.That(exception.InnerException!.Message, Does.EndWith("InnerPostfix must return 'void'"));
    }
}
