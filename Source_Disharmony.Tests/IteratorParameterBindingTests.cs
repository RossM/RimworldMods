namespace Disharmony.Tests;

public static partial class IteratorParameterBindingPatches
{
    public static int ParameterObserved;
    public static int FieldObserved;
    public static ClassMethodTargets? InstanceObserved;
    public static BindingReference? ReferenceObserved;
    public static BindingStruct StructObserved;
    public static ClassMethodTargets? ReplacementInstance;
    public static int StructInstanceFieldObserved;

    [InnerPrefix(typeof(InnerStaticMethodTargets), nameof(InnerStaticMethodTargets.IntIdentity))]
    [Target(typeof(ClassMethodTargets), nameof(ClassMethodTargets.EnumerateIdentity))]
    public static void InnerPrefix_IteratorOriginalParameter_Primitive_ReadByValue(int outerValue) => ParameterObserved = outerValue;

    [InnerPrefix(typeof(InnerStaticMethodTargets), nameof(InnerStaticMethodTargets.IntIdentity))]
    [Target(typeof(ClassMethodTargets), nameof(ClassMethodTargets.EnumerateIdentity))]
    public static void InnerPrefix_IteratorOriginalParameter_Primitive_WriteByReference(ref int outerValue) => outerValue = 42;

    [InnerPrefix(typeof(InnerStaticMethodTargets), nameof(InnerStaticMethodTargets.StringIdentity))]
    [Target(typeof(ClassMethodTargets), nameof(ClassMethodTargets.EnumerateReferenceIdentity))]
    public static void InnerPrefix_IteratorOriginalParameter_ReferenceType_ReadByValue(BindingReference outerValue) =>
        ReferenceObserved = outerValue;

    [InnerPrefix(typeof(InnerStaticMethodTargets), nameof(InnerStaticMethodTargets.StringIdentity))]
    [Target(typeof(ClassMethodTargets), nameof(ClassMethodTargets.EnumerateReferenceIdentity))]
    public static void InnerPrefix_IteratorOriginalParameter_ReferenceType_WriteByReference(ref BindingReference outerValue) =>
        outerValue = new BindingReference { Value = 42 };

    [InnerPrefix(typeof(InnerStaticMethodTargets), nameof(InnerStaticMethodTargets.StructIdentity))]
    [Target(typeof(ClassMethodTargets), nameof(ClassMethodTargets.EnumerateStructIdentity))]
    public static void InnerPrefix_IteratorOriginalParameter_Struct_ReadByValue(BindingStruct outerValue) => StructObserved = outerValue;

    [InnerPrefix(typeof(InnerStaticMethodTargets), nameof(InnerStaticMethodTargets.StructIdentity))]
    [Target(typeof(ClassMethodTargets), nameof(ClassMethodTargets.EnumerateStructIdentity))]
    public static void InnerPrefix_IteratorOriginalParameter_Struct_WriteByReference(ref BindingStruct outerValue) =>
        outerValue = new BindingStruct { Value = 42 };

    [InnerPostfix(typeof(InnerStaticMethodTargets), nameof(InnerStaticMethodTargets.IntIdentity))]
    [Target(typeof(ClassMethodTargets), nameof(ClassMethodTargets.EnumerateIdentity))]
    public static void InnerPostfix_IteratorOriginalParameter_Primitive_ReadByValue(int outerValue) => ParameterObserved = outerValue;

    [InnerPrefix(typeof(InnerStaticMethodTargets), nameof(InnerStaticMethodTargets.IntIdentity))]
    [Target(typeof(ClassMethodTargets), nameof(ClassMethodTargets.EnumerateIdentity))]
    public static void InnerPrefix_IteratorDeclaringField_Primitive_ReadByValue([Field("foo", Scope.Outer)] int value) =>
        FieldObserved = value;

    [InnerPrefix(typeof(InnerStaticMethodTargets), nameof(InnerStaticMethodTargets.IntIdentity))]
    [Target(typeof(ClassMethodTargets), nameof(ClassMethodTargets.EnumerateIdentity))]
    public static void InnerPrefix_IteratorDeclaringField_Primitive_WriteByReference(
        [Field("primitiveField", Scope.Outer)] ref int value) => value = 42;

