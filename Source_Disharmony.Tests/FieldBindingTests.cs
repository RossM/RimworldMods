namespace Disharmony.Tests;

public static class FieldBindingPatches
{
    public static int Observed;
    public static BindingReference? ReferenceObserved;
    public static BindingStruct StructObserved;

    [Prefix]
    [Target(typeof(ClassMethodTargets), nameof(ClassMethodTargets.Void))]
    public static void TripleUnderscoreParameterCanReadPrimitiveField(int ___primitiveField) => Observed = ___primitiveField;

    [Prefix]
    [Target(typeof(ClassMethodTargets), nameof(ClassMethodTargets.Void))]
    public static void TripleUnderscoreParameterCanWritePrimitiveFieldByReference(ref int ___primitiveField) => ___primitiveField = 42;

    [Prefix]
    [Target(typeof(ClassMethodTargets), nameof(ClassMethodTargets.Void))]
    public static void TripleUnderscoreParameterCanReadPrimitiveFieldThroughReference(ref int ___primitiveField) =>
        Observed = ___primitiveField;

    [Prefix]
    [Target(typeof(ClassMethodTargets), nameof(ClassMethodTargets.Void))]
    public static void TripleUnderscoreParameterCanReadReferenceTypeField(BindingReference ___referenceField) =>
        ReferenceObserved = ___referenceField;

    [Prefix]
    [Target(typeof(ClassMethodTargets), nameof(ClassMethodTargets.Void))]
    public static void TripleUnderscoreParameterCanWriteReferenceTypeFieldByReference(ref BindingReference ___referenceField) =>
        ___referenceField = new BindingReference { Value = 42 };

    [Prefix]
    [Target(typeof(ClassMethodTargets), nameof(ClassMethodTargets.Void))]
    public static void TripleUnderscoreParameterCanReadReferenceTypeFieldThroughReference(
        ref BindingReference ___referenceField) => ReferenceObserved = ___referenceField;

    [Prefix]
    [Target(typeof(ClassMethodTargets), nameof(ClassMethodTargets.Void))]
    public static void TripleUnderscoreParameterCanReadStructField(BindingStruct ___structField) => StructObserved = ___structField;

    [Prefix]
    [Target(typeof(ClassMethodTargets), nameof(ClassMethodTargets.Void))]
    public static void TripleUnderscoreParameterCanWriteStructFieldByReference(ref BindingStruct ___structField) =>
        ___structField = new BindingStruct { Value = 42 };

    [Prefix]
    [Target(typeof(ClassMethodTargets), nameof(ClassMethodTargets.Void))]
    public static void TripleUnderscoreParameterCanReadStructFieldThroughReference(ref BindingStruct ___structField) =>
        StructObserved = ___structField;

    [InnerPrefix(typeof(InstanceMethodTargetsWithoutFields), nameof(InstanceMethodTargetsWithoutFields.Void))]
    [Target(typeof(ClassMethodTargets), nameof(ClassMethodTargets.CallInnerWithoutField))]
    public static void TripleUnderscoreParameterCanReadOuterInstanceField(int ___foo) => Observed = ___foo;

    [InnerPostfix(typeof(InnerInstanceMethodTargets), nameof(InnerInstanceMethodTargets.Void))]
    [Target(typeof(ClassMethodTargets), nameof(ClassMethodTargets.CallInnerWithField))]
    public static void TripleUnderscoreParameterPrefersInnerInstanceField(int ___foo) => Observed = ___foo;

    [InnerPrefix(typeof(InstanceMethodTargetsWithoutFields), nameof(InstanceMethodTargetsWithoutFields.Void))]
    [Target(typeof(ClassMethodTargets), nameof(ClassMethodTargets.CallInnerWithoutField))]
    public static void TripleUnderscoreParameterCanWriteOuterInstanceFieldByReference(ref int ___foo) => ___foo = 42;

    [InnerPrefix(typeof(InstanceMethodTargetsWithoutFields), nameof(InstanceMethodTargetsWithoutFields.Void))]
    [Target(typeof(ClassMethodTargets), nameof(ClassMethodTargets.CallInnerWithoutField))]
    public static void TripleUnderscoreParameterCanReadOuterInstanceFieldThroughReference(ref int ___foo) => Observed = ___foo;

