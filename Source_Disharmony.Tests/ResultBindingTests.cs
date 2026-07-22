namespace Disharmony.Tests;

public static partial class ResultBindingPatches
{
    public static int ValueObserved;
    public static string? ReferenceObserved;
    public static int InnerObserved;
    public static BindingStruct StructObserved;

    [Prefix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.IntResult))]
    public static void PrefixReadsDefaultValueTypeResult(int __result) => ValueObserved = __result;

    [Postfix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.IntResult))]
    public static void PostfixReadsValueTypeResult(int __result) => ValueObserved = __result;

    [Prefix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.StringResult))]
    public static void PrefixReadsDefaultReferenceTypeResult(string? __result) => ReferenceObserved = __result;

    [Prefix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.StructResult))]
    public static void PrefixReadsDefaultStructResult(BindingStruct __result) => StructObserved = __result;

    [Postfix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.StringResult))]
    public static void PostfixReadsReferenceTypeResult(string __result) => ReferenceObserved = __result;

    [Postfix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.StructResult))]
    public static void PostfixReadsStructResult(BindingStruct __result) => StructObserved = __result;

    [Prefix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.ThrowingIntResult))]
    public static bool PrefixCanWriteValueTypeResultByReference(ref int __result)
    {
        __result = 42;
        return false;
    }

    [Postfix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.IntResult))]
    public static void PostfixCanWriteValueTypeResultByReference(ref int __result) => __result = 42;

    [Prefix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.ThrowingStringResult))]
    public static bool PrefixCanWriteReferenceTypeResultByReference(ref string? __result)
    {
        __result = "patched";
        return false;
    }

    [Prefix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.ThrowingStructResult))]
    public static bool PrefixCanWriteStructResultByReference(ref BindingStruct __result)
    {
        __result = new BindingStruct { Value = 42 };
        return false;
    }

    [Postfix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.StringResult))]
    public static void PostfixCanWriteReferenceTypeResultByReference(ref string __result) => __result = "patched";

    [Postfix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.StructResult))]
    public static void PostfixCanWriteStructResultByReference(ref BindingStruct __result) =>
        __result = new BindingStruct { Value = 42 };

    [InnerPrefix(typeof(InnerStaticMethodTargets), nameof(InnerStaticMethodTargets.IntResult))]
    [Target(typeof(OuterStaticMethodTargets), nameof(OuterStaticMethodTargets.IntResult))]
    public static void InnerPrefixReadsDefaultInnerResult(int __result) => InnerObserved = __result;

    [InnerPrefix(typeof(InnerStaticMethodTargets), nameof(InnerStaticMethodTargets.StringResult))]
    [Target(typeof(OuterStaticMethodTargets), nameof(OuterStaticMethodTargets.StringResult))]
    public static void InnerPrefixReadsDefaultInnerReferenceTypeResult(string? __result) => ReferenceObserved = __result;

    [InnerPrefix(typeof(InnerStaticMethodTargets), nameof(InnerStaticMethodTargets.StructResult))]
    [Target(typeof(OuterStaticMethodTargets), nameof(OuterStaticMethodTargets.StructResult))]
    public static void InnerPrefixReadsDefaultInnerStructResult(BindingStruct __result) => StructObserved = __result;

    [InnerPostfix(typeof(InnerStaticMethodTargets), nameof(InnerStaticMethodTargets.IntResult))]
    [Target(typeof(OuterStaticMethodTargets), nameof(OuterStaticMethodTargets.IntResult))]
    public static void InnerPostfixReadsInnerResult(int __result) => InnerObserved = __result;

    [InnerPostfix(typeof(InnerStaticMethodTargets), nameof(InnerStaticMethodTargets.StringResult))]
    [Target(typeof(OuterStaticMethodTargets), nameof(OuterStaticMethodTargets.StringResult))]
    public static void InnerPostfixReadsInnerReferenceTypeResult(string __result) => ReferenceObserved = __result;

    [InnerPostfix(typeof(InnerStaticMethodTargets), nameof(InnerStaticMethodTargets.StructResult))]
    [Target(typeof(OuterStaticMethodTargets), nameof(OuterStaticMethodTargets.StructResult))]
    public static void InnerPostfixReadsInnerStructResult(BindingStruct __result) => StructObserved = __result;

    [InnerPrefix(typeof(InnerStaticMethodTargets), nameof(InnerStaticMethodTargets.IntResult))]
    [Target(typeof(OuterStaticMethodTargets), nameof(OuterStaticMethodTargets.IntResult))]
    public static bool InnerPrefixCanWriteInnerResultByReference(ref int __result)
    {
        __result = 42;
        return false;
    }

    [InnerPrefix(typeof(InnerStaticMethodTargets), nameof(InnerStaticMethodTargets.StringResult))]
    [Target(typeof(OuterStaticMethodTargets), nameof(OuterStaticMethodTargets.StringResult))]
    public static bool InnerPrefixCanWriteInnerReferenceTypeResultByReference(ref string? __result)
    {
        __result = "patched";
        return false;
    }

    [InnerPrefix(typeof(InnerStaticMethodTargets), nameof(InnerStaticMethodTargets.StructResult))]
    [Target(typeof(OuterStaticMethodTargets), nameof(OuterStaticMethodTargets.StructResult))]
    public static bool InnerPrefixCanWriteInnerStructResultByReference(ref BindingStruct __result)
    {
        __result = new BindingStruct { Value = 42 };
        return false;
    }

    [InnerPostfix(typeof(InnerStaticMethodTargets), nameof(InnerStaticMethodTargets.IntResult))]
    [Target(typeof(OuterStaticMethodTargets), nameof(OuterStaticMethodTargets.IntResult))]
    public static void InnerPostfixCanWriteInnerResultByReference(ref int __result) => __result = 42;

    [InnerPostfix(typeof(InnerStaticMethodTargets), nameof(InnerStaticMethodTargets.StringResult))]
    [Target(typeof(OuterStaticMethodTargets), nameof(OuterStaticMethodTargets.StringResult))]
    public static void InnerPostfixCanWriteInnerReferenceTypeResultByReference(ref string __result) => __result = "patched";

    [InnerPostfix(typeof(InnerStaticMethodTargets), nameof(InnerStaticMethodTargets.StructResult))]
    [Target(typeof(OuterStaticMethodTargets), nameof(OuterStaticMethodTargets.StructResult))]
    public static void InnerPostfixCanWriteInnerStructResultByReference(ref BindingStruct __result) =>
        __result = new BindingStruct { Value = 42 };

    [Postfix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.IntResult))]
    public static void ReturnValueAttributeBindsWritableResult([ReturnValue] ref int value) => value = 42;

    [Prefix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.IntResult))]
    public static void ReturnValueAttributeCanReadDefaultPrimitiveResult([ReturnValue] int value) => ValueObserved = value;

    [Prefix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.StringResult))]
    public static void ReturnValueAttributeCanReadDefaultReferenceTypeResult([ReturnValue] string? value) =>
        ReferenceObserved = value;

    [Prefix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.ThrowingStringResult))]
    public static bool ReturnValueAttributeCanWriteReferenceTypeResultByReference([ReturnValue] ref string? value)
    {
        value = "patched";
        return false;
    }

    [Prefix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.StructResult))]
    public static void ReturnValueAttributeCanReadDefaultStructResult([ReturnValue] BindingStruct value) => StructObserved = value;

    [Prefix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.ThrowingStructResult))]
    public static bool ReturnValueAttributeCanWriteStructResultByReference([ReturnValue] ref BindingStruct value)
    {
        value = new BindingStruct { Value = 42 };
        return false;
    }

    [Postfix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.IntResult))]
    public static void ReturnValueAttributeCanReadPrimitiveResultInPostfix([ReturnValue] int value) => ValueObserved = value;

    [Postfix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.StringResult))]
    public static void ReturnValueAttributeCanReadReferenceTypeResultInPostfix([ReturnValue] string value) =>
        ReferenceObserved = value;

    [Postfix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.StringResult))]
    public static void ReturnValueAttributeCanWriteReferenceTypeResultInPostfix([ReturnValue] ref string value) =>
        value = "patched";

    [Postfix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.StructResult))]
    public static void ReturnValueAttributeCanReadStructResultInPostfix([ReturnValue] BindingStruct value) => StructObserved = value;

    [Postfix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.StructResult))]
    public static void ReturnValueAttributeCanWriteStructResultInPostfix([ReturnValue] ref BindingStruct value) =>
        value = new BindingStruct { Value = 42 };
}

