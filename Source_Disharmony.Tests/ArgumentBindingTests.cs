namespace Disharmony.Tests;

public static class ArgumentBindingPatches
{
    public static int ValueObserved;
    public static string? ReferenceObserved;
    public static int InnerObserved;

    [Prefix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.RefIntArgument))]
    public static void PatchCanReadRefParameterWithoutDeclaringRef(int value) => ValueObserved = value;

    [Prefix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.RefIntArgument))]
    public static void PatchCanWriteRefParameterWhenDeclaringRef(ref int value) => value = 42;

    [Prefix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.IntArgument))]
    public static void PrefixCanReadValueTypeParameter(int value) => ValueObserved = value;

    [Postfix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.IntArgument))]
    public static void PostfixCanReadValueTypeParameter(int value) => ValueObserved = value;

    [Prefix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.StringArgument))]
    public static void PrefixCanReadReferenceTypeParameter(string value) => ReferenceObserved = value;

    [Postfix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.StringArgument))]
    public static void PostfixCanReadReferenceTypeParameter(string value) => ReferenceObserved = value;

    [Prefix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.IntIdentity))]
    public static void PrefixCanWriteValueTypeParameterByReference(ref int value) => value = 42;

    [Postfix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.RefIntArgument))]
    public static void PostfixCanWriteValueTypeParameterByReference(ref int value) => value = 42;

    [Prefix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.StringIdentity))]
    public static void PrefixCanWriteReferenceTypeParameterByReference(ref string value) => value = "patched";

    [Postfix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.RefStringArgument))]
    public static void PostfixCanWriteReferenceTypeParameterByReference(ref string value) => value = "patched";

    [InnerPrefix(typeof(InnerStaticMethodTargets), nameof(InnerStaticMethodTargets.Void))]
    [Target(typeof(OuterStaticMethodTargets), nameof(OuterStaticMethodTargets.OuterArgument))]
    public static void InnerPrefixCanReadOuterArgumentWhenInnerHasNoMatchingArgument(int outerValue) => InnerObserved = outerValue;

    [InnerPostfix(typeof(InnerStaticMethodTargets), nameof(InnerStaticMethodTargets.Void))]
    [Target(typeof(OuterStaticMethodTargets), nameof(OuterStaticMethodTargets.OuterArgument))]
    public static void InnerPostfixCanReadOuterArgumentWhenInnerHasNoMatchingArgument(int outerValue) => InnerObserved = outerValue;

    [InnerPrefix(typeof(InnerStaticMethodTargets), nameof(InnerStaticMethodTargets.Void))]
    [Target(typeof(OuterStaticMethodTargets), nameof(OuterStaticMethodTargets.OuterArgument))]
    public static void InnerPrefixCannotWriteOuterArgumentByReference(ref int outerValue) => outerValue = 42;

    [InnerPostfix(typeof(InnerStaticMethodTargets), nameof(InnerStaticMethodTargets.Void))]
    [Target(typeof(OuterStaticMethodTargets), nameof(OuterStaticMethodTargets.OuterArgument))]
    public static void InnerPostfixCannotWriteOuterArgumentByReference(ref int outerValue) => outerValue = 42;

    [InnerPrefix(typeof(InnerStaticMethodTargets), nameof(InnerStaticMethodTargets.IntArgument))]
    [Target(typeof(OuterStaticMethodTargets), nameof(OuterStaticMethodTargets.SameNamedArgument))]
    public static void InnerPrefixPrefersInnerArgumentWhenOuterArgumentHasSameName(int value) => InnerObserved = value;

    [InnerPostfix(typeof(InnerStaticMethodTargets), nameof(InnerStaticMethodTargets.IntArgument))]
    [Target(typeof(OuterStaticMethodTargets), nameof(OuterStaticMethodTargets.SameNamedArgument))]
    public static void InnerPostfixPrefersInnerArgumentWhenOuterArgumentHasSameName(int value) => InnerObserved = value;

    [InnerPrefix(typeof(InnerStaticMethodTargets), nameof(InnerStaticMethodTargets.RefIntArgument))]
    [Target(typeof(OuterStaticMethodTargets), nameof(OuterStaticMethodTargets.SameNamedRefArgument))]
    public static void InnerPrefixCanWriteInnerArgumentWhenOuterArgumentHasSameName(ref int value) => value = 42;

    [InnerPostfix(typeof(InnerStaticMethodTargets), nameof(InnerStaticMethodTargets.RefIntArgument))]
    [Target(typeof(OuterStaticMethodTargets), nameof(OuterStaticMethodTargets.SameNamedRefArgument))]
    public static void InnerPostfixCanWriteInnerArgumentWhenOuterArgumentHasSameName(ref int value) => value = 42;

    [InnerPrefix(typeof(InnerStaticMethodTargets), nameof(InnerStaticMethodTargets.IntArgument))]
    [Target(typeof(OuterStaticMethodTargets), nameof(OuterStaticMethodTargets.IntArgument))]
    public static void InnerPrefixCanReadInnerArgument(int value) => InnerObserved = value;

    [InnerPostfix(typeof(InnerStaticMethodTargets), nameof(InnerStaticMethodTargets.IntArgument))]
    [Target(typeof(OuterStaticMethodTargets), nameof(OuterStaticMethodTargets.IntArgument))]
    public static void InnerPostfixCanReadInnerArgument(int value) => InnerObserved = value;

    [InnerPrefix(typeof(InnerStaticMethodTargets), nameof(InnerStaticMethodTargets.IntIdentity))]
    [Target(typeof(OuterStaticMethodTargets), nameof(OuterStaticMethodTargets.IntIdentity))]
    public static void InnerPrefixCanWriteInnerArgumentByReference(ref int value) => value = 42;

    [InnerPostfix(typeof(InnerStaticMethodTargets), nameof(InnerStaticMethodTargets.RefIntArgument))]
    [Target(typeof(OuterStaticMethodTargets), nameof(OuterStaticMethodTargets.RefIntArgument))]
    public static void InnerPostfixCanWriteInnerArgumentByReference(ref int value) => value = 42;

    [Prefix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.IntIdentity))]
    public static void ParameterAttributeCanBindWritableArgumentByIndex([Parameter(0)] ref int replacement) => replacement = 42;

    [InnerPrefix(typeof(InnerStaticMethodTargets), nameof(InnerStaticMethodTargets.IntArgument))]
    [Target(typeof(OuterStaticMethodTargets), nameof(OuterStaticMethodTargets.SameNamedArgument))]
    public static void ParameterAttributeCanSelectOuterArgumentByName(
        [Parameter("value", Scope.Outer)] int outerValue) => InnerObserved = outerValue;

    [InnerPrefix(typeof(InnerStaticMethodTargets), nameof(InnerStaticMethodTargets.IntArgument))]
    [Target(typeof(OuterStaticMethodTargets), nameof(OuterStaticMethodTargets.SameNamedArgument))]
    public static void ParameterAttributeCanSelectInnerArgumentByName(
        [Parameter("value", Scope.Inner)] int innerValue) => InnerObserved = innerValue;
}

