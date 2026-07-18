using NUnit.Framework;

namespace Disharmony.Tests;

public static class InstanceMethodPatchMethods
{
    [Prefix]
    [Target(typeof(ClassInstanceMethodTarget), nameof(ClassInstanceMethodTarget.PrefixTarget))]
    public static void RewriteClassMethodArgumentPrefix(ref int value) => value = 42;

    [Postfix]
    [Target(typeof(ClassInstanceMethodTarget), nameof(ClassInstanceMethodTarget.PostfixTarget))]
    public static void RewriteClassMethodResultPostfix(ref int __result) => __result = 42;

    [Prefix]
    [Target(typeof(StructInstanceMethodTarget), nameof(StructInstanceMethodTarget.PrefixTarget))]
    public static void RewriteStructMethodArgumentPrefix(ref int value) => value = 42;

    [Postfix]
    [Target(typeof(StructInstanceMethodTarget), nameof(StructInstanceMethodTarget.PostfixTarget))]
    public static void RewriteStructMethodResultPostfix(ref int __result) => __result = 42;
}

public sealed class ClassInstanceMethodTarget
{
    public int Value { get; private set; }

    public int PrefixTarget(int value)
    {
        Value = value;
        return Value;
    }

    public int PostfixTarget()
    {
        Value = 1;
        return Value;
    }
}

public struct StructInstanceMethodTarget
{
    public int Value { get; private set; }

    public int PrefixTarget(int value)
    {
        Value = value;
        return Value;
    }

    public int PostfixTarget()
    {
        Value = 1;
        return Value;
    }
}

[TestFixture]
public sealed class InstanceMethodTests
{
    private static void ApplyPatch(string patchMethodName) =>
        Autopatcher.Patch(typeof(InstanceMethodPatchMethods).GetMethod(patchMethodName));

    [Test]
    public void PrefixCanRewriteArgumentOfClassInstanceMethod()
    {
        ApplyPatch(nameof(InstanceMethodPatchMethods.RewriteClassMethodArgumentPrefix));
        var instance = new ClassInstanceMethodTarget();

        int result = instance.PrefixTarget(1);

        Assert.That(result, Is.EqualTo(42));
        Assert.That(instance.Value, Is.EqualTo(42));
    }

    [Test]
    public void PostfixCanRewriteResultOfClassInstanceMethod()
    {
        ApplyPatch(nameof(InstanceMethodPatchMethods.RewriteClassMethodResultPostfix));
        var instance = new ClassInstanceMethodTarget();

        int result = instance.PostfixTarget();

        Assert.That(result, Is.EqualTo(42));
        Assert.That(instance.Value, Is.EqualTo(1));
    }

    [Test]
    public void PrefixCanRewriteArgumentOfStructInstanceMethod()
    {
        ApplyPatch(nameof(InstanceMethodPatchMethods.RewriteStructMethodArgumentPrefix));
        var instance = new StructInstanceMethodTarget();

        int result = instance.PrefixTarget(1);

        Assert.That(result, Is.EqualTo(42));
        Assert.That(instance.Value, Is.EqualTo(42));
    }

    [Test]
    public void PostfixCanRewriteResultOfStructInstanceMethod()
    {
        ApplyPatch(nameof(InstanceMethodPatchMethods.RewriteStructMethodResultPostfix));
        var instance = new StructInstanceMethodTarget();

        int result = instance.PostfixTarget();

        Assert.That(result, Is.EqualTo(42));
        Assert.That(instance.Value, Is.EqualTo(1));
    }
}
