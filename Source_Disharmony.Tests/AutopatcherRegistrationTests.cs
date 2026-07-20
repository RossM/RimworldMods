using System.Reflection;
using HarmonyLib;
using NUnit.Framework;

namespace Disharmony.Tests;

public static class RegistrationPatchMethods
{
    [Postfix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.RegistrationResultA))]
    public static void ReplaceFirstResult(ref int __result) => __result = 42;

    [Postfix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.RegistrationResultB))]
    public static void ReplaceSecondResult(ref int __result) => __result = 42;
}

public static class MultipleTargetPatchMethods
{
    public static int OverloadPatchCalls;

    [Postfix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.RegistrationResultA))]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.RegistrationResultB))]
    public static void ReplaceBothResults(ref int __result) => __result = 42;

    [Postfix]
    [Targets(typeof(StaticMethodTargets), nameof(StaticMethodTargets.OverloadedVoid))]
    public static void ObserveOverload() => OverloadPatchCalls++;
}

[HarmonyPatch(typeof(StaticMethodTargets))]
[HarmonyPatchCategory("included")]
public static class IncludedCategoryPatchMethods
{
    [Postfix]
    [Target(nameof(StaticMethodTargets.RegistrationResultA))]
    public static void ReplaceResult(ref int __result) => __result = 42;
}

[HarmonyPatch(typeof(StaticMethodTargets))]
[HarmonyPatchCategory("excluded")]
public static class ExcludedCategoryPatchMethods
{
    [Postfix]
    [Target(nameof(StaticMethodTargets.RegistrationResultB))]
    public static void ReplaceResult(ref int __result) => __result = 42;
}

[TestFixture]
public sealed class AutopatcherRegistrationTests : PatchTestBase
{
    // TODO: Put assembly-scanning patch fixtures in a dedicated test assembly so RegisterAll/PatchAll do not scan unrelated patches.
    private static readonly Assembly TestAssembly = typeof(AutopatcherRegistrationTests).Assembly;

    [Test]
    public void RegisterMethodDefersPatchUntilApply()
    {
        MethodInfo patch = typeof(RegistrationPatchMethods).GetMethod(nameof(RegistrationPatchMethods.ReplaceFirstResult))!;

        Autopatcher.Register(patch);

        Assert.That(StaticMethodTargets.RegistrationResultA(), Is.EqualTo(1));

        Autopatcher.Apply();

        Assert.That(StaticMethodTargets.RegistrationResultA(), Is.EqualTo(42));
    }

    [Test]
    public void ForceApplyAppliesRegisteredMethod()
    {
        MethodInfo patch = typeof(RegistrationPatchMethods).GetMethod(nameof(RegistrationPatchMethods.ReplaceSecondResult))!;

        Autopatcher.Register(patch);
        Autopatcher.ForceApply();

        Assert.That(StaticMethodTargets.RegistrationResultB(), Is.EqualTo(42));
    }

    [Test]
    public void PatchTypeProcessesEveryPatchMethodOnType()
    {
        Autopatcher.Patch(typeof(RegistrationPatchMethods));

        Assert.That(StaticMethodTargets.RegistrationResultA(), Is.EqualTo(42));
        Assert.That(StaticMethodTargets.RegistrationResultB(), Is.EqualTo(42));
    }

    [Test]
    public void MultipleTargetAttributesPatchEachTarget()
    {
        MethodInfo patch = typeof(MultipleTargetPatchMethods).GetMethod(nameof(MultipleTargetPatchMethods.ReplaceBothResults))!;

        Autopatcher.Patch(patch);

        Assert.That(StaticMethodTargets.RegistrationResultA(), Is.EqualTo(42));
        Assert.That(StaticMethodTargets.RegistrationResultB(), Is.EqualTo(42));
    }

    [Test]
    public void TargetsAttributePatchesEveryOverload()
    {
        MultipleTargetPatchMethods.OverloadPatchCalls = 0;
        MethodInfo patch = typeof(MultipleTargetPatchMethods).GetMethod(nameof(MultipleTargetPatchMethods.ObserveOverload))!;
        Autopatcher.Patch(patch);

        StaticMethodTargets.OverloadedVoid(1);
        StaticMethodTargets.OverloadedVoid("value");

        Assert.That(MultipleTargetPatchMethods.OverloadPatchCalls, Is.EqualTo(2));
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
