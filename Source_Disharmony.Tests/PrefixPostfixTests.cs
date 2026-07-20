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

[TestFixture]
public sealed partial class ExecutionControlTests : PatchTestBase
{
    [Test]
    public void PrefixReturningTrueRunsValueTypeTarget()
    {
        ApplyPatch(nameof(PatchMethods.RunValueTypeTargetPrefix));
        Assert.That(StaticMethodTargets.IntResult(), Is.EqualTo(1));
    }

    [Test]
    public void PrefixReturningTrueRunsReferenceTypeTarget()
    {
        ApplyPatch(nameof(PatchMethods.RunReferenceTypeTargetPrefix));
        Assert.That(StaticMethodTargets.StringResult(), Is.EqualTo("original"));
    }

    [Test]
    public void PrefixReturningFalseSkipsValueTypeTarget()
    {
        ApplyPatch(nameof(PatchMethods.SkipValueTypeTargetPrefix));
        Assert.That(StaticMethodTargets.ThrowingIntResult(), Is.Zero);
    }

    [Test]
    public void PrefixReturningFalseSkipsReferenceTypeTarget()
    {
        ApplyPatch(nameof(PatchMethods.SkipReferenceTypeTargetPrefix));
        Assert.That(StaticMethodTargets.ThrowingStringResult(), Is.Null);
    }
}

[TestFixture]
public sealed partial class ArgumentBindingTests : PatchTestBase
{
    [Test]
    public void PrefixCanReadValueTypeParameter()
    {
        PatchMethods.ValueParameterObserved = 0;
        ApplyPatch(nameof(PatchMethods.ReadValueParameterPrefix));
        StaticMethodTargets.IntArgument(42);

        Assert.That(PatchMethods.ValueParameterObserved, Is.EqualTo(42));
    }

    [Test]
    public void PostfixCanReadValueTypeParameter()
    {
        PatchMethods.ValueParameterObserved = 0;
        ApplyPatch(nameof(PatchMethods.ReadValueParameterPostfix));
        StaticMethodTargets.IntArgument(42);

        Assert.That(PatchMethods.ValueParameterObserved, Is.EqualTo(42));
    }

    [Test]
    public void PrefixCanReadReferenceTypeParameter()
    {
        PatchMethods.ReferenceParameterObserved = null;
        ApplyPatch(nameof(PatchMethods.ReadReferenceParameterPrefix));
        StaticMethodTargets.StringArgument("original");

        Assert.That(PatchMethods.ReferenceParameterObserved, Is.EqualTo("original"));
    }

    [Test]
    public void PostfixCanReadReferenceTypeParameter()
    {
        PatchMethods.ReferenceParameterObserved = null;
        ApplyPatch(nameof(PatchMethods.ReadReferenceParameterPostfix));
        StaticMethodTargets.StringArgument("original");

        Assert.That(PatchMethods.ReferenceParameterObserved, Is.EqualTo("original"));
    }

    [Test]
    public void PrefixCanWriteValueTypeParameterByReference()
    {
        ApplyPatch(nameof(PatchMethods.WriteValueParameterPrefix));
        Assert.That(StaticMethodTargets.IntIdentity(1), Is.EqualTo(42));
    }

    [Test]
    public void PostfixCanWriteValueTypeParameterByReference()
    {
        ApplyPatch(nameof(PatchMethods.WriteValueParameterPostfix));
        int value = 1;
        StaticMethodTargets.RefIntArgument(ref value);
        Assert.That(value, Is.EqualTo(42));
    }

    [Test]
    public void PrefixCanWriteReferenceTypeParameterByReference()
    {
        ApplyPatch(nameof(PatchMethods.WriteReferenceParameterPrefix));
        Assert.That(StaticMethodTargets.StringIdentity("original"), Is.EqualTo("patched"));
    }

    [Test]
    public void PostfixCanWriteReferenceTypeParameterByReference()
    {
        ApplyPatch(nameof(PatchMethods.WriteReferenceParameterPostfix));
        string value = "original";
        StaticMethodTargets.RefStringArgument(ref value);
        Assert.That(value, Is.EqualTo("patched"));
    }
}

[TestFixture]
public sealed partial class ResultBindingTests : PatchTestBase
{
    [Test]
    public void PrefixReadsDefaultValueTypeResult()
    {
        PatchMethods.ValueResultObserved = -1;
        ApplyPatch(nameof(PatchMethods.ReadValueResultPrefix));
        StaticMethodTargets.IntResult();
        Assert.That(PatchMethods.ValueResultObserved, Is.Zero);
    }

