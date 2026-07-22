using NUnit.Framework;

namespace Disharmony.Tests;

[TestFixture]
public sealed partial class ExecutionControlTests
{
    [Test]
    public void PrefixResultIsReplacedByValueTypeTargetWhenReturningTrue()
    {
        ApplyPatch(nameof(PatchMethods.WriteValueResultAndRunTargetPrefix));
        Assert.That(StaticMethodTargets.IntResult(), Is.EqualTo(1));
    }

    [Test]
    public void PrefixResultIsRetainedForValueTypeTargetWhenReturningFalse()
    {
        ApplyPatch(nameof(PatchMethods.WriteValueResultAndSkipTargetPrefix));
        Assert.That(StaticMethodTargets.ThrowingIntResult(), Is.EqualTo(42));
    }

    [Test]
    public void PrefixResultIsReplacedByReferenceTypeTargetWhenReturningTrue()
    {
        ApplyPatch(nameof(PatchMethods.WriteReferenceResultAndRunTargetPrefix));
        Assert.That(StaticMethodTargets.StringResult(), Is.EqualTo("original"));
    }

    [Test]
    public void PrefixResultIsRetainedForReferenceTypeTargetWhenReturningFalse()
    {
        ApplyPatch(nameof(PatchMethods.WriteReferenceResultAndSkipTargetPrefix));
        Assert.That(StaticMethodTargets.ThrowingStringResult(), Is.EqualTo("patched"));
    }
}

[TestFixture]
public sealed partial class ExecutionControlTests : PatchTestBase
{
    [Test]
    public void PrefixReturningTrueRunsValueTypeTarget()
    {
        ApplyPatch(nameof(PatchMethods.RunValueTypeTargetPrefix));
        Assert.That(StaticMethodTargets.IntResult(), Is.EqualTo(1));
    }

    [Test]
    public void PrefixReturningTrueRunsReferenceTypeTarget()
    {
        ApplyPatch(nameof(PatchMethods.RunReferenceTypeTargetPrefix));
        Assert.That(StaticMethodTargets.StringResult(), Is.EqualTo("original"));
    }

    [Test]
    public void PrefixReturningFalseSkipsValueTypeTarget()
    {
        ApplyPatch(nameof(PatchMethods.SkipValueTypeTargetPrefix));
        Assert.That(StaticMethodTargets.ThrowingIntResult(), Is.Zero);
    }

    [Test]
    public void PrefixReturningFalseSkipsReferenceTypeTarget()
    {
        ApplyPatch(nameof(PatchMethods.SkipReferenceTypeTargetPrefix));
        Assert.That(StaticMethodTargets.ThrowingStringResult(), Is.Null);
    }
}

[TestFixture]
public sealed partial class ExecutionControlTests
{
    [Test]
    public void InnerPrefixReturningTrueRunsInnerTarget()
    {
        ApplyInnerPatch(nameof(InnerPatchMethods.RunTargetPrefix));
        Assert.That(OuterStaticMethodTargets.IntResult(), Is.EqualTo(1));
    }

    [Test]
    public void InnerPrefixReturningFalseSkipsInnerTarget()
    {
        ApplyInnerPatch(nameof(InnerPatchMethods.SkipTargetPrefix));
        Assert.That(OuterStaticMethodTargets.IntResult(), Is.Zero);
    }
}
