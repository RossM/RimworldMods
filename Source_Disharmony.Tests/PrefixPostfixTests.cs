using System;
using NUnit.Framework;

namespace Disharmony.Tests;

public static class PatchMethods
{
    public static int ValueParameterObserved;
    public static string? ReferenceParameterObserved;
    public static int ValueResultObserved;
    public static string? ReferenceResultObserved;
    public static ClassMethodTargets? InstanceObserved;
    public static ClassMethodTargets? ReplacementInstance;
    public static int CombinedPatchObserved;
    public static int StateObserved;

    [Prefix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.IntResult))]
    public static bool RunValueTypeTargetPrefix() => true;

    [Prefix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.StringResult))]
    public static bool RunReferenceTypeTargetPrefix() => true;

    [Prefix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.ThrowingIntResult))]
    public static bool SkipValueTypeTargetPrefix() => false;

    [Prefix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.ThrowingStringResult))]
    public static bool SkipReferenceTypeTargetPrefix() => false;

    [Prefix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.IntArgument))]
    public static void ReadValueParameterPrefix(int value) => ValueParameterObserved = value;

    [Postfix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.IntArgument))]
    public static void ReadValueParameterPostfix(int value) => ValueParameterObserved = value;

    [Prefix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.StringArgument))]
    public static void ReadReferenceParameterPrefix(string value) => ReferenceParameterObserved = value;

    [Postfix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.StringArgument))]
    public static void ReadReferenceParameterPostfix(string value) => ReferenceParameterObserved = value;

    [Prefix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.IntIdentity))]
    public static void WriteValueParameterPrefix(ref int value) => value = 42;

    [Postfix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.RefIntArgument))]
    public static void WriteValueParameterPostfix(ref int value) => value = 42;

    [Prefix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.StringIdentity))]
    public static void WriteReferenceParameterPrefix(ref string value) => value = "patched";

    [Postfix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.RefStringArgument))]
    public static void WriteReferenceParameterPostfix(ref string value) => value = "patched";

    [Prefix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.IntResult))]
    public static void ReadValueResultPrefix(int __result) => ValueResultObserved = __result;

    [Postfix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.IntResult))]
    public static void ReadValueResultPostfix(int __result) => ValueResultObserved = __result;

    [Prefix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.StringResult))]
    public static void ReadReferenceResultPrefix(string? __result) => ReferenceResultObserved = __result;

    [Postfix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.StringResult))]
    public static void ReadReferenceResultPostfix(string __result) => ReferenceResultObserved = __result;

    [Prefix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.ThrowingIntResult))]
    public static bool WriteValueResultPrefix(ref int __result)
    {
        __result = 42;
        return false;
    }

    [Postfix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.IntResult))]
    public static void WriteValueResultPostfix(ref int __result) => __result = 42;

    [Prefix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.ThrowingStringResult))]
    public static bool WriteReferenceResultPrefix(ref string? __result)
    {
        __result = "patched";
        return false;
    }

    [Postfix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.StringResult))]
    public static void WriteReferenceResultPostfix(ref string __result) => __result = "patched";

    [Prefix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.IntResult))]
    public static bool WriteValueResultAndRunTargetPrefix(ref int __result)
    {
        __result = 42;
        return true;
    }

    [Prefix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.ThrowingIntResult))]
    public static bool WriteValueResultAndSkipTargetPrefix(ref int __result)
    {
        __result = 42;
        return false;
    }

    [Prefix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.StringResult))]
    public static bool WriteReferenceResultAndRunTargetPrefix(ref string? __result)
    {
        __result = "patched";
        return true;
    }

    [Prefix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.ThrowingStringResult))]
    public static bool WriteReferenceResultAndSkipTargetPrefix(ref string? __result)
    {
        __result = "patched";
        return false;
    }

    [Prefix]
    [Target(typeof(ClassMethodTargets), nameof(ClassMethodTargets.Void))]
    public static void CaptureInstancePrefix(ClassMethodTargets __instance) => InstanceObserved = __instance;

    [Postfix]
    [Target(typeof(ClassMethodTargets), nameof(ClassMethodTargets.Void))]
    public static void CaptureInstancePostfix(ClassMethodTargets __instance) => InstanceObserved = __instance;

    [Prefix]
    [Target(typeof(ClassMethodTargets), nameof(ClassMethodTargets.Self))]
    public static void WriteInstancePrefix(ref ClassMethodTargets __instance) => __instance = ReplacementInstance!;

    [Postfix]
    [Target(typeof(ClassMethodTargets), nameof(ClassMethodTargets.Self))]
    public static void WriteInstancePostfix(
        ref ClassMethodTargets __instance,
        ref ClassMethodTargets __result)
    {
        __instance = ReplacementInstance!;
        __result = __instance;
    }

    [Prefix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.IntArgument))]
    public static bool WriteArgumentAndRunTargetPrefix(ref int value)
    {
        value = 42;
        return true;
    }

    [Postfix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.IntArgument))]
    public static void ObserveArgumentAfterTargetRunsPostfix(int value) => CombinedPatchObserved = value;

    [Prefix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.ThrowingIntArgument))]
    public static bool WriteArgumentAndSkipTargetPrefix(ref int value)
    {
        value = 42;
        return false;
    }

    [Postfix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.ThrowingIntArgument))]
    public static void ObserveArgumentAfterTargetIsSkippedPostfix(int value) => CombinedPatchObserved = value;

    [Prefix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.IntResult))]
    public static bool WriteResultAndRunTargetPrefix(ref int __result)
    {
        __result = 42;
        return true;
    }

    [Postfix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.IntResult))]
    public static void ObserveResultAfterTargetRunsPostfix(int __result) => CombinedPatchObserved = __result;

    [Prefix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.ThrowingIntResult))]
    public static bool WriteResultAndSkipTargetPrefix(ref int __result)
    {
        __result = 42;
        return false;
    }

    [Postfix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.ThrowingIntResult))]
    public static void ObserveResultAfterTargetIsSkippedPostfix(int __result) => CombinedPatchObserved = __result;

    [Prefix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.Void))]
    public static void WriteStatePrefix(out int __state) => __state = 42;

    [Postfix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.Void))]
    public static void ReadStatePostfix(int __state) => StateObserved = __state;

    [Postfix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.Void))]
    public static void WriteStatePostfix(ref int __state) => __state = 43;

    [Postfix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.Void))]
    public static void ReadWrittenStatePostfix(int __state) => StateObserved = __state;

    [Postfix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.IntResult))]
    public static int NonVoidPostfix() => 42;

    [Prefix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.RefIntArgument))]
    public static void ReadRefParameterPrefix(int value) => ValueParameterObserved = value;

    [Prefix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.RefIntArgument))]
    public static void WriteRefParameterPrefix(ref int value) => value = 42;
}

public abstract class PatchTestBase
{
    [SetUp]
    public void UnpatchBeforeTest() =>
        Autopatcher.UnpatchAll(typeof(PatchTestBase).Assembly);

    protected static void ApplyPatch(string patchMethodName) =>
        Autopatcher.Patch(typeof(PatchMethods).GetMethod(patchMethodName));

    protected static void ApplyPatch(Type patchMethodsType, string patchMethodName) =>
        Autopatcher.Patch(patchMethodsType.GetMethod(patchMethodName));

    protected static void ApplyPatches(string firstPatchMethodName, string secondPatchMethodName)
    {
        ApplyPatch(firstPatchMethodName);
        ApplyPatch(secondPatchMethodName);
    }

    protected static void ApplyInnerPatch(string patchMethodName) =>
        Autopatcher.Patch(typeof(InnerPatchMethods).GetMethod(patchMethodName));

    protected static void ApplyInnerParameterBindingPatch(string patchMethodName) =>
        Autopatcher.Patch(typeof(InnerParameterBindingPatchMethods).GetMethod(patchMethodName));
}