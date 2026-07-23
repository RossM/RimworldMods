namespace Disharmony.Tests;

public static class FieldBindingPatches
{
    public static int Observed;
    public static BindingReference? ReferenceObserved;
    public static BindingStruct StructObserved;

    [Prefix]
    [Target(typeof(ClassMethodTargets), nameof(ClassMethodTargets.Void))]
    public static void Prefix_TripleUnderscoreField_Primitive_ReadByValue(int ___primitiveField) => Observed = ___primitiveField;

    [Prefix]
    [Target(typeof(ClassMethodTargets), nameof(ClassMethodTargets.Void))]
    public static void Prefix_TripleUnderscoreField_Primitive_WriteByReference(ref int ___primitiveField) => ___primitiveField = 42;

    [Prefix]
    [Target(typeof(ClassMethodTargets), nameof(ClassMethodTargets.Void))]
    public static void Prefix_TripleUnderscoreField_Primitive_ReadByReference(ref int ___primitiveField) =>
        Observed = ___primitiveField;

    [Prefix]
    [Target(typeof(ClassMethodTargets), nameof(ClassMethodTargets.Void))]
    public static void Prefix_TripleUnderscoreField_ReferenceType_ReadByValue(BindingReference ___referenceField) =>
        ReferenceObserved = ___referenceField;

    [Prefix]
    [Target(typeof(ClassMethodTargets), nameof(ClassMethodTargets.Void))]
    public static void Prefix_TripleUnderscoreField_ReferenceType_WriteByReference(ref BindingReference ___referenceField) =>
        ___referenceField = new BindingReference { Value = 42 };

    [Prefix]
    [Target(typeof(ClassMethodTargets), nameof(ClassMethodTargets.Void))]
    public static void Prefix_TripleUnderscoreField_ReferenceType_ReadByReference(
        ref BindingReference ___referenceField) => ReferenceObserved = ___referenceField;

    [Prefix]
    [Target(typeof(ClassMethodTargets), nameof(ClassMethodTargets.Void))]
    public static void Prefix_TripleUnderscoreField_Struct_ReadByValue(BindingStruct ___structField) => StructObserved = ___structField;

    [Prefix]
    [Target(typeof(ClassMethodTargets), nameof(ClassMethodTargets.Void))]
    public static void Prefix_TripleUnderscoreField_Struct_WriteByReference(ref BindingStruct ___structField) =>
        ___structField = new BindingStruct { Value = 42 };

    [Prefix]
    [Target(typeof(ClassMethodTargets), nameof(ClassMethodTargets.Void))]
    public static void Prefix_TripleUnderscoreField_Struct_ReadByReference(ref BindingStruct ___structField) =>
        StructObserved = ___structField;

    [InnerPrefix(typeof(InstanceMethodTargetsWithoutFields), nameof(InstanceMethodTargetsWithoutFields.Void))]
    [Target(typeof(ClassMethodTargets), nameof(ClassMethodTargets.CallInnerWithoutField))]
    public static void InnerPrefix_TripleUnderscoreField_OuterClassInstance_Primitive_ReadByValue(int ___foo) => Observed = ___foo;

    [InnerPostfix(typeof(InnerInstanceMethodTargets), nameof(InnerInstanceMethodTargets.Void))]
    [Target(typeof(ClassMethodTargets), nameof(ClassMethodTargets.CallInnerWithField))]
    public static void InnerPostfix_TripleUnderscoreField_InnerClassInstance_Primitive_ReadByValue_WhenBothScopesMatch(int ___foo) =>
        Observed = ___foo;

    [InnerPrefix(typeof(InstanceMethodTargetsWithoutFields), nameof(InstanceMethodTargetsWithoutFields.Void))]
    [Target(typeof(ClassMethodTargets), nameof(ClassMethodTargets.CallInnerWithoutField))]
    public static void InnerPrefix_TripleUnderscoreField_OuterClassInstance_Primitive_WriteByReference(ref int ___foo) => ___foo = 42;