    [InnerPrefix(typeof(InnerStaticMethodTargets), nameof(InnerStaticMethodTargets.IntIdentity))]
    [Target(typeof(ClassMethodTargets), nameof(ClassMethodTargets.EnumerateIdentity))]
    public static void InnerPrefix_IteratorDeclaringField_Primitive_ReadByReference(
        [Field("primitiveField", Scope.Outer)] ref int value) => FieldObserved = value;

    [InnerPrefix(typeof(InnerStaticMethodTargets), nameof(InnerStaticMethodTargets.IntIdentity))]
    [Target(typeof(ClassMethodTargets), nameof(ClassMethodTargets.EnumerateIdentity))]
    public static void InnerPrefix_IteratorDeclaringField_ReferenceType_ReadByValue(
        [Field("referenceField", Scope.Outer)] BindingReference value) => ReferenceObserved = value;

    [InnerPrefix(typeof(InnerStaticMethodTargets), nameof(InnerStaticMethodTargets.IntIdentity))]
    [Target(typeof(ClassMethodTargets), nameof(ClassMethodTargets.EnumerateIdentity))]
    public static void InnerPrefix_IteratorDeclaringField_ReferenceType_WriteByReference(
        [Field("referenceField", Scope.Outer)] ref BindingReference value) =>
        value = new BindingReference { Value = 42 };

    [InnerPrefix(typeof(InnerStaticMethodTargets), nameof(InnerStaticMethodTargets.IntIdentity))]
    [Target(typeof(ClassMethodTargets), nameof(ClassMethodTargets.EnumerateIdentity))]
    public static void InnerPrefix_IteratorDeclaringField_ReferenceType_ReadByReference(
        [Field("referenceField", Scope.Outer)] ref BindingReference value) => ReferenceObserved = value;

    [InnerPrefix(typeof(InnerStaticMethodTargets), nameof(InnerStaticMethodTargets.IntIdentity))]
    [Target(typeof(ClassMethodTargets), nameof(ClassMethodTargets.EnumerateIdentity))]
    public static void InnerPrefix_IteratorDeclaringField_Struct_ReadByValue(
        [Field("structField", Scope.Outer)] BindingStruct value) => StructObserved = value;

    [InnerPrefix(typeof(InnerStaticMethodTargets), nameof(InnerStaticMethodTargets.IntIdentity))]
    [Target(typeof(ClassMethodTargets), nameof(ClassMethodTargets.EnumerateIdentity))]
    public static void InnerPrefix_IteratorDeclaringField_Struct_WriteByReference(
        [Field("structField", Scope.Outer)] ref BindingStruct value) =>
        value = new BindingStruct { Value = 42 };

    [InnerPrefix(typeof(InnerStaticMethodTargets), nameof(InnerStaticMethodTargets.IntIdentity))]
    [Target(typeof(ClassMethodTargets), nameof(ClassMethodTargets.EnumerateIdentity))]
    public static void InnerPrefix_IteratorDeclaringField_Struct_ReadByReference(
        [Field("structField", Scope.Outer)] ref BindingStruct value) => StructObserved = value;

    [InnerPostfix(typeof(InnerStaticMethodTargets), nameof(InnerStaticMethodTargets.IntIdentity))]
    [Target(typeof(ClassMethodTargets), nameof(ClassMethodTargets.EnumerateIdentity))]
    public static void InnerPostfix_IteratorDeclaringInstance_ReferenceType_ReadByValue(
        [Instance(Scope.Outer)] ClassMethodTargets instance) =>
        InstanceObserved = instance;

    [InnerPrefix(typeof(InnerStaticMethodTargets), nameof(InnerStaticMethodTargets.IntResult))]
    [Target(typeof(ClassMethodTargets), nameof(ClassMethodTargets.EnumerateDeclaringInstanceValue))]
    public static void InnerPrefix_IteratorDeclaringInstance_ReferenceType_ReadByValue(
        [Instance(Scope.Outer)] ClassMethodTargets instance) => InstanceObserved = instance;

    [InnerPrefix(typeof(InnerStaticMethodTargets), nameof(InnerStaticMethodTargets.IntResult))]
    [Target(typeof(ClassMethodTargets), nameof(ClassMethodTargets.EnumerateDeclaringInstanceValue))]
    public static void InnerPrefix_IteratorDeclaringInstance_ReferenceType_WriteByReference(
        [Instance(Scope.Outer)] ref ClassMethodTargets instance) => instance = ReplacementInstance!;

