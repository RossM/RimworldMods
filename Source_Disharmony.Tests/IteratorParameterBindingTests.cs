namespace Disharmony.Tests;

public static class IteratorParameterBindingPatches
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
    public static void InnerPrefixOnIteratorCanReadOriginalMethodParameter(int outerValue) => ParameterObserved = outerValue;

    [InnerPrefix(typeof(InnerStaticMethodTargets), nameof(InnerStaticMethodTargets.IntIdentity))]
    [Target(typeof(ClassMethodTargets), nameof(ClassMethodTargets.EnumerateIdentity))]
    public static void InnerPrefixOnIteratorCanWriteOriginalMethodParameterByReference(ref int outerValue) => outerValue = 42;

    [InnerPrefix(typeof(InnerStaticMethodTargets), nameof(InnerStaticMethodTargets.StringIdentity))]
    [Target(typeof(ClassMethodTargets), nameof(ClassMethodTargets.EnumerateReferenceIdentity))]
    public static void InnerPrefixOnIteratorCanReadOriginalReferenceTypeParameter(BindingReference outerValue) =>
        ReferenceObserved = outerValue;

    [InnerPrefix(typeof(InnerStaticMethodTargets), nameof(InnerStaticMethodTargets.StringIdentity))]
    [Target(typeof(ClassMethodTargets), nameof(ClassMethodTargets.EnumerateReferenceIdentity))]
    public static void InnerPrefixOnIteratorCanWriteOriginalReferenceTypeParameterByReference(ref BindingReference outerValue) =>
        outerValue = new BindingReference { Value = 42 };

    [InnerPrefix(typeof(InnerStaticMethodTargets), nameof(InnerStaticMethodTargets.StructIdentity))]
    [Target(typeof(ClassMethodTargets), nameof(ClassMethodTargets.EnumerateStructIdentity))]
    public static void InnerPrefixOnIteratorCanReadOriginalStructParameter(BindingStruct outerValue) => StructObserved = outerValue;

    [InnerPrefix(typeof(InnerStaticMethodTargets), nameof(InnerStaticMethodTargets.StructIdentity))]
    [Target(typeof(ClassMethodTargets), nameof(ClassMethodTargets.EnumerateStructIdentity))]
    public static void InnerPrefixOnIteratorCanWriteOriginalStructParameterByReference(ref BindingStruct outerValue) =>
        outerValue = new BindingStruct { Value = 42 };

    [InnerPostfix(typeof(InnerStaticMethodTargets), nameof(InnerStaticMethodTargets.IntIdentity))]
    [Target(typeof(ClassMethodTargets), nameof(ClassMethodTargets.EnumerateIdentity))]
    public static void InnerPostfixOnIteratorCanReadOriginalMethodParameter(int outerValue) => ParameterObserved = outerValue;

    [InnerPrefix(typeof(InnerStaticMethodTargets), nameof(InnerStaticMethodTargets.IntIdentity))]
    [Target(typeof(ClassMethodTargets), nameof(ClassMethodTargets.EnumerateIdentity))]
    public static void InnerPrefixOnIteratorCanReadDeclaringInstanceField([Field("foo", Scope.Outer)] int value) =>
        FieldObserved = value;

    [InnerPrefix(typeof(InnerStaticMethodTargets), nameof(InnerStaticMethodTargets.IntIdentity))]
    [Target(typeof(ClassMethodTargets), nameof(ClassMethodTargets.EnumerateIdentity))]
    public static void InnerPrefixOnIteratorCanWriteDeclaringInstancePrimitiveFieldByReference(
        [Field("primitiveField", Scope.Outer)] ref int value) => value = 42;

    [InnerPrefix(typeof(InnerStaticMethodTargets), nameof(InnerStaticMethodTargets.IntIdentity))]
    [Target(typeof(ClassMethodTargets), nameof(ClassMethodTargets.EnumerateIdentity))]
    public static void InnerPrefixOnIteratorCanReadDeclaringInstanceReferenceTypeField(
        [Field("referenceField", Scope.Outer)] BindingReference value) => ReferenceObserved = value;

    [InnerPrefix(typeof(InnerStaticMethodTargets), nameof(InnerStaticMethodTargets.IntIdentity))]
    [Target(typeof(ClassMethodTargets), nameof(ClassMethodTargets.EnumerateIdentity))]
    public static void InnerPrefixOnIteratorCanWriteDeclaringInstanceReferenceTypeFieldByReference(
        [Field("referenceField", Scope.Outer)] ref BindingReference value) =>
        value = new BindingReference { Value = 42 };

    [InnerPrefix(typeof(InnerStaticMethodTargets), nameof(InnerStaticMethodTargets.IntIdentity))]
    [Target(typeof(ClassMethodTargets), nameof(ClassMethodTargets.EnumerateIdentity))]
    public static void InnerPrefixOnIteratorCanReadDeclaringInstanceStructField(
        [Field("structField", Scope.Outer)] BindingStruct value) => StructObserved = value;

    [InnerPrefix(typeof(InnerStaticMethodTargets), nameof(InnerStaticMethodTargets.IntIdentity))]
    [Target(typeof(ClassMethodTargets), nameof(ClassMethodTargets.EnumerateIdentity))]
    public static void InnerPrefixOnIteratorCanWriteDeclaringInstanceStructFieldByReference(
        [Field("structField", Scope.Outer)] ref BindingStruct value) =>
        value = new BindingStruct { Value = 42 };

    [InnerPostfix(typeof(InnerStaticMethodTargets), nameof(InnerStaticMethodTargets.IntIdentity))]
    [Target(typeof(ClassMethodTargets), nameof(ClassMethodTargets.EnumerateIdentity))]
    public static void InnerPostfixOnIteratorCanReadDeclaringInstance([Instance(Scope.Outer)] ClassMethodTargets instance) =>
        InstanceObserved = instance;

    [InnerPrefix(typeof(InnerStaticMethodTargets), nameof(InnerStaticMethodTargets.IntResult))]
    [Target(typeof(ClassMethodTargets), nameof(ClassMethodTargets.EnumerateDeclaringInstanceValue))]
    public static void InnerPrefixOnIteratorCanReadDeclaringReferenceTypeInstance(
        [Instance(Scope.Outer)] ClassMethodTargets instance) => InstanceObserved = instance;

    [InnerPrefix(typeof(InnerStaticMethodTargets), nameof(InnerStaticMethodTargets.IntResult))]
    [Target(typeof(ClassMethodTargets), nameof(ClassMethodTargets.EnumerateDeclaringInstanceValue))]
    public static void InnerPrefixOnIteratorCanWriteDeclaringReferenceTypeInstanceByReference(
        [Instance(Scope.Outer)] ref ClassMethodTargets instance) => instance = ReplacementInstance!;

    [InnerPrefix(typeof(InnerStaticMethodTargets), nameof(InnerStaticMethodTargets.IntResult))]
    [Target(typeof(StructMethodTargets), nameof(StructMethodTargets.EnumerateDeclaringInstanceValue))]
    public static void InnerPrefixOnIteratorCanReadDeclaringStructInstance(
        [Instance(Scope.Outer)] StructMethodTargets instance) => StructInstanceFieldObserved = instance.foo;

    [InnerPrefix(typeof(InnerStaticMethodTargets), nameof(InnerStaticMethodTargets.IntResult))]
    [Target(typeof(StructMethodTargets), nameof(StructMethodTargets.EnumerateDeclaringInstanceValue))]
    public static void InnerPrefixOnIteratorCanWriteDeclaringStructInstanceByReference(
        [Instance(Scope.Outer)] ref StructMethodTargets instance) => instance.foo = 42;
}

