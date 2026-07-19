using NUnit.Framework;

namespace Disharmony.Tests;

public static class PatchMethods
{
    public static int ValueParameterObserved;
    public static string? ReferenceParameterObserved;
    public static int ValueResultObserved;
    public static string? ReferenceResultObserved;
    public static InstancePatchTarget? InstanceObserved;
    public static int CombinedPatchObserved;
    public static int StateObserved;

    [Prefix]
    [Target(typeof(PatchTargets), nameof(PatchTargets.RunValueTypeTarget))]
    public static bool RunValueTypeTargetPrefix() => true;

    [Prefix]
    [Target(typeof(PatchTargets), nameof(PatchTargets.RunReferenceTypeTarget))]
    public static bool RunReferenceTypeTargetPrefix() => true;

    [Prefix]
    [Target(typeof(PatchTargets), nameof(PatchTargets.SkipValueTypeTarget))]
    public static bool SkipValueTypeTargetPrefix() => false;

    [Prefix]
    [Target(typeof(PatchTargets), nameof(PatchTargets.SkipReferenceTypeTarget))]
    public static bool SkipReferenceTypeTargetPrefix() => false;

    [Prefix]
    [Target(typeof(PatchTargets), nameof(PatchTargets.ReadValueParameterInPrefix))]
    public static void ReadValueParameterPrefix(int value) => ValueParameterObserved = value;

    [Postfix]
    [Target(typeof(PatchTargets), nameof(PatchTargets.ReadValueParameterInPostfix))]
    public static void ReadValueParameterPostfix(int value) => ValueParameterObserved = value;

    [Prefix]
    [Target(typeof(PatchTargets), nameof(PatchTargets.ReadReferenceParameterInPrefix))]
    public static void ReadReferenceParameterPrefix(string value) => ReferenceParameterObserved = value;

    [Postfix]
    [Target(typeof(PatchTargets), nameof(PatchTargets.ReadReferenceParameterInPostfix))]
    public static void ReadReferenceParameterPostfix(string value) => ReferenceParameterObserved = value;

    [Prefix]
    [Target(typeof(PatchTargets), nameof(PatchTargets.WriteValueParameterInPrefix))]
    public static void WriteValueParameterPrefix(ref int value) => value = 42;

    [Postfix]
    [Target(typeof(PatchTargets), nameof(PatchTargets.WriteValueParameterInPostfix))]
    public static void WriteValueParameterPostfix(ref int value) => value = 42;

    [Prefix]
    [Target(typeof(PatchTargets), nameof(PatchTargets.WriteReferenceParameterInPrefix))]
    public static void WriteReferenceParameterPrefix(ref string value) => value = "patched";

    [Postfix]
    [Target(typeof(PatchTargets), nameof(PatchTargets.WriteReferenceParameterInPostfix))]
    public static void WriteReferenceParameterPostfix(ref string value) => value = "patched";

    [Prefix]
    [Target(typeof(PatchTargets), nameof(PatchTargets.ReadValueResultInPrefix))]
    public static void ReadValueResultPrefix(int __result) => ValueResultObserved = __result;

    [Postfix]
    [Target(typeof(PatchTargets), nameof(PatchTargets.ReadValueResultInPostfix))]
    public static void ReadValueResultPostfix(int __result) => ValueResultObserved = __result;

    [Prefix]
    [Target(typeof(PatchTargets), nameof(PatchTargets.ReadReferenceResultInPrefix))]
    public static void ReadReferenceResultPrefix(string? __result) => ReferenceResultObserved = __result;

    [Postfix]
    [Target(typeof(PatchTargets), nameof(PatchTargets.ReadReferenceResultInPostfix))]
    public static void ReadReferenceResultPostfix(string __result) => ReferenceResultObserved = __result;

    [Prefix]
    [Target(typeof(PatchTargets), nameof(PatchTargets.WriteValueResultInPrefix))]
    public static bool WriteValueResultPrefix(ref int __result)
    {
        __result = 42;
        return false;
    }

    [Postfix]
    [Target(typeof(PatchTargets), nameof(PatchTargets.WriteValueResultInPostfix))]
    public static void WriteValueResultPostfix(ref int __result) => __result = 42;

    [Prefix]
    [Target(typeof(PatchTargets), nameof(PatchTargets.WriteReferenceResultInPrefix))]
    public static bool WriteReferenceResultPrefix(ref string? __result)
    {
        __result = "patched";
        return false;
    }

    [Postfix]
    [Target(typeof(PatchTargets), nameof(PatchTargets.WriteReferenceResultInPostfix))]
    public static void WriteReferenceResultPostfix(ref string __result) => __result = "patched";