    [Test]
    public void PostfixReadsValueTypeResult()
    {
        PatchMethods.ValueResultObserved = 0;
        ApplyPatch(nameof(PatchMethods.ReadValueResultPostfix));
        StaticMethodTargets.IntResult();
        Assert.That(PatchMethods.ValueResultObserved, Is.EqualTo(1));
    }

    [Test]
    public void PrefixReadsDefaultReferenceTypeResult()
    {
        PatchMethods.ReferenceResultObserved = "sentinel";
        ApplyPatch(nameof(PatchMethods.ReadReferenceResultPrefix));
        StaticMethodTargets.StringResult();
        Assert.That(PatchMethods.ReferenceResultObserved, Is.Null);
    }

    [Test]
    public void PostfixReadsReferenceTypeResult()
    {
        PatchMethods.ReferenceResultObserved = null;
        ApplyPatch(nameof(PatchMethods.ReadReferenceResultPostfix));
        StaticMethodTargets.StringResult();
        Assert.That(PatchMethods.ReferenceResultObserved, Is.EqualTo("original"));
    }

    [Test]
    public void PrefixCanWriteValueTypeResultByReference()
    {
        ApplyPatch(nameof(PatchMethods.WriteValueResultPrefix));
        Assert.That(StaticMethodTargets.ThrowingIntResult(), Is.EqualTo(42));
    }

    [Test]
    public void PostfixCanWriteValueTypeResultByReference()
    {
        ApplyPatch(nameof(PatchMethods.WriteValueResultPostfix));
        Assert.That(StaticMethodTargets.IntResult(), Is.EqualTo(42));
    }

    [Test]
    public void PrefixCanWriteReferenceTypeResultByReference()
    {
        ApplyPatch(nameof(PatchMethods.WriteReferenceResultPrefix));
        Assert.That(StaticMethodTargets.ThrowingStringResult(), Is.EqualTo("patched"));
    }

    [Test]
    public void PostfixCanWriteReferenceTypeResultByReference()
    {
        ApplyPatch(nameof(PatchMethods.WriteReferenceResultPostfix));
        Assert.That(StaticMethodTargets.StringResult(), Is.EqualTo("patched"));
    }
}

[TestFixture]
public sealed partial class ExecutionControlTests
{
    [Test]
    public void PrefixResultIsReplacedByValueTypeTargetWhenReturningTrue()
    {
        ApplyPatch(nameof(PatchMethods.WriteValueResultAndRunTargetPrefix));
        Assert.That(StaticMethodTargets.IntResult(), Is.EqualTo(1));
    }

    [Test]
    public void PrefixResultIsRetainedForValueTypeTargetWhenReturningFalse()
    {
        ApplyPatch(nameof(PatchMethods.WriteValueResultAndSkipTargetPrefix));
        Assert.That(StaticMethodTargets.ThrowingIntResult(), Is.EqualTo(42));
    }

    [Test]
    public void PrefixResultIsReplacedByReferenceTypeTargetWhenReturningTrue()
    {
        ApplyPatch(nameof(PatchMethods.WriteReferenceResultAndRunTargetPrefix));
        Assert.That(StaticMethodTargets.StringResult(), Is.EqualTo("original"));
    }

    [Test]
    public void PrefixResultIsRetainedForReferenceTypeTargetWhenReturningFalse()
    {
        ApplyPatch(nameof(PatchMethods.WriteReferenceResultAndSkipTargetPrefix));
        Assert.That(StaticMethodTargets.ThrowingStringResult(), Is.EqualTo("patched"));
    }
}

[TestFixture]
public sealed partial class InstanceBindingTests : PatchTestBase
{
    [Test]
    public void PrefixCanCapturePatchedMethodInstance()
    {
        PatchMethods.InstanceObserved = null;
        ApplyPatch(nameof(PatchMethods.CaptureInstancePrefix));
        var instance = new ClassMethodTargets();

        instance.Void();

        Assert.That(PatchMethods.InstanceObserved, Is.SameAs(instance));
    }

    [Test]
    public void PostfixCanCapturePatchedMethodInstance()
    {
        PatchMethods.InstanceObserved = null;
        ApplyPatch(nameof(PatchMethods.CaptureInstancePostfix));
        var instance = new ClassMethodTargets();

        instance.Void();

        Assert.That(PatchMethods.InstanceObserved, Is.SameAs(instance));
    }

    [Test]
    public void PrefixCanWritePatchedMethodInstanceByReference()
    {
        var original = new ClassMethodTargets();
        var replacement = new ClassMethodTargets();
        PatchMethods.ReplacementInstance = replacement;
        ApplyPatch(nameof(PatchMethods.WriteInstancePrefix));

        ClassMethodTargets result = original.Self();

        Assert.That(result, Is.SameAs(replacement));
    }

