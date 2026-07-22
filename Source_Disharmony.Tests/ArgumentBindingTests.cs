namespace Disharmony.Tests;

public static class ArgumentBindingPatches
{
    public static int ValueObserved;
    public static string? ReferenceObserved;
    public static BindingStruct StructObserved;
    public static int InnerObserved;

    [Prefix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.RefIntArgument))]
    public static void PatchCanReadRefParameterWithoutDeclaringRef(int value) => ValueObserved = value;

    [Prefix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.RefIntArgument))]
    public static void PatchCanWriteRefParameterWhenDeclaringRef(ref int value) => value = 42;

    [Prefix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.RefStringArgument))]
    public static void PatchCanReadRefReferenceTypeParameterWithoutDeclaringRef(string value) => ReferenceObserved = value;

    [Prefix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.RefStringArgument))]
    public static void PatchCanWriteRefReferenceTypeParameterWhenDeclaringRef(ref string value) => value = "patched";

    [Prefix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.RefStructArgument))]
    public static void PatchCanReadRefStructParameterWithoutDeclaringRef(BindingStruct value) => StructObserved = value;

    [Prefix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.RefStructArgument))]
    public static void PatchCanWriteRefStructParameterWhenDeclaringRef(ref BindingStruct value) =>
        value = new BindingStruct { Value = 42 };

    [Prefix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.IntArgument))]
    public static void PrefixCanReadValueTypeParameter(int value) => ValueObserved = value;

    [Postfix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.IntArgument))]
    public static void PostfixCanReadValueTypeParameter(int value) => ValueObserved = value;

    [Prefix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.StringArgument))]
    public static void PrefixCanReadReferenceTypeParameter(string value) => ReferenceObserved = value;

    [Prefix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.StructArgument))]
    public static void PrefixCanReadStructParameter(BindingStruct value) => StructObserved = value;

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

    [Prefix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.StructIdentity))]
    public static void PrefixCanWriteStructParameterByReference(ref BindingStruct value) =>
        value = new BindingStruct { Value = 42 };

    [Postfix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.RefStringArgument))]
    public static void PostfixCanWriteReferenceTypeParameterByReference(ref string value) => value = "patched";

    [InnerPrefix(typeof(InnerStaticMethodTargets), nameof(InnerStaticMethodTargets.Void))]
    [Target(typeof(OuterStaticMethodTargets), nameof(OuterStaticMethodTargets.OuterArgument))]
    public static void InnerPrefixCanReadOuterArgumentWhenInnerHasNoMatchingArgument(int outerValue) => InnerObserved = outerValue;

    [InnerPrefix(typeof(InnerStaticMethodTargets), nameof(InnerStaticMethodTargets.Void))]
    [Target(typeof(OuterStaticMethodTargets), nameof(OuterStaticMethodTargets.OuterReferenceTypeArgument))]
    public static void InnerPrefixCanReadOuterReferenceTypeArgumentWhenInnerHasNoMatchingArgument(string outerValue) =>
        ReferenceObserved = outerValue;

    [InnerPrefix(typeof(InnerStaticMethodTargets), nameof(InnerStaticMethodTargets.Void))]
    [Target(typeof(OuterStaticMethodTargets), nameof(OuterStaticMethodTargets.OuterStructArgument))]
    public static void InnerPrefixCanReadOuterStructArgumentWhenInnerHasNoMatchingArgument(BindingStruct outerValue) =>
        StructObserved = outerValue;

    [InnerPostfix(typeof(InnerStaticMethodTargets), nameof(InnerStaticMethodTargets.Void))]
    [Target(typeof(OuterStaticMethodTargets), nameof(OuterStaticMethodTargets.OuterArgument))]
    public static void InnerPostfixCanReadOuterArgumentWhenInnerHasNoMatchingArgument(int outerValue) => InnerObserved = outerValue;

    [InnerPrefix(typeof(InnerStaticMethodTargets), nameof(InnerStaticMethodTargets.Void))]
    [Target(typeof(OuterStaticMethodTargets), nameof(OuterStaticMethodTargets.OuterArgument))]
    public static void InnerPrefixCannotWriteOuterArgumentByReference(ref int outerValue) => outerValue = 42;

    [InnerPrefix(typeof(InnerStaticMethodTargets), nameof(InnerStaticMethodTargets.Void))]
    [Target(typeof(OuterStaticMethodTargets), nameof(OuterStaticMethodTargets.OuterReferenceTypeArgument))]
    public static void InnerPrefixCannotWriteOuterReferenceTypeArgumentByReference(ref string outerValue) =>
        outerValue = "patched";

    [InnerPrefix(typeof(InnerStaticMethodTargets), nameof(InnerStaticMethodTargets.Void))]
    [Target(typeof(OuterStaticMethodTargets), nameof(OuterStaticMethodTargets.OuterStructArgument))]
    public static void InnerPrefixCannotWriteOuterStructArgumentByReference(ref BindingStruct outerValue) =>
        outerValue = new BindingStruct { Value = 42 };

    [InnerPostfix(typeof(InnerStaticMethodTargets), nameof(InnerStaticMethodTargets.Void))]
    [Target(typeof(OuterStaticMethodTargets), nameof(OuterStaticMethodTargets.OuterArgument))]
    public static void InnerPostfixCannotWriteOuterArgumentByReference(ref int outerValue) => outerValue = 42;

    [InnerPrefix(typeof(InnerStaticMethodTargets), nameof(InnerStaticMethodTargets.IntArgument))]
    [Target(typeof(OuterStaticMethodTargets), nameof(OuterStaticMethodTargets.SameNamedArgument))]
    public static void InnerPrefixPrefersInnerArgumentWhenOuterArgumentHasSameName(int value) => InnerObserved = value;

    [InnerPrefix(typeof(InnerStaticMethodTargets), nameof(InnerStaticMethodTargets.StringArgument))]
    [Target(typeof(OuterStaticMethodTargets), nameof(OuterStaticMethodTargets.SameNamedReferenceTypeArgument))]
    public static void InnerPrefixPrefersInnerReferenceTypeArgumentWhenOuterArgumentHasSameName(string value) =>
        ReferenceObserved = value;

    [InnerPrefix(typeof(InnerStaticMethodTargets), nameof(InnerStaticMethodTargets.StructArgument))]
    [Target(typeof(OuterStaticMethodTargets), nameof(OuterStaticMethodTargets.SameNamedStructArgument))]
    public static void InnerPrefixPrefersInnerStructArgumentWhenOuterArgumentHasSameName(BindingStruct value) =>
        StructObserved = value;

    [InnerPostfix(typeof(InnerStaticMethodTargets), nameof(InnerStaticMethodTargets.IntArgument))]
    [Target(typeof(OuterStaticMethodTargets), nameof(OuterStaticMethodTargets.SameNamedArgument))]
    public static void InnerPostfixPrefersInnerArgumentWhenOuterArgumentHasSameName(int value) => InnerObserved = value;

    [InnerPrefix(typeof(InnerStaticMethodTargets), nameof(InnerStaticMethodTargets.RefIntArgument))]
    [Target(typeof(OuterStaticMethodTargets), nameof(OuterStaticMethodTargets.SameNamedRefArgument))]
    public static void InnerPrefixCanWriteInnerArgumentWhenOuterArgumentHasSameName(ref int value) => value = 42;

    [InnerPrefix(typeof(InnerStaticMethodTargets), nameof(InnerStaticMethodTargets.RefStringArgument))]
    [Target(typeof(OuterStaticMethodTargets), nameof(OuterStaticMethodTargets.SameNamedRefReferenceTypeArgument))]
    public static void InnerPrefixCanWriteInnerReferenceTypeArgumentWhenOuterArgumentHasSameName(ref string value) =>
        value = "patched";

    [InnerPrefix(typeof(InnerStaticMethodTargets), nameof(InnerStaticMethodTargets.RefStructArgument))]
    [Target(typeof(OuterStaticMethodTargets), nameof(OuterStaticMethodTargets.SameNamedRefStructArgument))]
    public static void InnerPrefixCanWriteInnerStructArgumentWhenOuterArgumentHasSameName(ref BindingStruct value) =>
        value = new BindingStruct { Value = 42 };

    [InnerPostfix(typeof(InnerStaticMethodTargets), nameof(InnerStaticMethodTargets.RefIntArgument))]
    [Target(typeof(OuterStaticMethodTargets), nameof(OuterStaticMethodTargets.SameNamedRefArgument))]
    public static void InnerPostfixCanWriteInnerArgumentWhenOuterArgumentHasSameName(ref int value) => value = 42;

    [InnerPrefix(typeof(InnerStaticMethodTargets), nameof(InnerStaticMethodTargets.IntArgument))]
    [Target(typeof(OuterStaticMethodTargets), nameof(OuterStaticMethodTargets.IntArgument))]
    public static void InnerPrefixCanReadInnerArgument(int value) => InnerObserved = value;

    [InnerPrefix(typeof(InnerStaticMethodTargets), nameof(InnerStaticMethodTargets.StringArgument))]
    [Target(typeof(OuterStaticMethodTargets), nameof(OuterStaticMethodTargets.StringArgument))]
    public static void InnerPrefixCanReadInnerReferenceTypeArgument(string value) => ReferenceObserved = value;

    [InnerPrefix(typeof(InnerStaticMethodTargets), nameof(InnerStaticMethodTargets.StructArgument))]
    [Target(typeof(OuterStaticMethodTargets), nameof(OuterStaticMethodTargets.StructArgument))]
    public static void InnerPrefixCanReadInnerStructArgument(BindingStruct value) => StructObserved = value;

    [InnerPostfix(typeof(InnerStaticMethodTargets), nameof(InnerStaticMethodTargets.IntArgument))]
    [Target(typeof(OuterStaticMethodTargets), nameof(OuterStaticMethodTargets.IntArgument))]
    public static void InnerPostfixCanReadInnerArgument(int value) => InnerObserved = value;

    [InnerPrefix(typeof(InnerStaticMethodTargets), nameof(InnerStaticMethodTargets.IntIdentity))]
    [Target(typeof(OuterStaticMethodTargets), nameof(OuterStaticMethodTargets.IntIdentity))]
    public static void InnerPrefixCanWriteInnerArgumentByReference(ref int value) => value = 42;

    [InnerPrefix(typeof(InnerStaticMethodTargets), nameof(InnerStaticMethodTargets.StringIdentity))]
    [Target(typeof(OuterStaticMethodTargets), nameof(OuterStaticMethodTargets.StringIdentity))]
    public static void InnerPrefixCanWriteInnerReferenceTypeArgumentByReference(ref string value) => value = "patched";

    [InnerPrefix(typeof(InnerStaticMethodTargets), nameof(InnerStaticMethodTargets.StructIdentity))]
    [Target(typeof(OuterStaticMethodTargets), nameof(OuterStaticMethodTargets.StructIdentity))]
    public static void InnerPrefixCanWriteInnerStructArgumentByReference(ref BindingStruct value) =>
        value = new BindingStruct { Value = 42 };

    [InnerPostfix(typeof(InnerStaticMethodTargets), nameof(InnerStaticMethodTargets.RefIntArgument))]
    [Target(typeof(OuterStaticMethodTargets), nameof(OuterStaticMethodTargets.RefIntArgument))]
    public static void InnerPostfixCanWriteInnerArgumentByReference(ref int value) => value = 42;

    [Prefix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.IntIdentity))]
    public static void ParameterAttributeCanBindWritableArgumentByIndex([Parameter(0)] ref int replacement) => replacement = 42;

    [Prefix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.IntArgument))]
    public static void ParameterAttributeCanReadPrimitiveArgumentByIndex([Parameter(0)] int argument) => ValueObserved = argument;

    [Prefix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.StringArgument))]
    public static void ParameterAttributeCanReadReferenceTypeArgumentByIndex([Parameter(0)] string argument) =>
        ReferenceObserved = argument;

    [Prefix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.StringIdentity))]
    public static void ParameterAttributeCanWriteReferenceTypeArgumentByIndex([Parameter(0)] ref string argument) =>
        argument = "patched";

    [Prefix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.StructArgument))]
    public static void ParameterAttributeCanReadStructArgumentByIndex([Parameter(0)] BindingStruct argument) =>
        StructObserved = argument;

    [Prefix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.StructIdentity))]
    public static void ParameterAttributeCanWriteStructArgumentByIndex([Parameter(0)] ref BindingStruct argument) =>
        argument = new BindingStruct { Value = 42 };

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

    [Test]
    public void PatchCanReadRefReferenceTypeParameterWithoutDeclaringRef()
    {
        ArgumentBindingPatches.ReferenceObserved = null;
        ApplyPatch(typeof(ArgumentBindingPatches), nameof(ArgumentBindingPatches.PatchCanReadRefReferenceTypeParameterWithoutDeclaringRef));
        string value = "original";

        StaticMethodTargets.RefStringArgument(ref value);

        Assert.That(ArgumentBindingPatches.ReferenceObserved, Is.EqualTo("original"));
    }

    [Test]
    public void PatchCanWriteRefReferenceTypeParameterWhenDeclaringRef()
    {
        ApplyPatch(typeof(ArgumentBindingPatches), nameof(ArgumentBindingPatches.PatchCanWriteRefReferenceTypeParameterWhenDeclaringRef));
        string value = "original";

        StaticMethodTargets.RefStringArgument(ref value);

        Assert.That(value, Is.EqualTo("patched"));
    }

    [Test]
    public void PatchCanReadRefStructParameterWithoutDeclaringRef()
    {
        ArgumentBindingPatches.StructObserved = default;
        ApplyPatch(typeof(ArgumentBindingPatches), nameof(ArgumentBindingPatches.PatchCanReadRefStructParameterWithoutDeclaringRef));
        var value = new BindingStruct { Value = 42 };

        StaticMethodTargets.RefStructArgument(ref value);

        Assert.That(ArgumentBindingPatches.StructObserved.Value, Is.EqualTo(42));
    }

    [Test]
    public void PatchCanWriteRefStructParameterWhenDeclaringRef()
    {
        ApplyPatch(typeof(ArgumentBindingPatches), nameof(ArgumentBindingPatches.PatchCanWriteRefStructParameterWhenDeclaringRef));
        var value = new BindingStruct { Value = 1 };

        StaticMethodTargets.RefStructArgument(ref value);

        Assert.That(value.Value, Is.EqualTo(42));
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
    public void PrefixCanReadStructParameter()
    {
        ArgumentBindingPatches.StructObserved = default;
        ApplyPatch(typeof(ArgumentBindingPatches), nameof(ArgumentBindingPatches.PrefixCanReadStructParameter));

        StaticMethodTargets.StructArgument(new BindingStruct { Value = 42 });

        Assert.That(ArgumentBindingPatches.StructObserved.Value, Is.EqualTo(42));
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
    public void PrefixCanWriteStructParameterByReference()
    {
        ApplyPatch(typeof(ArgumentBindingPatches), nameof(ArgumentBindingPatches.PrefixCanWriteStructParameterByReference));

        BindingStruct result = StaticMethodTargets.StructIdentity(new BindingStruct { Value = 1 });

        Assert.That(result.Value, Is.EqualTo(42));
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
    public void InnerPrefixCanReadOuterReferenceTypeArgumentWhenInnerHasNoMatchingArgument()
    {
        ArgumentBindingPatches.ReferenceObserved = null;
        ApplyPatch(typeof(ArgumentBindingPatches), nameof(ArgumentBindingPatches.InnerPrefixCanReadOuterReferenceTypeArgumentWhenInnerHasNoMatchingArgument));

        OuterStaticMethodTargets.OuterReferenceTypeArgument("original");

        Assert.That(ArgumentBindingPatches.ReferenceObserved, Is.EqualTo("original"));
    }

    [Test]
    public void InnerPrefixCanReadOuterStructArgumentWhenInnerHasNoMatchingArgument()
    {
        ArgumentBindingPatches.StructObserved = default;
        ApplyPatch(typeof(ArgumentBindingPatches), nameof(ArgumentBindingPatches.InnerPrefixCanReadOuterStructArgumentWhenInnerHasNoMatchingArgument));

        OuterStaticMethodTargets.OuterStructArgument(new BindingStruct { Value = 42 });

        Assert.That(ArgumentBindingPatches.StructObserved.Value, Is.EqualTo(42));
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
    public void InnerPrefixCannotWriteOuterReferenceTypeArgumentByReference()
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            ApplyPatch(typeof(ArgumentBindingPatches), nameof(ArgumentBindingPatches.InnerPrefixCannotWriteOuterReferenceTypeArgumentByReference)));

        Assert.That(exception!.InnerException, Is.TypeOf<ParameterBindingException>());
        Assert.That(exception.InnerException!.Message, Is.EqualTo("outerValue: Outer method parameter can't be accessed by ref"));
    }

    [Test]
    public void InnerPrefixCannotWriteOuterStructArgumentByReference()
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            ApplyPatch(typeof(ArgumentBindingPatches), nameof(ArgumentBindingPatches.InnerPrefixCannotWriteOuterStructArgumentByReference)));

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
    public void InnerPrefixPrefersInnerReferenceTypeArgumentWhenOuterArgumentHasSameName()
    {
        ArgumentBindingPatches.ReferenceObserved = null;
        ApplyPatch(typeof(ArgumentBindingPatches), nameof(ArgumentBindingPatches.InnerPrefixPrefersInnerReferenceTypeArgumentWhenOuterArgumentHasSameName));

        OuterStaticMethodTargets.SameNamedReferenceTypeArgument("outer");

        Assert.That(ArgumentBindingPatches.ReferenceObserved, Is.EqualTo("inner"));
    }

    [Test]
    public void InnerPrefixPrefersInnerStructArgumentWhenOuterArgumentHasSameName()
    {
        ArgumentBindingPatches.StructObserved = default;
        ApplyPatch(typeof(ArgumentBindingPatches), nameof(ArgumentBindingPatches.InnerPrefixPrefersInnerStructArgumentWhenOuterArgumentHasSameName));

        OuterStaticMethodTargets.SameNamedStructArgument(new BindingStruct { Value = 1 });

        Assert.That(ArgumentBindingPatches.StructObserved.Value, Is.EqualTo(42));
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
    public void InnerPrefixCanWriteInnerReferenceTypeArgumentWhenOuterArgumentHasSameName()
    {
        ApplyPatch(typeof(ArgumentBindingPatches), nameof(ArgumentBindingPatches.InnerPrefixCanWriteInnerReferenceTypeArgumentWhenOuterArgumentHasSameName));
        string outerValue = "outer";

        string result = OuterStaticMethodTargets.SameNamedRefReferenceTypeArgument(ref outerValue);

        Assert.That(result, Is.EqualTo("patched"));
        Assert.That(outerValue, Is.EqualTo("outer"));
    }

    [Test]
    public void InnerPrefixCanWriteInnerStructArgumentWhenOuterArgumentHasSameName()
    {
        ApplyPatch(typeof(ArgumentBindingPatches), nameof(ArgumentBindingPatches.InnerPrefixCanWriteInnerStructArgumentWhenOuterArgumentHasSameName));
        var outerValue = new BindingStruct { Value = 7 };

        BindingStruct result = OuterStaticMethodTargets.SameNamedRefStructArgument(ref outerValue);

        Assert.That(result.Value, Is.EqualTo(42));
        Assert.That(outerValue.Value, Is.EqualTo(7));
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
    public void InnerPrefixCanReadInnerReferenceTypeArgument()
    {
        ArgumentBindingPatches.ReferenceObserved = null;
        ApplyPatch(typeof(ArgumentBindingPatches), nameof(ArgumentBindingPatches.InnerPrefixCanReadInnerReferenceTypeArgument));

        OuterStaticMethodTargets.StringArgument("original");

        Assert.That(ArgumentBindingPatches.ReferenceObserved, Is.EqualTo("original"));
    }

    [Test]
    public void InnerPrefixCanReadInnerStructArgument()
    {
        ArgumentBindingPatches.StructObserved = default;
        ApplyPatch(typeof(ArgumentBindingPatches), nameof(ArgumentBindingPatches.InnerPrefixCanReadInnerStructArgument));

        OuterStaticMethodTargets.StructArgument(new BindingStruct { Value = 42 });

        Assert.That(ArgumentBindingPatches.StructObserved.Value, Is.EqualTo(42));
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
    public void InnerPrefixCanWriteInnerReferenceTypeArgumentByReference()
    {
        ApplyPatch(typeof(ArgumentBindingPatches), nameof(ArgumentBindingPatches.InnerPrefixCanWriteInnerReferenceTypeArgumentByReference));

        string result = OuterStaticMethodTargets.StringIdentity("original");

        Assert.That(result, Is.EqualTo("patched"));
    }

    [Test]
    public void InnerPrefixCanWriteInnerStructArgumentByReference()
    {
        ApplyPatch(typeof(ArgumentBindingPatches), nameof(ArgumentBindingPatches.InnerPrefixCanWriteInnerStructArgumentByReference));

        BindingStruct result = OuterStaticMethodTargets.StructIdentity(new BindingStruct { Value = 1 });

        Assert.That(result.Value, Is.EqualTo(42));
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
    public void ParameterAttributeCanReadPrimitiveArgumentByIndex()
    {
        ArgumentBindingPatches.ValueObserved = 0;
        ApplyPatch(typeof(ArgumentBindingPatches), nameof(ArgumentBindingPatches.ParameterAttributeCanReadPrimitiveArgumentByIndex));

        StaticMethodTargets.IntArgument(42);

        Assert.That(ArgumentBindingPatches.ValueObserved, Is.EqualTo(42));
    }

    [Test]
    public void ParameterAttributeCanReadReferenceTypeArgumentByIndex()
    {
        ArgumentBindingPatches.ReferenceObserved = null;
        ApplyPatch(typeof(ArgumentBindingPatches), nameof(ArgumentBindingPatches.ParameterAttributeCanReadReferenceTypeArgumentByIndex));

        StaticMethodTargets.StringArgument("original");

        Assert.That(ArgumentBindingPatches.ReferenceObserved, Is.EqualTo("original"));
    }

    [Test]
    public void ParameterAttributeCanWriteReferenceTypeArgumentByIndex()
    {
        ApplyPatch(typeof(ArgumentBindingPatches), nameof(ArgumentBindingPatches.ParameterAttributeCanWriteReferenceTypeArgumentByIndex));

        string result = StaticMethodTargets.StringIdentity("original");

        Assert.That(result, Is.EqualTo("patched"));
    }

    [Test]
    public void ParameterAttributeCanReadStructArgumentByIndex()
    {
        ArgumentBindingPatches.StructObserved = default;
        ApplyPatch(typeof(ArgumentBindingPatches), nameof(ArgumentBindingPatches.ParameterAttributeCanReadStructArgumentByIndex));

        StaticMethodTargets.StructArgument(new BindingStruct { Value = 42 });

        Assert.That(ArgumentBindingPatches.StructObserved.Value, Is.EqualTo(42));
    }

    [Test]
    public void ParameterAttributeCanWriteStructArgumentByIndex()
    {
        ApplyPatch(typeof(ArgumentBindingPatches), nameof(ArgumentBindingPatches.ParameterAttributeCanWriteStructArgumentByIndex));

        BindingStruct result = StaticMethodTargets.StructIdentity(new BindingStruct { Value = 1 });

        Assert.That(result.Value, Is.EqualTo(42));
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
