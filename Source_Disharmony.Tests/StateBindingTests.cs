using NUnit.Framework;

namespace Disharmony.Tests;

[TestFixture]
public sealed partial class StateBindingTests : PatchTestBase
{
    [Test]
    public void PostfixCanReadStateWrittenByPrefix()
    {
        PatchMethods.StateObserved = 0;
        ApplyPatches(
            nameof(PatchMethods.WriteStatePrefix),
            nameof(PatchMethods.ReadStatePostfix));

        StaticMethodTargets.Void();

        Assert.That(PatchMethods.StateObserved, Is.EqualTo(42));
    }

    [Test]
    public void PostfixCanWriteStateByReferenceForLaterPostfix()
    {
        PatchMethods.StateObserved = 0;
        ApplyPatch(nameof(PatchMethods.WriteStatePrefix));
        ApplyPatch(nameof(PatchMethods.WriteStatePostfix));
        ApplyPatch(nameof(PatchMethods.ReadWrittenStatePostfix));

        StaticMethodTargets.Void();

        Assert.That(PatchMethods.StateObserved, Is.EqualTo(43));
    }
}
