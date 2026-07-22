namespace Disharmony.Tests;

public static class InnerMemberPatchMethods
{
    [InnerPrefix(typeof(InnerStaticMethodTargets), nameof(InnerStaticMethodTargets.Field), MemberType.Getter)]
    [Target(typeof(OuterStaticMethodTargets), nameof(OuterStaticMethodTargets.FieldResult))]
    public static bool ReplaceFieldReadPrefix(ref int __result)
    {
        __result = 42;
        return false;
    }

    [InnerPostfix(typeof(InnerStaticMethodTargets), nameof(InnerStaticMethodTargets.Field), MemberType.Getter)]
    [Target(typeof(OuterStaticMethodTargets), nameof(OuterStaticMethodTargets.FieldResult))]
    public static void ReplaceFieldReadPostfix(ref int __result) => __result = 42;

    [InnerPostfix(typeof(InnerStaticMethodTargets), nameof(InnerStaticMethodTargets.Property), MemberType.Getter)]
    [Target(typeof(OuterStaticMethodTargets), nameof(OuterStaticMethodTargets.PropertyResult))]
    public static void ReplacePropertyGetterResult(ref int __result) => __result = 42;

    [InnerPostfix(typeof(InnerStaticMethodTargets), nameof(InnerStaticMethodTargets.IntResult))]
    [Target(typeof(OuterStaticMethodTargets), nameof(OuterStaticMethodTargets.EnumerateIntResult))]
    public static void ReplaceIteratorInnerResult(ref int __result) => __result = 42;

    [InnerPostfix(typeof(InnerInstanceMethodTargets), nameof(InnerInstanceMethodTargets.foo), MemberType.Getter)]
    [Target(typeof(OuterStaticMethodTargets), nameof(OuterStaticMethodTargets.ReadInstanceField))]
    public static void ReplaceInstanceFieldRead(ref int __result) => __result = 42;

    [InnerPostfix(typeof(InnerStructMethodTargets), nameof(InnerStructMethodTargets.foo), MemberType.Getter)]
    [Target(typeof(OuterStaticMethodTargets), nameof(OuterStaticMethodTargets.ReadStructField))]
    public static void ReplaceStructFieldRead(ref int __result) => __result = 42;
}

[TestFixture]
public sealed class InnerMemberAccessTests : PatchTestBase
{
    [Test]
    public void InnerPrefixCanReplaceStaticFieldRead()
    {
        InnerStaticMethodTargets.Field = 1;
        ApplyPatch(typeof(InnerMemberPatchMethods), nameof(InnerMemberPatchMethods.ReplaceFieldReadPrefix));

        Assert.That(OuterStaticMethodTargets.FieldResult(), Is.EqualTo(42));
    }

    [Test]
    public void InnerPostfixCanReplaceStaticFieldRead()
    {
        InnerStaticMethodTargets.Field = 1;
        ApplyPatch(typeof(InnerMemberPatchMethods), nameof(InnerMemberPatchMethods.ReplaceFieldReadPostfix));

        Assert.That(OuterStaticMethodTargets.FieldResult(), Is.EqualTo(42));
    }

    [Test]
    public void InnerPostfixCanReplacePropertyGetterResult()
    {
        ApplyPatch(typeof(InnerMemberPatchMethods), nameof(InnerMemberPatchMethods.ReplacePropertyGetterResult));

        Assert.That(OuterStaticMethodTargets.PropertyResult(), Is.EqualTo(42));
    }

    [Test]
    public void InnerPostfixCanPatchCallInsideIteratorStateMachine()
    {
        ApplyPatch(typeof(InnerMemberPatchMethods), nameof(InnerMemberPatchMethods.ReplaceIteratorInnerResult));

        int result = OuterStaticMethodTargets.EnumerateIntResult().Single();

        Assert.That(result, Is.EqualTo(42));
    }

    [Test]
    public void InnerPostfixCanReplaceInstanceFieldRead()
    {
        ApplyPatch(typeof(InnerMemberPatchMethods), nameof(InnerMemberPatchMethods.ReplaceInstanceFieldRead));
        var inner = new InnerInstanceMethodTargets { foo = 1 };

        int result = OuterStaticMethodTargets.ReadInstanceField(inner);

        Assert.That(result, Is.EqualTo(42));
        Assert.That(inner.foo, Is.EqualTo(1));
    }

    [Test]
    public void InnerPostfixCanReplaceStructFieldRead()
    {
        ApplyPatch(typeof(InnerMemberPatchMethods), nameof(InnerMemberPatchMethods.ReplaceStructFieldRead));
        var inner = new InnerStructMethodTargets { foo = 1 };

        int result = OuterStaticMethodTargets.ReadStructField(inner);

        Assert.That(result, Is.EqualTo(42));
        Assert.That(inner.foo, Is.EqualTo(1));
    }
}
