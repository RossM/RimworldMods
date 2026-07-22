namespace Disharmony.Tests;

public static class IteratorParameterBindingPatches
{
    public static int ParameterObserved;
    public static int FieldObserved;
    public static ClassMethodTargets? InstanceObserved;

    [InnerPrefix(typeof(InnerStaticMethodTargets), nameof(InnerStaticMethodTargets.IntIdentity))]
    [Target(typeof(ClassMethodTargets), nameof(ClassMethodTargets.EnumerateIdentity))]
    public static void InnerPrefixOnIteratorCanReadOriginalMethodParameter(int outerValue) => ParameterObserved = outerValue;

    [InnerPostfix(typeof(InnerStaticMethodTargets), nameof(InnerStaticMethodTargets.IntIdentity))]
    [Target(typeof(ClassMethodTargets), nameof(ClassMethodTargets.EnumerateIdentity))]
    public static void InnerPostfixOnIteratorCanReadOriginalMethodParameter(int outerValue) => ParameterObserved = outerValue;

    [InnerPrefix(typeof(InnerStaticMethodTargets), nameof(InnerStaticMethodTargets.IntIdentity))]
    [Target(typeof(ClassMethodTargets), nameof(ClassMethodTargets.EnumerateIdentity))]
    public static void InnerPrefixOnIteratorCanReadDeclaringInstanceField([Field("foo", Scope.Outer)] int value) =>
        FieldObserved = value;

    [InnerPostfix(typeof(InnerStaticMethodTargets), nameof(InnerStaticMethodTargets.IntIdentity))]
    [Target(typeof(ClassMethodTargets), nameof(ClassMethodTargets.EnumerateIdentity))]
    public static void InnerPostfixOnIteratorCanReadDeclaringInstance([Instance(Scope.Outer)] ClassMethodTargets instance) =>
        InstanceObserved = instance;
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
}
