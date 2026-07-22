namespace Disharmony.Tests;

public static class IteratorParameterBindingPatchMethods
{
    public static int ParameterObserved;
    public static int FieldObserved;
    public static ClassMethodTargets? InstanceObserved;

    [InnerPrefix(typeof(InnerStaticMethodTargets), nameof(InnerStaticMethodTargets.IntIdentity))]
    [Target(typeof(ClassMethodTargets), nameof(ClassMethodTargets.EnumerateIdentity))]
    public static void ReadOriginalParameterPrefix(int outerValue) => ParameterObserved = outerValue;

    [InnerPostfix(typeof(InnerStaticMethodTargets), nameof(InnerStaticMethodTargets.IntIdentity))]
    [Target(typeof(ClassMethodTargets), nameof(ClassMethodTargets.EnumerateIdentity))]
    public static void ReadOriginalParameterPostfix(int outerValue) => ParameterObserved = outerValue;

    [InnerPrefix(typeof(InnerStaticMethodTargets), nameof(InnerStaticMethodTargets.IntIdentity))]
    [Target(typeof(ClassMethodTargets), nameof(ClassMethodTargets.EnumerateIdentity))]
    public static void ReadDeclaringInstanceField([Field("foo", Scope.Outer)] int value) => FieldObserved = value;

    [InnerPostfix(typeof(InnerStaticMethodTargets), nameof(InnerStaticMethodTargets.IntIdentity))]
    [Target(typeof(ClassMethodTargets), nameof(ClassMethodTargets.EnumerateIdentity))]
    public static void ReadDeclaringInstance([Instance(Scope.Outer)] ClassMethodTargets instance) => InstanceObserved = instance;
}

[TestFixture]
public sealed class IteratorParameterBindingTests : PatchTestBase
{
    [Test]
    public void InnerPrefixOnIteratorCanReadOriginalMethodParameter()
    {
        IteratorParameterBindingPatchMethods.ParameterObserved = 0;
        var target = new ClassMethodTargets();
        ApplyPatch(
            typeof(IteratorParameterBindingPatchMethods),
            nameof(IteratorParameterBindingPatchMethods.ReadOriginalParameterPrefix));

        int result = target.EnumerateIdentity(42).Single();

        Assert.That(result, Is.EqualTo(42));
        Assert.That(IteratorParameterBindingPatchMethods.ParameterObserved, Is.EqualTo(42));
    }

    [Test]
    public void InnerPostfixOnIteratorCanReadOriginalMethodParameter()
    {
        IteratorParameterBindingPatchMethods.ParameterObserved = 0;
        var target = new ClassMethodTargets();
        ApplyPatch(
            typeof(IteratorParameterBindingPatchMethods),
            nameof(IteratorParameterBindingPatchMethods.ReadOriginalParameterPostfix));

        int result = target.EnumerateIdentity(42).Single();

        Assert.That(result, Is.EqualTo(42));
        Assert.That(IteratorParameterBindingPatchMethods.ParameterObserved, Is.EqualTo(42));
    }

    [Test]
    public void InnerPrefixOnIteratorCanReadDeclaringInstanceField()
    {
        IteratorParameterBindingPatchMethods.FieldObserved = 0;
        var target = new ClassMethodTargets { foo = 42 };
        ApplyPatch(
            typeof(IteratorParameterBindingPatchMethods),
            nameof(IteratorParameterBindingPatchMethods.ReadDeclaringInstanceField));

        int result = target.EnumerateIdentity(1).Single();

        Assert.That(result, Is.EqualTo(1));
        Assert.That(IteratorParameterBindingPatchMethods.FieldObserved, Is.EqualTo(42));
    }

    [Test]
    public void InnerPostfixOnIteratorCanReadDeclaringInstance()
    {
        IteratorParameterBindingPatchMethods.InstanceObserved = null;
        var target = new ClassMethodTargets();
        ApplyPatch(
            typeof(IteratorParameterBindingPatchMethods),
            nameof(IteratorParameterBindingPatchMethods.ReadDeclaringInstance));

        int result = target.EnumerateIdentity(42).Single();

        Assert.That(result, Is.EqualTo(42));
        Assert.That(IteratorParameterBindingPatchMethods.InstanceObserved, Is.SameAs(target));
    }
}