    [InnerPrefix(typeof(InstanceMethodTargetsWithoutFields), nameof(InstanceMethodTargetsWithoutFields.Void))]
    [Target(typeof(ClassMethodTargets), nameof(ClassMethodTargets.CallInnerWithoutField))]
    public static void InnerPrefix_TripleUnderscoreField_OuterClassInstance_Primitive_ReadByReference(ref int ___foo) => Observed = ___foo;

    [InnerPostfix(typeof(InnerInstanceMethodTargets), nameof(InnerInstanceMethodTargets.Void))]
    [Target(typeof(ClassMethodTargets), nameof(ClassMethodTargets.CallInnerWithField))]
    public static void InnerPostfix_TripleUnderscoreField_InnerClassInstance_Primitive_WriteByReference(ref int ___foo) => ___foo = 42;

    [InnerPostfix(typeof(InnerInstanceMethodTargets), nameof(InnerInstanceMethodTargets.Void))]
    [Target(typeof(ClassMethodTargets), nameof(ClassMethodTargets.CallInnerWithField))]
    public static void InnerPostfix_TripleUnderscoreField_InnerClassInstance_Primitive_ReadByReference(ref int ___foo) => Observed = ___foo;

    [InnerPrefix(typeof(InstanceMethodTargetsWithoutFields), nameof(InstanceMethodTargetsWithoutFields.Void))]
    [Target(typeof(StructMethodTargets), nameof(StructMethodTargets.CallInnerWithoutField))]
    public static void InnerPrefix_TripleUnderscoreField_OuterStructInstance_Primitive_ReadByValue(int ___foo) => Observed = ___foo;

    [InnerPostfix(typeof(InnerStructMethodTargets), nameof(InnerStructMethodTargets.Void))]
    [Target(typeof(StructMethodTargets), nameof(StructMethodTargets.CallInnerWithField))]
    public static void InnerPostfix_TripleUnderscoreField_InnerStructInstance_Primitive_ReadByValue_WhenBothScopesMatch(int ___foo) =>
        Observed = ___foo;

    [InnerPrefix(typeof(InstanceMethodTargetsWithoutFields), nameof(InstanceMethodTargetsWithoutFields.Void))]
    [Target(typeof(StructMethodTargets), nameof(StructMethodTargets.CallInnerWithoutField))]
    public static void InnerPrefix_TripleUnderscoreField_OuterStructInstance_Primitive_WriteByReference(ref int ___foo) => ___foo = 42;

    [InnerPrefix(typeof(InstanceMethodTargetsWithoutFields), nameof(InstanceMethodTargetsWithoutFields.Void))]
    [Target(typeof(StructMethodTargets), nameof(StructMethodTargets.CallInnerWithoutField))]
    public static void InnerPrefix_TripleUnderscoreField_OuterStructInstance_Primitive_ReadByReference(ref int ___foo) => Observed = ___foo;

    [InnerPrefix(typeof(InnerStructMethodTargets), nameof(InnerStructMethodTargets.Void))]
    [Target(typeof(StructMethodTargets), nameof(StructMethodTargets.CallInnerWithField))]
    public static void InnerPrefix_TripleUnderscoreField_InnerStructInstance_Primitive_WriteByReference(ref int ___foo) => ___foo = 42;

    [InnerPrefix(typeof(InnerStructMethodTargets), nameof(InnerStructMethodTargets.Void))]
    [Target(typeof(StructMethodTargets), nameof(StructMethodTargets.CallInnerWithField))]
    public static void InnerPrefix_TripleUnderscoreField_InnerStructInstance_Primitive_ReadByReference(ref int ___foo) => Observed = ___foo;

