using NUnit.Framework;

namespace Disharmony.Tests;

[TestFixture]
public sealed partial class ResultBindingTests : PatchTestBase
{
    [Test]
    public void PrefixReadsDefaultValueTypeResult()
    {
        PatchMethods.ValueResultObserved = -1;
        ApplyPatch(nameof(PatchMethods.ReadValueResultPrefix));
        StaticMethodTargets.IntResult();
        Assert.That(PatchMethods.ValueResultObserved, Is.Zero);
    }

    [Test]
    public void PostfixReadsValueTypeResult()
    {
        PatchMethods.ValueResultObserved = 0;
        ApplyPatch(nameof(PatchMethods.ReadValueResultPostfix));
        StaticMethodTargets.IntResult();
        Assert.That(PatchMethods.ValueResultObserved, Is.EqualTo(1));
    }

    [Test]
    public void PrefixReadsDefaultReferenceTypeResult()
    {
        PatchMethods.ReferenceResultObserved = "sentinel";
        ApplyPatch(nameof(PatchMethods.ReadReferenceResultPrefix));
        StaticMethodTargets.StringResult();
        Assert.That(PatchMethods.ReferenceResultObserved, Is.Null);
    }

    [Test]
    public void PostfixReadsReferenceTypeResult()
    {
        PatchMethods.ReferenceResultObserved = null;
        ApplyPatch(nameof(PatchMethods.ReadReferenceResultPostfix));
        StaticMethodTargets.StringResult();
        Assert.That(PatchMethods.ReferenceResultObserved, Is.EqualTo("original"));
    }

    [Test]
    public void PrefixCanWriteValueTypeResultByReference()
    {
        ApplyPatch(nameof(PatchMethods.WriteValueResultPrefix));
        Assert.That(StaticMethodTargets.ThrowingIntResult(), Is.EqualTo(42));
    }

    [Test]
    public void PostfixCanWriteValueTypeResultByReference()
    {
        ApplyPatch(nameof(PatchMethods.WriteValueResultPostfix));
        Assert.That(StaticMethodTargets.IntResult(), Is.EqualTo(42));
    }

    [Test]
    public void PrefixCanWriteReferenceTypeResultByReference()
    {
        ApplyPatch(nameof(PatchMethods.WriteReferenceResultPrefix));
        Assert.That(StaticMethodTargets.ThrowingStringResult(), Is.EqualTo("patched"));
    }

    [Test]
    public void PostfixCanWriteReferenceTypeResultByReference()
    {
        ApplyPatch(nameof(PatchMethods.WriteReferenceResultPostfix));
        Assert.That(StaticMethodTargets.StringResult(), Is.EqualTo("patched"));
    }
}

[TestFixture]
public sealed partial class ResultBindingTests
{
    [Test]
    public void InnerPrefixReadsDefaultInnerResult()
    {
        InnerPatchMethods.ResultObserved = -1;
        ApplyInnerPatch(nameof(InnerPatchMethods.ReadResultPrefix));
        OuterStaticMethodTargets.IntResult();
        Assert.That(InnerPatchMethods.ResultObserved, Is.Zero);
    }

    [Test]
    public void InnerPostfixReadsInnerResult()
    {
        InnerPatchMethods.ResultObserved = 0;
        ApplyInnerPatch(nameof(InnerPatchMethods.ReadResultPostfix));
        OuterStaticMethodTargets.IntResult();
        Assert.That(InnerPatchMethods.ResultObserved, Is.EqualTo(1));
    }

    [Test]
    public void InnerPrefixCanWriteInnerResultByReference()
    {
        ApplyInnerPatch(nameof(InnerPatchMethods.WriteResultPrefix));
        Assert.That(OuterStaticMethodTargets.IntResult(), Is.EqualTo(42));
    }

    [Test]
    public void InnerPostfixCanWriteInnerResultByReference()
    {
        ApplyInnerPatch(nameof(InnerPatchMethods.WriteResultPostfix));
        Assert.That(OuterStaticMethodTargets.IntResult(), Is.EqualTo(42));
    }
}
