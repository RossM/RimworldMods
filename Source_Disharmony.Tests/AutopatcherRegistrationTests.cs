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

    [Postfix]
    public static void Register_TargetsOnly_UsesAttributesAndDefersUntilApply(ref int __result) => __result = 42;

    [InnerPostfix(typeof(InnerStaticMethodTargets), nameof(InnerStaticMethodTargets.IntResult))]
    public static void Patch_TargetsOnly_UsesAttributesForInnerPatch(ref int __result) => __result = 42;

    public static bool Register_AllInformation_UsesExplicitPrefixAndDefersUntilApply() => false;

    public static void Patch_AllInformation_UsesExplicitPostfixForEveryTarget(ref int __result) => __result = 42;

    public static bool Patch_AllInformation_UsesExplicitInnerPrefix() => false;

    public static void Patch_AllInformation_UsesExplicitInnerPostfix(ref int __result) => __result = 42;

    public static void Patch_AllInformation_UsesExplicitFieldSetter(ref int value) => value = 42;
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

[Patch(typeof(StaticMethodTargets))]
[Category("preferred-attributes")]
public static class PreferredRegistrationAttributePatches
{
    [Postfix]
    [Target(nameof(StaticMethodTargets.IntIdentity))]
    public static void PatchAttributeMarksClassForAssemblyProcessing(ref int __result) => __result = 42;

    [Postfix]
    [Target(nameof(StaticMethodTargets.StringIdentity))]
    public static void CategoryAttributeMarksClassForCategoryProcessing(ref string __result) => __result = "patched";
}

[Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.RegistrationResultA))]
public static class ClassTargetAttributePatches
{
    public static int FirstPatchCalls;
    public static int SecondPatchCalls;

    [Postfix]
    public static void ClassTargetAttributeAppliesToEveryPatchMethod_First() => FirstPatchCalls++;

    [Postfix]
    public static void ClassTargetAttributeAppliesToEveryPatchMethod_Second() => SecondPatchCalls++;
}

[Targets(typeof(StaticMethodTargets), nameof(StaticMethodTargets.OverloadedVoid))]
public static class ClassTargetsAttributePatches
{
    public static int FirstPatchCalls;
    public static int SecondPatchCalls;

    [Postfix]
    public static void ClassTargetsAttributeAppliesToEveryPatchMethod_First() => FirstPatchCalls++;

    [Postfix]
    public static void ClassTargetsAttributeAppliesToEveryPatchMethod_Second() => SecondPatchCalls++;
}

[HarmonyPatch(typeof(StaticMethodTargets))]
[HarmonyPatchCategory("included")]
public static class IncludedCategoryPatches
{
    [Postfix]
    [Target(nameof(StaticMethodTargets.RegistrationResultA))]
    public static void PatchAllProcessesAllAssemblyPatchCategories_Included(ref int __result) => __result = 42;

    [Postfix]
    [Target(nameof(StaticMethodTargets.RegistrationResultA))]
    public static void PatchCategoryProcessesOnlyMatchingCategory(ref int __result) => __result = 42;

    [Postfix]
    [Target(nameof(StaticMethodTargets.RegistrationResultA))]
    public static void RegisterAllDefersAssemblyPatchesUntilApply_Included(ref int __result) => __result = 42;
}

[HarmonyPatch(typeof(StaticMethodTargets))]
[HarmonyPatchCategory("excluded")]
public static class ExcludedCategoryPatches
{
    [Postfix]
    [Target(nameof(StaticMethodTargets.RegistrationResultB))]
    public static void PatchAllProcessesAllAssemblyPatchCategories_Excluded(ref int __result) => __result = 42;

    [Postfix]
    [Target(nameof(StaticMethodTargets.RegistrationResultB))]
    public static void PatchCategoryProcessesOnlyMatchingCategory(ref int __result) => __result = 42;

    [Postfix]
    [Target(nameof(StaticMethodTargets.RegistrationResultB))]
    public static void RegisterAllDefersAssemblyPatchesUntilApply_Excluded(ref int __result) => __result = 42;
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

        Patcher.Register(patch);

        Assert.That(StaticMethodTargets.RegistrationResultA(), Is.EqualTo(1));

