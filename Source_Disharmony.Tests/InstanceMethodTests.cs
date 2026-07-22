namespace Disharmony.Tests;

public static class InstanceMethodPatchMethods
{
    [Prefix]
    [Target(typeof(ClassMethodTargets), nameof(ClassMethodTargets.IntIdentity))]
    public static void RewriteClassMethodArgumentPrefix(ref int value) => value = 42;

    [Postfix]
    [Target(typeof(ClassMethodTargets), nameof(ClassMethodTargets.IntResult))]
    public static void RewriteClassMethodResultPostfix(ref int __result) => __result = 42;

    [Prefix]
    [Target(typeof(StructMethodTargets), nameof(StructMethodTargets.IntIdentity))]
    public static void RewriteStructMethodArgumentPrefix(ref int value) => value = 42;

    [Postfix]
    [Target(typeof(StructMethodTargets), nameof(StructMethodTargets.IntResult))]
    public static void RewriteStructMethodResultPostfix(ref int __result) => __result = 42;
}

[TestFixture]
public sealed class InstanceMethodTests : PatchTestBase
{
    private static void ApplyInstanceMethodPatch(string patchMethodName) =>
        Autopatcher.Patch(typeof(InstanceMethodPatchMethods).GetMethod(patchMethodName));

    [Test]
    public void PrefixCanRewriteArgumentOfClassInstanceMethod()
    {
        ApplyInstanceMethodPatch(nameof(InstanceMethodPatchMethods.RewriteClassMethodArgumentPrefix));
        var instance = new ClassMethodTargets();

        int result = instance.IntIdentity(1);

        Assert.That(result, Is.EqualTo(42));
        Assert.That(instance.Value, Is.EqualTo(42));
    }

    [Test]
    public void PostfixCanRewriteResultOfClassInstanceMethod()
    {
        ApplyInstanceMethodPatch(nameof(InstanceMethodPatchMethods.RewriteClassMethodResultPostfix));
        var instance = new ClassMethodTargets();

        int result = instance.IntResult();

        Assert.That(result, Is.EqualTo(42));
        Assert.That(instance.Value, Is.EqualTo(1));
    }

    [Test]
    public void PrefixCanRewriteArgumentOfStructInstanceMethod()
    {
        ApplyInstanceMethodPatch(nameof(InstanceMethodPatchMethods.RewriteStructMethodArgumentPrefix));
        var instance = new StructMethodTargets();

        int result = instance.IntIdentity(1);

        Assert.That(result, Is.EqualTo(42));
        Assert.That(instance.Value, Is.EqualTo(42));
    }

    [Test]
    public void PostfixCanRewriteResultOfStructInstanceMethod()
    {
        ApplyInstanceMethodPatch(nameof(InstanceMethodPatchMethods.RewriteStructMethodResultPostfix));
        var instance = new StructMethodTargets();

        int result = instance.IntResult();

        Assert.That(result, Is.EqualTo(42));
        Assert.That(instance.Value, Is.EqualTo(1));
    }
}