    [InnerPostfix(typeof(InnerInstanceMethodTargets), nameof(InnerInstanceMethodTargets.Void))]
    [Target(typeof(ClassMethodTargets), nameof(ClassMethodTargets.CallInnerWithField))]
    public static void TripleUnderscoreParameterCanWriteInnerInstanceFieldByReference(ref int ___foo) => ___foo = 42;

    [InnerPrefix(typeof(InnerInstanceMethodTargets), nameof(InnerInstanceMethodTargets.Void))]
    [Target(typeof(ClassMethodTargets), nameof(ClassMethodTargets.CallInnerWithField))]
    public static void TripleUnderscoreParameterCanReadInnerInstanceFieldThroughReference(ref int ___foo) => Observed = ___foo;

    [InnerPrefix(typeof(InstanceMethodTargetsWithoutFields), nameof(InstanceMethodTargetsWithoutFields.Void))]
    [Target(typeof(StructMethodTargets), nameof(StructMethodTargets.CallInnerWithoutField))]
    public static void TripleUnderscoreParameterCanReadOuterStructField(int ___foo) => Observed = ___foo;

    [InnerPostfix(typeof(InnerStructMethodTargets), nameof(InnerStructMethodTargets.Void))]
    [Target(typeof(StructMethodTargets), nameof(StructMethodTargets.CallInnerWithField))]
    public static void TripleUnderscoreParameterPrefersInnerStructField(int ___foo) => Observed = ___foo;

    [InnerPrefix(typeof(InstanceMethodTargetsWithoutFields), nameof(InstanceMethodTargetsWithoutFields.Void))]
    [Target(typeof(StructMethodTargets), nameof(StructMethodTargets.CallInnerWithoutField))]
    public static void TripleUnderscoreParameterCanWriteOuterStructFieldByReference(ref int ___foo) => ___foo = 42;

    [InnerPrefix(typeof(InstanceMethodTargetsWithoutFields), nameof(InstanceMethodTargetsWithoutFields.Void))]
    [Target(typeof(StructMethodTargets), nameof(StructMethodTargets.CallInnerWithoutField))]
    public static void TripleUnderscoreParameterCanReadOuterStructFieldThroughReference(ref int ___foo) => Observed = ___foo;

    [InnerPrefix(typeof(InnerStructMethodTargets), nameof(InnerStructMethodTargets.Void))]
    [Target(typeof(StructMethodTargets), nameof(StructMethodTargets.CallInnerWithField))]
    public static void TripleUnderscoreParameterCanWriteInnerStructFieldByReference(ref int ___foo) => ___foo = 42;

    [InnerPrefix(typeof(InnerStructMethodTargets), nameof(InnerStructMethodTargets.Void))]
    [Target(typeof(StructMethodTargets), nameof(StructMethodTargets.CallInnerWithField))]
    public static void TripleUnderscoreParameterCanReadInnerStructFieldThroughReference(ref int ___foo) => Observed = ___foo;

    [InnerPrefix(typeof(InnerStructMethodTargets), nameof(InnerStructMethodTargets.Void))]
    [Target(typeof(StructMethodTargets), nameof(StructMethodTargets.CallInnerWithFieldByValue))]
    public static void TripleUnderscoreParameterCanWriteFieldOfInnerStructPassedByValue(ref int ___foo) => ___foo = 42;

    [InnerPrefix(typeof(InnerStructMethodTargets), nameof(InnerStructMethodTargets.Void))]
    [Target(typeof(StructMethodTargets), nameof(StructMethodTargets.CallInnerWithFieldByValue))]
    public static void TripleUnderscoreParameterCanReadFieldOfInnerStructPassedByValueThroughReference(ref int ___foo) =>
        Observed = ___foo;

    [Prefix]
    [Target(typeof(ClassMethodTargets), nameof(ClassMethodTargets.Void))]
    public static void FieldAttributeBindsWritableInstanceField([Field("foo")] ref int field) => field = 42;

    [Prefix]
    [Target(typeof(ClassMethodTargets), nameof(ClassMethodTargets.Void))]
    public static void FieldAttributeCanReadPrimitiveFieldThroughReference([Field("foo")] ref int field) => Observed = field;

    [Prefix]
    [Target(typeof(ClassMethodTargets), nameof(ClassMethodTargets.Void))]
    public static void FieldAttributeCanReadPrimitiveField([Field("primitiveField")] int field) => Observed = field;

