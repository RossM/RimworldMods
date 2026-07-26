namespace Disharmony.Tests;

public static class InnerMemberAccessPatches
{
    public static InnerInstanceMethodTargets? InstanceObserved;

    [InnerPrefix(typeof(InnerStaticMethodTargets), nameof(InnerStaticMethodTargets.Field), MemberType.Getter)]
    [Target(typeof(OuterStaticMethodTargets), nameof(OuterStaticMethodTargets.FieldResult))]
    public static bool InnerPrefixCanReplaceStaticFieldRead(ref int __result)
    {
        __result = 42;
        return false;
    }

    [InnerPostfix(typeof(InnerStaticMethodTargets), nameof(InnerStaticMethodTargets.Field), MemberType.Getter)]
    [Target(typeof(OuterStaticMethodTargets), nameof(OuterStaticMethodTargets.FieldResult))]
    public static void InnerPostfixCanReplaceStaticFieldRead(ref int __result) => __result = 42;

    [InnerPostfix(typeof(InnerStaticMethodTargets), nameof(InnerStaticMethodTargets.Property), MemberType.Getter)]
    [Target(typeof(OuterStaticMethodTargets), nameof(OuterStaticMethodTargets.PropertyResult))]
    public static void InnerPostfixCanReplacePropertyGetterResult(ref int __result) => __result = 42;

    [InnerPostfix(typeof(InnerStaticMethodTargets), nameof(InnerStaticMethodTargets.IntResult))]
    [Target(typeof(OuterStaticMethodTargets), nameof(OuterStaticMethodTargets.EnumerateIntResult))]
    public static void InnerPostfixCanPatchCallInsideIteratorStateMachine(ref int __result) => __result = 42;

    [InnerPostfix(typeof(InnerInstanceMethodTargets), nameof(InnerInstanceMethodTargets.foo), MemberType.Getter)]
    [Target(typeof(OuterStaticMethodTargets), nameof(OuterStaticMethodTargets.ReadInstanceField))]
    public static void InnerPostfixCanReplaceInstanceFieldRead(ref int __result) => __result = 42;

    [InnerPostfix(typeof(InnerInstanceMethodTargets), nameof(InnerInstanceMethodTargets.foo), MemberType.Getter)]
    [Target(typeof(OuterStaticMethodTargets), nameof(OuterStaticMethodTargets.ReadInstanceField))]
    public static void InnerPostfix_InstanceField_ReferenceType_Instance_ReadByReference(
        [Instance(Scope.Inner)] ref InnerInstanceMethodTargets instance) => InstanceObserved = instance;

    [InnerPostfix(typeof(InnerStructMethodTargets), nameof(InnerStructMethodTargets.foo), MemberType.Getter)]
    [Target(typeof(OuterStaticMethodTargets), nameof(OuterStaticMethodTargets.ReadStructField))]
    public static void InnerPostfixCanReplaceStructFieldRead(ref int __result) => __result = 42;
}

[TestFixture]
public sealed class InnerMemberAccessTests : PatchTestBase
{
    [Test]
    public void InnerPrefixCanReplaceStaticFieldRead()
    {
        InnerStaticMethodTargets.Field = 1;
        ApplyPatch(typeof(InnerMemberAccessPatches), nameof(InnerMemberAccessPatches.InnerPrefixCanReplaceStaticFieldRead));

        Assert.That(OuterStaticMethodTargets.FieldResult(), Is.EqualTo(42));
    }

    [Test]
    public void InnerPostfixCanReplaceStaticFieldRead()
    {
        InnerStaticMethodTargets.Field = 1;
        ApplyPatch(typeof(InnerMemberAccessPatches), nameof(InnerMemberAccessPatches.InnerPostfixCanReplaceStaticFieldRead));

        Assert.That(OuterStaticMethodTargets.FieldResult(), Is.EqualTo(42));
    }

    [Test]
    public void InnerPostfixCanReplacePropertyGetterResult()
    {
        ApplyPatch(typeof(InnerMemberAccessPatches), nameof(InnerMemberAccessPatches.InnerPostfixCanReplacePropertyGetterResult));

        Assert.That(OuterStaticMethodTargets.PropertyResult(), Is.EqualTo(42));
    }

    [Test]
    public void InnerPostfixCanPatchCallInsideIteratorStateMachine()
    {
        ApplyPatch(typeof(InnerMemberAccessPatches), nameof(InnerMemberAccessPatches.InnerPostfixCanPatchCallInsideIteratorStateMachine));

        int result = OuterStaticMethodTargets.EnumerateIntResult().Single();

        Assert.That(result, Is.EqualTo(42));
    }

    [Test]
    public void InnerPostfixCanReplaceInstanceFieldRead()
    {
        ApplyPatch(typeof(InnerMemberAccessPatches), nameof(InnerMemberAccessPatches.InnerPostfixCanReplaceInstanceFieldRead));
        var inner = new InnerInstanceMethodTargets { foo = 1 };

        int result = OuterStaticMethodTargets.ReadInstanceField(inner);

        Assert.That(result, Is.EqualTo(42));
        Assert.That(inner.foo, Is.EqualTo(1));
    }

    [Test]
    public void InnerPostfix_InstanceField_ReferenceType_Instance_ReadByReference()
    {
        InnerMemberAccessPatches.InstanceObserved = null;
        var inner = new InnerInstanceMethodTargets { foo = 42 };
        ApplyPatch(
            typeof(InnerMemberAccessPatches),
            nameof(InnerMemberAccessPatches.InnerPostfix_InstanceField_ReferenceType_Instance_ReadByReference));

        int result = OuterStaticMethodTargets.ReadInstanceField(inner);

        Assert.That(result, Is.EqualTo(42));
        Assert.That(InnerMemberAccessPatches.InstanceObserved, Is.SameAs(inner));
    }

    [Test]
    public void InnerPostfixCanReplaceStructFieldRead()
    {
        ApplyPatch(typeof(InnerMemberAccessPatches), nameof(InnerMemberAccessPatches.InnerPostfixCanReplaceStructFieldRead));
        var inner = new InnerStructMethodTargets { foo = 1 };

        int result = OuterStaticMethodTargets.ReadStructField(inner);

        Assert.That(result, Is.EqualTo(42));
        Assert.That(inner.foo, Is.EqualTo(1));
    }
}
