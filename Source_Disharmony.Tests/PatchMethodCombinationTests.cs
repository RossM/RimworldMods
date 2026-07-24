namespace Disharmony.Tests;

public static class PatchMethodCombinationPatches
{
    public static int FirstObserved;
    public static ClassMethodTargets? InstanceObserved;

    [Prefix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.IntArgument))]
    public static void Prefix_ExplicitParameterBinding_ReservedName_Result_BindsArgument(
        [Parameter("value")] int __result) =>
        FirstObserved = __result;

    [Prefix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.IntIdentity))]
    public static void Prefix_DuplicateArgumentBinding_ReadByValueThenWriteByReference(
        int value,
        [Parameter("value")] ref int replacement)
    {
        FirstObserved = value;
        replacement = 42;
    }

    [Postfix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.IntResult))]
    public static void Postfix_DuplicateResultBinding_ReadByValueThenWriteByReference(
        [ReturnValue] int original,
        [ReturnValue] ref int replacement)
    {
        FirstObserved = original;
        replacement = 42;
    }

    [Prefix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.ThrowingIntResult))]
    public static bool Prefix_DuplicateResultBinding_ReadByValueThenWriteByReference_SkipsTarget(
        [ReturnValue] int original,
        [ReturnValue] ref int replacement)
    {
        FirstObserved = original;
        replacement = 42;
        return false;
    }

    [InnerPostfix(typeof(InnerStaticMethodTargets), nameof(InnerStaticMethodTargets.IntResult))]
    [Target(typeof(OuterStaticMethodTargets), nameof(OuterStaticMethodTargets.IntResult))]
    public static void InnerPostfix_DuplicateResultBinding_ReadByValueThenWriteByReference(
        [ReturnValue] int original,
        [ReturnValue] ref int replacement)
    {
        FirstObserved = original;
        replacement = 42;
    }

    [InnerPrefix(typeof(InnerStaticMethodTargets), nameof(InnerStaticMethodTargets.IntResult))]
    [Target(typeof(OuterStaticMethodTargets), nameof(OuterStaticMethodTargets.IntResult))]
    public static bool InnerPrefix_DuplicateResultBinding_ReadByValueThenWriteByReference_SkipsTarget(
        [ReturnValue] int original,
        [ReturnValue] ref int replacement)
    {
        FirstObserved = original;
        replacement = 42;
        return false;
    }

    [Prefix]
    [Target(typeof(ClassMethodTargets), nameof(ClassMethodTargets.IntIdentity))]
    public static bool Prefix_CombinedBindings_InstanceFieldArgumentResult_SkipsTarget(
        [Instance] ClassMethodTargets instance,
        [Field("foo")] ref int field,
        [Parameter("value")] ref int argument,
        [ReturnValue] ref int result)
    {
        InstanceObserved = instance;
        field = 41;
        argument = 42;
        FirstObserved = argument;
        result = 43;
        return false;
    }

    [InnerPrefix(typeof(InnerStaticMethodTargets), nameof(InnerStaticMethodTargets.RefIntArgument))]
    [Target(typeof(OuterStaticMethodTargets), nameof(OuterStaticMethodTargets.SameNamedRefArgument))]
    public static void InnerPrefix_CombinedScopes_SameNamedRefArgument_WritesInnerOnly(
        [Parameter("value", Scope.Outer)] int outerValue,
        [Parameter("value", Scope.Inner)] ref int innerValue)
    {
        FirstObserved = outerValue;
        innerValue = 42;
    }
}

[TestFixture]
public sealed class PatchMethodCombinationTests : PatchTestBase
{
    [Test]
    public void Prefix_ExplicitParameterBinding_ReservedName_Result_BindsArgument()
    {
        PatchMethodCombinationPatches.FirstObserved = 0;
        ApplyPatch(
            typeof(PatchMethodCombinationPatches),
            nameof(PatchMethodCombinationPatches.Prefix_ExplicitParameterBinding_ReservedName_Result_BindsArgument));

        StaticMethodTargets.IntArgument(42);

        Assert.That(PatchMethodCombinationPatches.FirstObserved, Is.EqualTo(42));
    }

    [Test]
    public void Prefix_DuplicateArgumentBinding_ReadByValueThenWriteByReference()
    {
        PatchMethodCombinationPatches.FirstObserved = 0;
        ApplyPatch(
            typeof(PatchMethodCombinationPatches),
            nameof(PatchMethodCombinationPatches.Prefix_DuplicateArgumentBinding_ReadByValueThenWriteByReference));

        int result = StaticMethodTargets.IntIdentity(1);

        Assert.That(PatchMethodCombinationPatches.FirstObserved, Is.EqualTo(1));
        Assert.That(result, Is.EqualTo(42));
    }