public static partial class ResultBindingPatches
{
    [Prefix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.IntResult))]
    public static void PrefixCanReadValueTypeResultThroughReference(ref int __result) => ValueObserved = __result;

    [Prefix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.StringResult))]
    public static void PrefixCanReadReferenceTypeResultThroughReference(ref string? __result) => ReferenceObserved = __result;

    [Prefix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.StructResult))]
    public static void PrefixCanReadStructResultThroughReference(ref BindingStruct __result) => StructObserved = __result;

    [Postfix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.IntResult))]
    public static void PostfixCanReadValueTypeResultThroughReference(ref int __result) => ValueObserved = __result;

    [Postfix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.StringResult))]
    public static void PostfixCanReadReferenceTypeResultThroughReference(ref string __result) => ReferenceObserved = __result;

    [Postfix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.StructResult))]
    public static void PostfixCanReadStructResultThroughReference(ref BindingStruct __result) => StructObserved = __result;

    [InnerPrefix(typeof(InnerStaticMethodTargets), nameof(InnerStaticMethodTargets.IntResult))]
    [Target(typeof(OuterStaticMethodTargets), nameof(OuterStaticMethodTargets.IntResult))]
    public static void InnerPrefixCanReadInnerValueTypeResultThroughReference(ref int __result) => InnerObserved = __result;

    [InnerPrefix(typeof(InnerStaticMethodTargets), nameof(InnerStaticMethodTargets.StringResult))]
    [Target(typeof(OuterStaticMethodTargets), nameof(OuterStaticMethodTargets.StringResult))]
    public static void InnerPrefixCanReadInnerReferenceTypeResultThroughReference(ref string? __result) =>
        ReferenceObserved = __result;

    [InnerPrefix(typeof(InnerStaticMethodTargets), nameof(InnerStaticMethodTargets.StructResult))]
    [Target(typeof(OuterStaticMethodTargets), nameof(OuterStaticMethodTargets.StructResult))]
    public static void InnerPrefixCanReadInnerStructResultThroughReference(ref BindingStruct __result) => StructObserved = __result;

    [InnerPostfix(typeof(InnerStaticMethodTargets), nameof(InnerStaticMethodTargets.IntResult))]
    [Target(typeof(OuterStaticMethodTargets), nameof(OuterStaticMethodTargets.IntResult))]
    public static void InnerPostfixCanReadInnerValueTypeResultThroughReference(ref int __result) => InnerObserved = __result;

    [InnerPostfix(typeof(InnerStaticMethodTargets), nameof(InnerStaticMethodTargets.StringResult))]
    [Target(typeof(OuterStaticMethodTargets), nameof(OuterStaticMethodTargets.StringResult))]
    public static void InnerPostfixCanReadInnerReferenceTypeResultThroughReference(ref string __result) =>
        ReferenceObserved = __result;

    [InnerPostfix(typeof(InnerStaticMethodTargets), nameof(InnerStaticMethodTargets.StructResult))]
    [Target(typeof(OuterStaticMethodTargets), nameof(OuterStaticMethodTargets.StructResult))]
    public static void InnerPostfixCanReadInnerStructResultThroughReference(ref BindingStruct __result) => StructObserved = __result;

    [Postfix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.IntResult))]
    public static void ReturnValueAttributeCanReadPrimitiveResultThroughReference([ReturnValue] ref int value) =>
        ValueObserved = value;

    [Prefix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.StringResult))]
    public static void ReturnValueAttributeCanReadDefaultReferenceTypeResultThroughReference(
        [ReturnValue] ref string? value) => ReferenceObserved = value;

    [Prefix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.StructResult))]
    public static void ReturnValueAttributeCanReadDefaultStructResultThroughReference(
        [ReturnValue] ref BindingStruct value) => StructObserved = value;

    [Postfix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.StringResult))]
    public static void ReturnValueAttributeCanReadReferenceTypeResultThroughReference(
        [ReturnValue] ref string value) => ReferenceObserved = value;

    [Postfix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.StructResult))]
    public static void ReturnValueAttributeCanReadStructResultThroughReference(
        [ReturnValue] ref BindingStruct value) => StructObserved = value;
}