        Patcher.Apply();

        Assert.That(StaticMethodTargets.RegistrationResultA(), Is.EqualTo(42));
    }

    [Test]
    public void ForceApplyAppliesRegisteredMethod()
    {
        MethodInfo patch = typeof(AutopatcherRegistrationPatches)
            .GetMethod(nameof(AutopatcherRegistrationPatches.ForceApplyAppliesRegisteredMethod))!;

        Patcher.Register(patch);
        Patcher.ForceApply();

        Assert.That(StaticMethodTargets.RegistrationResultB(), Is.EqualTo(42));
    }

    [Test]
    public void PatchTypeProcessesEveryPatchMethodOnType()
    {
        Patcher.Patch(typeof(PatchTypeProcessesEveryPatchMethodOnTypePatches));

        Assert.That(StaticMethodTargets.RegistrationResultA(), Is.EqualTo(42));
        Assert.That(StaticMethodTargets.RegistrationResultB(), Is.EqualTo(42));
    }

    [Test]
    public void PatchAttributeMarksClassForAssemblyProcessing()
    {
        Patcher.PatchAll(TestAssembly);

        Assert.That(StaticMethodTargets.IntIdentity(1), Is.EqualTo(42));
    }

    [Test]
    public void CategoryAttributeMarksClassForCategoryProcessing()
    {
        Patcher.PatchCategory(TestAssembly, "preferred-attributes");

        Assert.That(StaticMethodTargets.StringIdentity("original"), Is.EqualTo("patched"));
    }

    [Test]
    public void ClassTargetAttributeAppliesToEveryPatchMethod()
    {
        ClassTargetAttributePatches.FirstPatchCalls = 0;
        ClassTargetAttributePatches.SecondPatchCalls = 0;
        Patcher.Patch(typeof(ClassTargetAttributePatches));

        StaticMethodTargets.RegistrationResultA();

        Assert.That(ClassTargetAttributePatches.FirstPatchCalls, Is.EqualTo(1));
        Assert.That(ClassTargetAttributePatches.SecondPatchCalls, Is.EqualTo(1));
    }

    [Test]
    public void ClassTargetsAttributeAppliesToEveryPatchMethod()
    {
        ClassTargetsAttributePatches.FirstPatchCalls = 0;
        ClassTargetsAttributePatches.SecondPatchCalls = 0;
        Patcher.Patch(typeof(ClassTargetsAttributePatches));

        StaticMethodTargets.OverloadedVoid(1);
        StaticMethodTargets.OverloadedVoid("value");

        Assert.That(ClassTargetsAttributePatches.FirstPatchCalls, Is.EqualTo(2));
        Assert.That(ClassTargetsAttributePatches.SecondPatchCalls, Is.EqualTo(2));
    }

    [Test]
    public void MultipleTargetAttributesPatchEachTarget()
    {
        MethodInfo patch = typeof(AutopatcherRegistrationPatches)
            .GetMethod(nameof(AutopatcherRegistrationPatches.MultipleTargetAttributesPatchEachTarget))!;

        Patcher.Patch(patch);

        Assert.That(StaticMethodTargets.RegistrationResultA(), Is.EqualTo(42));
        Assert.That(StaticMethodTargets.RegistrationResultB(), Is.EqualTo(42));
    }

    [Test]
    public void TargetsAttributePatchesEveryOverload()
    {
        AutopatcherRegistrationPatches.OverloadPatchCalls = 0;
        MethodInfo patch = typeof(AutopatcherRegistrationPatches)
            .GetMethod(nameof(AutopatcherRegistrationPatches.TargetsAttributePatchesEveryOverload))!;
        Patcher.Patch(patch);

        StaticMethodTargets.OverloadedVoid(1);
        StaticMethodTargets.OverloadedVoid("value");

        Assert.That(AutopatcherRegistrationPatches.OverloadPatchCalls, Is.EqualTo(2));
    }

    [Test]
    public void Register_TargetsOnly_UsesAttributesAndDefersUntilApply()
    {
        MethodInfo patch = typeof(AutopatcherRegistrationPatches)
            .GetMethod(nameof(AutopatcherRegistrationPatches.Register_TargetsOnly_UsesAttributesAndDefersUntilApply))!;
        MethodInfo firstTarget = typeof(StaticMethodTargets)
            .GetMethod(nameof(StaticMethodTargets.RegistrationResultA))!;
        MethodInfo secondTarget = typeof(StaticMethodTargets)
            .GetMethod(nameof(StaticMethodTargets.RegistrationResultB))!;

        Patcher.Register(patch, [firstTarget, secondTarget]);

        Assert.That(StaticMethodTargets.RegistrationResultA(), Is.EqualTo(1));
        Assert.That(StaticMethodTargets.RegistrationResultB(), Is.EqualTo(2));

        Patcher.Apply();

        Assert.That(StaticMethodTargets.RegistrationResultA(), Is.EqualTo(42));
        Assert.That(StaticMethodTargets.RegistrationResultB(), Is.EqualTo(42));
    }

    [Test]
    public void Patch_TargetsOnly_UsesAttributesForInnerPatch()
    {
        MethodInfo patch = typeof(AutopatcherRegistrationPatches)
            .GetMethod(nameof(AutopatcherRegistrationPatches.Patch_TargetsOnly_UsesAttributesForInnerPatch))!;
        MethodInfo target = typeof(OuterStaticMethodTargets)
            .GetMethod(nameof(OuterStaticMethodTargets.IntResult))!;

        Patcher.Patch(patch, target);

        Assert.That(OuterStaticMethodTargets.IntResult(), Is.EqualTo(42));
    }

    [Test]
    public void Register_AllInformation_UsesExplicitPrefixAndDefersUntilApply()
    {
        MethodInfo patch = typeof(AutopatcherRegistrationPatches)
            .GetMethod(nameof(AutopatcherRegistrationPatches.Register_AllInformation_UsesExplicitPrefixAndDefersUntilApply))!;
        MethodInfo target = typeof(StaticMethodTargets)
            .GetMethod(nameof(StaticMethodTargets.RegistrationResultA))!;

        Patcher.Register(patch, PatchType.Prefix, targets: [target]);

        Assert.That(StaticMethodTargets.RegistrationResultA(), Is.EqualTo(1));

        Patcher.Apply();

        Assert.That(StaticMethodTargets.RegistrationResultA(), Is.Zero);
    }

    [Test]
    public void Patch_AllInformation_UsesExplicitPostfixForEveryTarget()
    {
        MethodInfo patch = typeof(AutopatcherRegistrationPatches)
            .GetMethod(nameof(AutopatcherRegistrationPatches.Patch_AllInformation_UsesExplicitPostfixForEveryTarget))!;
        MethodInfo firstTarget = typeof(StaticMethodTargets)
            .GetMethod(nameof(StaticMethodTargets.RegistrationResultA))!;
        MethodInfo secondTarget = typeof(StaticMethodTargets)
            .GetMethod(nameof(StaticMethodTargets.RegistrationResultB))!;

        Patcher.Patch(patch, PatchType.Postfix, targets: [firstTarget, secondTarget]);

        Assert.That(StaticMethodTargets.RegistrationResultA(), Is.EqualTo(42));
        Assert.That(StaticMethodTargets.RegistrationResultB(), Is.EqualTo(42));
    }

    [Test]
    public void Patch_AllInformation_UsesExplicitInnerPrefix()
    {
        MethodInfo patch = typeof(AutopatcherRegistrationPatches)
            .GetMethod(nameof(AutopatcherRegistrationPatches.Patch_AllInformation_UsesExplicitInnerPrefix))!;
        MethodInfo innerTarget = typeof(InnerStaticMethodTargets)
            .GetMethod(nameof(InnerStaticMethodTargets.IntResult))!;
        MethodInfo outerTarget = typeof(OuterStaticMethodTargets)
            .GetMethod(nameof(OuterStaticMethodTargets.IntResult))!;

        Patcher.Patch(
            patch,
            PatchType.InnerPrefix,
            innerTarget: innerTarget,
            targets: [outerTarget]);

        Assert.That(OuterStaticMethodTargets.IntResult(), Is.Zero);
    }

    [Test]
    public void Patch_AllInformation_UsesExplicitInnerPostfix()
    {
        MethodInfo patch = typeof(AutopatcherRegistrationPatches)
            .GetMethod(nameof(AutopatcherRegistrationPatches.Patch_AllInformation_UsesExplicitInnerPostfix))!;
        MethodInfo innerTarget = typeof(InnerStaticMethodTargets)
            .GetMethod(nameof(InnerStaticMethodTargets.IntResult))!;
        MethodInfo outerTarget = typeof(OuterStaticMethodTargets)
            .GetMethod(nameof(OuterStaticMethodTargets.IntResult))!;

        Patcher.Patch(
            patch,
            PatchType.InnerPostfix,
            innerTarget: innerTarget,
            targets: [outerTarget]);

        Assert.That(OuterStaticMethodTargets.IntResult(), Is.EqualTo(42));
    }

    [Test]
    public void Patch_AllInformation_UsesExplicitFieldSetter()
    {
        MethodInfo patch = typeof(AutopatcherRegistrationPatches)
            .GetMethod(nameof(AutopatcherRegistrationPatches.Patch_AllInformation_UsesExplicitFieldSetter))!;
        FieldInfo innerTarget = typeof(InnerStaticMethodTargets)
            .GetField(nameof(InnerStaticMethodTargets.Field))!;
        MethodInfo outerTarget = typeof(OuterStaticMethodTargets)
            .GetMethod(nameof(OuterStaticMethodTargets.SetStaticField))!;
        InnerStaticMethodTargets.Field = 0;

        Patcher.Patch(
            patch,
            PatchType.InnerPrefix,
            innerTarget: innerTarget,
            innerMemberType: MemberType.Setter,
            targets: [outerTarget]);

        OuterStaticMethodTargets.SetStaticField(1);

        Assert.That(InnerStaticMethodTargets.Field, Is.EqualTo(42));
    }

    [Test]
    public void Patch_AllInformation_UsesInlineOption()
    {
        MethodInfo patch = typeof(AutopatcherRegistrationInlinePatches)
            .GetMethod(nameof(AutopatcherRegistrationInlinePatches.Patch_AllInformation_UsesInlineOption))!;
        MethodInfo target = typeof(StaticMethodTargets)
            .GetMethod(nameof(StaticMethodTargets.RegistrationResultA))!;
        AutopatcherRegistrationInlinePatches.ObservedMethod = null;

        Patcher.Patch(
            patch,
            PatchType.Prefix,
            options: PatchOptions.Inline,
            targets: [target]);

        StaticMethodTargets.RegistrationResultA();

        Assert.That(AutopatcherRegistrationInlinePatches.ObservedMethod, Is.Not.Null);
        Assert.That(AutopatcherRegistrationInlinePatches.ObservedMethod, Is.Not.EqualTo(patch));
    }

    [Test]
    public void PatchCategoryProcessesOnlyMatchingCategory()
    {
        Patcher.PatchCategory(TestAssembly, "included");

        Assert.That(StaticMethodTargets.RegistrationResultA(), Is.EqualTo(42));
        Assert.That(StaticMethodTargets.RegistrationResultB(), Is.EqualTo(2));
    }

    [Test]
    public void RegisterAllDefersAssemblyPatchesUntilApply()
    {
        Patcher.RegisterAll(TestAssembly);

        Assert.That(StaticMethodTargets.RegistrationResultA(), Is.EqualTo(1));
        Assert.That(StaticMethodTargets.RegistrationResultB(), Is.EqualTo(2));

        Patcher.Apply();

        Assert.That(StaticMethodTargets.RegistrationResultA(), Is.EqualTo(42));
        Assert.That(StaticMethodTargets.RegistrationResultB(), Is.EqualTo(42));
    }

    [Test]
    public void PatchAllProcessesAllAssemblyPatchCategories()
    {
        Patcher.PatchAll(TestAssembly);

        Assert.That(StaticMethodTargets.RegistrationResultA(), Is.EqualTo(42));
        Assert.That(StaticMethodTargets.RegistrationResultB(), Is.EqualTo(42));
    }
}