    [InnerPrefix(typeof(InnerStructMethodTargets), nameof(InnerStructMethodTargets.Void))]
    [Target(typeof(StructMethodTargets), nameof(StructMethodTargets.CallInnerWithFieldByValue))]
    public static void InnerPrefix_TripleUnderscoreField_InnerStructPassedByValue_Primitive_WriteByReference(ref int ___foo) => ___foo = 42;

    [InnerPrefix(typeof(InnerStructMethodTargets), nameof(InnerStructMethodTargets.Void))]
    [Target(typeof(StructMethodTargets), nameof(StructMethodTargets.CallInnerWithFieldByValue))]
    public static void InnerPrefix_TripleUnderscoreField_InnerStructPassedByValue_Primitive_ReadByReference(ref int ___foo) =>
        Observed = ___foo;

    [Prefix]
    [Target(typeof(ClassMethodTargets), nameof(ClassMethodTargets.Void))]
    public static void Prefix_FieldAttribute_Primitive_WriteByReference([Field("foo")] ref int field) => field = 42;

    [Prefix]
    [Target(typeof(ClassMethodTargets), nameof(ClassMethodTargets.Void))]
    public static void Prefix_FieldAttribute_Primitive_ReadByReference([Field("foo")] ref int field) => Observed = field;

    [Prefix]
    [Target(typeof(ClassMethodTargets), nameof(ClassMethodTargets.Void))]
    public static void Prefix_FieldAttribute_Primitive_ReadByValue([Field("primitiveField")] int field) => Observed = field;

    [Prefix]
    [Target(typeof(ClassMethodTargets), nameof(ClassMethodTargets.Void))]
    public static void Prefix_FieldAttribute_ReferenceType_ReadByValue([Field("referenceField")] BindingReference field) =>
        ReferenceObserved = field;

    [Prefix]
    [Target(typeof(ClassMethodTargets), nameof(ClassMethodTargets.Void))]
    public static void Prefix_FieldAttribute_ReferenceType_WriteByReference([Field("referenceField")] ref BindingReference field) =>
        field = new BindingReference { Value = 42 };

    [Prefix]
    [Target(typeof(ClassMethodTargets), nameof(ClassMethodTargets.Void))]
    public static void Prefix_FieldAttribute_ReferenceType_ReadByReference(
        [Field("referenceField")] ref BindingReference field) => ReferenceObserved = field;

    [Prefix]
    [Target(typeof(ClassMethodTargets), nameof(ClassMethodTargets.Void))]
    public static void Prefix_FieldAttribute_Struct_ReadByValue([Field("structField")] BindingStruct field) => StructObserved = field;

    [Prefix]
    [Target(typeof(ClassMethodTargets), nameof(ClassMethodTargets.Void))]
    public static void Prefix_FieldAttribute_Struct_WriteByReference([Field("structField")] ref BindingStruct field) =>
        field = new BindingStruct { Value = 42 };

    [Prefix]
    [Target(typeof(ClassMethodTargets), nameof(ClassMethodTargets.Void))]
    public static void Prefix_FieldAttribute_Struct_ReadByReference([Field("structField")] ref BindingStruct field) =>
        StructObserved = field;

    [InnerPrefix(typeof(InnerInstanceMethodTargets), nameof(InnerInstanceMethodTargets.Void))]
    [Target(typeof(ClassMethodTargets), nameof(ClassMethodTargets.CallInnerWithField))]
    public static void InnerPrefix_FieldAttribute_OuterScope_Primitive_ReadByValue([Field("foo", Scope.Outer)] int field) =>
        Observed = field;

    [InnerPrefix(typeof(InnerInstanceMethodTargets), nameof(InnerInstanceMethodTargets.Void))]
    [Target(typeof(ClassMethodTargets), nameof(ClassMethodTargets.CallInnerWithField))]
    public static void InnerPrefix_FieldAttribute_InnerScope_Primitive_ReadByValue([Field("foo", Scope.Inner)] int field) =>
        Observed = field;
}