[TestFixture]
public sealed partial class ResultBindingTests
{
    [Test]
    public void PrefixCanReadValueTypeResultThroughReference()
    {
        ResultBindingPatches.ValueObserved = -1;
        ApplyPatch(typeof(ResultBindingPatches), nameof(ResultBindingPatches.PrefixCanReadValueTypeResultThroughReference));
        StaticMethodTargets.IntResult();
        Assert.That(ResultBindingPatches.ValueObserved, Is.Zero);
    }

    [Test]
    public void PrefixCanReadReferenceTypeResultThroughReference()
    {
        ResultBindingPatches.ReferenceObserved = "sentinel";
        ApplyPatch(typeof(ResultBindingPatches), nameof(ResultBindingPatches.PrefixCanReadReferenceTypeResultThroughReference));
        StaticMethodTargets.StringResult();
        Assert.That(ResultBindingPatches.ReferenceObserved, Is.Null);
    }

    [Test]
    public void PrefixCanReadStructResultThroughReference()
    {
        ResultBindingPatches.StructObserved = new BindingStruct { Value = -1 };
        ApplyPatch(typeof(ResultBindingPatches), nameof(ResultBindingPatches.PrefixCanReadStructResultThroughReference));
        StaticMethodTargets.StructResult();
        Assert.That(ResultBindingPatches.StructObserved.Value, Is.Zero);
    }