    [Prefix]
    [Target(typeof(ClassMethodTargets), nameof(ClassMethodTargets.Void))]
    public static void FieldAttributeCanReadReferenceTypeField([Field("referenceField")] BindingReference field) =>
        ReferenceObserved = field;

    [Prefix]
    [Target(typeof(ClassMethodTargets), nameof(ClassMethodTargets.Void))]
    public static void FieldAttributeCanWriteReferenceTypeFieldByReference([Field("referenceField")] ref BindingReference field) =>
        field = new BindingReference { Value = 42 };

    [Prefix]
    [Target(typeof(ClassMethodTargets), nameof(ClassMethodTargets.Void))]
    public static void FieldAttributeCanReadReferenceTypeFieldThroughReference(
        [Field("referenceField")] ref BindingReference field) => ReferenceObserved = field;

    [Prefix]
    [Target(typeof(ClassMethodTargets), nameof(ClassMethodTargets.Void))]
    public static void FieldAttributeCanReadStructField([Field("structField")] BindingStruct field) => StructObserved = field;

    [Prefix]
    [Target(typeof(ClassMethodTargets), nameof(ClassMethodTargets.Void))]
    public static void FieldAttributeCanWriteStructFieldByReference([Field("structField")] ref BindingStruct field) =>
        field = new BindingStruct { Value = 42 };

    [Prefix]
    [Target(typeof(ClassMethodTargets), nameof(ClassMethodTargets.Void))]
    public static void FieldAttributeCanReadStructFieldThroughReference([Field("structField")] ref BindingStruct field) =>
        StructObserved = field;

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
    public void TripleUnderscoreParameterCanReadPrimitiveField()
    {
        FieldBindingPatches.Observed = 0;
        ApplyPatch(typeof(FieldBindingPatches), nameof(FieldBindingPatches.TripleUnderscoreParameterCanReadPrimitiveField));
        var target = new ClassMethodTargets { primitiveField = 42 };

        target.Void();

        Assert.That(FieldBindingPatches.Observed, Is.EqualTo(42));
    }

    [Test]
    public void TripleUnderscoreParameterCanWritePrimitiveFieldByReference()
    {
        ApplyPatch(typeof(FieldBindingPatches), nameof(FieldBindingPatches.TripleUnderscoreParameterCanWritePrimitiveFieldByReference));
        var target = new ClassMethodTargets { primitiveField = 1 };

        target.Void();

        Assert.That(target.primitiveField, Is.EqualTo(42));
    }

    [Test]
    public void TripleUnderscoreParameterCanReadReferenceTypeField()
    {
        FieldBindingPatches.ReferenceObserved = null;
        var field = new BindingReference { Value = 42 };
        var target = new ClassMethodTargets { referenceField = field };
        ApplyPatch(typeof(FieldBindingPatches), nameof(FieldBindingPatches.TripleUnderscoreParameterCanReadReferenceTypeField));

        target.Void();

        Assert.That(FieldBindingPatches.ReferenceObserved, Is.SameAs(field));
    }

    [Test]
    public void TripleUnderscoreParameterCanWriteReferenceTypeFieldByReference()
    {
        ApplyPatch(typeof(FieldBindingPatches), nameof(FieldBindingPatches.TripleUnderscoreParameterCanWriteReferenceTypeFieldByReference));
        var target = new ClassMethodTargets { referenceField = new BindingReference { Value = 1 } };

        target.Void();

        Assert.That(target.referenceField.Value, Is.EqualTo(42));
    }

    [Test]
    public void TripleUnderscoreParameterCanReadStructField()
    {
        FieldBindingPatches.StructObserved = default;
        ApplyPatch(typeof(FieldBindingPatches), nameof(FieldBindingPatches.TripleUnderscoreParameterCanReadStructField));
        var target = new ClassMethodTargets { structField = new BindingStruct { Value = 42 } };

        target.Void();

        Assert.That(FieldBindingPatches.StructObserved.Value, Is.EqualTo(42));
    }