[TestFixture]
public sealed partial class FieldBindingTests : PatchTestBase
{
    [Test]
    public void Prefix_TripleUnderscoreField_Primitive_ReadByValue()
    {
        FieldBindingPatches.Observed = 0;
        ApplyPatch(typeof(FieldBindingPatches), nameof(FieldBindingPatches.Prefix_TripleUnderscoreField_Primitive_ReadByValue));
        var target = new ClassMethodTargets { primitiveField = 42 };

        target.Void();

        Assert.That(FieldBindingPatches.Observed, Is.EqualTo(42));
    }

    [Test]
    public void Prefix_TripleUnderscoreField_Primitive_WriteByReference()
    {
        ApplyPatch(typeof(FieldBindingPatches), nameof(FieldBindingPatches.Prefix_TripleUnderscoreField_Primitive_WriteByReference));
        var target = new ClassMethodTargets { primitiveField = 1 };

        target.Void();

        Assert.That(target.primitiveField, Is.EqualTo(42));
    }

    [Test]
    public void Prefix_TripleUnderscoreField_ReferenceType_ReadByValue()
    {
        FieldBindingPatches.ReferenceObserved = null;
        var field = new BindingReference { Value = 42 };
        var target = new ClassMethodTargets { referenceField = field };
        ApplyPatch(typeof(FieldBindingPatches), nameof(FieldBindingPatches.Prefix_TripleUnderscoreField_ReferenceType_ReadByValue));

        target.Void();

        Assert.That(FieldBindingPatches.ReferenceObserved, Is.SameAs(field));
    }

    [Test]
    public void Prefix_TripleUnderscoreField_ReferenceType_WriteByReference()
    {
        ApplyPatch(typeof(FieldBindingPatches), nameof(FieldBindingPatches.Prefix_TripleUnderscoreField_ReferenceType_WriteByReference));
        var target = new ClassMethodTargets { referenceField = new BindingReference { Value = 1 } };

        target.Void();

        Assert.That(target.referenceField.Value, Is.EqualTo(42));
    }

    [Test]
    public void Prefix_TripleUnderscoreField_Struct_ReadByValue()
    {
        FieldBindingPatches.StructObserved = default;
        ApplyPatch(typeof(FieldBindingPatches), nameof(FieldBindingPatches.Prefix_TripleUnderscoreField_Struct_ReadByValue));
        var target = new ClassMethodTargets { structField = new BindingStruct { Value = 42 } };

        target.Void();

        Assert.That(FieldBindingPatches.StructObserved.Value, Is.EqualTo(42));
    }

    [Test]
    public void Prefix_TripleUnderscoreField_Struct_WriteByReference()
    {
        ApplyPatch(typeof(FieldBindingPatches), nameof(FieldBindingPatches.Prefix_TripleUnderscoreField_Struct_WriteByReference));
        var target = new ClassMethodTargets { structField = new BindingStruct { Value = 1 } };

        target.Void();

        Assert.That(target.structField.Value, Is.EqualTo(42));
    }

    [Test]
    public void InnerPrefix_TripleUnderscoreField_OuterClassInstance_Primitive_ReadByValue()
    {
        FieldBindingPatches.Observed = 0;
        ApplyPatch(typeof(FieldBindingPatches),
            nameof(FieldBindingPatches.InnerPrefix_TripleUnderscoreField_OuterClassInstance_Primitive_ReadByValue));
        var outer = new ClassMethodTargets { foo = 42 };

        outer.CallInnerWithoutField(new InstanceMethodTargetsWithoutFields());

        Assert.That(FieldBindingPatches.Observed, Is.EqualTo(42));
    }

