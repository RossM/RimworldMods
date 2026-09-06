namespace Disharmony.Tests.EndToEnd.PatchLifecycle;

public static class PatcherRegistrationPatches
{
    public static int overloadPatchCalls;

    [Postfix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.RegistrationResultA))]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.RegistrationResultB))]
    public static void MultipleTargetAttributesPatchEachTarget(ref int __result) => __result = 42;

    [Postfix]
    [Targets(typeof(StaticMethodTargets), nameof(StaticMethodTargets.OverloadedVoid))]
    public static void TargetsAttributePatchesEveryOverload() => overloadPatchCalls++;

    [Postfix] [Inner(typeof(InnerStaticMethodTargets), nameof(InnerStaticMethodTargets.IntResult))]
    public static void Patch_TargetsOnly_UsesAttributesForInnerPatch(ref int __result) => __result = 42;

    public static void Patch_PatchConfig_UsesExplicitPostfixForEveryTarget(ref int __result) => __result = 42;

    public static bool Patch_PatchConfig_UsesExplicitInnerPrefix() => false;

    public static void Patch_PatchConfig_UsesExplicitInnerPostfix(ref int __result) => __result = 42;

    public static void Patch_PatchConfig_UsesExplicitFieldSetter(ref int value) => value = 42;
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
    public static int firstPatchCalls;
    public static int secondPatchCalls;

    [Postfix]
    public static void ClassTargetAttributeAppliesToEveryPatchMethod_First() => firstPatchCalls++;

    [Postfix]
    public static void ClassTargetAttributeAppliesToEveryPatchMethod_Second() => secondPatchCalls++;
}

[Targets(typeof(StaticMethodTargets), nameof(StaticMethodTargets.OverloadedVoid))]
public static class ClassTargetsAttributePatches
{
    public static int firstPatchCalls;
    public static int secondPatchCalls;

    [Postfix]
    public static void ClassTargetsAttributeAppliesToEveryPatchMethod_First() => firstPatchCalls++;

    [Postfix]
    public static void ClassTargetsAttributeAppliesToEveryPatchMethod_Second() => secondPatchCalls++;
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
}

[TestFixture]
public sealed class PatcherRegistrationTests : PatchTestBase
{
    // TODO: Put assembly-scanning patch fixtures in a dedicated test assembly so RegisterAll/PatchAll do not scan unrelated patches.
    private static readonly Assembly TestAssembly = typeof(PatcherRegistrationTests).Assembly;
    private static readonly Assembly CategoryAssembly = CreateCategoryAssembly();