[TestFixture]
public sealed partial class ArgumentBindingTests
{
    [Test]
    public void PatchCanReadRefParameterWithoutDeclaringRef()
    {
        ArgumentBindingPatches.ValueObserved = 0;
        ApplyPatch(typeof(ArgumentBindingPatches), nameof(ArgumentBindingPatches.PatchCanReadRefParameterWithoutDeclaringRef));
        int value = 42;

        StaticMethodTargets.RefIntArgument(ref value);

        Assert.That(ArgumentBindingPatches.ValueObserved, Is.EqualTo(42));
    }

    [Test]
    public void PatchCanWriteRefParameterWhenDeclaringRef()
    {
        ApplyPatch(typeof(ArgumentBindingPatches), nameof(ArgumentBindingPatches.PatchCanWriteRefParameterWhenDeclaringRef));
        int value = 1;

        StaticMethodTargets.RefIntArgument(ref value);

        Assert.That(value, Is.EqualTo(42));
    }
}

[TestFixture]
public sealed partial class ArgumentBindingTests : PatchTestBase
{
    [Test]
    public void PrefixCanReadValueTypeParameter()
    {
        ArgumentBindingPatches.ValueObserved = 0;
        ApplyPatch(typeof(ArgumentBindingPatches), nameof(ArgumentBindingPatches.PrefixCanReadValueTypeParameter));
        StaticMethodTargets.IntArgument(42);

        Assert.That(ArgumentBindingPatches.ValueObserved, Is.EqualTo(42));
    }

    [Test]
    public void PostfixCanReadValueTypeParameter()
    {
        ArgumentBindingPatches.ValueObserved = 0;
        ApplyPatch(typeof(ArgumentBindingPatches), nameof(ArgumentBindingPatches.PostfixCanReadValueTypeParameter));
        StaticMethodTargets.IntArgument(42);

        Assert.That(ArgumentBindingPatches.ValueObserved, Is.EqualTo(42));
    }

    [Test]
    public void PrefixCanReadReferenceTypeParameter()
    {
        ArgumentBindingPatches.ReferenceObserved = null;
        ApplyPatch(typeof(ArgumentBindingPatches), nameof(ArgumentBindingPatches.PrefixCanReadReferenceTypeParameter));
        StaticMethodTargets.StringArgument("original");

        Assert.That(ArgumentBindingPatches.ReferenceObserved, Is.EqualTo("original"));
    }

