namespace Disharmony.Tests;

[TestFixture]
public sealed partial class ExecutionControlTests
{
    [Test]
    public void PrefixResultIsReplacedByValueTypeTargetWhenReturningTrue()
    {
        ApplyPatch(typeof(PatchMethods), nameof(PatchMethods.WriteValueResultAndRunTargetPrefix));
        Assert.That(StaticMethodTargets.IntResult(), Is.EqualTo(1));
    }

    [Test]
    public void PrefixResultIsRetainedForValueTypeTargetWhenReturningFalse()
    {
        ApplyPatch(typeof(PatchMethods), nameof(PatchMethods.WriteValueResultAndSkipTargetPrefix));
        Assert.That(StaticMethodTargets.ThrowingIntResult(), Is.EqualTo(42));
    }

    [Test]
    public void PrefixResultIsReplacedByReferenceTypeTargetWhenReturningTrue()
    {
        ApplyPatch(typeof(PatchMethods), nameof(PatchMethods.WriteReferenceResultAndRunTargetPrefix));
        Assert.That(StaticMethodTargets.StringResult(), Is.EqualTo("original"));
    }

    [Test]
    public void PrefixResultIsRetainedForReferenceTypeTargetWhenReturningFalse()
    {
        ApplyPatch(typeof(PatchMethods), nameof(PatchMethods.WriteReferenceResultAndSkipTargetPrefix));
        Assert.That(StaticMethodTargets.ThrowingStringResult(), Is.EqualTo("patched"));
    }
}

[TestFixture]
public sealed partial class ExecutionControlTests : PatchTestBase
{
    [Test]
    public void PrefixReturningTrueRunsValueTypeTarget()
    {
        ApplyPatch(typeof(PatchMethods), nameof(PatchMethods.RunValueTypeTargetPrefix));
        Assert.That(StaticMethodTargets.IntResult(), Is.EqualTo(1));
    }

    [Test]
    public void PrefixReturningTrueRunsReferenceTypeTarget()
    {
        ApplyPatch(typeof(PatchMethods), nameof(PatchMethods.RunReferenceTypeTargetPrefix));
        Assert.That(StaticMethodTargets.StringResult(), Is.EqualTo("original"));
    }

    [Test]
    public void PrefixReturningFalseSkipsValueTypeTarget()
    {
        ApplyPatch(typeof(PatchMethods), nameof(PatchMethods.SkipValueTypeTargetPrefix));
        Assert.That(StaticMethodTargets.ThrowingIntResult(), Is.Zero);
    }

    [Test]
    public void PrefixReturningFalseSkipsReferenceTypeTarget()
    {
        ApplyPatch(typeof(PatchMethods), nameof(PatchMethods.SkipReferenceTypeTargetPrefix));
        Assert.That(StaticMethodTargets.ThrowingStringResult(), Is.Null);
    }
}

[TestFixture]
public sealed partial class ExecutionControlTests
{
    [Test]
    public void InnerPrefixReturningTrueRunsInnerTarget()
    {
        ApplyPatch(typeof(InnerPatchMethods), nameof(InnerPatchMethods.RunTargetPrefix));
        Assert.That(OuterStaticMethodTargets.IntResult(), Is.EqualTo(1));
    }

    [Test]
    public void InnerPrefixReturningFalseSkipsInnerTarget()
    {
        ApplyPatch(typeof(InnerPatchMethods), nameof(InnerPatchMethods.SkipTargetPrefix));
        Assert.That(OuterStaticMethodTargets.IntResult(), Is.Zero);
    }
}