    [Test]
    public void InnerPostfix_TripleUnderscoreField_InnerClassInstance_Primitive_ReadByValue_WhenBothScopesMatch()
    {
        FieldBindingPatches.Observed = 0;
        ApplyPatch(typeof(FieldBindingPatches),
            nameof(FieldBindingPatches.InnerPostfix_TripleUnderscoreField_InnerClassInstance_Primitive_ReadByValue_WhenBothScopesMatch));
        var outer = new ClassMethodTargets { foo = 1 };
        var inner = new InnerInstanceMethodTargets { foo = 42 };

        outer.CallInnerWithField(inner);

        Assert.That(FieldBindingPatches.Observed, Is.EqualTo(42));
    }

    [Test]
    public void InnerPrefix_TripleUnderscoreField_OuterClassInstance_Primitive_WriteByReference()
    {
        ApplyPatch(typeof(FieldBindingPatches),
            nameof(FieldBindingPatches.InnerPrefix_TripleUnderscoreField_OuterClassInstance_Primitive_WriteByReference));
        var outer = new ClassMethodTargets { foo = 1 };

        outer.CallInnerWithoutField(new InstanceMethodTargetsWithoutFields());

        Assert.That(outer.foo, Is.EqualTo(42));
    }

    [Test]
    public void InnerPostfix_TripleUnderscoreField_InnerClassInstance_Primitive_WriteByReference()
    {
        ApplyPatch(typeof(FieldBindingPatches),
            nameof(FieldBindingPatches.InnerPostfix_TripleUnderscoreField_InnerClassInstance_Primitive_WriteByReference));
        var outer = new ClassMethodTargets { foo = 1 };
        var inner = new InnerInstanceMethodTargets { foo = 1 };

        outer.CallInnerWithField(inner);

        Assert.That(inner.foo, Is.EqualTo(42));
        Assert.That(outer.foo, Is.EqualTo(1));
    }

    [Test]
    public void InnerPrefix_TripleUnderscoreField_OuterStructInstance_Primitive_ReadByValue()
    {
        FieldBindingPatches.Observed = 0;
        ApplyPatch(typeof(FieldBindingPatches),
            nameof(FieldBindingPatches.InnerPrefix_TripleUnderscoreField_OuterStructInstance_Primitive_ReadByValue));
        var outer = new StructMethodTargets { foo = 42 };

        outer.CallInnerWithoutField(new InstanceMethodTargetsWithoutFields());

        Assert.That(FieldBindingPatches.Observed, Is.EqualTo(42));
    }

    [Test]
    public void InnerPostfix_TripleUnderscoreField_InnerStructInstance_Primitive_ReadByValue_WhenBothScopesMatch()
    {
        FieldBindingPatches.Observed = 0;
        ApplyPatch(typeof(FieldBindingPatches),
            nameof(FieldBindingPatches.InnerPostfix_TripleUnderscoreField_InnerStructInstance_Primitive_ReadByValue_WhenBothScopesMatch));
        var outer = new StructMethodTargets { foo = 1 };
        var inner = new InnerStructMethodTargets { foo = 42 };

        outer.CallInnerWithField(ref inner);

        Assert.That(FieldBindingPatches.Observed, Is.EqualTo(42));
    }

    [Test]
    public void InnerPrefix_TripleUnderscoreField_OuterStructInstance_Primitive_WriteByReference()
    {
        ApplyPatch(typeof(FieldBindingPatches),
            nameof(FieldBindingPatches.InnerPrefix_TripleUnderscoreField_OuterStructInstance_Primitive_WriteByReference));
        var outer = new StructMethodTargets { foo = 1 };

        outer.CallInnerWithoutField(new InstanceMethodTargetsWithoutFields());

        Assert.That(outer.foo, Is.EqualTo(42));
    }