    [Test]
    public void PostfixCanReadReferenceTypeParameter()
    {
        ArgumentBindingPatches.ReferenceObserved = null;
        ApplyPatch(typeof(ArgumentBindingPatches), nameof(ArgumentBindingPatches.PostfixCanReadReferenceTypeParameter));
        StaticMethodTargets.StringArgument("original");

        Assert.That(ArgumentBindingPatches.ReferenceObserved, Is.EqualTo("original"));
    }

    [Test]
    public void PrefixCanWriteValueTypeParameterByReference()
    {
        ApplyPatch(typeof(ArgumentBindingPatches), nameof(ArgumentBindingPatches.PrefixCanWriteValueTypeParameterByReference));
        Assert.That(StaticMethodTargets.IntIdentity(1), Is.EqualTo(42));
    }

    [Test]
    public void PostfixCanWriteValueTypeParameterByReference()
    {
        ApplyPatch(typeof(ArgumentBindingPatches), nameof(ArgumentBindingPatches.PostfixCanWriteValueTypeParameterByReference));
        int value = 1;
        StaticMethodTargets.RefIntArgument(ref value);
        Assert.That(value, Is.EqualTo(42));
    }

    [Test]
    public void PrefixCanWriteReferenceTypeParameterByReference()
    {
        ApplyPatch(typeof(ArgumentBindingPatches), nameof(ArgumentBindingPatches.PrefixCanWriteReferenceTypeParameterByReference));
        Assert.That(StaticMethodTargets.StringIdentity("original"), Is.EqualTo("patched"));
    }

    [Test]
    public void PostfixCanWriteReferenceTypeParameterByReference()
    {
        ApplyPatch(typeof(ArgumentBindingPatches), nameof(ArgumentBindingPatches.PostfixCanWriteReferenceTypeParameterByReference));
        string value = "original";
        StaticMethodTargets.RefStringArgument(ref value);
        Assert.That(value, Is.EqualTo("patched"));
    }
}

[TestFixture]
public sealed partial class ArgumentBindingTests
{
    [Test]
    public void InnerPrefixCanReadOuterArgumentWhenInnerHasNoMatchingArgument()
    {
        ArgumentBindingPatches.InnerObserved = 0;
        ApplyPatch(typeof(ArgumentBindingPatches), nameof(ArgumentBindingPatches.InnerPrefixCanReadOuterArgumentWhenInnerHasNoMatchingArgument));
        OuterStaticMethodTargets.OuterArgument(42);
        Assert.That(ArgumentBindingPatches.InnerObserved, Is.EqualTo(42));
    }

    [Test]
    public void InnerPostfixCanReadOuterArgumentWhenInnerHasNoMatchingArgument()
    {
        ArgumentBindingPatches.InnerObserved = 0;
        ApplyPatch(typeof(ArgumentBindingPatches), nameof(ArgumentBindingPatches.InnerPostfixCanReadOuterArgumentWhenInnerHasNoMatchingArgument));
        OuterStaticMethodTargets.OuterArgument(42);
        Assert.That(ArgumentBindingPatches.InnerObserved, Is.EqualTo(42));
    }

