namespace Disharmony.Tests.EndToEnd.PatchLifecycle;

public static class UnpatchPatches
{
    public static int observedPatch;
    public static int firstPatchCalls;
    public static int secondPatchCalls;

    [Postfix]
    [Target(typeof(UnpatchPatchTargets), nameof(UnpatchPatchTargets.TargetA))]
    public static void PatchHandle_UnpatchesOnlySelectedPatch_OnDifferentTargets_FirstPostfix(ref int __result) =>
        __result = 42;

    [Postfix]
    [Target(typeof(UnpatchPatchTargets), nameof(UnpatchPatchTargets.TargetB))]
    public static void PatchHandle_UnpatchesOnlySelectedPatch_OnDifferentTargets_SecondPostfix(ref int __result) =>
        __result = 42;

    [Prefix]
    [Target(typeof(UnpatchPatchTargets), nameof(UnpatchPatchTargets.ApplyUnpatchApplyTarget))]
    public static void ApplyUnpatchApply_ExecutesSecondPatch_First() => observedPatch = 1;

    [Prefix]
    [Target(typeof(UnpatchPatchTargets), nameof(UnpatchPatchTargets.ApplyUnpatchApplyTarget))]
    public static void ApplyUnpatchApply_ExecutesSecondPatch_Second() => observedPatch = 2;

    [Prefix]
    [Target(typeof(UnpatchPatchTargets), nameof(UnpatchPatchTargets.ApplyUnpatchApplyTarget))]
    public static void PatchHandle_UnpatchesOnlySelectedPatch_OnSameTarget_FirstPrefix() => firstPatchCalls++;

    [Prefix]
    [Target(typeof(UnpatchPatchTargets), nameof(UnpatchPatchTargets.ApplyUnpatchApplyTarget))]
    public static void PatchHandle_UnpatchesOnlySelectedPatch_OnSameTarget_SecondPrefix() => secondPatchCalls++;

    [Postfix]
    [Target(typeof(UnpatchPatchTargets), nameof(UnpatchPatchTargets.TargetB))]
    public static void PatchAllHandle_UnpatchesOnlyPatchesOwnedByThatHandle_IndependentPostfix(ref int __result) =>
        __result = 43;
}

public static class UnpatchPatchAllPatches
{
    [Postfix]
    [Target(typeof(UnpatchPatchTargets), nameof(UnpatchPatchTargets.TargetA))]
    public static void PatchAllHandle_UnpatchesOnlyPatchesOwnedByThatHandle(ref int __result) => __result = 42;
}

internal class UnpatchTests
{
    private static void ThrowRuntimeException(Exception exception) =>
        throw new InvalidOperationException("Runtime exception", exception);

    [SetUp]
    public void SetUp()
    {
        Patcher.UnpatchAll();
        Patcher.RuntimeExceptionHandler -= ThrowRuntimeException;
        Patcher.RuntimeExceptionHandler += ThrowRuntimeException;
        HarmonyInterface.Instance.optimizerEnabled = false;
        UnpatchPatches.observedPatch = 0;
        UnpatchPatches.firstPatchCalls = 0;
        UnpatchPatches.secondPatchCalls = 0;
    }

    [TearDown]
    public void TearDown()
    {
        Patcher.RuntimeExceptionHandler -= ThrowRuntimeException;
        Patcher.UnpatchAll();
    }

    [Test]
    public void PatchHandle_UnpatchesOnlySelectedPatch_OnDifferentTargets()
    {
        Assert.That(UnpatchPatchTargets.TargetA(), Is.EqualTo(1));
        Assert.That(UnpatchPatchTargets.TargetB(), Is.EqualTo(2));

        PatchHandle firstHandle = Patcher.Patch(typeof(UnpatchPatches)
            .GetMethod(nameof(UnpatchPatches.PatchHandle_UnpatchesOnlySelectedPatch_OnDifferentTargets_FirstPostfix))!);
        PatchHandle secondHandle = Patcher.Patch(typeof(UnpatchPatches)
            .GetMethod(nameof(UnpatchPatches.PatchHandle_UnpatchesOnlySelectedPatch_OnDifferentTargets_SecondPostfix))!);

        Assert.That(UnpatchPatchTargets.TargetA(), Is.EqualTo(42));
        Assert.That(UnpatchPatchTargets.TargetB(), Is.EqualTo(42));

        Patcher.Unpatch(firstHandle);

        Assert.That(UnpatchPatchTargets.TargetA(), Is.EqualTo(1));
        Assert.That(UnpatchPatchTargets.TargetB(), Is.EqualTo(42));

        Patcher.Unpatch(secondHandle);

        Assert.That(UnpatchPatchTargets.TargetA(), Is.EqualTo(1));
        Assert.That(UnpatchPatchTargets.TargetB(), Is.EqualTo(2));
    }