    [InnerPrefix(typeof(InnerStaticMethodTargets), nameof(InnerStaticMethodTargets.IntResult))]
    [Target(typeof(StructMethodTargets), nameof(StructMethodTargets.EnumerateDeclaringInstanceValue))]
    public static void InnerPrefix_IteratorDeclaringInstance_Struct_ReadByValue(
        [Instance(Scope.Outer)] StructMethodTargets instance) => StructInstanceFieldObserved = instance.foo;

    [InnerPrefix(typeof(InnerStaticMethodTargets), nameof(InnerStaticMethodTargets.IntResult))]
    [Target(typeof(StructMethodTargets), nameof(StructMethodTargets.EnumerateDeclaringInstanceValue))]
    public static void InnerPrefix_IteratorDeclaringInstance_Struct_WriteByReference(
        [Instance(Scope.Outer)] ref StructMethodTargets instance) => instance.foo = 42;
}

[TestFixture]
public sealed partial class IteratorParameterBindingTests : PatchTestBase
{
    [Test]
    public void InnerPrefix_IteratorOriginalParameter_Primitive_ReadByValue()
    {
        IteratorParameterBindingPatches.ParameterObserved = 0;
        var target = new ClassMethodTargets();
        ApplyPatch(
            typeof(IteratorParameterBindingPatches),
            nameof(IteratorParameterBindingPatches.InnerPrefix_IteratorOriginalParameter_Primitive_ReadByValue));

        int result = target.EnumerateIdentity(42).Single();

        Assert.That(result, Is.EqualTo(42));
        Assert.That(IteratorParameterBindingPatches.ParameterObserved, Is.EqualTo(42));
    }

    [Test]
    public void InnerPrefix_IteratorOriginalParameter_Primitive_WriteByReference()
    {
        var target = new ClassMethodTargets();
        ApplyPatch(
            typeof(IteratorParameterBindingPatches),
            nameof(IteratorParameterBindingPatches.InnerPrefix_IteratorOriginalParameter_Primitive_WriteByReference));

        int result = target.EnumerateIdentity(1).Single();

        Assert.That(result, Is.EqualTo(42));
    }

    [Test]
    public void InnerPrefix_IteratorOriginalParameter_ReferenceType_ReadByValue()
    {
        IteratorParameterBindingPatches.ReferenceObserved = null;
        var value = new BindingReference { Value = 42 };
        var target = new ClassMethodTargets();
        ApplyPatch(
            typeof(IteratorParameterBindingPatches),
            nameof(IteratorParameterBindingPatches.InnerPrefix_IteratorOriginalParameter_ReferenceType_ReadByValue));

        target.EnumerateReferenceIdentity(value).Single();

        Assert.That(IteratorParameterBindingPatches.ReferenceObserved, Is.SameAs(value));
    }

    [Test]
    public void InnerPrefix_IteratorOriginalParameter_ReferenceType_WriteByReference()
    {
        var target = new ClassMethodTargets();
        ApplyPatch(
            typeof(IteratorParameterBindingPatches),
            nameof(IteratorParameterBindingPatches.InnerPrefix_IteratorOriginalParameter_ReferenceType_WriteByReference));

        BindingReference result = target.EnumerateReferenceIdentity(new BindingReference { Value = 1 }).Single();

        Assert.That(result.Value, Is.EqualTo(42));
    }

    [Test]
    public void InnerPrefix_IteratorOriginalParameter_Struct_ReadByValue()
    {
        IteratorParameterBindingPatches.StructObserved = default;
        var target = new ClassMethodTargets();
        ApplyPatch(
            typeof(IteratorParameterBindingPatches),
            nameof(IteratorParameterBindingPatches.InnerPrefix_IteratorOriginalParameter_Struct_ReadByValue));

        target.EnumerateStructIdentity(new BindingStruct { Value = 42 }).Single();

        Assert.That(IteratorParameterBindingPatches.StructObserved.Value, Is.EqualTo(42));
    }

