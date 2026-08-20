namespace Disharmony.Tests.EndToEnd.PatchLifecycle;

public static class UnpatchPatches
{
    public static int observedPatch;

    [Postfix]
    [Target(typeof(UnpatchPatchTargets), nameof(UnpatchPatchTargets.TargetA))]
    public static void PrefixReturningFalseSkipsValueTypeTarget_FirstPostfix(ref int __result) => __result = 42;

    [Postfix]
    [Target(typeof(UnpatchPatchTargets), nameof(UnpatchPatchTargets.TargetB))]
    public static void PrefixReturningFalseSkipsValueTypeTarget_SecondPostfix(ref int __result) => __result = 42;

    [Prefix]
    [Target(typeof(UnpatchPatchTargets), nameof(UnpatchPatchTargets.ApplyUnpatchApplyTarget))]
    public static void ApplyUnpatchApply_ExecutesSecondPatch_First() => observedPatch = 1;

    [Prefix]
    [Target(typeof(UnpatchPatchTargets), nameof(UnpatchPatchTargets.ApplyUnpatchApplyTarget))]
    public static void ApplyUnpatchApply_ExecutesSecondPatch_Second() => observedPatch = 2;
}

internal class UnpatchTests
{
    private static void ThrowRuntimeException(Exception exception) =>
        throw new InvalidOperationException("Runtime exception", exception);

    private static void ApplyPatch(string patchMethodName) =>
        Patcher.Patch(typeof(UnpatchPatches).GetMethod(patchMethodName));

    [SetUp]
    public void SetUp()
    {
        Patcher.RuntimeExceptionHandler -= ThrowRuntimeException;
        Patcher.RuntimeExceptionHandler += ThrowRuntimeException;
        HarmonyInterface.Instance.optimizerEnabled = false;
    }

    [TearDown]
    public void TearDown() =>
        Patcher.RuntimeExceptionHandler -= ThrowRuntimeException;

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

        Patcher.UnpatchAll(typeof(UnpatchPatches).Assembly);

        Assert.That(UnpatchPatchTargets.TargetA(), Is.EqualTo(1));
        Assert.That(UnpatchPatchTargets.TargetB(), Is.EqualTo(2));
    }

    [Test]
    public void ApplyUnpatchApply_ExecutesSecondPatch()
    {
        Patcher.UnpatchAll(typeof(UnpatchPatches).Assembly);
        UnpatchPatches.observedPatch = 0;

        ApplyPatch(nameof(UnpatchPatches.ApplyUnpatchApply_ExecutesSecondPatch_First));
        Patcher.UnpatchAll(typeof(UnpatchPatches).Assembly);
        ApplyPatch(nameof(UnpatchPatches.ApplyUnpatchApply_ExecutesSecondPatch_Second));

        UnpatchPatchTargets.ApplyUnpatchApplyTarget();
        int observedPatch = UnpatchPatches.observedPatch;

        Patcher.UnpatchAll(typeof(UnpatchPatches).Assembly);
        Assert.That(observedPatch, Is.EqualTo(2));
    }
}