    [Test]
    public void InnerPrefix_TripleUnderscoreField_InnerStructInstance_Primitive_WriteByReference()
    {
        InnerStructMethodTargets.FieldObserved = 0;
        ApplyPatch(typeof(FieldBindingPatches),
            nameof(FieldBindingPatches.InnerPrefix_TripleUnderscoreField_InnerStructInstance_Primitive_WriteByReference));
        var outer = new StructMethodTargets { foo = 1 };
        var inner = new InnerStructMethodTargets { foo = 1 };

        outer.CallInnerWithField(ref inner);

        Assert.That(InnerStructMethodTargets.FieldObserved, Is.EqualTo(42));
        Assert.That(inner.foo, Is.EqualTo(42));
        Assert.That(outer.foo, Is.EqualTo(1));
    }

    [Test]
    public void InnerPrefix_TripleUnderscoreField_InnerStructPassedByValue_Primitive_WriteByReference()
    {
        InnerStructMethodTargets.FieldObserved = 0;
        ApplyPatch(typeof(FieldBindingPatches),
            nameof(FieldBindingPatches.InnerPrefix_TripleUnderscoreField_InnerStructPassedByValue_Primitive_WriteByReference));
        var outer = new StructMethodTargets { foo = 1 };
        var inner = new InnerStructMethodTargets { foo = 1 };

        outer.CallInnerWithFieldByValue(inner);

        Assert.That(InnerStructMethodTargets.FieldObserved, Is.EqualTo(42));
        Assert.That(inner.foo, Is.EqualTo(1));
        Assert.That(outer.foo, Is.EqualTo(1));
    }

    [Test]
    public void Prefix_FieldAttribute_Primitive_WriteByReference()
    {
        ApplyPatch(typeof(FieldBindingPatches), nameof(FieldBindingPatches.Prefix_FieldAttribute_Primitive_WriteByReference));
        var target = new ClassMethodTargets { foo = 1 };

        target.Void();

        Assert.That(target.foo, Is.EqualTo(42));
    }

    [Test]
    public void Prefix_FieldAttribute_Primitive_ReadByValue()
    {
        FieldBindingPatches.Observed = 0;
        ApplyPatch(typeof(FieldBindingPatches), nameof(FieldBindingPatches.Prefix_FieldAttribute_Primitive_ReadByValue));
        var target = new ClassMethodTargets { primitiveField = 42 };

        target.Void();

        Assert.That(FieldBindingPatches.Observed, Is.EqualTo(42));
    }

    [Test]
    public void Prefix_FieldAttribute_ReferenceType_ReadByValue()
    {
        FieldBindingPatches.ReferenceObserved = null;
        var field = new BindingReference { Value = 42 };
        var target = new ClassMethodTargets { referenceField = field };
        ApplyPatch(typeof(FieldBindingPatches), nameof(FieldBindingPatches.Prefix_FieldAttribute_ReferenceType_ReadByValue));

        target.Void();

        Assert.That(FieldBindingPatches.ReferenceObserved, Is.SameAs(field));
    }

    [Test]
    public void Prefix_FieldAttribute_ReferenceType_WriteByReference()
    {
        ApplyPatch(typeof(FieldBindingPatches), nameof(FieldBindingPatches.Prefix_FieldAttribute_ReferenceType_WriteByReference));
        var target = new ClassMethodTargets { referenceField = new BindingReference { Value = 1 } };

        target.Void();

        Assert.That(target.referenceField.Value, Is.EqualTo(42));
    }

    [Test]
    public void Prefix_FieldAttribute_Struct_ReadByValue()
    {
        FieldBindingPatches.StructObserved = default;
        ApplyPatch(typeof(FieldBindingPatches), nameof(FieldBindingPatches.Prefix_FieldAttribute_Struct_ReadByValue));
        var target = new ClassMethodTargets { structField = new BindingStruct { Value = 42 } };

        target.Void();

        Assert.That(FieldBindingPatches.StructObserved.Value, Is.EqualTo(42));
    }

    [Test]
    public void Prefix_FieldAttribute_Struct_WriteByReference()
    {
        ApplyPatch(typeof(FieldBindingPatches), nameof(FieldBindingPatches.Prefix_FieldAttribute_Struct_WriteByReference));
        var target = new ClassMethodTargets { structField = new BindingStruct { Value = 1 } };

        target.Void();

        Assert.That(target.structField.Value, Is.EqualTo(42));
    }

