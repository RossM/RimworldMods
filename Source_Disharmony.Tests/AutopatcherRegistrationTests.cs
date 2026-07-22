using HarmonyLib;

namespace Disharmony.Tests;

public static class AutopatcherRegistrationPatches
{
    public static int OverloadPatchCalls;

    [Postfix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.RegistrationResultA))]
    public static void RegisterMethodDefersPatchUntilApply(ref int __result) => __result = 42;

    [Postfix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.RegistrationResultB))]
    public static void ForceApplyAppliesRegisteredMethod(ref int __result) => __result = 42;

    [Postfix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.RegistrationResultA))]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.RegistrationResultB))]
    public static void MultipleTargetAttributesPatchEachTarget(ref int __result) => __result = 42;

    [Postfix]
    [Targets(typeof(StaticMethodTargets), nameof(StaticMethodTargets.OverloadedVoid))]
    public static void TargetsAttributePatchesEveryOverload() => OverloadPatchCalls++;
}

public static class PatchTypeProcessesEveryPatchMethodOnTypePatches
{
    [Postfix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.RegistrationResultA))]
    public static void PatchTypeProcessesEveryPatchMethodOnType_First(ref int __result) => __result = 42;

    [Postfix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.RegistrationResultB))]
    public static void PatchTypeProcessesEveryPatchMethodOnType_Second(ref int __result) => __result = 42;
}

[HarmonyPatch(typeof(StaticMethodTargets))]
[HarmonyPatchCategory("included")]
public static class IncludedCategoryPatches
{
    [Postfix]
    [Target(nameof(StaticMethodTargets.RegistrationResultA))]
    public static void PatchCategoryProcessesOnlyMatchingCategory(ref int __result) => __result = 42;

    [Postfix]
    [Target(nameof(StaticMethodTargets.RegistrationResultA))]
    public static void RegisterAllDefersAssemblyPatchesUntilApply_Included(ref int __result) => __result = 42;

    [Postfix]
    [Target(nameof(StaticMethodTargets.RegistrationResultA))]
    public static void PatchAllProcessesAllAssemblyPatchCategories_Included(ref int __result) => __result = 42;
}

[HarmonyPatch(typeof(StaticMethodTargets))]
[HarmonyPatchCategory("excluded")]
public static class ExcludedCategoryPatches
{
    [Postfix]
    [Target(nameof(StaticMethodTargets.RegistrationResultB))]
    public static void PatchCategoryProcessesOnlyMatchingCategory(ref int __result) => __result = 42;

    [Postfix]
    [Target(nameof(StaticMethodTargets.RegistrationResultB))]
    public static void RegisterAllDefersAssemblyPatchesUntilApply_Excluded(ref int __result) => __result = 42;

    [Postfix]
    [Target(nameof(StaticMethodTargets.RegistrationResultB))]
    public static void PatchAllProcessesAllAssemblyPatchCategories_Excluded(ref int __result) => __result = 42;
}

[TestFixture]
public sealed class AutopatcherRegistrationTests : PatchTestBase
{
    // TODO: Put assembly-scanning patch fixtures in a dedicated test assembly so RegisterAll/PatchAll do not scan unrelated patches.
    private static readonly Assembly TestAssembly = typeof(AutopatcherRegistrationTests).Assembly;

    [Test]
    public void RegisterMethodDefersPatchUntilApply()
    {
        MethodInfo patch = typeof(AutopatcherRegistrationPatches)
            .GetMethod(nameof(AutopatcherRegistrationPatches.RegisterMethodDefersPatchUntilApply))!;

        Autopatcher.Register(patch);

        Assert.That(StaticMethodTargets.RegistrationResultA(), Is.EqualTo(1));

        Autopatcher.Apply();

        Assert.That(StaticMethodTargets.RegistrationResultA(), Is.EqualTo(42));
    }

    [Test]
    public void ForceApplyAppliesRegisteredMethod()
    {
        MethodInfo patch = typeof(AutopatcherRegistrationPatches)
            .GetMethod(nameof(AutopatcherRegistrationPatches.ForceApplyAppliesRegisteredMethod))!;

        Autopatcher.Register(patch);
        Autopatcher.ForceApply();

        Assert.That(StaticMethodTargets.RegistrationResultB(), Is.EqualTo(42));
    }

    [Test]
    public void PatchTypeProcessesEveryPatchMethodOnType()
    {
        Autopatcher.Patch(typeof(PatchTypeProcessesEveryPatchMethodOnTypePatches));

        Assert.That(StaticMethodTargets.RegistrationResultA(), Is.EqualTo(42));
        Assert.That(StaticMethodTargets.RegistrationResultB(), Is.EqualTo(42));
    }

    [Test]
    public void MultipleTargetAttributesPatchEachTarget()
    {
        MethodInfo patch = typeof(AutopatcherRegistrationPatches)
            .GetMethod(nameof(AutopatcherRegistrationPatches.MultipleTargetAttributesPatchEachTarget))!;

        Autopatcher.Patch(patch);

        Assert.That(StaticMethodTargets.RegistrationResultA(), Is.EqualTo(42));
        Assert.That(StaticMethodTargets.RegistrationResultB(), Is.EqualTo(42));
    }

    [Test]
    public void TargetsAttributePatchesEveryOverload()
    {
        AutopatcherRegistrationPatches.OverloadPatchCalls = 0;
        MethodInfo patch = typeof(AutopatcherRegistrationPatches)
            .GetMethod(nameof(AutopatcherRegistrationPatches.TargetsAttributePatchesEveryOverload))!;
        Autopatcher.Patch(patch);

        StaticMethodTargets.OverloadedVoid(1);
        StaticMethodTargets.OverloadedVoid("value");

        Assert.That(AutopatcherRegistrationPatches.OverloadPatchCalls, Is.EqualTo(2));
    }

    [Test]
    public void PatchCategoryProcessesOnlyMatchingCategory()
    {
        Autopatcher.PatchCategory(TestAssembly, "included");

        Assert.That(StaticMethodTargets.RegistrationResultA(), Is.EqualTo(42));
        Assert.That(StaticMethodTargets.RegistrationResultB(), Is.EqualTo(2));
    }

    [Test]
    public void RegisterAllDefersAssemblyPatchesUntilApply()
    {
        Autopatcher.RegisterAll(TestAssembly);

        Assert.That(StaticMethodTargets.RegistrationResultA(), Is.EqualTo(1));
        Assert.That(StaticMethodTargets.RegistrationResultB(), Is.EqualTo(2));

        Autopatcher.Apply();

        Assert.That(StaticMethodTargets.RegistrationResultA(), Is.EqualTo(42));
        Assert.That(StaticMethodTargets.RegistrationResultB(), Is.EqualTo(42));
    }

    [Test]
    public void PatchAllProcessesAllAssemblyPatchCategories()
    {
        Autopatcher.PatchAll(TestAssembly);

        Assert.That(StaticMethodTargets.RegistrationResultA(), Is.EqualTo(42));
        Assert.That(StaticMethodTargets.RegistrationResultB(), Is.EqualTo(42));
    }
}
