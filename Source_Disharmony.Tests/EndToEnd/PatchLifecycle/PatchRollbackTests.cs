namespace Disharmony.Tests.EndToEnd.PatchLifecycle;

public static class PatchRollbackPatches
{
    public static void Patch_MultipleConfigs_RollsBackEarlierConfigWhenLaterConfigIsInvalid_GroupedPostfix(
        ref int __result) => __result = 42;

    public static void Patch_MultipleConfigs_RollsBackEarlierConfigWhenLaterConfigIsInvalid_IndependentPostfix(
        ref int __result) => __result = 43;

    public static void Patch_MultipleTargets_RollsBackEarlierTargetWhenLaterTargetIsInvalid(ref int __result) =>
        __result = 42;
}

public sealed class PatchRollbackPatchAllPatches
{
    [Postfix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.RegistrationResultA))]
    public static void PatchAll_Type_RollsBackEarlierPatchWhenLaterPatchIsInvalid_ValidPostfix(ref int __result) =>
        __result = 42;

    [Prefix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.Void))]
    public void PatchAll_Type_RollsBackEarlierPatchWhenLaterPatchIsInvalid_InvalidInstancePrefix() { }
}

[TestFixture]
public sealed class PatchRollbackTests : PatchTestBase
{
    [Test]
    public void Patch_MultipleConfigs_RollsBackEarlierConfigWhenLaterConfigIsInvalid()
    {
        MethodInfo groupedPatch = typeof(PatchRollbackPatches)
            .GetMethod(nameof(PatchRollbackPatches
                .Patch_MultipleConfigs_RollsBackEarlierConfigWhenLaterConfigIsInvalid_GroupedPostfix))!;
        MethodInfo independentPatch = typeof(PatchRollbackPatches)
            .GetMethod(nameof(PatchRollbackPatches
                .Patch_MultipleConfigs_RollsBackEarlierConfigWhenLaterConfigIsInvalid_IndependentPostfix))!;
        MethodInfo groupedTarget = typeof(StaticMethodTargets)
            .GetMethod(nameof(StaticMethodTargets.RegistrationResultA))!;
        MethodInfo independentTarget = typeof(StaticMethodTargets)
            .GetMethod(nameof(StaticMethodTargets.RegistrationResultB))!;
        MethodInfo invalidTarget = typeof(StaticMethodTargets)
            .GetMethod(nameof(StaticMethodTargets.Void))!;

        Patcher.Patch(Patch.Postfix.With(independentPatch).Of(independentTarget));

        PatchConfig validPatch = Patch.Postfix.With(groupedPatch).Of(groupedTarget);
        PatchConfig invalidPatch = Patch.Prefix.Of(invalidTarget);
        Assert.Throws<ArgumentException>(() => Patcher.Patch(validPatch, invalidPatch));

        Patcher.ForceApply();

        Assert.That(StaticMethodTargets.RegistrationResultA(), Is.EqualTo(1));
        Assert.That(StaticMethodTargets.RegistrationResultB(), Is.EqualTo(43));
    }

    [Test]
    public void Patch_MultipleTargets_RollsBackEarlierTargetWhenLaterTargetIsInvalid()
    {
        MethodInfo patch = typeof(PatchRollbackPatches)
            .GetMethod(nameof(PatchRollbackPatches
                .Patch_MultipleTargets_RollsBackEarlierTargetWhenLaterTargetIsInvalid))!;
        MethodInfo validTarget = typeof(StaticMethodTargets)
            .GetMethod(nameof(StaticMethodTargets.RegistrationResultA))!;
        MethodInfo invalidTarget = typeof(StaticMethodTargets)
            .GetMethod(nameof(StaticMethodTargets.Void))!;

        Assert.Throws<PatchException>(() =>
            Patcher.Patch(Patch.Postfix.With(patch), validTarget, invalidTarget));

        Patcher.ForceApply();

        Assert.That(StaticMethodTargets.RegistrationResultA(), Is.EqualTo(1));
    }

    [Test]
    public void PatchAll_Type_RollsBackEarlierPatchWhenLaterPatchIsInvalid()
    {
        Assert.Throws<PatchException>(() => Patcher.PatchAll(typeof(PatchRollbackPatchAllPatches)));

        Patcher.ForceApply();

        Assert.That(StaticMethodTargets.RegistrationResultA(), Is.EqualTo(1));
    }
}