    [Test]
    public void Postfix_DuplicateResultBinding_ReadByValueThenWriteByReference()
    {
        PatchMethodCombinationPatches.FirstObserved = 0;
        ApplyPatch(
            typeof(PatchMethodCombinationPatches),
            nameof(PatchMethodCombinationPatches.Postfix_DuplicateResultBinding_ReadByValueThenWriteByReference));

        int result = StaticMethodTargets.IntResult();

        Assert.That(PatchMethodCombinationPatches.FirstObserved, Is.EqualTo(1));
        Assert.That(result, Is.EqualTo(42));
    }

    [Test]
    public void Prefix_DuplicateResultBinding_ReadByValueThenWriteByReference_SkipsTarget()
    {
        PatchMethodCombinationPatches.FirstObserved = -1;
        ApplyPatch(
            typeof(PatchMethodCombinationPatches),
            nameof(PatchMethodCombinationPatches.Prefix_DuplicateResultBinding_ReadByValueThenWriteByReference_SkipsTarget));

        int result = StaticMethodTargets.ThrowingIntResult();

        Assert.That(PatchMethodCombinationPatches.FirstObserved, Is.Zero);
        Assert.That(result, Is.EqualTo(42));
    }

    [Test]
    public void InnerPostfix_DuplicateResultBinding_ReadByValueThenWriteByReference()
    {
        PatchMethodCombinationPatches.FirstObserved = 0;
        ApplyPatch(
            typeof(PatchMethodCombinationPatches),
            nameof(PatchMethodCombinationPatches.InnerPostfix_DuplicateResultBinding_ReadByValueThenWriteByReference));

        int result = OuterStaticMethodTargets.IntResult();

        Assert.That(PatchMethodCombinationPatches.FirstObserved, Is.EqualTo(1));
        Assert.That(result, Is.EqualTo(42));
    }

    [Test]
    public void InnerPrefix_DuplicateResultBinding_ReadByValueThenWriteByReference_SkipsTarget()
    {
        PatchMethodCombinationPatches.FirstObserved = -1;
        ApplyPatch(
            typeof(PatchMethodCombinationPatches),
            nameof(PatchMethodCombinationPatches.InnerPrefix_DuplicateResultBinding_ReadByValueThenWriteByReference_SkipsTarget));

        int result = OuterStaticMethodTargets.IntResult();

        Assert.That(PatchMethodCombinationPatches.FirstObserved, Is.Zero);
        Assert.That(result, Is.EqualTo(42));
    }

    [Test]
    public void Prefix_CombinedBindings_InstanceFieldArgumentResult_SkipsTarget()
    {
        PatchMethodCombinationPatches.FirstObserved = 0;
        PatchMethodCombinationPatches.InstanceObserved = null;
        var target = new ClassMethodTargets { foo = 1 };
        ApplyPatch(
            typeof(PatchMethodCombinationPatches),
            nameof(PatchMethodCombinationPatches.Prefix_CombinedBindings_InstanceFieldArgumentResult_SkipsTarget));

        int result = target.IntIdentity(2);

        Assert.That(PatchMethodCombinationPatches.InstanceObserved, Is.SameAs(target));
        Assert.That(PatchMethodCombinationPatches.FirstObserved, Is.EqualTo(42));
        Assert.That(target.foo, Is.EqualTo(41));
        Assert.That(target.Value, Is.Zero);
        Assert.That(result, Is.EqualTo(43));
    }

    [Test]
    public void InnerPrefix_CombinedScopes_SameNamedRefArgument_WritesInnerOnly()
    {
        PatchMethodCombinationPatches.FirstObserved = 0;
        ApplyPatch(
            typeof(PatchMethodCombinationPatches),
            nameof(PatchMethodCombinationPatches.InnerPrefix_CombinedScopes_SameNamedRefArgument_WritesInnerOnly));
        int outerValue = 7;

        int result = OuterStaticMethodTargets.SameNamedRefArgument(ref outerValue);

        Assert.That(PatchMethodCombinationPatches.FirstObserved, Is.EqualTo(7));
        Assert.That(outerValue, Is.EqualTo(7));
        Assert.That(result, Is.EqualTo(42));
    }
}
