namespace Disharmony.Tests;

public static class InstanceMethodPatches
{
    [Prefix]
    [Target(typeof(ClassMethodTargets), nameof(ClassMethodTargets.IntIdentity))]
    public static void PrefixCanRewriteArgumentOfClassInstanceMethod(ref int value) => value = 42;

    [Postfix]
    [Target(typeof(ClassMethodTargets), nameof(ClassMethodTargets.IntResult))]
    public static void PostfixCanRewriteResultOfClassInstanceMethod(ref int __result) => __result = 42;

    [Prefix]
    [Target(typeof(StructMethodTargets), nameof(StructMethodTargets.IntIdentity))]
    public static void PrefixCanRewriteArgumentOfStructInstanceMethod(ref int value) => value = 42;

    [Postfix]
    [Target(typeof(StructMethodTargets), nameof(StructMethodTargets.IntResult))]
    public static void PostfixCanRewriteResultOfStructInstanceMethod(ref int __result) => __result = 42;
}

[TestFixture]
public sealed class InstanceMethodTests : PatchTestBase
{
    [Test]
    public void PrefixCanRewriteArgumentOfClassInstanceMethod()
    {
        ApplyPatch(typeof(InstanceMethodPatches), nameof(InstanceMethodPatches.PrefixCanRewriteArgumentOfClassInstanceMethod));
        var instance = new ClassMethodTargets();

        int result = instance.IntIdentity(1);

        Assert.That(result, Is.EqualTo(42));
        Assert.That(instance.Value, Is.EqualTo(42));
    }

    [Test]
    public void PostfixCanRewriteResultOfClassInstanceMethod()
    {
        ApplyPatch(typeof(InstanceMethodPatches), nameof(InstanceMethodPatches.PostfixCanRewriteResultOfClassInstanceMethod));
        var instance = new ClassMethodTargets();

        int result = instance.IntResult();

        Assert.That(result, Is.EqualTo(42));
        Assert.That(instance.Value, Is.EqualTo(1));
    }

    [Test]
    public void PrefixCanRewriteArgumentOfStructInstanceMethod()
    {
        ApplyPatch(typeof(InstanceMethodPatches), nameof(InstanceMethodPatches.PrefixCanRewriteArgumentOfStructInstanceMethod));
        var instance = new StructMethodTargets();

        int result = instance.IntIdentity(1);

        Assert.That(result, Is.EqualTo(42));
        Assert.That(instance.Value, Is.EqualTo(42));
    }

    [Test]
    public void PostfixCanRewriteResultOfStructInstanceMethod()
    {
        ApplyPatch(typeof(InstanceMethodPatches), nameof(InstanceMethodPatches.PostfixCanRewriteResultOfStructInstanceMethod));
        var instance = new StructMethodTargets();

        int result = instance.IntResult();

        Assert.That(result, Is.EqualTo(42));
        Assert.That(instance.Value, Is.EqualTo(1));
    }
}