    [Test]
    public void PostfixCanReadValueTypeResultThroughReference()
    {
        ResultBindingPatches.ValueObserved = 0;
        ApplyPatch(typeof(ResultBindingPatches), nameof(ResultBindingPatches.PostfixCanReadValueTypeResultThroughReference));
        StaticMethodTargets.IntResult();
        Assert.That(ResultBindingPatches.ValueObserved, Is.EqualTo(1));
    }

    [Test]
    public void PostfixCanReadReferenceTypeResultThroughReference()
    {
        ResultBindingPatches.ReferenceObserved = null;
        ApplyPatch(typeof(ResultBindingPatches), nameof(ResultBindingPatches.PostfixCanReadReferenceTypeResultThroughReference));
        StaticMethodTargets.StringResult();
        Assert.That(ResultBindingPatches.ReferenceObserved, Is.EqualTo("original"));
    }

    [Test]
    public void PostfixCanReadStructResultThroughReference()
    {
        ResultBindingPatches.StructObserved = default;
        ApplyPatch(typeof(ResultBindingPatches), nameof(ResultBindingPatches.PostfixCanReadStructResultThroughReference));
        StaticMethodTargets.StructResult();
        Assert.That(ResultBindingPatches.StructObserved.Value, Is.EqualTo(1));
    }

    [Test]
    public void InnerPrefixCanReadInnerValueTypeResultThroughReference()
    {
        ResultBindingPatches.InnerObserved = -1;
        ApplyPatch(typeof(ResultBindingPatches), nameof(ResultBindingPatches.InnerPrefixCanReadInnerValueTypeResultThroughReference));
        OuterStaticMethodTargets.IntResult();
        Assert.That(ResultBindingPatches.InnerObserved, Is.Zero);
    }

    [Test]
    public void InnerPrefixCanReadInnerReferenceTypeResultThroughReference()
    {
        ResultBindingPatches.ReferenceObserved = "sentinel";
        ApplyPatch(typeof(ResultBindingPatches), nameof(ResultBindingPatches.InnerPrefixCanReadInnerReferenceTypeResultThroughReference));
        OuterStaticMethodTargets.StringResult();
        Assert.That(ResultBindingPatches.ReferenceObserved, Is.Null);
    }

    [Test]
    public void InnerPrefixCanReadInnerStructResultThroughReference()
    {
        ResultBindingPatches.StructObserved = new BindingStruct { Value = -1 };
        ApplyPatch(typeof(ResultBindingPatches), nameof(ResultBindingPatches.InnerPrefixCanReadInnerStructResultThroughReference));
        OuterStaticMethodTargets.StructResult();
        Assert.That(ResultBindingPatches.StructObserved.Value, Is.Zero);
    }

    [Test]
    public void InnerPostfixCanReadInnerValueTypeResultThroughReference()
    {
        ResultBindingPatches.InnerObserved = 0;
        ApplyPatch(typeof(ResultBindingPatches), nameof(ResultBindingPatches.InnerPostfixCanReadInnerValueTypeResultThroughReference));
        OuterStaticMethodTargets.IntResult();
        Assert.That(ResultBindingPatches.InnerObserved, Is.EqualTo(1));
    }

