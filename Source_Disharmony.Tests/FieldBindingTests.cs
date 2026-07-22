namespace Disharmony.Tests;

public static class FieldBindingPatches
{
    public static int Observed;

    [InnerPrefix(typeof(InstanceMethodTargetsWithoutFields), nameof(InstanceMethodTargetsWithoutFields.Void))]
    [Target(typeof(ClassMethodTargets), nameof(ClassMethodTargets.CallInnerWithoutField))]
    public static void TripleUnderscoreParameterCanReadOuterInstanceField(int ___foo) => Observed = ___foo;

    [InnerPostfix(typeof(InnerInstanceMethodTargets), nameof(InnerInstanceMethodTargets.Void))]
    [Target(typeof(ClassMethodTargets), nameof(ClassMethodTargets.CallInnerWithField))]
    public static void TripleUnderscoreParameterPrefersInnerInstanceField(int ___foo) => Observed = ___foo;

    [InnerPrefix(typeof(InstanceMethodTargetsWithoutFields), nameof(InstanceMethodTargetsWithoutFields.Void))]
    [Target(typeof(ClassMethodTargets), nameof(ClassMethodTargets.CallInnerWithoutField))]
    public static void TripleUnderscoreParameterCanWriteOuterInstanceFieldByReference(ref int ___foo) => ___foo = 42;

    [InnerPostfix(typeof(InnerInstanceMethodTargets), nameof(InnerInstanceMethodTargets.Void))]
    [Target(typeof(ClassMethodTargets), nameof(ClassMethodTargets.CallInnerWithField))]
    public static void TripleUnderscoreParameterCanWriteInnerInstanceFieldByReference(ref int ___foo) => ___foo = 42;

    [InnerPrefix(typeof(InstanceMethodTargetsWithoutFields), nameof(InstanceMethodTargetsWithoutFields.Void))]
    [Target(typeof(StructMethodTargets), nameof(StructMethodTargets.CallInnerWithoutField))]
    public static void TripleUnderscoreParameterCanReadOuterStructField(int ___foo) => Observed = ___foo;

    [InnerPostfix(typeof(InnerStructMethodTargets), nameof(InnerStructMethodTargets.Void))]
    [Target(typeof(StructMethodTargets), nameof(StructMethodTargets.CallInnerWithField))]
    public static void TripleUnderscoreParameterPrefersInnerStructField(int ___foo) => Observed = ___foo;

    [InnerPrefix(typeof(InstanceMethodTargetsWithoutFields), nameof(InstanceMethodTargetsWithoutFields.Void))]
    [Target(typeof(StructMethodTargets), nameof(StructMethodTargets.CallInnerWithoutField))]
    public static void TripleUnderscoreParameterCanWriteOuterStructFieldByReference(ref int ___foo) => ___foo = 42;

    [InnerPrefix(typeof(InnerStructMethodTargets), nameof(InnerStructMethodTargets.Void))]
    [Target(typeof(StructMethodTargets), nameof(StructMethodTargets.CallInnerWithField))]
    public static void TripleUnderscoreParameterCanWriteInnerStructFieldByReference(ref int ___foo) => ___foo = 42;

    [InnerPrefix(typeof(InnerStructMethodTargets), nameof(InnerStructMethodTargets.Void))]
    [Target(typeof(StructMethodTargets), nameof(StructMethodTargets.CallInnerWithFieldByValue))]
    public static void TripleUnderscoreParameterCanWriteFieldOfInnerStructPassedByValue(ref int ___foo) => ___foo = 42;

    [Prefix]
    [Target(typeof(ClassMethodTargets), nameof(ClassMethodTargets.Void))]
    public static void FieldAttributeBindsWritableInstanceField([Field("foo")] ref int field) => field = 42;

    [InnerPrefix(typeof(InnerInstanceMethodTargets), nameof(InnerInstanceMethodTargets.Void))]
    [Target(typeof(ClassMethodTargets), nameof(ClassMethodTargets.CallInnerWithField))]
    public static void FieldAttributeCanSelectOuterInstanceField([Field("foo", Scope.Outer)] int field) => Observed = field;

    [InnerPrefix(typeof(InnerInstanceMethodTargets), nameof(InnerInstanceMethodTargets.Void))]
    [Target(typeof(ClassMethodTargets), nameof(ClassMethodTargets.CallInnerWithField))]
    public static void FieldAttributeCanSelectInnerInstanceField([Field("foo", Scope.Inner)] int field) => Observed = field;
}

[TestFixture]
public sealed partial class FieldBindingTests : PatchTestBase
{
    [Test]
    public void TripleUnderscoreParameterCanReadOuterInstanceField()
    {
        FieldBindingPatches.Observed = 0;
        ApplyPatch(typeof(FieldBindingPatches), nameof(FieldBindingPatches.TripleUnderscoreParameterCanReadOuterInstanceField));
        var outer = new ClassMethodTargets { foo = 42 };

        outer.CallInnerWithoutField(new InstanceMethodTargetsWithoutFields());

        Assert.That(FieldBindingPatches.Observed, Is.EqualTo(42));
    }

    [Test]
    public void TripleUnderscoreParameterPrefersInnerInstanceField()
    {
        FieldBindingPatches.Observed = 0;
        ApplyPatch(typeof(FieldBindingPatches), nameof(FieldBindingPatches.TripleUnderscoreParameterPrefersInnerInstanceField));
        var outer = new ClassMethodTargets { foo = 1 };
        var inner = new InnerInstanceMethodTargets { foo = 42 };

        outer.CallInnerWithField(inner);

        Assert.That(FieldBindingPatches.Observed, Is.EqualTo(42));
    }