    [Test]
    public void PostfixCanWritePatchedMethodInstanceByReference()
    {
        var original = new ClassMethodTargets();
        var replacement = new ClassMethodTargets();
        PatchMethods.ReplacementInstance = replacement;
        ApplyPatch(nameof(PatchMethods.WriteInstancePostfix));

        ClassMethodTargets result = original.Self();

        Assert.That(result, Is.SameAs(replacement));
    }
}

[TestFixture]
public sealed class PatchInteractionTests : PatchTestBase
{
    [Test]
    public void PostfixObservesArgumentWrittenByPrefixWhenTargetRuns()
    {
        PatchMethods.CombinedPatchObserved = 0;
        ApplyPatches(
            nameof(PatchMethods.WriteArgumentAndRunTargetPrefix),
            nameof(PatchMethods.ObserveArgumentAfterTargetRunsPostfix));

        StaticMethodTargets.IntArgument(1);

        Assert.That(PatchMethods.CombinedPatchObserved, Is.EqualTo(42));
    }

    [Test]
    public void PostfixObservesArgumentWrittenByPrefixWhenTargetIsSkipped()
    {
        PatchMethods.CombinedPatchObserved = 0;
        ApplyPatches(
            nameof(PatchMethods.WriteArgumentAndSkipTargetPrefix),
            nameof(PatchMethods.ObserveArgumentAfterTargetIsSkippedPostfix));

        StaticMethodTargets.ThrowingIntArgument(1);

        Assert.That(PatchMethods.CombinedPatchObserved, Is.EqualTo(42));
    }

    [Test]
    public void PostfixObservesTargetResultWhenPrefixWritesResultAndTargetRuns()
    {
        PatchMethods.CombinedPatchObserved = 0;
        ApplyPatches(
            nameof(PatchMethods.WriteResultAndRunTargetPrefix),
            nameof(PatchMethods.ObserveResultAfterTargetRunsPostfix));

        StaticMethodTargets.IntResult();

        Assert.That(PatchMethods.CombinedPatchObserved, Is.EqualTo(1));
    }

    [Test]
    public void PostfixObservesPrefixResultWhenPrefixWritesResultAndTargetIsSkipped()
    {
        PatchMethods.CombinedPatchObserved = 0;
        ApplyPatches(
            nameof(PatchMethods.WriteResultAndSkipTargetPrefix),
            nameof(PatchMethods.ObserveResultAfterTargetIsSkippedPostfix));

        StaticMethodTargets.ThrowingIntResult();

        Assert.That(PatchMethods.CombinedPatchObserved, Is.EqualTo(42));
    }
}

[TestFixture]
public sealed partial class StateBindingTests : PatchTestBase
{
    [Test]
    public void PostfixCanReadStateWrittenByPrefix()
    {
        PatchMethods.StateObserved = 0;
        ApplyPatches(
            nameof(PatchMethods.WriteStatePrefix),
            nameof(PatchMethods.ReadStatePostfix));

        StaticMethodTargets.Void();

        Assert.That(PatchMethods.StateObserved, Is.EqualTo(42));
    }

    [Test]
    public void PostfixCanWriteStateByReferenceForLaterPostfix()
    {
        PatchMethods.StateObserved = 0;
        ApplyPatch(nameof(PatchMethods.WriteStatePrefix));
        ApplyPatch(nameof(PatchMethods.WriteStatePostfix));
        ApplyPatch(nameof(PatchMethods.ReadWrittenStatePostfix));

        StaticMethodTargets.Void();

        Assert.That(PatchMethods.StateObserved, Is.EqualTo(43));
    }
}

[TestFixture]
public sealed partial class PostfixReturnValueTests : PatchTestBase
{
    [Test]
    public void PostfixReturnValueIsDiscarded()
    {
        ApplyPatch(nameof(PatchMethods.NonVoidPostfix));
        Assert.That(StaticMethodTargets.IntResult(), Is.EqualTo(1));
    }
}

[TestFixture]
public sealed partial class ArgumentBindingTests
{
    [Test]
    public void PatchCanReadRefParameterWithoutDeclaringRef()
    {
        PatchMethods.ValueParameterObserved = 0;
        ApplyPatch(nameof(PatchMethods.ReadRefParameterPrefix));
        int value = 42;

        StaticMethodTargets.RefIntArgument(ref value);

        Assert.That(PatchMethods.ValueParameterObserved, Is.EqualTo(42));
    }

    [Test]
    public void PatchCanWriteRefParameterWhenDeclaringRef()
    {
        ApplyPatch(nameof(PatchMethods.WriteRefParameterPrefix));
        int value = 1;

        StaticMethodTargets.RefIntArgument(ref value);

        Assert.That(value, Is.EqualTo(42));
    }
}