    [Prefix]
    [Target(typeof(PatchTargets), nameof(PatchTargets.WriteValueResultAndRunTarget))]
    public static bool WriteValueResultAndRunTargetPrefix(ref int __result)
    {
        __result = 42;
        return true;
    }

    [Prefix]
    [Target(typeof(PatchTargets), nameof(PatchTargets.WriteValueResultAndSkipTarget))]
    public static bool WriteValueResultAndSkipTargetPrefix(ref int __result)
    {
        __result = 42;
        return false;
    }

    [Prefix]
    [Target(typeof(PatchTargets), nameof(PatchTargets.WriteReferenceResultAndRunTarget))]
    public static bool WriteReferenceResultAndRunTargetPrefix(ref string? __result)
    {
        __result = "patched";
        return true;
    }

    [Prefix]
    [Target(typeof(PatchTargets), nameof(PatchTargets.WriteReferenceResultAndSkipTarget))]
    public static bool WriteReferenceResultAndSkipTargetPrefix(ref string? __result)
    {
        __result = "patched";
        return false;
    }

    [Prefix]
    [Target(typeof(InstancePatchTarget), nameof(InstancePatchTarget.PrefixTarget))]
    public static void CaptureInstancePrefix(InstancePatchTarget __instance) => InstanceObserved = __instance;

    [Postfix]
    [Target(typeof(InstancePatchTarget), nameof(InstancePatchTarget.PostfixTarget))]
    public static void CaptureInstancePostfix(InstancePatchTarget __instance) => InstanceObserved = __instance;

    [Prefix]
    [Target(typeof(PatchTargets), nameof(PatchTargets.WriteArgumentAndRunTarget))]
    public static bool WriteArgumentAndRunTargetPrefix(ref int value)
    {
        value = 42;
        return true;
    }

    [Postfix]
    [Target(typeof(PatchTargets), nameof(PatchTargets.WriteArgumentAndRunTarget))]
    public static void ObserveArgumentAfterTargetRunsPostfix(int value) => CombinedPatchObserved = value;

    [Prefix]
    [Target(typeof(PatchTargets), nameof(PatchTargets.WriteArgumentAndSkipTarget))]
    public static bool WriteArgumentAndSkipTargetPrefix(ref int value)
    {
        value = 42;
        return false;
    }

    [Postfix]
    [Target(typeof(PatchTargets), nameof(PatchTargets.WriteArgumentAndSkipTarget))]
    public static void ObserveArgumentAfterTargetIsSkippedPostfix(int value) => CombinedPatchObserved = value;

    [Prefix]
    [Target(typeof(PatchTargets), nameof(PatchTargets.WriteResultAndRunTarget))]
    public static bool WriteResultAndRunTargetPrefix(ref int __result)
    {
        __result = 42;
        return true;
    }

    [Postfix]
    [Target(typeof(PatchTargets), nameof(PatchTargets.WriteResultAndRunTarget))]
    public static void ObserveResultAfterTargetRunsPostfix(int __result) => CombinedPatchObserved = __result;

    [Prefix]
    [Target(typeof(PatchTargets), nameof(PatchTargets.WriteResultAndSkipTarget))]
    public static bool WriteResultAndSkipTargetPrefix(ref int __result)
    {
        __result = 42;
        return false;
    }

    [Postfix]
    [Target(typeof(PatchTargets), nameof(PatchTargets.WriteResultAndSkipTarget))]
    public static void ObserveResultAfterTargetIsSkippedPostfix(int __result) => CombinedPatchObserved = __result;

    [Prefix]
    [Target(typeof(PatchTargets), nameof(PatchTargets.StateTarget))]
    public static void WriteStatePrefix(out int __state) => __state = 42;

    [Postfix]
    [Target(typeof(PatchTargets), nameof(PatchTargets.StateTarget))]
    public static void ReadStatePostfix(int __state) => StateObserved = __state;

    [Postfix]
    [Target(typeof(PatchTargets), nameof(PatchTargets.NonVoidPostfixTarget))]
    public static int NonVoidPostfix() => 42;

    [Prefix]
    [Target(typeof(PatchTargets), nameof(PatchTargets.ReadRefParameterTarget))]
    public static void ReadRefParameterPrefix(int value) => ValueParameterObserved = value;

    [Prefix]
    [Target(typeof(PatchTargets), nameof(PatchTargets.WriteRefParameterTarget))]
    public static void WriteRefParameterPrefix(ref int value) => value = 42;
}