    [Test]
    public void TripleUnderscoreParameterCanWriteOuterInstanceFieldByReference()
    {
        ApplyPatch(typeof(FieldBindingPatches), nameof(FieldBindingPatches.TripleUnderscoreParameterCanWriteOuterInstanceFieldByReference));
        var outer = new ClassMethodTargets { foo = 1 };

        outer.CallInnerWithoutField(new InstanceMethodTargetsWithoutFields());

        Assert.That(outer.foo, Is.EqualTo(42));
    }

    [Test]
    public void TripleUnderscoreParameterCanWriteInnerInstanceFieldByReference()
    {
        ApplyPatch(typeof(FieldBindingPatches), nameof(FieldBindingPatches.TripleUnderscoreParameterCanWriteInnerInstanceFieldByReference));
        var outer = new ClassMethodTargets { foo = 1 };
        var inner = new InnerInstanceMethodTargets { foo = 1 };

        outer.CallInnerWithField(inner);

        Assert.That(inner.foo, Is.EqualTo(42));
        Assert.That(outer.foo, Is.EqualTo(1));
    }

    [Test]
    public void TripleUnderscoreParameterCanReadOuterStructField()
    {
        FieldBindingPatches.Observed = 0;
        ApplyPatch(typeof(FieldBindingPatches), nameof(FieldBindingPatches.TripleUnderscoreParameterCanReadOuterStructField));
        var outer = new StructMethodTargets { foo = 42 };

        outer.CallInnerWithoutField(new InstanceMethodTargetsWithoutFields());

        Assert.That(FieldBindingPatches.Observed, Is.EqualTo(42));
    }

    [Test]
    public void TripleUnderscoreParameterPrefersInnerStructField()
    {
        FieldBindingPatches.Observed = 0;
        ApplyPatch(typeof(FieldBindingPatches), nameof(FieldBindingPatches.TripleUnderscoreParameterPrefersInnerStructField));
        var outer = new StructMethodTargets { foo = 1 };
        var inner = new InnerStructMethodTargets { foo = 42 };

        outer.CallInnerWithField(ref inner);

        Assert.That(FieldBindingPatches.Observed, Is.EqualTo(42));
    }

    [Test]
    public void TripleUnderscoreParameterCanWriteOuterStructFieldByReference()
    {
        ApplyPatch(typeof(FieldBindingPatches), nameof(FieldBindingPatches.TripleUnderscoreParameterCanWriteOuterStructFieldByReference));
        var outer = new StructMethodTargets { foo = 1 };

        outer.CallInnerWithoutField(new InstanceMethodTargetsWithoutFields());

        Assert.That(outer.foo, Is.EqualTo(42));
    }

    [Test]
    public void TripleUnderscoreParameterCanWriteInnerStructFieldByReference()
    {
        InnerStructMethodTargets.FieldObserved = 0;
        ApplyPatch(typeof(FieldBindingPatches), nameof(FieldBindingPatches.TripleUnderscoreParameterCanWriteInnerStructFieldByReference));
        var outer = new StructMethodTargets { foo = 1 };
        var inner = new InnerStructMethodTargets { foo = 1 };

        outer.CallInnerWithField(ref inner);

        Assert.That(InnerStructMethodTargets.FieldObserved, Is.EqualTo(42));
        Assert.That(inner.foo, Is.EqualTo(42));
        Assert.That(outer.foo, Is.EqualTo(1));
    }

    [Test]
    public void TripleUnderscoreParameterCanWriteFieldOfInnerStructPassedByValue()
    {
        InnerStructMethodTargets.FieldObserved = 0;
        ApplyPatch(typeof(FieldBindingPatches), nameof(FieldBindingPatches.TripleUnderscoreParameterCanWriteFieldOfInnerStructPassedByValue));
        var outer = new StructMethodTargets { foo = 1 };
        var inner = new InnerStructMethodTargets { foo = 1 };

        outer.CallInnerWithFieldByValue(inner);

        Assert.That(InnerStructMethodTargets.FieldObserved, Is.EqualTo(42));
        Assert.That(inner.foo, Is.EqualTo(1));
        Assert.That(outer.foo, Is.EqualTo(1));
    }

    [Test]
    public void FieldAttributeBindsWritableInstanceField()
    {
        ApplyPatch(typeof(FieldBindingPatches), nameof(FieldBindingPatches.FieldAttributeBindsWritableInstanceField));
        var target = new ClassMethodTargets { foo = 1 };

        target.Void();

        Assert.That(target.foo, Is.EqualTo(42));
    }

    [Test]
    public void FieldAttributeCanSelectOuterInstanceField()
    {
        FieldBindingPatches.Observed = 0;
        ApplyPatch(typeof(FieldBindingPatches), nameof(FieldBindingPatches.FieldAttributeCanSelectOuterInstanceField));
        var outer = new ClassMethodTargets { foo = 1 };
        var inner = new InnerInstanceMethodTargets { foo = 42 };

        outer.CallInnerWithField(inner);

        Assert.That(FieldBindingPatches.Observed, Is.EqualTo(1));
    }

    [Test]
    public void FieldAttributeCanSelectInnerInstanceField()
    {
        FieldBindingPatches.Observed = 0;
        ApplyPatch(typeof(FieldBindingPatches), nameof(FieldBindingPatches.FieldAttributeCanSelectInnerInstanceField));
        var outer = new ClassMethodTargets { foo = 1 };
        var inner = new InnerInstanceMethodTargets { foo = 42 };

        outer.CallInnerWithField(inner);

        Assert.That(FieldBindingPatches.Observed, Is.EqualTo(42));
    }
}