    [Test]
    public void InnerPrefix_FieldAttribute_OuterScope_Primitive_ReadByValue()
    {
        FieldBindingPatches.Observed = 0;
        ApplyPatch(typeof(FieldBindingPatches), nameof(FieldBindingPatches.InnerPrefix_FieldAttribute_OuterScope_Primitive_ReadByValue));
        var outer = new ClassMethodTargets { foo = 1 };
        var inner = new InnerInstanceMethodTargets { foo = 42 };

        outer.CallInnerWithField(inner);

        Assert.That(FieldBindingPatches.Observed, Is.EqualTo(1));
    }

    [Test]
    public void InnerPrefix_FieldAttribute_InnerScope_Primitive_ReadByValue()
    {
        FieldBindingPatches.Observed = 0;
        ApplyPatch(typeof(FieldBindingPatches), nameof(FieldBindingPatches.InnerPrefix_FieldAttribute_InnerScope_Primitive_ReadByValue));
        var outer = new ClassMethodTargets { foo = 1 };
        var inner = new InnerInstanceMethodTargets { foo = 42 };

        outer.CallInnerWithField(inner);

        Assert.That(FieldBindingPatches.Observed, Is.EqualTo(42));
    }

    [Test]
    public void Prefix_TripleUnderscoreField_Primitive_ReadByReference()
    {
        FieldBindingPatches.Observed = 0;
        ApplyPatch(typeof(FieldBindingPatches), nameof(FieldBindingPatches.Prefix_TripleUnderscoreField_Primitive_ReadByReference));
        var target = new ClassMethodTargets { primitiveField = 42 };

        target.Void();

        Assert.That(FieldBindingPatches.Observed, Is.EqualTo(42));
    }

    [Test]
    public void Prefix_TripleUnderscoreField_ReferenceType_ReadByReference()
    {
        FieldBindingPatches.ReferenceObserved = null;
        var field = new BindingReference { Value = 42 };
        var target = new ClassMethodTargets { referenceField = field };
        ApplyPatch(typeof(FieldBindingPatches), nameof(FieldBindingPatches.Prefix_TripleUnderscoreField_ReferenceType_ReadByReference));

        target.Void();

        Assert.That(FieldBindingPatches.ReferenceObserved, Is.SameAs(field));
    }

    [Test]
    public void Prefix_TripleUnderscoreField_Struct_ReadByReference()
    {
        FieldBindingPatches.StructObserved = default;
        ApplyPatch(typeof(FieldBindingPatches), nameof(FieldBindingPatches.Prefix_TripleUnderscoreField_Struct_ReadByReference));
        var target = new ClassMethodTargets { structField = new BindingStruct { Value = 42 } };

        target.Void();

        Assert.That(FieldBindingPatches.StructObserved.Value, Is.EqualTo(42));
    }

    [Test]
    public void InnerPrefix_TripleUnderscoreField_OuterClassInstance_Primitive_ReadByReference()
    {
        FieldBindingPatches.Observed = 0;
        ApplyPatch(typeof(FieldBindingPatches),
            nameof(FieldBindingPatches.InnerPrefix_TripleUnderscoreField_OuterClassInstance_Primitive_ReadByReference));
        var outer = new ClassMethodTargets { foo = 42 };

        outer.CallInnerWithoutField(new InstanceMethodTargetsWithoutFields());

        Assert.That(FieldBindingPatches.Observed, Is.EqualTo(42));
    }

