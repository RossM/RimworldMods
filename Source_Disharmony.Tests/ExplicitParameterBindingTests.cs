using NUnit.Framework;

namespace Disharmony.Tests;

public static class ExplicitBindingPatchMethods
{
    public static int ArgumentObserved;
    public static object? InstanceObserved;
    public static int StateObserved;
    public static int FieldObserved;
    public static int StructInstanceFieldObserved;

    [Prefix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.IntIdentity))]
    public static void WriteArgumentByIndex([Parameter(0)] ref int replacement) => replacement = 42;

    [InnerPrefix(typeof(InnerStaticMethodTargets), nameof(InnerStaticMethodTargets.IntArgument))]
    [Target(typeof(OuterStaticMethodTargets), nameof(OuterStaticMethodTargets.SameNamedArgument))]
    public static void ReadOuterArgument([Parameter("value", Scope.Outer)] int outerValue) => ArgumentObserved = outerValue;

    [InnerPrefix(typeof(InnerStaticMethodTargets), nameof(InnerStaticMethodTargets.IntArgument))]
    [Target(typeof(OuterStaticMethodTargets), nameof(OuterStaticMethodTargets.SameNamedArgument))]
    public static void ReadInnerArgument([Parameter("value", Scope.Inner)] int innerValue) => ArgumentObserved = innerValue;

    [Prefix]
    [Target(typeof(ClassMethodTargets), nameof(ClassMethodTargets.Void))]
    public static void ReadInstance([Instance] ClassMethodTargets target) => InstanceObserved = target;

    [Prefix]
    [Target(typeof(StructMethodTargets), nameof(StructMethodTargets.IntResult))]
    public static void WriteStructInstance([Instance] ref StructMethodTargets target) => target.foo = 42;

    [Postfix]
    [Target(typeof(StructMethodTargets), nameof(StructMethodTargets.IntResult))]
    public static void ReadStructInstance([Instance] StructMethodTargets target) => StructInstanceFieldObserved = target.foo;

    [InnerPrefix(typeof(InnerInstanceMethodTargets), nameof(InnerInstanceMethodTargets.Void))]
    [Target(typeof(ClassMethodTargets), nameof(ClassMethodTargets.CallInnerWithField))]
    public static void ReadOuterInstance([Instance(Scope.Outer)] ClassMethodTargets target) => InstanceObserved = target;

    [Postfix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.IntResult))]
    public static void WriteReturnValue([ReturnValue] ref int value) => value = 42;

    [Prefix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.Void))]
    public static void WriteState([State] out int state) => state = 42;

    [Postfix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.Void))]
    public static void ReadState([State] int state) => StateObserved = state;

    [Prefix]
    [Target(typeof(ClassMethodTargets), nameof(ClassMethodTargets.Void))]
    public static void WriteField([Field("foo")] ref int field) => field = 42;

    [InnerPrefix(typeof(InnerInstanceMethodTargets), nameof(InnerInstanceMethodTargets.Void))]
    [Target(typeof(ClassMethodTargets), nameof(ClassMethodTargets.CallInnerWithField))]
    public static void ReadOuterField([Field("foo", Scope.Outer)] int field) => FieldObserved = field;

    [InnerPrefix(typeof(InnerInstanceMethodTargets), nameof(InnerInstanceMethodTargets.Void))]
    [Target(typeof(ClassMethodTargets), nameof(ClassMethodTargets.CallInnerWithField))]
    public static void ReadInnerField([Field("foo", Scope.Inner)] int field) => FieldObserved = field;
}

public sealed partial class ArgumentBindingTests
{
    [Test]
    public void ParameterAttributeCanBindWritableArgumentByIndex()
    {
        ApplyPatch(typeof(ExplicitBindingPatchMethods), nameof(ExplicitBindingPatchMethods.WriteArgumentByIndex));

        int result = StaticMethodTargets.IntIdentity(1);

        Assert.That(result, Is.EqualTo(42));
    }

    [Test]
    public void ParameterAttributeCanSelectOuterArgumentByName()
    {
        ExplicitBindingPatchMethods.ArgumentObserved = 0;
        ApplyPatch(typeof(ExplicitBindingPatchMethods), nameof(ExplicitBindingPatchMethods.ReadOuterArgument));

        OuterStaticMethodTargets.SameNamedArgument(1);

        Assert.That(ExplicitBindingPatchMethods.ArgumentObserved, Is.EqualTo(1));
    }

    [Test]
    public void ParameterAttributeCanSelectInnerArgumentByName()
    {
        ExplicitBindingPatchMethods.ArgumentObserved = 0;
        ApplyPatch(typeof(ExplicitBindingPatchMethods), nameof(ExplicitBindingPatchMethods.ReadInnerArgument));

        OuterStaticMethodTargets.SameNamedArgument(1);

        Assert.That(ExplicitBindingPatchMethods.ArgumentObserved, Is.EqualTo(42));
    }
}