[TestFixture]
public sealed class IteratorParameterBindingTests : PatchTestBase
{
    [Test]
    public void InnerPrefixOnIteratorCanReadOriginalMethodParameter()
    {
        IteratorParameterBindingPatches.ParameterObserved = 0;
        var target = new ClassMethodTargets();
        ApplyPatch(
            typeof(IteratorParameterBindingPatches),
            nameof(IteratorParameterBindingPatches.InnerPrefixOnIteratorCanReadOriginalMethodParameter));

        int result = target.EnumerateIdentity(42).Single();

        Assert.That(result, Is.EqualTo(42));
        Assert.That(IteratorParameterBindingPatches.ParameterObserved, Is.EqualTo(42));
    }

    [Test]
    public void InnerPrefixOnIteratorCanWriteOriginalMethodParameterByReference()
    {
        var target = new ClassMethodTargets();
        ApplyPatch(
            typeof(IteratorParameterBindingPatches),
            nameof(IteratorParameterBindingPatches.InnerPrefixOnIteratorCanWriteOriginalMethodParameterByReference));

        int result = target.EnumerateIdentity(1).Single();

        Assert.That(result, Is.EqualTo(42));
    }

    [Test]
    public void InnerPrefixOnIteratorCanReadOriginalReferenceTypeParameter()
    {
        IteratorParameterBindingPatches.ReferenceObserved = null;
        var value = new BindingReference { Value = 42 };
        var target = new ClassMethodTargets();
        ApplyPatch(
            typeof(IteratorParameterBindingPatches),
            nameof(IteratorParameterBindingPatches.InnerPrefixOnIteratorCanReadOriginalReferenceTypeParameter));

        target.EnumerateReferenceIdentity(value).Single();

        Assert.That(IteratorParameterBindingPatches.ReferenceObserved, Is.SameAs(value));
    }