public static class PatchTargets
{
    public static int RunValueTypeTarget() => 42;
    public static string RunReferenceTypeTarget() => "ran";

    public static int SkipValueTypeTarget()
    {
        Assert.Fail("The target should have been skipped.");
        return 1;
    }

    public static string SkipReferenceTypeTarget()
    {
        Assert.Fail("The target should have been skipped.");
        return "original";
    }

    public static void ReadValueParameterInPrefix(int value) { }
    public static void ReadValueParameterInPostfix(int value) { }
    public static void ReadReferenceParameterInPrefix(string value) { }
    public static void ReadReferenceParameterInPostfix(string value) { }

    public static int WriteValueParameterInPrefix(int value) => value;
    public static void WriteValueParameterInPostfix(ref int value) { }
    public static string WriteReferenceParameterInPrefix(string value) => value;
    public static void WriteReferenceParameterInPostfix(ref string value) { }

    public static int ReadValueResultInPrefix() => 42;
    public static int ReadValueResultInPostfix() => 42;
    public static string ReadReferenceResultInPrefix() => "original";
    public static string ReadReferenceResultInPostfix() => "original";

    public static int WriteValueResultInPrefix()
    {
        Assert.Fail("The target should have been skipped.");
        return 1;
    }

    public static int WriteValueResultInPostfix() => 1;

    public static string WriteReferenceResultInPrefix()
    {
        Assert.Fail("The target should have been skipped.");
        return "original";
    }

    public static string WriteReferenceResultInPostfix() => "original";

    public static int WriteValueResultAndRunTarget() => 1;

    public static int WriteValueResultAndSkipTarget()
    {
        Assert.Fail("The target should have been skipped.");
        return 1;
    }

    public static string WriteReferenceResultAndRunTarget() => "original";

    public static string WriteReferenceResultAndSkipTarget()
    {
        Assert.Fail("The target should have been skipped.");
        return "original";
    }

    public static void WriteArgumentAndRunTarget(int value) { }

    public static void WriteArgumentAndSkipTarget(int value) =>
        Assert.Fail("The target should have been skipped.");

    public static int WriteResultAndRunTarget() => 1;

    public static int WriteResultAndSkipTarget()
    {
        Assert.Fail("The target should have been skipped.");
        return 1;
    }

    public static void StateTarget() { }
    public static int NonVoidPostfixTarget() => 1;
    public static void ReadRefParameterTarget(ref int value) { }
    public static void WriteRefParameterTarget(ref int value) { }
}

public sealed class InstancePatchTarget
{
    public void PrefixTarget() { }
    public void PostfixTarget() { }
}