public sealed partial class InstanceBindingTests
{
    [Test]
    public void InstanceAttributeBindsPatchedMethodInstance()
    {
        ExplicitBindingPatchMethods.InstanceObserved = null;
        ApplyPatch(typeof(ExplicitBindingPatchMethods), nameof(ExplicitBindingPatchMethods.ReadInstance));
        var target = new ClassMethodTargets();

        target.Void();

        Assert.That(ExplicitBindingPatchMethods.InstanceObserved, Is.SameAs(target));
    }

    [Test]
    public void InstanceAttributeCanSelectOuterInstance()
    {
        ExplicitBindingPatchMethods.InstanceObserved = null;
        ApplyPatch(typeof(ExplicitBindingPatchMethods), nameof(ExplicitBindingPatchMethods.ReadOuterInstance));
        var outer = new ClassMethodTargets();

        outer.CallInnerWithField(new InnerInstanceMethodTargets());

        Assert.That(ExplicitBindingPatchMethods.InstanceObserved, Is.SameAs(outer));
    }

    [Test]
    public void InstanceAttributeCanWriteStructInstanceByReference()
    {
        ApplyPatch(typeof(ExplicitBindingPatchMethods), nameof(ExplicitBindingPatchMethods.WriteStructInstance));
        var target = new StructMethodTargets { foo = 1 };

        target.IntResult();

        Assert.That(target.foo, Is.EqualTo(42));
    }

    [Test]
    public void InstanceAttributeCanReadStructInstanceByValue()
    {
        ExplicitBindingPatchMethods.StructInstanceFieldObserved = 0;
        ApplyPatch(typeof(ExplicitBindingPatchMethods), nameof(ExplicitBindingPatchMethods.ReadStructInstance));
        var target = new StructMethodTargets { foo = 42 };

        target.IntResult();

        Assert.That(ExplicitBindingPatchMethods.StructInstanceFieldObserved, Is.EqualTo(42));
    }
}

public sealed partial class ResultBindingTests
{
    [Test]
    public void ReturnValueAttributeBindsWritableResult()
    {
        ApplyPatch(typeof(ExplicitBindingPatchMethods), nameof(ExplicitBindingPatchMethods.WriteReturnValue));

        Assert.That(StaticMethodTargets.IntResult(), Is.EqualTo(42));
    }
}

public sealed partial class StateBindingTests
{
    [Test]
    public void StateAttributeSharesStateBetweenPrefixAndPostfix()
    {
        ExplicitBindingPatchMethods.StateObserved = 0;
        ApplyPatch(typeof(ExplicitBindingPatchMethods), nameof(ExplicitBindingPatchMethods.WriteState));
        ApplyPatch(typeof(ExplicitBindingPatchMethods), nameof(ExplicitBindingPatchMethods.ReadState));

        StaticMethodTargets.Void();

        Assert.That(ExplicitBindingPatchMethods.StateObserved, Is.EqualTo(42));
    }
}

public sealed partial class FieldBindingTests
{
    [Test]
    public void FieldAttributeBindsWritableInstanceField()
    {
        ApplyPatch(typeof(ExplicitBindingPatchMethods), nameof(ExplicitBindingPatchMethods.WriteField));
        var target = new ClassMethodTargets { foo = 1 };

        target.Void();

        Assert.That(target.foo, Is.EqualTo(42));
    }

    [Test]
    public void FieldAttributeCanSelectOuterInstanceField()
    {
        ExplicitBindingPatchMethods.FieldObserved = 0;
        ApplyPatch(typeof(ExplicitBindingPatchMethods), nameof(ExplicitBindingPatchMethods.ReadOuterField));
        var outer = new ClassMethodTargets { foo = 1 };
        var inner = new InnerInstanceMethodTargets { foo = 42 };

        outer.CallInnerWithField(inner);

        Assert.That(ExplicitBindingPatchMethods.FieldObserved, Is.EqualTo(1));
    }

    [Test]
    public void FieldAttributeCanSelectInnerInstanceField()
    {
        ExplicitBindingPatchMethods.FieldObserved = 0;
        ApplyPatch(typeof(ExplicitBindingPatchMethods), nameof(ExplicitBindingPatchMethods.ReadInnerField));
        var outer = new ClassMethodTargets { foo = 1 };
        var inner = new InnerInstanceMethodTargets { foo = 42 };

        outer.CallInnerWithField(inner);

        Assert.That(ExplicitBindingPatchMethods.FieldObserved, Is.EqualTo(42));
    }
}