    [Test]
    public void InnerPrefix_IteratorOriginalParameter_Struct_WriteByReference()
    {
        var target = new ClassMethodTargets();
        ApplyPatch(
            typeof(IteratorParameterBindingPatches),
            nameof(IteratorParameterBindingPatches.InnerPrefix_IteratorOriginalParameter_Struct_WriteByReference));

        BindingStruct result = target.EnumerateStructIdentity(new BindingStruct { Value = 1 }).Single();

        Assert.That(result.Value, Is.EqualTo(42));
    }

    [Test]
    public void InnerPostfix_IteratorOriginalParameter_Primitive_ReadByValue()
    {
        IteratorParameterBindingPatches.ParameterObserved = 0;
        var target = new ClassMethodTargets();
        ApplyPatch(
            typeof(IteratorParameterBindingPatches),
            nameof(IteratorParameterBindingPatches.InnerPostfix_IteratorOriginalParameter_Primitive_ReadByValue));

        int result = target.EnumerateIdentity(42).Single();

        Assert.That(result, Is.EqualTo(42));
        Assert.That(IteratorParameterBindingPatches.ParameterObserved, Is.EqualTo(42));
    }

    [Test]
    public void InnerPrefix_IteratorDeclaringField_Primitive_ReadByValue()
    {
        IteratorParameterBindingPatches.FieldObserved = 0;
        var target = new ClassMethodTargets { foo = 42 };
        ApplyPatch(
            typeof(IteratorParameterBindingPatches),
            nameof(IteratorParameterBindingPatches.InnerPrefix_IteratorDeclaringField_Primitive_ReadByValue));

        int result = target.EnumerateIdentity(1).Single();

        Assert.That(result, Is.EqualTo(1));
        Assert.That(IteratorParameterBindingPatches.FieldObserved, Is.EqualTo(42));
    }

    [Test]
    public void InnerPrefix_IteratorDeclaringField_Primitive_WriteByReference()
    {
        var target = new ClassMethodTargets { primitiveField = 1 };
        ApplyPatch(
            typeof(IteratorParameterBindingPatches),
            nameof(IteratorParameterBindingPatches.InnerPrefix_IteratorDeclaringField_Primitive_WriteByReference));

        target.EnumerateIdentity(1).Single();

        Assert.That(target.primitiveField, Is.EqualTo(42));
    }

    [Test]
    public void InnerPrefix_IteratorDeclaringField_Primitive_ReadByReference()
    {
        IteratorParameterBindingPatches.FieldObserved = 0;
        var target = new ClassMethodTargets { primitiveField = 42 };
        ApplyPatch(
            typeof(IteratorParameterBindingPatches),
            nameof(IteratorParameterBindingPatches.InnerPrefix_IteratorDeclaringField_Primitive_ReadByReference));

        target.EnumerateIdentity(1).Single();

        Assert.That(IteratorParameterBindingPatches.FieldObserved, Is.EqualTo(42));
    }

    [Test]
    public void InnerPrefix_IteratorDeclaringField_ReferenceType_ReadByValue()
    {
        IteratorParameterBindingPatches.ReferenceObserved = null;
        var field = new BindingReference { Value = 42 };
        var target = new ClassMethodTargets { referenceField = field };
        ApplyPatch(
            typeof(IteratorParameterBindingPatches),
            nameof(IteratorParameterBindingPatches.InnerPrefix_IteratorDeclaringField_ReferenceType_ReadByValue));

        target.EnumerateIdentity(1).Single();

        Assert.That(IteratorParameterBindingPatches.ReferenceObserved, Is.SameAs(field));
    }

    [Test]
    public void InnerPrefix_IteratorDeclaringField_ReferenceType_WriteByReference()
    {
        var target = new ClassMethodTargets { referenceField = new BindingReference { Value = 1 } };
        ApplyPatch(
            typeof(IteratorParameterBindingPatches),
            nameof(IteratorParameterBindingPatches.InnerPrefix_IteratorDeclaringField_ReferenceType_WriteByReference));

        target.EnumerateIdentity(1).Single();

        Assert.That(target.referenceField.Value, Is.EqualTo(42));
    }

    [Test]
    public void InnerPrefix_IteratorDeclaringField_ReferenceType_ReadByReference()
    {
        IteratorParameterBindingPatches.ReferenceObserved = null;
        var field = new BindingReference { Value = 42 };
        var target = new ClassMethodTargets { referenceField = field };
        ApplyPatch(
            typeof(IteratorParameterBindingPatches),
            nameof(IteratorParameterBindingPatches.InnerPrefix_IteratorDeclaringField_ReferenceType_ReadByReference));

        target.EnumerateIdentity(1).Single();

        Assert.That(IteratorParameterBindingPatches.ReferenceObserved, Is.SameAs(field));
    }