public abstract class PatchTestBase
{
    protected static void ApplyPatch(string patchMethodName) =>
        Autopatcher.Patch(typeof(PatchMethods).GetMethod(patchMethodName));

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
        Assert.That(PatchTargets.RunValueTypeTarget(), Is.EqualTo(42));
    }

    [Test]
    public void PrefixReturningTrueRunsReferenceTypeTarget()
    {
        ApplyPatch(nameof(PatchMethods.RunReferenceTypeTargetPrefix));
        Assert.That(PatchTargets.RunReferenceTypeTarget(), Is.EqualTo("ran"));
    }

    [Test]
    public void PrefixReturningFalseSkipsValueTypeTarget()
    {
        ApplyPatch(nameof(PatchMethods.SkipValueTypeTargetPrefix));
        Assert.That(PatchTargets.SkipValueTypeTarget(), Is.Zero);
    }

    [Test]
    public void PrefixReturningFalseSkipsReferenceTypeTarget()
    {
        ApplyPatch(nameof(PatchMethods.SkipReferenceTypeTargetPrefix));
        Assert.That(PatchTargets.SkipReferenceTypeTarget(), Is.Null);
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
        PatchTargets.ReadValueParameterInPrefix(42);

        Assert.That(PatchMethods.ValueParameterObserved, Is.EqualTo(42));
    }

    [Test]
    public void PostfixCanReadValueTypeParameter()
    {
        PatchMethods.ValueParameterObserved = 0;
        ApplyPatch(nameof(PatchMethods.ReadValueParameterPostfix));
        PatchTargets.ReadValueParameterInPostfix(42);

        Assert.That(PatchMethods.ValueParameterObserved, Is.EqualTo(42));
    }

    [Test]
    public void PrefixCanReadReferenceTypeParameter()
    {
        PatchMethods.ReferenceParameterObserved = null;
        ApplyPatch(nameof(PatchMethods.ReadReferenceParameterPrefix));
        PatchTargets.ReadReferenceParameterInPrefix("original");

        Assert.That(PatchMethods.ReferenceParameterObserved, Is.EqualTo("original"));
    }

    [Test]
    public void PostfixCanReadReferenceTypeParameter()
    {
        PatchMethods.ReferenceParameterObserved = null;
        ApplyPatch(nameof(PatchMethods.ReadReferenceParameterPostfix));
        PatchTargets.ReadReferenceParameterInPostfix("original");

        Assert.That(PatchMethods.ReferenceParameterObserved, Is.EqualTo("original"));
    }

    [Test]
    public void PrefixCanWriteValueTypeParameterByReference()
    {
        ApplyPatch(nameof(PatchMethods.WriteValueParameterPrefix));
        Assert.That(PatchTargets.WriteValueParameterInPrefix(1), Is.EqualTo(42));
    }

    [Test]
    public void PostfixCanWriteValueTypeParameterByReference()
    {
        ApplyPatch(nameof(PatchMethods.WriteValueParameterPostfix));
        int value = 1;
        PatchTargets.WriteValueParameterInPostfix(ref value);
        Assert.That(value, Is.EqualTo(42));
    }

    [Test]
    public void PrefixCanWriteReferenceTypeParameterByReference()
    {
        ApplyPatch(nameof(PatchMethods.WriteReferenceParameterPrefix));
        Assert.That(PatchTargets.WriteReferenceParameterInPrefix("original"), Is.EqualTo("patched"));
    }

    [Test]
    public void PostfixCanWriteReferenceTypeParameterByReference()
    {
        ApplyPatch(nameof(PatchMethods.WriteReferenceParameterPostfix));
        string value = "original";
        PatchTargets.WriteReferenceParameterInPostfix(ref value);
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
        PatchTargets.ReadValueResultInPrefix();
        Assert.That(PatchMethods.ValueResultObserved, Is.Zero);
    }

    [Test]
    public void PostfixReadsValueTypeResult()
    {
        PatchMethods.ValueResultObserved = 0;
        ApplyPatch(nameof(PatchMethods.ReadValueResultPostfix));
        PatchTargets.ReadValueResultInPostfix();
        Assert.That(PatchMethods.ValueResultObserved, Is.EqualTo(42));
    }

    [Test]
    public void PrefixReadsDefaultReferenceTypeResult()
    {
        PatchMethods.ReferenceResultObserved = "sentinel";
        ApplyPatch(nameof(PatchMethods.ReadReferenceResultPrefix));
        PatchTargets.ReadReferenceResultInPrefix();
        Assert.That(PatchMethods.ReferenceResultObserved, Is.Null);
    }

    [Test]
    public void PostfixReadsReferenceTypeResult()
    {
        PatchMethods.ReferenceResultObserved = null;
        ApplyPatch(nameof(PatchMethods.ReadReferenceResultPostfix));
        PatchTargets.ReadReferenceResultInPostfix();
        Assert.That(PatchMethods.ReferenceResultObserved, Is.EqualTo("original"));
    }

    [Test]
    public void PrefixCanWriteValueTypeResultByReference()
    {
        ApplyPatch(nameof(PatchMethods.WriteValueResultPrefix));
        Assert.That(PatchTargets.WriteValueResultInPrefix(), Is.EqualTo(42));
    }

    [Test]
    public void PostfixCanWriteValueTypeResultByReference()
    {
        ApplyPatch(nameof(PatchMethods.WriteValueResultPostfix));
        Assert.That(PatchTargets.WriteValueResultInPostfix(), Is.EqualTo(42));
    }

    [Test]
    public void PrefixCanWriteReferenceTypeResultByReference()
    {
        ApplyPatch(nameof(PatchMethods.WriteReferenceResultPrefix));
        Assert.That(PatchTargets.WriteReferenceResultInPrefix(), Is.EqualTo("patched"));
    }

    [Test]
    public void PostfixCanWriteReferenceTypeResultByReference()
    {
        ApplyPatch(nameof(PatchMethods.WriteReferenceResultPostfix));
        Assert.That(PatchTargets.WriteReferenceResultInPostfix(), Is.EqualTo("patched"));
    }
}

[TestFixture]
public sealed partial class ExecutionControlTests
{
    [Test]
    public void PrefixResultIsReplacedByValueTypeTargetWhenReturningTrue()
    {
        ApplyPatch(nameof(PatchMethods.WriteValueResultAndRunTargetPrefix));
        Assert.That(PatchTargets.WriteValueResultAndRunTarget(), Is.EqualTo(1));
    }