    [Test]
    public void InnerPostfixCanReadInnerReferenceTypeResultThroughReference()
    {
        ResultBindingPatches.ReferenceObserved = null;
        ApplyPatch(typeof(ResultBindingPatches), nameof(ResultBindingPatches.InnerPostfixCanReadInnerReferenceTypeResultThroughReference));
        OuterStaticMethodTargets.StringResult();
        Assert.That(ResultBindingPatches.ReferenceObserved, Is.EqualTo("original"));
    }

    [Test]
    public void InnerPostfixCanReadInnerStructResultThroughReference()
    {
        ResultBindingPatches.StructObserved = default;
        ApplyPatch(typeof(ResultBindingPatches), nameof(ResultBindingPatches.InnerPostfixCanReadInnerStructResultThroughReference));
        OuterStaticMethodTargets.StructResult();
        Assert.That(ResultBindingPatches.StructObserved.Value, Is.EqualTo(1));
    }

    [Test]
    public void ReturnValueAttributeCanReadPrimitiveResultThroughReference()
    {
        ResultBindingPatches.ValueObserved = 0;
        ApplyPatch(typeof(ResultBindingPatches), nameof(ResultBindingPatches.ReturnValueAttributeCanReadPrimitiveResultThroughReference));
        StaticMethodTargets.IntResult();
        Assert.That(ResultBindingPatches.ValueObserved, Is.EqualTo(1));
    }

    [Test]
    public void ReturnValueAttributeCanReadDefaultReferenceTypeResultThroughReference()
    {
        ResultBindingPatches.ReferenceObserved = "sentinel";
        ApplyPatch(typeof(ResultBindingPatches), nameof(ResultBindingPatches.ReturnValueAttributeCanReadDefaultReferenceTypeResultThroughReference));
        StaticMethodTargets.StringResult();
        Assert.That(ResultBindingPatches.ReferenceObserved, Is.Null);
    }

    [Test]
    public void ReturnValueAttributeCanReadDefaultStructResultThroughReference()
    {
        ResultBindingPatches.StructObserved = new BindingStruct { Value = -1 };
        ApplyPatch(typeof(ResultBindingPatches), nameof(ResultBindingPatches.ReturnValueAttributeCanReadDefaultStructResultThroughReference));
        StaticMethodTargets.StructResult();
        Assert.That(ResultBindingPatches.StructObserved.Value, Is.Zero);
    }

    [Test]
    public void ReturnValueAttributeCanReadReferenceTypeResultThroughReference()
    {
        ResultBindingPatches.ReferenceObserved = null;
        ApplyPatch(typeof(ResultBindingPatches), nameof(ResultBindingPatches.ReturnValueAttributeCanReadReferenceTypeResultThroughReference));
        StaticMethodTargets.StringResult();
        Assert.That(ResultBindingPatches.ReferenceObserved, Is.EqualTo("original"));
    }

    [Test]
    public void ReturnValueAttributeCanReadStructResultThroughReference()
    {
        ResultBindingPatches.StructObserved = default;
        ApplyPatch(typeof(ResultBindingPatches), nameof(ResultBindingPatches.ReturnValueAttributeCanReadStructResultThroughReference));
        StaticMethodTargets.StructResult();
        Assert.That(ResultBindingPatches.StructObserved.Value, Is.EqualTo(1));
    }
}

[TestFixture]
public sealed partial class ResultBindingTests : PatchTestBase
{
    [Test]
    public void PrefixReadsDefaultValueTypeResult()
    {
        ResultBindingPatches.ValueObserved = -1;
        ApplyPatch(typeof(ResultBindingPatches), nameof(ResultBindingPatches.PrefixReadsDefaultValueTypeResult));
        StaticMethodTargets.IntResult();
        Assert.That(ResultBindingPatches.ValueObserved, Is.Zero);
    }

    [Test]
    public void PostfixReadsValueTypeResult()
    {
        ResultBindingPatches.ValueObserved = 0;
        ApplyPatch(typeof(ResultBindingPatches), nameof(ResultBindingPatches.PostfixReadsValueTypeResult));
        StaticMethodTargets.IntResult();
        Assert.That(ResultBindingPatches.ValueObserved, Is.EqualTo(1));
    }

    [Test]
    public void PrefixReadsDefaultReferenceTypeResult()
    {
        ResultBindingPatches.ReferenceObserved = "sentinel";
        ApplyPatch(typeof(ResultBindingPatches), nameof(ResultBindingPatches.PrefixReadsDefaultReferenceTypeResult));
        StaticMethodTargets.StringResult();
        Assert.That(ResultBindingPatches.ReferenceObserved, Is.Null);
    }