    [Test]
    public void InnerPrefix_IteratorDeclaringField_Struct_ReadByValue()
    {
        IteratorParameterBindingPatches.StructObserved = default;
        var target = new ClassMethodTargets { structField = new BindingStruct { Value = 42 } };
        ApplyPatch(
            typeof(IteratorParameterBindingPatches),
            nameof(IteratorParameterBindingPatches.InnerPrefix_IteratorDeclaringField_Struct_ReadByValue));

        target.EnumerateIdentity(1).Single();

        Assert.That(IteratorParameterBindingPatches.StructObserved.Value, Is.EqualTo(42));
    }

    [Test]
    public void InnerPrefix_IteratorDeclaringField_Struct_WriteByReference()
    {
        var target = new ClassMethodTargets { structField = new BindingStruct { Value = 1 } };
        ApplyPatch(
            typeof(IteratorParameterBindingPatches),
            nameof(IteratorParameterBindingPatches.InnerPrefix_IteratorDeclaringField_Struct_WriteByReference));

        target.EnumerateIdentity(1).Single();

        Assert.That(target.structField.Value, Is.EqualTo(42));
    }

    [Test]
    public void InnerPrefix_IteratorDeclaringField_Struct_ReadByReference()
    {
        IteratorParameterBindingPatches.StructObserved = default;
        var target = new ClassMethodTargets { structField = new BindingStruct { Value = 42 } };
        ApplyPatch(
            typeof(IteratorParameterBindingPatches),
            nameof(IteratorParameterBindingPatches.InnerPrefix_IteratorDeclaringField_Struct_ReadByReference));

        target.EnumerateIdentity(1).Single();

        Assert.That(IteratorParameterBindingPatches.StructObserved.Value, Is.EqualTo(42));
    }

    [Test]
    public void InnerPostfix_IteratorDeclaringInstance_ReferenceType_ReadByValue()
    {
        IteratorParameterBindingPatches.InstanceObserved = null;
        var target = new ClassMethodTargets();
        ApplyPatch(
            typeof(IteratorParameterBindingPatches),
            nameof(IteratorParameterBindingPatches.InnerPostfix_IteratorDeclaringInstance_ReferenceType_ReadByValue));

        int result = target.EnumerateIdentity(42).Single();

        Assert.That(result, Is.EqualTo(42));
        Assert.That(IteratorParameterBindingPatches.InstanceObserved, Is.SameAs(target));
    }

    [Test]
    public void InnerPrefix_IteratorDeclaringInstance_ReferenceType_ReadByValue()
    {
        IteratorParameterBindingPatches.InstanceObserved = null;
        var target = new ClassMethodTargets { foo = 42 };
        ApplyPatch(
            typeof(IteratorParameterBindingPatches),
            nameof(IteratorParameterBindingPatches.InnerPrefix_IteratorDeclaringInstance_ReferenceType_ReadByValue));

        target.EnumerateDeclaringInstanceValue().Single();

        Assert.That(IteratorParameterBindingPatches.InstanceObserved, Is.SameAs(target));
    }

    [Test]
    public void InnerPrefix_IteratorDeclaringInstance_ReferenceType_WriteByReference()
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            ApplyPatch(
                typeof(IteratorParameterBindingPatches),
                nameof(IteratorParameterBindingPatches.InnerPrefix_IteratorDeclaringInstance_ReferenceType_WriteByReference)));

        Assert.That(exception!.InnerException, Is.TypeOf<ParameterBindingException>());
        Assert.That(
            exception.InnerException!.Message,
            Is.EqualTo("instance: Accessing 'this' by reference is not supported for iterator state machine methods"));
    }

    [Test]
    public void InnerPrefix_IteratorDeclaringInstance_Struct_ReadByValue()
    {
        IteratorParameterBindingPatches.StructInstanceFieldObserved = 0;
        var target = new StructMethodTargets { foo = 42 };
        ApplyPatch(
            typeof(IteratorParameterBindingPatches),
            nameof(IteratorParameterBindingPatches.InnerPrefix_IteratorDeclaringInstance_Struct_ReadByValue));

        target.EnumerateDeclaringInstanceValue().Single();

        Assert.That(IteratorParameterBindingPatches.StructInstanceFieldObserved, Is.EqualTo(42));
    }

    [Test]
    public void InnerPrefix_IteratorDeclaringInstance_Struct_WriteByReference()
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            ApplyPatch(
                typeof(IteratorParameterBindingPatches),
                nameof(IteratorParameterBindingPatches.InnerPrefix_IteratorDeclaringInstance_Struct_WriteByReference)));

        Assert.That(exception!.InnerException, Is.TypeOf<ParameterBindingException>());
        Assert.That(
            exception.InnerException!.Message,
            Is.EqualTo("instance: Accessing 'this' by reference is not supported for iterator state machine methods"));
    }
}