    [Test]
    public void InnerPrefixOnIteratorCanWriteOriginalReferenceTypeParameterByReference()
    {
        var target = new ClassMethodTargets();
        ApplyPatch(
            typeof(IteratorParameterBindingPatches),
            nameof(IteratorParameterBindingPatches.InnerPrefixOnIteratorCanWriteOriginalReferenceTypeParameterByReference));

        BindingReference result = target.EnumerateReferenceIdentity(new BindingReference { Value = 1 }).Single();

        Assert.That(result.Value, Is.EqualTo(42));
    }

    [Test]
    public void InnerPrefixOnIteratorCanReadOriginalStructParameter()
    {
        IteratorParameterBindingPatches.StructObserved = default;
        var target = new ClassMethodTargets();
        ApplyPatch(
            typeof(IteratorParameterBindingPatches),
            nameof(IteratorParameterBindingPatches.InnerPrefixOnIteratorCanReadOriginalStructParameter));

        target.EnumerateStructIdentity(new BindingStruct { Value = 42 }).Single();

        Assert.That(IteratorParameterBindingPatches.StructObserved.Value, Is.EqualTo(42));
    }

    [Test]
    public void InnerPrefixOnIteratorCanWriteOriginalStructParameterByReference()
    {
        var target = new ClassMethodTargets();
        ApplyPatch(
            typeof(IteratorParameterBindingPatches),
            nameof(IteratorParameterBindingPatches.InnerPrefixOnIteratorCanWriteOriginalStructParameterByReference));

        BindingStruct result = target.EnumerateStructIdentity(new BindingStruct { Value = 1 }).Single();

        Assert.That(result.Value, Is.EqualTo(42));
    }

    [Test]
    public void InnerPostfixOnIteratorCanReadOriginalMethodParameter()
    {
        IteratorParameterBindingPatches.ParameterObserved = 0;
        var target = new ClassMethodTargets();
        ApplyPatch(
            typeof(IteratorParameterBindingPatches),
            nameof(IteratorParameterBindingPatches.InnerPostfixOnIteratorCanReadOriginalMethodParameter));

        int result = target.EnumerateIdentity(42).Single();

        Assert.That(result, Is.EqualTo(42));
        Assert.That(IteratorParameterBindingPatches.ParameterObserved, Is.EqualTo(42));
    }

    [Test]
    public void InnerPrefixOnIteratorCanReadDeclaringInstanceField()
    {
        IteratorParameterBindingPatches.FieldObserved = 0;
        var target = new ClassMethodTargets { foo = 42 };
        ApplyPatch(
            typeof(IteratorParameterBindingPatches),
            nameof(IteratorParameterBindingPatches.InnerPrefixOnIteratorCanReadDeclaringInstanceField));

        int result = target.EnumerateIdentity(1).Single();

        Assert.That(result, Is.EqualTo(1));
        Assert.That(IteratorParameterBindingPatches.FieldObserved, Is.EqualTo(42));
    }

    [Test]
    public void InnerPrefixOnIteratorCanWriteDeclaringInstancePrimitiveFieldByReference()
    {
        var target = new ClassMethodTargets { primitiveField = 1 };
        ApplyPatch(
            typeof(IteratorParameterBindingPatches),
            nameof(IteratorParameterBindingPatches.InnerPrefixOnIteratorCanWriteDeclaringInstancePrimitiveFieldByReference));

        target.EnumerateIdentity(1).Single();

        Assert.That(target.primitiveField, Is.EqualTo(42));
    }

    [Test]
    public void InnerPrefixOnIteratorCanReadDeclaringInstanceReferenceTypeField()
    {
        IteratorParameterBindingPatches.ReferenceObserved = null;
        var field = new BindingReference { Value = 42 };
        var target = new ClassMethodTargets { referenceField = field };
        ApplyPatch(
            typeof(IteratorParameterBindingPatches),
            nameof(IteratorParameterBindingPatches.InnerPrefixOnIteratorCanReadDeclaringInstanceReferenceTypeField));

        target.EnumerateIdentity(1).Single();

        Assert.That(IteratorParameterBindingPatches.ReferenceObserved, Is.SameAs(field));
    }

