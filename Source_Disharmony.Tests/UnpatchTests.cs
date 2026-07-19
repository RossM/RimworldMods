using NUnit.Framework;

namespace Disharmony.Tests;

public static class UnpatchPatchMethods
{
    [Postfix]
    [Target(typeof(UnpatchPatchTargets), nameof(UnpatchPatchTargets.TargetA))]
    public static void PatchA(ref int __result) => __result = 42;

    [Postfix]
    [Target(typeof(UnpatchPatchTargets), nameof(UnpatchPatchTargets.TargetB))]
    public static void PatchB(ref int __result) => __result = 42;
}

public static class UnpatchPatchTargets
{
    public static int TargetA() => 1;
    public static int TargetB() => 2;
}

internal class UnpatchTests
{
    private static void ApplyPatch(string patchMethodName) =>
        Autopatcher.Patch(typeof(UnpatchPatchMethods).GetMethod(patchMethodName));

    [Test]
    public void PrefixReturningFalseSkipsValueTypeTarget()
    {
        Assert.That(UnpatchPatchTargets.TargetA(), Is.EqualTo(1));
        Assert.That(UnpatchPatchTargets.TargetB(), Is.EqualTo(2));

        ApplyPatch(nameof(UnpatchPatchMethods.PatchA));

        Assert.That(UnpatchPatchTargets.TargetA(), Is.EqualTo(42));
        Assert.That(UnpatchPatchTargets.TargetB(), Is.EqualTo(2));

        ApplyPatch(nameof(UnpatchPatchMethods.PatchB));

        Assert.That(UnpatchPatchTargets.TargetA(), Is.EqualTo(42));
        Assert.That(UnpatchPatchTargets.TargetB(), Is.EqualTo(42));

        Autopatcher.UnpatchAll(typeof(UnpatchPatchMethods).Assembly);

        Assert.That(UnpatchPatchTargets.TargetA(), Is.EqualTo(1));
        Assert.That(UnpatchPatchTargets.TargetB(), Is.EqualTo(2));
    }

}
