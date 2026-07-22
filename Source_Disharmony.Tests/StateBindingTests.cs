namespace Disharmony.Tests;

[TestFixture]
public sealed partial class StateBindingTests : PatchTestBase
{
    [Test]
    public void PostfixCanReadStateWrittenByPrefix()
    {
        PatchMethods.StateObserved = 0;
        ApplyPatch(typeof(PatchMethods), nameof(PatchMethods.WriteStatePrefix));
        ApplyPatch(typeof(PatchMethods), nameof(PatchMethods.ReadStatePostfix));

        StaticMethodTargets.Void();

        Assert.That(PatchMethods.StateObserved, Is.EqualTo(42));
    }

    [Test]
    public void PostfixCanWriteStateByReferenceForLaterPostfix()
    {
        PatchMethods.StateObserved = 0;
        ApplyPatch(typeof(PatchMethods), nameof(PatchMethods.WriteStatePrefix));
        ApplyPatch(typeof(PatchMethods), nameof(PatchMethods.WriteStatePostfix));
        ApplyPatch(typeof(PatchMethods), nameof(PatchMethods.ReadWrittenStatePostfix));

        StaticMethodTargets.Void();

        Assert.That(PatchMethods.StateObserved, Is.EqualTo(43));
    }
}