    [Test]
    public void ApplyUnpatchApply_ExecutesSecondPatch()
    {
        PatchHandle firstHandle = Patcher.Patch(typeof(UnpatchPatches)
            .GetMethod(nameof(UnpatchPatches.ApplyUnpatchApply_ExecutesSecondPatch_First))!);
        Patcher.Unpatch(firstHandle);
        Patcher.Patch(typeof(UnpatchPatches)
            .GetMethod(nameof(UnpatchPatches.ApplyUnpatchApply_ExecutesSecondPatch_Second))!);

        UnpatchPatchTargets.ApplyUnpatchApplyTarget();
        int observedPatch = UnpatchPatches.observedPatch;

        Assert.That(observedPatch, Is.EqualTo(2));
    }

    [Test]
    public void PatchHandle_UnpatchesOnlySelectedPatch_OnSameTarget()
    {
        PatchHandle firstHandle = Patcher.Patch(typeof(UnpatchPatches)
            .GetMethod(nameof(UnpatchPatches.PatchHandle_UnpatchesOnlySelectedPatch_OnSameTarget_FirstPrefix))!);
        PatchHandle secondHandle = Patcher.Patch(typeof(UnpatchPatches)
            .GetMethod(nameof(UnpatchPatches.PatchHandle_UnpatchesOnlySelectedPatch_OnSameTarget_SecondPrefix))!);

        UnpatchPatchTargets.ApplyUnpatchApplyTarget();
        Assert.That(UnpatchPatches.firstPatchCalls, Is.EqualTo(1));
        Assert.That(UnpatchPatches.secondPatchCalls, Is.EqualTo(1));

        Patcher.Unpatch(firstHandle);
        UnpatchPatchTargets.ApplyUnpatchApplyTarget();

        Assert.That(UnpatchPatches.firstPatchCalls, Is.EqualTo(1));
        Assert.That(UnpatchPatches.secondPatchCalls, Is.EqualTo(2));

        Patcher.Unpatch(secondHandle);
        UnpatchPatchTargets.ApplyUnpatchApplyTarget();

        Assert.That(UnpatchPatches.firstPatchCalls, Is.EqualTo(1));
        Assert.That(UnpatchPatches.secondPatchCalls, Is.EqualTo(2));
    }

    [Test]
    public void PatchAllHandle_UnpatchesOnlyPatchesOwnedByThatHandle()
    {
        PatchHandle patchAllHandle = Patcher.PatchAll(typeof(UnpatchPatchAllPatches));
        PatchHandle independentHandle = Patcher.Patch(typeof(UnpatchPatches)
            .GetMethod(nameof(UnpatchPatches.PatchAllHandle_UnpatchesOnlyPatchesOwnedByThatHandle_IndependentPostfix))!);

        Assert.That(UnpatchPatchTargets.TargetA(), Is.EqualTo(42));
        Assert.That(UnpatchPatchTargets.TargetB(), Is.EqualTo(43));

        Patcher.Unpatch(patchAllHandle);

        Assert.That(UnpatchPatchTargets.TargetA(), Is.EqualTo(1));
        Assert.That(UnpatchPatchTargets.TargetB(), Is.EqualTo(43));

        Patcher.Unpatch(independentHandle);

        Assert.That(UnpatchPatchTargets.TargetA(), Is.EqualTo(1));
        Assert.That(UnpatchPatchTargets.TargetB(), Is.EqualTo(2));
    }
}
