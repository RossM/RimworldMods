using System;
using NUnit.Framework;

namespace Disharmony.Tests;

[TestFixture]
public sealed partial class ArgumentBindingTests
{
    [Test]
    public void PatchCanReadRefParameterWithoutDeclaringRef()
    {
        PatchMethods.ValueParameterObserved = 0;
        ApplyPatch(nameof(PatchMethods.ReadRefParameterPrefix));
        int value = 42;

        StaticMethodTargets.RefIntArgument(ref value);

        Assert.That(PatchMethods.ValueParameterObserved, Is.EqualTo(42));
    }

    [Test]
    public void PatchCanWriteRefParameterWhenDeclaringRef()
    {
        ApplyPatch(nameof(PatchMethods.WriteRefParameterPrefix));
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
        PatchMethods.ValueParameterObserved = 0;
        ApplyPatch(nameof(PatchMethods.ReadValueParameterPrefix));
        StaticMethodTargets.IntArgument(42);

        Assert.That(PatchMethods.ValueParameterObserved, Is.EqualTo(42));
    }

    [Test]
    public void PostfixCanReadValueTypeParameter()
    {
        PatchMethods.ValueParameterObserved = 0;
        ApplyPatch(nameof(PatchMethods.ReadValueParameterPostfix));
        StaticMethodTargets.IntArgument(42);

        Assert.That(PatchMethods.ValueParameterObserved, Is.EqualTo(42));
    }

    [Test]
    public void PrefixCanReadReferenceTypeParameter()
    {
        PatchMethods.ReferenceParameterObserved = null;
        ApplyPatch(nameof(PatchMethods.ReadReferenceParameterPrefix));
        StaticMethodTargets.StringArgument("original");

        Assert.That(PatchMethods.ReferenceParameterObserved, Is.EqualTo("original"));
    }

    [Test]
    public void PostfixCanReadReferenceTypeParameter()
    {
        PatchMethods.ReferenceParameterObserved = null;
        ApplyPatch(nameof(PatchMethods.ReadReferenceParameterPostfix));
        StaticMethodTargets.StringArgument("original");

        Assert.That(PatchMethods.ReferenceParameterObserved, Is.EqualTo("original"));
    }

    [Test]
    public void PrefixCanWriteValueTypeParameterByReference()
    {
        ApplyPatch(nameof(PatchMethods.WriteValueParameterPrefix));
        Assert.That(StaticMethodTargets.IntIdentity(1), Is.EqualTo(42));
    }

    [Test]
    public void PostfixCanWriteValueTypeParameterByReference()
    {
        ApplyPatch(nameof(PatchMethods.WriteValueParameterPostfix));
        int value = 1;
        StaticMethodTargets.RefIntArgument(ref value);
        Assert.That(value, Is.EqualTo(42));
    }

    [Test]
    public void PrefixCanWriteReferenceTypeParameterByReference()
    {
        ApplyPatch(nameof(PatchMethods.WriteReferenceParameterPrefix));
        Assert.That(StaticMethodTargets.StringIdentity("original"), Is.EqualTo("patched"));
    }

    [Test]
    public void PostfixCanWriteReferenceTypeParameterByReference()
    {
        ApplyPatch(nameof(PatchMethods.WriteReferenceParameterPostfix));
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
        InnerPatchMethods.ArgumentObserved = 0;
        ApplyInnerPatch(nameof(InnerPatchMethods.ReadOuterArgumentPrefix));
        OuterStaticMethodTargets.OuterArgument(42);
        Assert.That(InnerPatchMethods.ArgumentObserved, Is.EqualTo(42));
    }

    [Test]
    public void InnerPostfixCanReadOuterArgumentWhenInnerHasNoMatchingArgument()
    {
        InnerPatchMethods.ArgumentObserved = 0;
        ApplyInnerPatch(nameof(InnerPatchMethods.ReadOuterArgumentPostfix));
        OuterStaticMethodTargets.OuterArgument(42);
        Assert.That(InnerPatchMethods.ArgumentObserved, Is.EqualTo(42));
    }