    [Test]
    public void InnerPrefixCannotWriteOuterArgumentByReference()
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            ApplyPatch(typeof(ArgumentBindingPatches), nameof(ArgumentBindingPatches.InnerPrefixCannotWriteOuterArgumentByReference)));

        Assert.That(exception!.InnerException, Is.TypeOf<ParameterBindingException>());
        Assert.That(exception.InnerException!.Message, Is.EqualTo("outerValue: Outer method parameter can't be accessed by ref"));
    }

    [Test]
    public void InnerPostfixCannotWriteOuterArgumentByReference()
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            ApplyPatch(typeof(ArgumentBindingPatches), nameof(ArgumentBindingPatches.InnerPostfixCannotWriteOuterArgumentByReference)));

        Assert.That(exception!.InnerException, Is.TypeOf<ParameterBindingException>());
        Assert.That(exception.InnerException!.Message, Is.EqualTo("outerValue: Outer method parameter can't be accessed by ref"));
    }

    [Test]
    public void InnerPrefixPrefersInnerArgumentWhenOuterArgumentHasSameName()
    {
        ArgumentBindingPatches.InnerObserved = 0;
        ApplyPatch(typeof(ArgumentBindingPatches), nameof(ArgumentBindingPatches.InnerPrefixPrefersInnerArgumentWhenOuterArgumentHasSameName));
        OuterStaticMethodTargets.SameNamedArgument(1);
        Assert.That(ArgumentBindingPatches.InnerObserved, Is.EqualTo(42));
    }

    [Test]
    public void InnerPostfixPrefersInnerArgumentWhenOuterArgumentHasSameName()
    {
        ArgumentBindingPatches.InnerObserved = 0;
        ApplyPatch(typeof(ArgumentBindingPatches), nameof(ArgumentBindingPatches.InnerPostfixPrefersInnerArgumentWhenOuterArgumentHasSameName));
        OuterStaticMethodTargets.SameNamedArgument(1);
        Assert.That(ArgumentBindingPatches.InnerObserved, Is.EqualTo(42));
    }

    [Test]
    public void InnerPrefixCanWriteInnerArgumentWhenOuterArgumentHasSameName()
    {
        ApplyPatch(typeof(ArgumentBindingPatches), nameof(ArgumentBindingPatches.InnerPrefixCanWriteInnerArgumentWhenOuterArgumentHasSameName));
        int outerValue = 7;

        int result = OuterStaticMethodTargets.SameNamedRefArgument(ref outerValue);

        Assert.That(result, Is.EqualTo(42));
        Assert.That(outerValue, Is.EqualTo(7));
    }

    [Test]
    public void InnerPostfixCanWriteInnerArgumentWhenOuterArgumentHasSameName()
    {
        ApplyPatch(typeof(ArgumentBindingPatches), nameof(ArgumentBindingPatches.InnerPostfixCanWriteInnerArgumentWhenOuterArgumentHasSameName));
        int outerValue = 7;

        int result = OuterStaticMethodTargets.SameNamedRefArgument(ref outerValue);

        Assert.That(result, Is.EqualTo(42));
        Assert.That(outerValue, Is.EqualTo(7));
    }

    [Test]
    public void InnerPrefixCanReadInnerArgument()
    {
        ArgumentBindingPatches.InnerObserved = 0;
        ApplyPatch(typeof(ArgumentBindingPatches), nameof(ArgumentBindingPatches.InnerPrefixCanReadInnerArgument));
        OuterStaticMethodTargets.IntArgument(42);
        Assert.That(ArgumentBindingPatches.InnerObserved, Is.EqualTo(42));
    }

    [Test]
    public void InnerPostfixCanReadInnerArgument()
    {
        ArgumentBindingPatches.InnerObserved = 0;
        ApplyPatch(typeof(ArgumentBindingPatches), nameof(ArgumentBindingPatches.InnerPostfixCanReadInnerArgument));
        OuterStaticMethodTargets.IntArgument(42);
        Assert.That(ArgumentBindingPatches.InnerObserved, Is.EqualTo(42));
    }

    [Test]
    public void InnerPrefixCanWriteInnerArgumentByReference()
    {
        ApplyPatch(typeof(ArgumentBindingPatches), nameof(ArgumentBindingPatches.InnerPrefixCanWriteInnerArgumentByReference));
        Assert.That(OuterStaticMethodTargets.IntIdentity(1), Is.EqualTo(42));
    }

    [Test]
    public void InnerPostfixCanWriteInnerArgumentByReference()
    {
        ApplyPatch(typeof(ArgumentBindingPatches), nameof(ArgumentBindingPatches.InnerPostfixCanWriteInnerArgumentByReference));
        int value = 1;
        OuterStaticMethodTargets.RefIntArgument(ref value);
        Assert.That(value, Is.EqualTo(42));
    }

    [Test]
    public void ParameterAttributeCanBindWritableArgumentByIndex()
    {
        ApplyPatch(typeof(ArgumentBindingPatches), nameof(ArgumentBindingPatches.ParameterAttributeCanBindWritableArgumentByIndex));

        int result = StaticMethodTargets.IntIdentity(1);

        Assert.That(result, Is.EqualTo(42));
    }

    [Test]
    public void ParameterAttributeCanSelectOuterArgumentByName()
    {
        ArgumentBindingPatches.InnerObserved = 0;
        ApplyPatch(typeof(ArgumentBindingPatches), nameof(ArgumentBindingPatches.ParameterAttributeCanSelectOuterArgumentByName));

        OuterStaticMethodTargets.SameNamedArgument(1);

        Assert.That(ArgumentBindingPatches.InnerObserved, Is.EqualTo(1));
    }

    [Test]
    public void ParameterAttributeCanSelectInnerArgumentByName()
    {
        ArgumentBindingPatches.InnerObserved = 0;
        ApplyPatch(typeof(ArgumentBindingPatches), nameof(ArgumentBindingPatches.ParameterAttributeCanSelectInnerArgumentByName));

        OuterStaticMethodTargets.SameNamedArgument(1);

        Assert.That(ArgumentBindingPatches.InnerObserved, Is.EqualTo(42));
    }
}