    private static Assembly CreateCategoryAssembly()
    {
        // Isolate assembly discovery, particularly null-category selection, from unrelated test patches.
        var assembly = AppDomain.CurrentDomain.DefineDynamicAssembly(
            new AssemblyName("CategoryPatches"), AssemblyBuilderAccess.Run);
        var module = assembly.DefineDynamicModule("CategoryPatches");
        (string Target, string[] Categories, string[] HarmonyCategories)[] containers =
        [
            (nameof(CategoryTargets.Multiple), ["first", "second"], []),
            (nameof(CategoryTargets.Uncategorized), [], []),
            (nameof(CategoryTargets.Other), ["other"], []),
            (nameof(CategoryTargets.Empty), [""], []),
            (nameof(CategoryTargets.Duplicate), ["duplicate", "duplicate"], []),
            (nameof(CategoryTargets.Mixed), ["disharmony"], ["harmony"]),
        ];

        foreach (var container in containers)
        {
            var type = module.DefineType(container.Target + "Patches",
                TypeAttributes.Public | TypeAttributes.Abstract | TypeAttributes.Sealed);
            type.SetCustomAttribute(new CustomAttributeBuilder(
                typeof(PatchAttribute).GetConstructor([typeof(Type)])!, new object[] { typeof(CategoryTargets) }));
            foreach (string category in container.Categories)
                type.SetCustomAttribute(new CustomAttributeBuilder(
                    typeof(CategoryAttribute).GetConstructor([typeof(string)])!, new object[] { category }));
            foreach (string category in container.HarmonyCategories)
                type.SetCustomAttribute(new CustomAttributeBuilder(
                    typeof(HarmonyPatchCategory).GetConstructor([typeof(string)])!, new object[] { category }));

            var postfix = type.DefineMethod("Postfix", MethodAttributes.Public | MethodAttributes.Static,
                typeof(void), [typeof(string).MakeByRefType()]);
            postfix.DefineParameter(1, ParameterAttributes.None, "__result");
            postfix.SetCustomAttribute(new CustomAttributeBuilder(typeof(PostfixAttribute).GetConstructor(Type.EmptyTypes)!, []));
            postfix.SetCustomAttribute(new CustomAttributeBuilder(
                typeof(TargetAttribute).GetConstructor([typeof(Type), typeof(string)])!,
                new object[] { typeof(CategoryTargets), container.Target }));

            // __result = "patched:" + __result; repeated application is observable in the result.
            ILGenerator il = postfix.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldstr, "patched:");
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldind_Ref);
            il.Emit(OpCodes.Call, typeof(string).GetMethod(nameof(string.Concat), [typeof(string), typeof(string)])!);
            il.Emit(OpCodes.Stind_Ref);
            il.Emit(OpCodes.Ret);
            type.CreateType();
        }

        return assembly;
    }

    [Test]
    public void PatchCategory_CategoryNames_AreCaseSensitive()
    {
        Patcher.PatchCategory(CategoryAssembly, "First");

        Assert.That(CategoryTargets.Multiple(), Is.EqualTo("original"));
        Assert.That(CategoryTargets.Uncategorized(), Is.EqualTo("original"));
    }

    [Test]
    public void PatchCategory_MultipleCategories_FirstCategoryMatches()
    {
        Patcher.PatchCategory(CategoryAssembly, "first");

        Assert.That(CategoryTargets.Multiple(), Is.EqualTo("patched:original"));
        Assert.That(CategoryTargets.Other(), Is.EqualTo("original"));
        Assert.That(CategoryTargets.Uncategorized(), Is.EqualTo("original"));
    }

    [Test]
    public void PatchCategory_MultipleCategories_SecondCategoryMatches()
    {
        Patcher.PatchCategory(CategoryAssembly, "second");

        Assert.That(CategoryTargets.Multiple(), Is.EqualTo("patched:original"));
        Assert.That(CategoryTargets.Other(), Is.EqualTo("original"));
        Assert.That(CategoryTargets.Uncategorized(), Is.EqualTo("original"));
    }

    [Test]
    public void PatchCategory_UnmatchedCategory_AppliesNothing()
    {
        Patcher.PatchCategory(CategoryAssembly, "missing");

        Assert.That(CategoryTargets.Multiple(), Is.EqualTo("original"));
        Assert.That(CategoryTargets.Uncategorized(), Is.EqualTo("original"));
        Assert.That(CategoryTargets.Other(), Is.EqualTo("original"));
        Assert.That(CategoryTargets.Empty(), Is.EqualTo("original"));
        Assert.That(CategoryTargets.Duplicate(), Is.EqualTo("original"));
        Assert.That(CategoryTargets.Mixed(), Is.EqualTo("original"));
    }

    [Test]
    public void PatchCategory_NullCategory_AppliesOnlyUncategorizedClasses()
    {
        Patcher.PatchCategory(CategoryAssembly, null);

        Assert.That(CategoryTargets.Uncategorized(), Is.EqualTo("patched:original"));
        Assert.That(CategoryTargets.Multiple(), Is.EqualTo("original"));
        Assert.That(CategoryTargets.Other(), Is.EqualTo("original"));
        Assert.That(CategoryTargets.Empty(), Is.EqualTo("original"));
        Assert.That(CategoryTargets.Duplicate(), Is.EqualTo("original"));
        Assert.That(CategoryTargets.Mixed(), Is.EqualTo("original"));
    }

    [Test]
    public void PatchCategory_EmptyCategory_DoesNotMeanUncategorized()
    {
        Patcher.PatchCategory(CategoryAssembly, "");

        Assert.That(CategoryTargets.Empty(), Is.EqualTo("patched:original"));
        Assert.That(CategoryTargets.Uncategorized(), Is.EqualTo("original"));
        Assert.That(CategoryTargets.Multiple(), Is.EqualTo("original"));
    }

    [Test]
    public void PatchCategory_DuplicateCategories_AppliesPatchOnlyOnce()
    {
        Patcher.PatchCategory(CategoryAssembly, "duplicate");

        Assert.That(CategoryTargets.Duplicate(), Is.EqualTo("patched:original"));
    }

    [Test]
    public void PatchCategory_MixedAttributes_DisharmonyCategoryMatches()
    {
        Patcher.PatchCategory(CategoryAssembly, "disharmony");

        Assert.That(CategoryTargets.Mixed(), Is.EqualTo("patched:original"));
        Assert.That(CategoryTargets.Uncategorized(), Is.EqualTo("original"));
    }

    [Test]
    public void PatchCategory_MixedAttributes_HarmonyCategoryMatches()
    {
        Patcher.PatchCategory(CategoryAssembly, "harmony");

        Assert.That(CategoryTargets.Mixed(), Is.EqualTo("patched:original"));
        Assert.That(CategoryTargets.Uncategorized(), Is.EqualTo("original"));
    }

    [Test]
    public void PatchAll_MultipleCategories_AppliesEveryClassOnlyOnce()
    {
        Patcher.PatchAll(CategoryAssembly);

        Assert.That(CategoryTargets.Multiple(), Is.EqualTo("patched:original"));
        Assert.That(CategoryTargets.Uncategorized(), Is.EqualTo("patched:original"));
        Assert.That(CategoryTargets.Other(), Is.EqualTo("patched:original"));
        Assert.That(CategoryTargets.Empty(), Is.EqualTo("patched:original"));
        Assert.That(CategoryTargets.Duplicate(), Is.EqualTo("patched:original"));
        Assert.That(CategoryTargets.Mixed(), Is.EqualTo("patched:original"));
    }

    [Test]
    public void PatchTypeProcessesEveryPatchMethodOnType()
    {
        Patcher.PatchAll(typeof(PatchTypeProcessesEveryPatchMethodOnTypePatches));

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
        ClassTargetAttributePatches.firstPatchCalls = 0;
        ClassTargetAttributePatches.secondPatchCalls = 0;
        Patcher.PatchAll(typeof(ClassTargetAttributePatches));

        StaticMethodTargets.RegistrationResultA();

        Assert.That(ClassTargetAttributePatches.firstPatchCalls, Is.EqualTo(1));
        Assert.That(ClassTargetAttributePatches.secondPatchCalls, Is.EqualTo(1));
    }

    [Test]
    public void ClassTargetsAttributeAppliesToEveryPatchMethod()
    {
        ClassTargetsAttributePatches.firstPatchCalls = 0;
        ClassTargetsAttributePatches.secondPatchCalls = 0;
        Patcher.PatchAll(typeof(ClassTargetsAttributePatches));

        StaticMethodTargets.OverloadedVoid(1);
        StaticMethodTargets.OverloadedVoid("value");

        Assert.That(ClassTargetsAttributePatches.firstPatchCalls, Is.EqualTo(2));
        Assert.That(ClassTargetsAttributePatches.secondPatchCalls, Is.EqualTo(2));
    }

    [Test]
    public void MultipleTargetAttributesPatchEachTarget()
    {
        MethodInfo patch = typeof(PatcherRegistrationPatches)
            .GetMethod(nameof(PatcherRegistrationPatches.MultipleTargetAttributesPatchEachTarget))!;

        Patcher.Patch(patch);

        Assert.That(StaticMethodTargets.RegistrationResultA(), Is.EqualTo(42));
        Assert.That(StaticMethodTargets.RegistrationResultB(), Is.EqualTo(42));
    }

    [Test]
    public void TargetsAttributePatchesEveryOverload()
    {
        PatcherRegistrationPatches.overloadPatchCalls = 0;
        MethodInfo patch = typeof(PatcherRegistrationPatches)
            .GetMethod(nameof(PatcherRegistrationPatches.TargetsAttributePatchesEveryOverload))!;
        Patcher.Patch(patch);

        StaticMethodTargets.OverloadedVoid(1);
        StaticMethodTargets.OverloadedVoid("value");

        Assert.That(PatcherRegistrationPatches.overloadPatchCalls, Is.EqualTo(2));
    }

    [Test]
    public void Patch_PatchConfig_UsesExplicitPostfixForEveryTarget()
    {
        MethodInfo patch = typeof(PatcherRegistrationPatches)
            .GetMethod(nameof(PatcherRegistrationPatches.Patch_PatchConfig_UsesExplicitPostfixForEveryTarget))!;
        MethodInfo firstTarget = typeof(StaticMethodTargets)
            .GetMethod(nameof(StaticMethodTargets.RegistrationResultA))!;
        MethodInfo secondTarget = typeof(StaticMethodTargets)
            .GetMethod(nameof(StaticMethodTargets.RegistrationResultB))!;

        Patcher.Patch(Patch.Postfix.With(patch), firstTarget, secondTarget);

        Assert.That(StaticMethodTargets.RegistrationResultA(), Is.EqualTo(42));
        Assert.That(StaticMethodTargets.RegistrationResultB(), Is.EqualTo(42));
    }

    [Test]
    public void Patch_PatchConfig_UsesExplicitInnerPrefix()
    {
        MethodInfo patch = typeof(PatcherRegistrationPatches)
            .GetMethod(nameof(PatcherRegistrationPatches.Patch_PatchConfig_UsesExplicitInnerPrefix))!;
        MethodInfo innerTarget = typeof(InnerStaticMethodTargets)
            .GetMethod(nameof(InnerStaticMethodTargets.IntResult))!;
        MethodInfo outerTarget = typeof(OuterStaticMethodTargets)
            .GetMethod(nameof(OuterStaticMethodTargets.IntResult))!;

        Patcher.Patch(Patch.Prefix.Inner(innerTarget).With(patch).Of(outerTarget));

        Assert.That(OuterStaticMethodTargets.IntResult(), Is.Zero);
    }

    [Test]
    public void Patch_PatchConfig_UsesExplicitInnerPostfix()
    {
        MethodInfo patch = typeof(PatcherRegistrationPatches)
            .GetMethod(nameof(PatcherRegistrationPatches.Patch_PatchConfig_UsesExplicitInnerPostfix))!;
        MethodInfo innerTarget = typeof(InnerStaticMethodTargets)
            .GetMethod(nameof(InnerStaticMethodTargets.IntResult))!;
        MethodInfo outerTarget = typeof(OuterStaticMethodTargets)
            .GetMethod(nameof(OuterStaticMethodTargets.IntResult))!;

        Patcher.Patch(Patch.Postfix.Inner(innerTarget).With(patch).Of(outerTarget));

        Assert.That(OuterStaticMethodTargets.IntResult(), Is.EqualTo(42));
    }

    [Test]
    public void Patch_PatchConfig_UsesExplicitFieldSetter()
    {
        MethodInfo patch = typeof(PatcherRegistrationPatches)
            .GetMethod(nameof(PatcherRegistrationPatches.Patch_PatchConfig_UsesExplicitFieldSetter))!;
        FieldInfo innerTarget = typeof(InnerStaticMethodTargets)
            .GetField(nameof(InnerStaticMethodTargets.Field))!;
        MethodInfo outerTarget = typeof(OuterStaticMethodTargets)
            .GetMethod(nameof(OuterStaticMethodTargets.SetStaticField))!;
        InnerStaticMethodTargets.Field = 0;

        Patcher.Patch(Patch.Prefix.InnerSet(innerTarget).With(patch).Of(outerTarget));

        OuterStaticMethodTargets.SetStaticField(1);

        Assert.That(InnerStaticMethodTargets.Field, Is.EqualTo(42));
    }

    [Test]
    public void Patch_PatchConfig_UsesInlineOption()
    {
        MethodInfo patch = typeof(PatcherRegistrationInlinePatches)
            .GetMethod(nameof(PatcherRegistrationInlinePatches.Patch_PatchConfig_UsesInlineOption))!;
        MethodInfo target = typeof(StaticMethodTargets)
            .GetMethod(nameof(StaticMethodTargets.RegistrationResultA))!;
        PatcherRegistrationInlinePatches.ObservedMethod = null;

        Patcher.Patch(Patch.Prefix.With(patch).Options(PatchOptions.Inline).Of(target));

        StaticMethodTargets.RegistrationResultA();

        Assert.That(PatcherRegistrationInlinePatches.ObservedMethod, Is.Not.Null);
        Assert.That(PatcherRegistrationInlinePatches.ObservedMethod, Is.Not.EqualTo(patch));
    }

    [Test]
    public void PatchCategoryProcessesOnlyMatchingCategory()
    {
        Patcher.PatchCategory(TestAssembly, "included");

        Assert.That(StaticMethodTargets.RegistrationResultA(), Is.EqualTo(42));
        Assert.That(StaticMethodTargets.RegistrationResultB(), Is.EqualTo(2));
    }

    [Test]
    public void PatchAllProcessesAllAssemblyPatchCategories()
    {
        Patcher.PatchAll(TestAssembly);

        Assert.That(StaticMethodTargets.RegistrationResultA(), Is.EqualTo(42));
        Assert.That(StaticMethodTargets.RegistrationResultB(), Is.EqualTo(42));
    }
}