public static partial class IteratorParameterBindingPatches
{
    [InnerPrefix(typeof(InnerStaticMethodTargets), nameof(InnerStaticMethodTargets.IntIdentity))]
    [Target(typeof(ClassMethodTargets), nameof(ClassMethodTargets.EnumerateIdentity))]
    public static void InnerPrefix_IteratorOriginalParameter_Primitive_ReadByReference(ref int outerValue) =>
        ParameterObserved = outerValue;

    [InnerPrefix(typeof(InnerStaticMethodTargets), nameof(InnerStaticMethodTargets.StringIdentity))]
    [Target(typeof(ClassMethodTargets), nameof(ClassMethodTargets.EnumerateReferenceIdentity))]
    public static void InnerPrefix_IteratorOriginalParameter_ReferenceType_ReadByReference(
        ref BindingReference outerValue) => ReferenceObserved = outerValue;

    [InnerPrefix(typeof(InnerStaticMethodTargets), nameof(InnerStaticMethodTargets.StructIdentity))]
    [Target(typeof(ClassMethodTargets), nameof(ClassMethodTargets.EnumerateStructIdentity))]
    public static void InnerPrefix_IteratorOriginalParameter_Struct_ReadByReference(ref BindingStruct outerValue) =>
        StructObserved = outerValue;

    [InnerPrefix(typeof(InnerStaticMethodTargets), nameof(InnerStaticMethodTargets.IntResult))]
    [Target(typeof(ClassMethodTargets), nameof(ClassMethodTargets.EnumerateDeclaringInstanceValue))]
    public static void InnerPrefix_IteratorDeclaringInstance_ReferenceType_ReadByReference(
        [Instance(Scope.Outer)] ref ClassMethodTargets instance) => InstanceObserved = instance;

    [InnerPrefix(typeof(InnerStaticMethodTargets), nameof(InnerStaticMethodTargets.IntResult))]
    [Target(typeof(StructMethodTargets), nameof(StructMethodTargets.EnumerateDeclaringInstanceValue))]
    public static void InnerPrefix_IteratorDeclaringInstance_Struct_ReadByReference(
        [Instance(Scope.Outer)] ref StructMethodTargets instance) => StructInstanceFieldObserved = instance.foo;
}

[TestFixture]
public sealed partial class IteratorParameterBindingTests
{
    [Test]
    public void InnerPrefix_IteratorOriginalParameter_Primitive_ReadByReference()
    {
        IteratorParameterBindingPatches.ParameterObserved = 0;
        ApplyPatch(
            typeof(IteratorParameterBindingPatches),
            nameof(IteratorParameterBindingPatches.InnerPrefix_IteratorOriginalParameter_Primitive_ReadByReference));
        new ClassMethodTargets().EnumerateIdentity(42).Single();
        Assert.That(IteratorParameterBindingPatches.ParameterObserved, Is.EqualTo(42));
    }

    [Test]
    public void InnerPrefix_IteratorOriginalParameter_ReferenceType_ReadByReference()
    {
        IteratorParameterBindingPatches.ReferenceObserved = null;
        var value = new BindingReference { Value = 42 };
        ApplyPatch(
            typeof(IteratorParameterBindingPatches),
            nameof(IteratorParameterBindingPatches.InnerPrefix_IteratorOriginalParameter_ReferenceType_ReadByReference));
        new ClassMethodTargets().EnumerateReferenceIdentity(value).Single();
        Assert.That(IteratorParameterBindingPatches.ReferenceObserved, Is.SameAs(value));
    }