    [Test]
    public void TripleUnderscoreParameterCanWriteStructFieldByReference()
    {
        ApplyPatch(typeof(FieldBindingPatches), nameof(FieldBindingPatches.TripleUnderscoreParameterCanWriteStructFieldByReference));
        var target = new ClassMethodTargets { structField = new BindingStruct { Value = 1 } };

        target.Void();

        Assert.That(target.structField.Value, Is.EqualTo(42));
    }

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
    public void FieldAttributeCanReadPrimitiveField()
    {
        FieldBindingPatches.Observed = 0;
        ApplyPatch(typeof(FieldBindingPatches), nameof(FieldBindingPatches.FieldAttributeCanReadPrimitiveField));
        var target = new ClassMethodTargets { primitiveField = 42 };

        target.Void();

        Assert.That(FieldBindingPatches.Observed, Is.EqualTo(42));
    }

    [Test]
    public void FieldAttributeCanReadReferenceTypeField()
    {
        FieldBindingPatches.ReferenceObserved = null;
        var field = new BindingReference { Value = 42 };
        var target = new ClassMethodTargets { referenceField = field };
        ApplyPatch(typeof(FieldBindingPatches), nameof(FieldBindingPatches.FieldAttributeCanReadReferenceTypeField));

        target.Void();

        Assert.That(FieldBindingPatches.ReferenceObserved, Is.SameAs(field));
    }

    [Test]
    public void FieldAttributeCanWriteReferenceTypeFieldByReference()
    {
        ApplyPatch(typeof(FieldBindingPatches), nameof(FieldBindingPatches.FieldAttributeCanWriteReferenceTypeFieldByReference));
        var target = new ClassMethodTargets { referenceField = new BindingReference { Value = 1 } };

        target.Void();

        Assert.That(target.referenceField.Value, Is.EqualTo(42));
    }

    [Test]
    public void FieldAttributeCanReadStructField()
    {
        FieldBindingPatches.StructObserved = default;
        ApplyPatch(typeof(FieldBindingPatches), nameof(FieldBindingPatches.FieldAttributeCanReadStructField));
        var target = new ClassMethodTargets { structField = new BindingStruct { Value = 42 } };

        target.Void();

        Assert.That(FieldBindingPatches.StructObserved.Value, Is.EqualTo(42));
    }