    [Test]
    public void PrefixReadsDefaultStructResult()
    {
        ResultBindingPatches.StructObserved = new BindingStruct { Value = -1 };
        ApplyPatch(typeof(ResultBindingPatches), nameof(ResultBindingPatches.PrefixReadsDefaultStructResult));

        StaticMethodTargets.StructResult();

        Assert.That(ResultBindingPatches.StructObserved.Value, Is.Zero);
    }

    [Test]
    public void PostfixReadsReferenceTypeResult()
    {
        ResultBindingPatches.ReferenceObserved = null;
        ApplyPatch(typeof(ResultBindingPatches), nameof(ResultBindingPatches.PostfixReadsReferenceTypeResult));
        StaticMethodTargets.StringResult();
        Assert.That(ResultBindingPatches.ReferenceObserved, Is.EqualTo("original"));
    }

    [Test]
    public void PostfixReadsStructResult()
    {
        ResultBindingPatches.StructObserved = default;
        ApplyPatch(typeof(ResultBindingPatches), nameof(ResultBindingPatches.PostfixReadsStructResult));

        StaticMethodTargets.StructResult();

        Assert.That(ResultBindingPatches.StructObserved.Value, Is.EqualTo(1));
    }

    [Test]
    public void PrefixCanWriteValueTypeResultByReference()
    {
        ApplyPatch(typeof(ResultBindingPatches), nameof(ResultBindingPatches.PrefixCanWriteValueTypeResultByReference));
        Assert.That(StaticMethodTargets.ThrowingIntResult(), Is.EqualTo(42));
    }

    [Test]
    public void PostfixCanWriteValueTypeResultByReference()
    {
        ApplyPatch(typeof(ResultBindingPatches), nameof(ResultBindingPatches.PostfixCanWriteValueTypeResultByReference));
        Assert.That(StaticMethodTargets.IntResult(), Is.EqualTo(42));
    }

    [Test]
    public void PrefixCanWriteReferenceTypeResultByReference()
    {
        ApplyPatch(typeof(ResultBindingPatches), nameof(ResultBindingPatches.PrefixCanWriteReferenceTypeResultByReference));
        Assert.That(StaticMethodTargets.ThrowingStringResult(), Is.EqualTo("patched"));
    }

    [Test]
    public void PrefixCanWriteStructResultByReference()
    {
        ApplyPatch(typeof(ResultBindingPatches), nameof(ResultBindingPatches.PrefixCanWriteStructResultByReference));

        BindingStruct result = StaticMethodTargets.ThrowingStructResult();

        Assert.That(result.Value, Is.EqualTo(42));
    }

    [Test]
    public void PostfixCanWriteReferenceTypeResultByReference()
    {
        ApplyPatch(typeof(ResultBindingPatches), nameof(ResultBindingPatches.PostfixCanWriteReferenceTypeResultByReference));
        Assert.That(StaticMethodTargets.StringResult(), Is.EqualTo("patched"));
    }

    [Test]
    public void PostfixCanWriteStructResultByReference()
    {
        ApplyPatch(typeof(ResultBindingPatches), nameof(ResultBindingPatches.PostfixCanWriteStructResultByReference));

        BindingStruct result = StaticMethodTargets.StructResult();

        Assert.That(result.Value, Is.EqualTo(42));
    }
}

[TestFixture]
public sealed partial class ResultBindingTests
{
    [Test]
    public void InnerPrefixReadsDefaultInnerResult()
    {
        ResultBindingPatches.InnerObserved = -1;
        ApplyPatch(typeof(ResultBindingPatches), nameof(ResultBindingPatches.InnerPrefixReadsDefaultInnerResult));
        OuterStaticMethodTargets.IntResult();
        Assert.That(ResultBindingPatches.InnerObserved, Is.Zero);
    }

    [Test]
    public void InnerPrefixReadsDefaultInnerReferenceTypeResult()
    {
        ResultBindingPatches.ReferenceObserved = "sentinel";
        ApplyPatch(typeof(ResultBindingPatches), nameof(ResultBindingPatches.InnerPrefixReadsDefaultInnerReferenceTypeResult));

        OuterStaticMethodTargets.StringResult();

        Assert.That(ResultBindingPatches.ReferenceObserved, Is.Null);
    }