    [Test]
    public void InnerPrefix_IteratorOriginalParameter_Struct_ReadByReference()
    {
        IteratorParameterBindingPatches.StructObserved = default;
        ApplyPatch(
            typeof(IteratorParameterBindingPatches),
            nameof(IteratorParameterBindingPatches.InnerPrefix_IteratorOriginalParameter_Struct_ReadByReference));
        new ClassMethodTargets().EnumerateStructIdentity(new BindingStruct { Value = 42 }).Single();
        Assert.That(IteratorParameterBindingPatches.StructObserved.Value, Is.EqualTo(42));
    }

    [Test]
    public void InnerPrefix_IteratorDeclaringInstance_ReferenceType_ReadByReference()
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            ApplyPatch(
                typeof(IteratorParameterBindingPatches),
                nameof(IteratorParameterBindingPatches.InnerPrefix_IteratorDeclaringInstance_ReferenceType_ReadByReference)));

        Assert.That(exception!.InnerException, Is.TypeOf<ParameterBindingException>());
        Assert.That(
            exception.InnerException!.Message,
            Is.EqualTo("instance: Accessing 'this' by reference is not supported for iterator state machine methods"));
    }

    [Test]
    public void InnerPrefix_IteratorDeclaringInstance_Struct_ReadByReference()
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            ApplyPatch(
                typeof(IteratorParameterBindingPatches),
                nameof(IteratorParameterBindingPatches.InnerPrefix_IteratorDeclaringInstance_Struct_ReadByReference)));

        Assert.That(exception!.InnerException, Is.TypeOf<ParameterBindingException>());
        Assert.That(
            exception.InnerException!.Message,
            Is.EqualTo("instance: Accessing 'this' by reference is not supported for iterator state machine methods"));
    }
}

public static partial class IteratorParameterBindingPatches
{
    [InnerPrefix(typeof(InnerStaticMethodTargets), nameof(InnerStaticMethodTargets.IntIdentity))]
    [Target(typeof(LocalFunctionTargets), "PrimitiveLocalIterator.LocalIterator")]
    public static void InnerPrefix_LocalIteratorEnclosingParameter_Primitive_ReadByValue(int enclosingValue) =>
        ParameterObserved = enclosingValue;

    [InnerPrefix(typeof(InnerStaticMethodTargets), nameof(InnerStaticMethodTargets.IntIdentity))]
    [Target(typeof(LocalFunctionTargets), "PrimitiveLocalIterator.LocalIterator")]
    public static void InnerPrefix_LocalIteratorEnclosingParameter_Primitive_ReadByReference(ref int enclosingValue) =>
        ParameterObserved = enclosingValue;

    [InnerPrefix(typeof(InnerStaticMethodTargets), nameof(InnerStaticMethodTargets.StringIdentity))]
    [Target(typeof(LocalFunctionTargets), "ReferenceTypeLocalIterator.LocalIterator")]
    public static void InnerPrefix_LocalIteratorEnclosingParameter_ReferenceType_ReadByValue(
        BindingReference enclosingValue) => ReferenceObserved = enclosingValue;

    [InnerPrefix(typeof(InnerStaticMethodTargets), nameof(InnerStaticMethodTargets.StringIdentity))]
    [Target(typeof(LocalFunctionTargets), "ReferenceTypeLocalIterator.LocalIterator")]
    public static void InnerPrefix_LocalIteratorEnclosingParameter_ReferenceType_ReadByReference(
        ref BindingReference enclosingValue) => ReferenceObserved = enclosingValue;

    [InnerPrefix(typeof(InnerStaticMethodTargets), nameof(InnerStaticMethodTargets.StructIdentity))]
    [Target(typeof(LocalFunctionTargets), "StructLocalIterator.LocalIterator")]
    public static void InnerPrefix_LocalIteratorEnclosingParameter_Struct_ReadByValue(BindingStruct enclosingValue) =>
        StructObserved = enclosingValue;

    [InnerPrefix(typeof(InnerStaticMethodTargets), nameof(InnerStaticMethodTargets.StructIdentity))]
    [Target(typeof(LocalFunctionTargets), "StructLocalIterator.LocalIterator")]
    public static void InnerPrefix_LocalIteratorEnclosingParameter_Struct_ReadByReference(ref BindingStruct enclosingValue) =>
        StructObserved = enclosingValue;
}