    [Test]
    public void FieldAttributeCanWriteStructFieldByReference()
    {
        ApplyPatch(typeof(FieldBindingPatches), nameof(FieldBindingPatches.FieldAttributeCanWriteStructFieldByReference));
        var target = new ClassMethodTargets { structField = new BindingStruct { Value = 1 } };

        target.Void();

        Assert.That(target.structField.Value, Is.EqualTo(42));
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

    [Test]
    public void TripleUnderscoreParameterCanReadPrimitiveFieldThroughReference()
    {
        FieldBindingPatches.Observed = 0;
        ApplyPatch(typeof(FieldBindingPatches), nameof(FieldBindingPatches.TripleUnderscoreParameterCanReadPrimitiveFieldThroughReference));
        var target = new ClassMethodTargets { primitiveField = 42 };

        target.Void();

        Assert.That(FieldBindingPatches.Observed, Is.EqualTo(42));
    }

    [Test]
    public void TripleUnderscoreParameterCanReadReferenceTypeFieldThroughReference()
    {
        FieldBindingPatches.ReferenceObserved = null;
        var field = new BindingReference { Value = 42 };
        var target = new ClassMethodTargets { referenceField = field };
        ApplyPatch(typeof(FieldBindingPatches), nameof(FieldBindingPatches.TripleUnderscoreParameterCanReadReferenceTypeFieldThroughReference));

        target.Void();

        Assert.That(FieldBindingPatches.ReferenceObserved, Is.SameAs(field));
    }

    [Test]
    public void TripleUnderscoreParameterCanReadStructFieldThroughReference()
    {
        FieldBindingPatches.StructObserved = default;
        ApplyPatch(typeof(FieldBindingPatches), nameof(FieldBindingPatches.TripleUnderscoreParameterCanReadStructFieldThroughReference));
        var target = new ClassMethodTargets { structField = new BindingStruct { Value = 42 } };

        target.Void();

        Assert.That(FieldBindingPatches.StructObserved.Value, Is.EqualTo(42));
    }

    [Test]
    public void TripleUnderscoreParameterCanReadOuterInstanceFieldThroughReference()
    {
        FieldBindingPatches.Observed = 0;
        ApplyPatch(typeof(FieldBindingPatches), nameof(FieldBindingPatches.TripleUnderscoreParameterCanReadOuterInstanceFieldThroughReference));
        var outer = new ClassMethodTargets { foo = 42 };

        outer.CallInnerWithoutField(new InstanceMethodTargetsWithoutFields());

        Assert.That(FieldBindingPatches.Observed, Is.EqualTo(42));
    }

    [Test]
    public void TripleUnderscoreParameterCanReadInnerInstanceFieldThroughReference()
    {
        FieldBindingPatches.Observed = 0;
        ApplyPatch(typeof(FieldBindingPatches), nameof(FieldBindingPatches.TripleUnderscoreParameterCanReadInnerInstanceFieldThroughReference));
        var outer = new ClassMethodTargets { foo = 1 };
        var inner = new InnerInstanceMethodTargets { foo = 42 };

        outer.CallInnerWithField(inner);

        Assert.That(FieldBindingPatches.Observed, Is.EqualTo(42));
    }

    [Test]
    public void TripleUnderscoreParameterCanReadOuterStructFieldThroughReference()
    {
        FieldBindingPatches.Observed = 0;
        ApplyPatch(typeof(FieldBindingPatches), nameof(FieldBindingPatches.TripleUnderscoreParameterCanReadOuterStructFieldThroughReference));
        var outer = new StructMethodTargets { foo = 42 };

        outer.CallInnerWithoutField(new InstanceMethodTargetsWithoutFields());

        Assert.That(FieldBindingPatches.Observed, Is.EqualTo(42));
    }

    [Test]
    public void TripleUnderscoreParameterCanReadInnerStructFieldThroughReference()
    {
        FieldBindingPatches.Observed = 0;
        ApplyPatch(typeof(FieldBindingPatches), nameof(FieldBindingPatches.TripleUnderscoreParameterCanReadInnerStructFieldThroughReference));
        var outer = new StructMethodTargets { foo = 1 };
        var inner = new InnerStructMethodTargets { foo = 42 };

        outer.CallInnerWithField(ref inner);

        Assert.That(FieldBindingPatches.Observed, Is.EqualTo(42));
    }

    [Test]
    public void TripleUnderscoreParameterCanReadFieldOfInnerStructPassedByValueThroughReference()
    {
        FieldBindingPatches.Observed = 0;
        ApplyPatch(typeof(FieldBindingPatches), nameof(FieldBindingPatches.TripleUnderscoreParameterCanReadFieldOfInnerStructPassedByValueThroughReference));
        var outer = new StructMethodTargets { foo = 1 };
        var inner = new InnerStructMethodTargets { foo = 42 };

        outer.CallInnerWithFieldByValue(inner);

        Assert.That(FieldBindingPatches.Observed, Is.EqualTo(42));
    }

    [Test]
    public void FieldAttributeCanReadPrimitiveFieldThroughReference()
    {
        FieldBindingPatches.Observed = 0;
        ApplyPatch(typeof(FieldBindingPatches), nameof(FieldBindingPatches.FieldAttributeCanReadPrimitiveFieldThroughReference));
        var target = new ClassMethodTargets { foo = 42 };

        target.Void();

        Assert.That(FieldBindingPatches.Observed, Is.EqualTo(42));
    }

    [Test]
    public void FieldAttributeCanReadReferenceTypeFieldThroughReference()
    {
        FieldBindingPatches.ReferenceObserved = null;
        var field = new BindingReference { Value = 42 };
        var target = new ClassMethodTargets { referenceField = field };
        ApplyPatch(typeof(FieldBindingPatches), nameof(FieldBindingPatches.FieldAttributeCanReadReferenceTypeFieldThroughReference));

        target.Void();

        Assert.That(FieldBindingPatches.ReferenceObserved, Is.SameAs(field));
    }

    [Test]
    public void FieldAttributeCanReadStructFieldThroughReference()
    {
        FieldBindingPatches.StructObserved = default;
        ApplyPatch(typeof(FieldBindingPatches), nameof(FieldBindingPatches.FieldAttributeCanReadStructFieldThroughReference));
        var target = new ClassMethodTargets { structField = new BindingStruct { Value = 42 } };

        target.Void();

        Assert.That(FieldBindingPatches.StructObserved.Value, Is.EqualTo(42));
    }
}