    [Test]
    public void InnerPrefixReadsDefaultInnerStructResult()
    {
        ResultBindingPatches.StructObserved = new BindingStruct { Value = -1 };
        ApplyPatch(typeof(ResultBindingPatches), nameof(ResultBindingPatches.InnerPrefixReadsDefaultInnerStructResult));

        OuterStaticMethodTargets.StructResult();

        Assert.That(ResultBindingPatches.StructObserved.Value, Is.Zero);
    }

    [Test]
    public void InnerPostfixReadsInnerResult()
    {
        ResultBindingPatches.InnerObserved = 0;
        ApplyPatch(typeof(ResultBindingPatches), nameof(ResultBindingPatches.InnerPostfixReadsInnerResult));
        OuterStaticMethodTargets.IntResult();
        Assert.That(ResultBindingPatches.InnerObserved, Is.EqualTo(1));
    }

    [Test]
    public void InnerPostfixReadsInnerReferenceTypeResult()
    {
        ResultBindingPatches.ReferenceObserved = null;
        ApplyPatch(typeof(ResultBindingPatches), nameof(ResultBindingPatches.InnerPostfixReadsInnerReferenceTypeResult));

        OuterStaticMethodTargets.StringResult();

        Assert.That(ResultBindingPatches.ReferenceObserved, Is.EqualTo("original"));
    }

    [Test]
    public void InnerPostfixReadsInnerStructResult()
    {
        ResultBindingPatches.StructObserved = default;
        ApplyPatch(typeof(ResultBindingPatches), nameof(ResultBindingPatches.InnerPostfixReadsInnerStructResult));

        OuterStaticMethodTargets.StructResult();

        Assert.That(ResultBindingPatches.StructObserved.Value, Is.EqualTo(1));
    }

    [Test]
    public void InnerPrefixCanWriteInnerResultByReference()
    {
        ApplyPatch(typeof(ResultBindingPatches), nameof(ResultBindingPatches.InnerPrefixCanWriteInnerResultByReference));
        Assert.That(OuterStaticMethodTargets.IntResult(), Is.EqualTo(42));
    }

    [Test]
    public void InnerPrefixCanWriteInnerReferenceTypeResultByReference()
    {
        ApplyPatch(typeof(ResultBindingPatches), nameof(ResultBindingPatches.InnerPrefixCanWriteInnerReferenceTypeResultByReference));

        Assert.That(OuterStaticMethodTargets.StringResult(), Is.EqualTo("patched"));
    }

    [Test]
    public void InnerPrefixCanWriteInnerStructResultByReference()
    {
        ApplyPatch(typeof(ResultBindingPatches), nameof(ResultBindingPatches.InnerPrefixCanWriteInnerStructResultByReference));

        BindingStruct result = OuterStaticMethodTargets.StructResult();

        Assert.That(result.Value, Is.EqualTo(42));
    }

    [Test]
    public void InnerPostfixCanWriteInnerResultByReference()
    {
        ApplyPatch(typeof(ResultBindingPatches), nameof(ResultBindingPatches.InnerPostfixCanWriteInnerResultByReference));
        Assert.That(OuterStaticMethodTargets.IntResult(), Is.EqualTo(42));
    }

    [Test]
    public void InnerPostfixCanWriteInnerReferenceTypeResultByReference()
    {
        ApplyPatch(typeof(ResultBindingPatches), nameof(ResultBindingPatches.InnerPostfixCanWriteInnerReferenceTypeResultByReference));

        Assert.That(OuterStaticMethodTargets.StringResult(), Is.EqualTo("patched"));
    }

    [Test]
    public void InnerPostfixCanWriteInnerStructResultByReference()
    {
        ApplyPatch(typeof(ResultBindingPatches), nameof(ResultBindingPatches.InnerPostfixCanWriteInnerStructResultByReference));

        BindingStruct result = OuterStaticMethodTargets.StructResult();

        Assert.That(result.Value, Is.EqualTo(42));
    }

    [Test]
    public void ReturnValueAttributeBindsWritableResult()
    {
        ApplyPatch(typeof(ResultBindingPatches), nameof(ResultBindingPatches.ReturnValueAttributeBindsWritableResult));

        Assert.That(StaticMethodTargets.IntResult(), Is.EqualTo(42));
    }