    [Test]
    public void InnerPostfix_TripleUnderscoreField_InnerClassInstance_Primitive_ReadByReference()
    {
        FieldBindingPatches.Observed = 0;
        ApplyPatch(typeof(FieldBindingPatches),
            nameof(FieldBindingPatches.InnerPostfix_TripleUnderscoreField_InnerClassInstance_Primitive_ReadByReference));
        var outer = new ClassMethodTargets { foo = 1 };
        var inner = new InnerInstanceMethodTargets { foo = 42 };

        outer.CallInnerWithField(inner);

        Assert.That(FieldBindingPatches.Observed, Is.EqualTo(42));
    }

    [Test]
    public void InnerPrefix_TripleUnderscoreField_OuterStructInstance_Primitive_ReadByReference()
    {
        FieldBindingPatches.Observed = 0;
        ApplyPatch(typeof(FieldBindingPatches),
            nameof(FieldBindingPatches.InnerPrefix_TripleUnderscoreField_OuterStructInstance_Primitive_ReadByReference));
        var outer = new StructMethodTargets { foo = 42 };

        outer.CallInnerWithoutField(new InstanceMethodTargetsWithoutFields());

        Assert.That(FieldBindingPatches.Observed, Is.EqualTo(42));
    }

    [Test]
    public void InnerPrefix_TripleUnderscoreField_InnerStructInstance_Primitive_ReadByReference()
    {
        FieldBindingPatches.Observed = 0;
        ApplyPatch(typeof(FieldBindingPatches),
            nameof(FieldBindingPatches.InnerPrefix_TripleUnderscoreField_InnerStructInstance_Primitive_ReadByReference));
        var outer = new StructMethodTargets { foo = 1 };
        var inner = new InnerStructMethodTargets { foo = 42 };

        outer.CallInnerWithField(ref inner);

        Assert.That(FieldBindingPatches.Observed, Is.EqualTo(42));
    }

    [Test]
    public void InnerPrefix_TripleUnderscoreField_InnerStructPassedByValue_Primitive_ReadByReference()
    {
        FieldBindingPatches.Observed = 0;
        ApplyPatch(typeof(FieldBindingPatches),
            nameof(FieldBindingPatches.InnerPrefix_TripleUnderscoreField_InnerStructPassedByValue_Primitive_ReadByReference));
        var outer = new StructMethodTargets { foo = 1 };
        var inner = new InnerStructMethodTargets { foo = 42 };

        outer.CallInnerWithFieldByValue(inner);

        Assert.That(FieldBindingPatches.Observed, Is.EqualTo(42));
    }

    [Test]
    public void Prefix_FieldAttribute_Primitive_ReadByReference()
    {
        FieldBindingPatches.Observed = 0;
        ApplyPatch(typeof(FieldBindingPatches), nameof(FieldBindingPatches.Prefix_FieldAttribute_Primitive_ReadByReference));
        var target = new ClassMethodTargets { foo = 42 };

        target.Void();

        Assert.That(FieldBindingPatches.Observed, Is.EqualTo(42));
    }

    [Test]
    public void Prefix_FieldAttribute_ReferenceType_ReadByReference()
    {
        FieldBindingPatches.ReferenceObserved = null;
        var field = new BindingReference { Value = 42 };
        var target = new ClassMethodTargets { referenceField = field };
        ApplyPatch(typeof(FieldBindingPatches), nameof(FieldBindingPatches.Prefix_FieldAttribute_ReferenceType_ReadByReference));

        target.Void();

        Assert.That(FieldBindingPatches.ReferenceObserved, Is.SameAs(field));
    }

    [Test]
    public void Prefix_FieldAttribute_Struct_ReadByReference()
    {
        FieldBindingPatches.StructObserved = default;
        ApplyPatch(typeof(FieldBindingPatches), nameof(FieldBindingPatches.Prefix_FieldAttribute_Struct_ReadByReference));
        var target = new ClassMethodTargets { structField = new BindingStruct { Value = 42 } };

        target.Void();

        Assert.That(FieldBindingPatches.StructObserved.Value, Is.EqualTo(42));
    }
}
