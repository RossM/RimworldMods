namespace Disharmony.Tests;

public static class UnpatchPatches
{
    public static int ObservedPatch;

    [Postfix]
    [Target(typeof(UnpatchPatchTargets), nameof(UnpatchPatchTargets.TargetA))]
    public static void PrefixReturningFalseSkipsValueTypeTarget_FirstPostfix(ref int __result) => __result = 42;

    [Postfix]
    [Target(typeof(UnpatchPatchTargets), nameof(UnpatchPatchTargets.TargetB))]
    public static void PrefixReturningFalseSkipsValueTypeTarget_SecondPostfix(ref int __result) => __result = 42;

    [Prefix]
    [Target(typeof(UnpatchPatchTargets), nameof(UnpatchPatchTargets.ApplyUnpatchApplyTarget))]
    public static void ApplyUnpatchApply_ExecutesSecondPatch_First() => ObservedPatch = 1;

    [Prefix]
    [Target(typeof(UnpatchPatchTargets), nameof(UnpatchPatchTargets.ApplyUnpatchApplyTarget))]
    public static void ApplyUnpatchApply_ExecutesSecondPatch_Second() => ObservedPatch = 2;
}

internal class UnpatchTests
{
    private static void ApplyPatch(string patchMethodName) =>
        Autopatcher.Patch(typeof(UnpatchPatches).GetMethod(patchMethodName));

    [SetUp]
    public void DisableOptimizer() =>
        Patcher.Instance.optimizerEnabled = false;

    [Test]
    public void PrefixReturningFalseSkipsValueTypeTarget()
    {
        Assert.That(UnpatchPatchTargets.TargetA(), Is.EqualTo(1));
        Assert.That(UnpatchPatchTargets.TargetB(), Is.EqualTo(2));

        ApplyPatch(nameof(UnpatchPatches.PrefixReturningFalseSkipsValueTypeTarget_FirstPostfix));

        Assert.That(UnpatchPatchTargets.TargetA(), Is.EqualTo(42));
        Assert.That(UnpatchPatchTargets.TargetB(), Is.EqualTo(2));

        ApplyPatch(nameof(UnpatchPatches.PrefixReturningFalseSkipsValueTypeTarget_SecondPostfix));

        Assert.That(UnpatchPatchTargets.TargetA(), Is.EqualTo(42));
        Assert.That(UnpatchPatchTargets.TargetB(), Is.EqualTo(42));

        Autopatcher.UnpatchAll(typeof(UnpatchPatches).Assembly);

        Assert.That(UnpatchPatchTargets.TargetA(), Is.EqualTo(1));
        Assert.That(UnpatchPatchTargets.TargetB(), Is.EqualTo(2));
    }

    [Test]
    public void ApplyUnpatchApply_ExecutesSecondPatch()
    {
        Autopatcher.UnpatchAll(typeof(UnpatchPatches).Assembly);
        UnpatchPatches.ObservedPatch = 0;

        ApplyPatch(nameof(UnpatchPatches.ApplyUnpatchApply_ExecutesSecondPatch_First));
        Autopatcher.UnpatchAll(typeof(UnpatchPatches).Assembly);
        ApplyPatch(nameof(UnpatchPatches.ApplyUnpatchApply_ExecutesSecondPatch_Second));

        UnpatchPatchTargets.ApplyUnpatchApplyTarget();
        int observedPatch = UnpatchPatches.ObservedPatch;

        Autopatcher.UnpatchAll(typeof(UnpatchPatches).Assembly);
        Assert.That(observedPatch, Is.EqualTo(2));
    }
}