    [Test]
    public void ReturnValueAttributeCanReadDefaultPrimitiveResult()
    {
        ResultBindingPatches.ValueObserved = -1;
        ApplyPatch(typeof(ResultBindingPatches), nameof(ResultBindingPatches.ReturnValueAttributeCanReadDefaultPrimitiveResult));

        StaticMethodTargets.IntResult();

        Assert.That(ResultBindingPatches.ValueObserved, Is.Zero);
    }

    [Test]
    public void ReturnValueAttributeCanReadDefaultReferenceTypeResult()
    {
        ResultBindingPatches.ReferenceObserved = "sentinel";
        ApplyPatch(typeof(ResultBindingPatches), nameof(ResultBindingPatches.ReturnValueAttributeCanReadDefaultReferenceTypeResult));

        StaticMethodTargets.StringResult();

        Assert.That(ResultBindingPatches.ReferenceObserved, Is.Null);
    }

    [Test]
    public void ReturnValueAttributeCanWriteReferenceTypeResultByReference()
    {
        ApplyPatch(typeof(ResultBindingPatches), nameof(ResultBindingPatches.ReturnValueAttributeCanWriteReferenceTypeResultByReference));

        Assert.That(StaticMethodTargets.ThrowingStringResult(), Is.EqualTo("patched"));
    }

    [Test]
    public void ReturnValueAttributeCanReadDefaultStructResult()
    {
        ResultBindingPatches.StructObserved = new BindingStruct { Value = -1 };
        ApplyPatch(typeof(ResultBindingPatches), nameof(ResultBindingPatches.ReturnValueAttributeCanReadDefaultStructResult));

        StaticMethodTargets.StructResult();

        Assert.That(ResultBindingPatches.StructObserved.Value, Is.Zero);
    }

    [Test]
    public void ReturnValueAttributeCanWriteStructResultByReference()
    {
        ApplyPatch(typeof(ResultBindingPatches), nameof(ResultBindingPatches.ReturnValueAttributeCanWriteStructResultByReference));

        BindingStruct result = StaticMethodTargets.ThrowingStructResult();

        Assert.That(result.Value, Is.EqualTo(42));
    }

    [Test]
    public void ReturnValueAttributeCanReadPrimitiveResultInPostfix()
    {
        ResultBindingPatches.ValueObserved = 0;
        ApplyPatch(typeof(ResultBindingPatches), nameof(ResultBindingPatches.ReturnValueAttributeCanReadPrimitiveResultInPostfix));

        StaticMethodTargets.IntResult();

        Assert.That(ResultBindingPatches.ValueObserved, Is.EqualTo(1));
    }

    [Test]
    public void ReturnValueAttributeCanReadReferenceTypeResultInPostfix()
    {
        ResultBindingPatches.ReferenceObserved = null;
        ApplyPatch(typeof(ResultBindingPatches), nameof(ResultBindingPatches.ReturnValueAttributeCanReadReferenceTypeResultInPostfix));

        StaticMethodTargets.StringResult();

        Assert.That(ResultBindingPatches.ReferenceObserved, Is.EqualTo("original"));
    }

    [Test]
    public void ReturnValueAttributeCanWriteReferenceTypeResultInPostfix()
    {
        ApplyPatch(typeof(ResultBindingPatches), nameof(ResultBindingPatches.ReturnValueAttributeCanWriteReferenceTypeResultInPostfix));

        Assert.That(StaticMethodTargets.StringResult(), Is.EqualTo("patched"));
    }

    [Test]
    public void ReturnValueAttributeCanReadStructResultInPostfix()
    {
        ResultBindingPatches.StructObserved = default;
        ApplyPatch(typeof(ResultBindingPatches), nameof(ResultBindingPatches.ReturnValueAttributeCanReadStructResultInPostfix));

        StaticMethodTargets.StructResult();

        Assert.That(ResultBindingPatches.StructObserved.Value, Is.EqualTo(1));
    }

    [Test]
    public void ReturnValueAttributeCanWriteStructResultInPostfix()
    {
        ApplyPatch(typeof(ResultBindingPatches), nameof(ResultBindingPatches.ReturnValueAttributeCanWriteStructResultInPostfix));

        BindingStruct result = StaticMethodTargets.StructResult();

        Assert.That(result.Value, Is.EqualTo(42));
    }
}