    [Test]
    public void PrefixResultIsRetainedForValueTypeTargetWhenReturningFalse()
    {
        ApplyPatch(nameof(PatchMethods.WriteValueResultAndSkipTargetPrefix));
        Assert.That(PatchTargets.WriteValueResultAndSkipTarget(), Is.EqualTo(42));
    }

    [Test]
    public void PrefixResultIsReplacedByReferenceTypeTargetWhenReturningTrue()
    {
        ApplyPatch(nameof(PatchMethods.WriteReferenceResultAndRunTargetPrefix));
        Assert.That(PatchTargets.WriteReferenceResultAndRunTarget(), Is.EqualTo("original"));
    }

    [Test]
    public void PrefixResultIsRetainedForReferenceTypeTargetWhenReturningFalse()
    {
        ApplyPatch(nameof(PatchMethods.WriteReferenceResultAndSkipTargetPrefix));
        Assert.That(PatchTargets.WriteReferenceResultAndSkipTarget(), Is.EqualTo("patched"));
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
        var instance = new InstancePatchTarget();

        instance.PrefixTarget();

        Assert.That(PatchMethods.InstanceObserved, Is.SameAs(instance));
    }

    [Test]
    public void PostfixCanCapturePatchedMethodInstance()
    {
        PatchMethods.InstanceObserved = null;
        ApplyPatch(nameof(PatchMethods.CaptureInstancePostfix));
        var instance = new InstancePatchTarget();

        instance.PostfixTarget();

        Assert.That(PatchMethods.InstanceObserved, Is.SameAs(instance));
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

        PatchTargets.WriteArgumentAndRunTarget(1);

        Assert.That(PatchMethods.CombinedPatchObserved, Is.EqualTo(42));
    }

    [Test]
    public void PostfixObservesArgumentWrittenByPrefixWhenTargetIsSkipped()
    {
        PatchMethods.CombinedPatchObserved = 0;
        ApplyPatches(
            nameof(PatchMethods.WriteArgumentAndSkipTargetPrefix),
            nameof(PatchMethods.ObserveArgumentAfterTargetIsSkippedPostfix));

        PatchTargets.WriteArgumentAndSkipTarget(1);

        Assert.That(PatchMethods.CombinedPatchObserved, Is.EqualTo(42));
    }

    [Test]
    public void PostfixObservesTargetResultWhenPrefixWritesResultAndTargetRuns()
    {
        PatchMethods.CombinedPatchObserved = 0;
        ApplyPatches(
            nameof(PatchMethods.WriteResultAndRunTargetPrefix),
            nameof(PatchMethods.ObserveResultAfterTargetRunsPostfix));

        PatchTargets.WriteResultAndRunTarget();

        Assert.That(PatchMethods.CombinedPatchObserved, Is.EqualTo(1));
    }

    [Test]
    public void PostfixObservesPrefixResultWhenPrefixWritesResultAndTargetIsSkipped()
    {
        PatchMethods.CombinedPatchObserved = 0;
        ApplyPatches(
            nameof(PatchMethods.WriteResultAndSkipTargetPrefix),
            nameof(PatchMethods.ObserveResultAfterTargetIsSkippedPostfix));

        PatchTargets.WriteResultAndSkipTarget();

        Assert.That(PatchMethods.CombinedPatchObserved, Is.EqualTo(42));
    }
}

[TestFixture]
public sealed class StateBindingTests : PatchTestBase
{
    [Test]
    public void PostfixCanReadStateWrittenByPrefix()
    {
        PatchMethods.StateObserved = 0;
        ApplyPatches(
            nameof(PatchMethods.WriteStatePrefix),
            nameof(PatchMethods.ReadStatePostfix));

        PatchTargets.StateTarget();

        Assert.That(PatchMethods.StateObserved, Is.EqualTo(42));
    }
}

[TestFixture]
public sealed partial class PostfixReturnValueTests : PatchTestBase
{
    [Test]
    public void PostfixReturnValueIsDiscarded()
    {
        ApplyPatch(nameof(PatchMethods.NonVoidPostfix));
        Assert.That(PatchTargets.NonVoidPostfixTarget(), Is.EqualTo(1));
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

        PatchTargets.ReadRefParameterTarget(ref value);

        Assert.That(PatchMethods.ValueParameterObserved, Is.EqualTo(42));
    }

    [Test]
    public void PatchCanWriteRefParameterWhenDeclaringRef()
    {
        ApplyPatch(nameof(PatchMethods.WriteRefParameterPrefix));
        int value = 1;

        PatchTargets.WriteRefParameterTarget(ref value);

        Assert.That(value, Is.EqualTo(42));
    }
}