    [Test]
    public void InnerPrefixOnIteratorCanWriteDeclaringInstanceReferenceTypeFieldByReference()
    {
        var target = new ClassMethodTargets { referenceField = new BindingReference { Value = 1 } };
        ApplyPatch(
            typeof(IteratorParameterBindingPatches),
            nameof(IteratorParameterBindingPatches.InnerPrefixOnIteratorCanWriteDeclaringInstanceReferenceTypeFieldByReference));

        target.EnumerateIdentity(1).Single();

        Assert.That(target.referenceField.Value, Is.EqualTo(42));
    }

    [Test]
    public void InnerPrefixOnIteratorCanReadDeclaringInstanceStructField()
    {
        IteratorParameterBindingPatches.StructObserved = default;
        var target = new ClassMethodTargets { structField = new BindingStruct { Value = 42 } };
        ApplyPatch(
            typeof(IteratorParameterBindingPatches),
            nameof(IteratorParameterBindingPatches.InnerPrefixOnIteratorCanReadDeclaringInstanceStructField));

        target.EnumerateIdentity(1).Single();

        Assert.That(IteratorParameterBindingPatches.StructObserved.Value, Is.EqualTo(42));
    }

    [Test]
    public void InnerPrefixOnIteratorCanWriteDeclaringInstanceStructFieldByReference()
    {
        var target = new ClassMethodTargets { structField = new BindingStruct { Value = 1 } };
        ApplyPatch(
            typeof(IteratorParameterBindingPatches),
            nameof(IteratorParameterBindingPatches.InnerPrefixOnIteratorCanWriteDeclaringInstanceStructFieldByReference));

        target.EnumerateIdentity(1).Single();

        Assert.That(target.structField.Value, Is.EqualTo(42));
    }

    [Test]
    public void InnerPostfixOnIteratorCanReadDeclaringInstance()
    {
        IteratorParameterBindingPatches.InstanceObserved = null;
        var target = new ClassMethodTargets();
        ApplyPatch(
            typeof(IteratorParameterBindingPatches),
            nameof(IteratorParameterBindingPatches.InnerPostfixOnIteratorCanReadDeclaringInstance));

        int result = target.EnumerateIdentity(42).Single();

        Assert.That(result, Is.EqualTo(42));
        Assert.That(IteratorParameterBindingPatches.InstanceObserved, Is.SameAs(target));
    }

    [Test]
    public void InnerPrefixOnIteratorCanReadDeclaringReferenceTypeInstance()
    {
        IteratorParameterBindingPatches.InstanceObserved = null;
        var target = new ClassMethodTargets { foo = 42 };
        ApplyPatch(
            typeof(IteratorParameterBindingPatches),
            nameof(IteratorParameterBindingPatches.InnerPrefixOnIteratorCanReadDeclaringReferenceTypeInstance));

        target.EnumerateDeclaringInstanceValue().Single();

        Assert.That(IteratorParameterBindingPatches.InstanceObserved, Is.SameAs(target));
    }

    [Test]
    public void InnerPrefixOnIteratorCanWriteDeclaringReferenceTypeInstanceByReference()
    {
        var original = new ClassMethodTargets { foo = 1 };
        var replacement = new ClassMethodTargets { foo = 42 };
        IteratorParameterBindingPatches.ReplacementInstance = replacement;
        ApplyPatch(
            typeof(IteratorParameterBindingPatches),
            nameof(IteratorParameterBindingPatches.InnerPrefixOnIteratorCanWriteDeclaringReferenceTypeInstanceByReference));

        int result = original.EnumerateDeclaringInstanceValue().Single();

        Assert.That(result, Is.EqualTo(42));
    }

    [Test]
    public void InnerPrefixOnIteratorCanReadDeclaringStructInstance()
    {
        IteratorParameterBindingPatches.StructInstanceFieldObserved = 0;
        var target = new StructMethodTargets { foo = 42 };
        ApplyPatch(
            typeof(IteratorParameterBindingPatches),
            nameof(IteratorParameterBindingPatches.InnerPrefixOnIteratorCanReadDeclaringStructInstance));

        target.EnumerateDeclaringInstanceValue().Single();

        Assert.That(IteratorParameterBindingPatches.StructInstanceFieldObserved, Is.EqualTo(42));
    }

    [Test]
    public void InnerPrefixOnIteratorCanWriteDeclaringStructInstanceByReference()
    {
        var target = new StructMethodTargets { foo = 1 };
        ApplyPatch(
            typeof(IteratorParameterBindingPatches),
            nameof(IteratorParameterBindingPatches.InnerPrefixOnIteratorCanWriteDeclaringStructInstanceByReference));

        int result = target.EnumerateDeclaringInstanceValue().Single();

        Assert.That(result, Is.EqualTo(42));
        Assert.That(target.foo, Is.EqualTo(1));
    }
}
