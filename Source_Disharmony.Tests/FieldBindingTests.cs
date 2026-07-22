namespace Disharmony.Tests;

[TestFixture]
public sealed partial class FieldBindingTests : PatchTestBase
{
    [Test]
    public void TripleUnderscoreParameterCanReadOuterInstanceField()
    {
        InnerParameterBindingPatchMethods.FieldObserved = 0;
        ApplyPatch(typeof(InnerParameterBindingPatchMethods), nameof(InnerParameterBindingPatchMethods.ReadOuterFieldPrefix));
        var outer = new ClassMethodTargets { foo = 42 };

        outer.CallInnerWithoutField(new InstanceMethodTargetsWithoutFields());

        Assert.That(InnerParameterBindingPatchMethods.FieldObserved, Is.EqualTo(42));
    }

    [Test]
    public void TripleUnderscoreParameterPrefersInnerInstanceField()
    {
        InnerParameterBindingPatchMethods.FieldObserved = 0;
        ApplyPatch(typeof(InnerParameterBindingPatchMethods), nameof(InnerParameterBindingPatchMethods.ReadInnerFieldPostfix));
        var outer = new ClassMethodTargets { foo = 1 };
        var inner = new InnerInstanceMethodTargets { foo = 42 };

        outer.CallInnerWithField(inner);

        Assert.That(InnerParameterBindingPatchMethods.FieldObserved, Is.EqualTo(42));
    }

    [Test]
    public void TripleUnderscoreParameterCanWriteOuterInstanceFieldByReference()
    {
        ApplyPatch(typeof(InnerParameterBindingPatchMethods), nameof(InnerParameterBindingPatchMethods.WriteOuterFieldPrefix));
        var outer = new ClassMethodTargets { foo = 1 };

        outer.CallInnerWithoutField(new InstanceMethodTargetsWithoutFields());

        Assert.That(outer.foo, Is.EqualTo(42));
    }

    [Test]
    public void TripleUnderscoreParameterCanWriteInnerInstanceFieldByReference()
    {
        ApplyPatch(typeof(InnerParameterBindingPatchMethods), nameof(InnerParameterBindingPatchMethods.WriteInnerFieldPostfix));
        var outer = new ClassMethodTargets { foo = 1 };
        var inner = new InnerInstanceMethodTargets { foo = 1 };

        outer.CallInnerWithField(inner);

        Assert.That(inner.foo, Is.EqualTo(42));
        Assert.That(outer.foo, Is.EqualTo(1));
    }

    [Test]
    public void TripleUnderscoreParameterCanReadOuterStructField()
    {
        InnerParameterBindingPatchMethods.FieldObserved = 0;
        ApplyPatch(typeof(InnerParameterBindingPatchMethods), nameof(InnerParameterBindingPatchMethods.ReadOuterStructFieldPrefix));
        var outer = new StructMethodTargets { foo = 42 };

        outer.CallInnerWithoutField(new InstanceMethodTargetsWithoutFields());

        Assert.That(InnerParameterBindingPatchMethods.FieldObserved, Is.EqualTo(42));
    }

    [Test]
    public void TripleUnderscoreParameterPrefersInnerStructField()
    {
        InnerParameterBindingPatchMethods.FieldObserved = 0;
        ApplyPatch(typeof(InnerParameterBindingPatchMethods), nameof(InnerParameterBindingPatchMethods.ReadInnerStructFieldPostfix));
        var outer = new StructMethodTargets { foo = 1 };
        var inner = new InnerStructMethodTargets { foo = 42 };

        outer.CallInnerWithField(ref inner);

        Assert.That(InnerParameterBindingPatchMethods.FieldObserved, Is.EqualTo(42));
    }

    [Test]
    public void TripleUnderscoreParameterCanWriteOuterStructFieldByReference()
    {
        ApplyPatch(typeof(InnerParameterBindingPatchMethods), nameof(InnerParameterBindingPatchMethods.WriteOuterStructFieldPrefix));
        var outer = new StructMethodTargets { foo = 1 };

        outer.CallInnerWithoutField(new InstanceMethodTargetsWithoutFields());

        Assert.That(outer.foo, Is.EqualTo(42));
    }

    [Test]
    public void TripleUnderscoreParameterCanWriteInnerStructFieldByReference()
    {
        InnerStructMethodTargets.FieldObserved = 0;
        ApplyPatch(typeof(InnerParameterBindingPatchMethods), nameof(InnerParameterBindingPatchMethods.WriteInnerStructFieldPrefix));
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
        ApplyPatch(typeof(InnerParameterBindingPatchMethods), nameof(InnerParameterBindingPatchMethods.WriteInnerStructFieldPassedByValuePrefix));
        var outer = new StructMethodTargets { foo = 1 };
        var inner = new InnerStructMethodTargets { foo = 1 };

        outer.CallInnerWithFieldByValue(inner);

        Assert.That(InnerStructMethodTargets.FieldObserved, Is.EqualTo(42));
        Assert.That(inner.foo, Is.EqualTo(1));
        Assert.That(outer.foo, Is.EqualTo(1));
    }
}