[TestFixture]
public sealed partial class IteratorParameterBindingTests
{
    [Test]
    public void InnerPrefix_LocalIteratorEnclosingParameter_Primitive_ReadByValue()
    {
        IteratorParameterBindingPatches.ParameterObserved = 0;
        ApplyPatch(
            typeof(IteratorParameterBindingPatches),
            nameof(IteratorParameterBindingPatches.InnerPrefix_LocalIteratorEnclosingParameter_Primitive_ReadByValue));

        int result = LocalFunctionTargets.PrimitiveLocalIterator(42).Single();

        Assert.That(result, Is.EqualTo(42));
        Assert.That(IteratorParameterBindingPatches.ParameterObserved, Is.EqualTo(42));
    }

    [Test]
    public void InnerPrefix_LocalIteratorEnclosingParameter_Primitive_ReadByReference()
    {
        IteratorParameterBindingPatches.ParameterObserved = 0;
        ApplyPatch(
            typeof(IteratorParameterBindingPatches),
            nameof(IteratorParameterBindingPatches.InnerPrefix_LocalIteratorEnclosingParameter_Primitive_ReadByReference));

        int result = LocalFunctionTargets.PrimitiveLocalIterator(42).Single();

        Assert.That(result, Is.EqualTo(42));
        Assert.That(IteratorParameterBindingPatches.ParameterObserved, Is.EqualTo(42));
    }

    [Test]
    public void InnerPrefix_LocalIteratorEnclosingParameter_ReferenceType_ReadByValue()
    {
        IteratorParameterBindingPatches.ReferenceObserved = null;
        var value = new BindingReference { Value = 42 };
        ApplyPatch(
            typeof(IteratorParameterBindingPatches),
            nameof(IteratorParameterBindingPatches.InnerPrefix_LocalIteratorEnclosingParameter_ReferenceType_ReadByValue));

        BindingReference result = LocalFunctionTargets.ReferenceTypeLocalIterator(value).Single();

        Assert.That(result, Is.SameAs(value));
        Assert.That(IteratorParameterBindingPatches.ReferenceObserved, Is.SameAs(value));
    }

    [Test]
    public void InnerPrefix_LocalIteratorEnclosingParameter_ReferenceType_ReadByReference()
    {
        IteratorParameterBindingPatches.ReferenceObserved = null;
        var value = new BindingReference { Value = 42 };
        ApplyPatch(
            typeof(IteratorParameterBindingPatches),
            nameof(IteratorParameterBindingPatches.InnerPrefix_LocalIteratorEnclosingParameter_ReferenceType_ReadByReference));

        BindingReference result = LocalFunctionTargets.ReferenceTypeLocalIterator(value).Single();

        Assert.That(result, Is.SameAs(value));
        Assert.That(IteratorParameterBindingPatches.ReferenceObserved, Is.SameAs(value));
    }

    [Test]
    public void InnerPrefix_LocalIteratorEnclosingParameter_Struct_ReadByValue()
    {
        IteratorParameterBindingPatches.StructObserved = default;
        ApplyPatch(
            typeof(IteratorParameterBindingPatches),
            nameof(IteratorParameterBindingPatches.InnerPrefix_LocalIteratorEnclosingParameter_Struct_ReadByValue));

        BindingStruct result = LocalFunctionTargets.StructLocalIterator(new BindingStruct { Value = 42 }).Single();

        Assert.That(result.Value, Is.EqualTo(42));
        Assert.That(IteratorParameterBindingPatches.StructObserved.Value, Is.EqualTo(42));
    }

    [Test]
    public void InnerPrefix_LocalIteratorEnclosingParameter_Struct_ReadByReference()
    {
        IteratorParameterBindingPatches.StructObserved = default;
        ApplyPatch(
            typeof(IteratorParameterBindingPatches),
            nameof(IteratorParameterBindingPatches.InnerPrefix_LocalIteratorEnclosingParameter_Struct_ReadByReference));

        BindingStruct result = LocalFunctionTargets.StructLocalIterator(new BindingStruct { Value = 42 }).Single();

        Assert.That(result.Value, Is.EqualTo(42));
        Assert.That(IteratorParameterBindingPatches.StructObserved.Value, Is.EqualTo(42));
    }
}