    [Test]
    public void InnerPrefixCannotWriteOuterArgumentByReference()
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            ApplyInnerPatch(nameof(InnerPatchMethods.WriteOuterArgumentPrefix)));

        Assert.That(exception!.InnerException, Is.TypeOf<ParameterBindingException>());
        Assert.That(exception.InnerException!.Message, Is.EqualTo("outerValue: Outer method parameter can't be accessed by ref"));
    }

    [Test]
    public void InnerPostfixCannotWriteOuterArgumentByReference()
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            ApplyInnerPatch(nameof(InnerPatchMethods.WriteOuterArgumentPostfix)));

        Assert.That(exception!.InnerException, Is.TypeOf<ParameterBindingException>());
        Assert.That(exception.InnerException!.Message, Is.EqualTo("outerValue: Outer method parameter can't be accessed by ref"));
    }

    [Test]
    public void InnerPrefixPrefersInnerArgumentWhenOuterArgumentHasSameName()
    {
        InnerPatchMethods.ArgumentObserved = 0;
        ApplyInnerPatch(nameof(InnerPatchMethods.ReadSameNamedArgumentPrefix));
        OuterStaticMethodTargets.SameNamedArgument(1);
        Assert.That(InnerPatchMethods.ArgumentObserved, Is.EqualTo(42));
    }

    [Test]
    public void InnerPostfixPrefersInnerArgumentWhenOuterArgumentHasSameName()
    {
        InnerPatchMethods.ArgumentObserved = 0;
        ApplyInnerPatch(nameof(InnerPatchMethods.ReadSameNamedArgumentPostfix));
        OuterStaticMethodTargets.SameNamedArgument(1);
        Assert.That(InnerPatchMethods.ArgumentObserved, Is.EqualTo(42));
    }

    [Test]
    public void InnerPrefixCanWriteInnerArgumentWhenOuterArgumentHasSameName()
    {
        ApplyInnerPatch(nameof(InnerPatchMethods.WriteSameNamedArgumentPrefix));
        int outerValue = 7;

        int result = OuterStaticMethodTargets.SameNamedRefArgument(ref outerValue);

        Assert.That(result, Is.EqualTo(42));
        Assert.That(outerValue, Is.EqualTo(7));
    }

    [Test]
    public void InnerPostfixCanWriteInnerArgumentWhenOuterArgumentHasSameName()
    {
        ApplyInnerPatch(nameof(InnerPatchMethods.WriteSameNamedArgumentPostfix));
        int outerValue = 7;

        int result = OuterStaticMethodTargets.SameNamedRefArgument(ref outerValue);

        Assert.That(result, Is.EqualTo(42));
        Assert.That(outerValue, Is.EqualTo(7));
    }

    [Test]
    public void InnerPrefixCanReadInnerArgument()
    {
        InnerPatchMethods.ArgumentObserved = 0;
        ApplyInnerPatch(nameof(InnerPatchMethods.ReadArgumentPrefix));
        OuterStaticMethodTargets.IntArgument(42);
        Assert.That(InnerPatchMethods.ArgumentObserved, Is.EqualTo(42));
    }

    [Test]
    public void InnerPostfixCanReadInnerArgument()
    {
        InnerPatchMethods.ArgumentObserved = 0;
        ApplyInnerPatch(nameof(InnerPatchMethods.ReadArgumentPostfix));
        OuterStaticMethodTargets.IntArgument(42);
        Assert.That(InnerPatchMethods.ArgumentObserved, Is.EqualTo(42));
    }

    [Test]
    public void InnerPrefixCanWriteInnerArgumentByReference()
    {
        ApplyInnerPatch(nameof(InnerPatchMethods.WriteArgumentPrefix));
        Assert.That(OuterStaticMethodTargets.IntIdentity(1), Is.EqualTo(42));
    }

    [Test]
    public void InnerPostfixCanWriteInnerArgumentByReference()
    {
        ApplyInnerPatch(nameof(InnerPatchMethods.WriteArgumentPostfix));
        int value = 1;
        OuterStaticMethodTargets.RefIntArgument(ref value);
        Assert.That(value, Is.EqualTo(42));
    }
}
