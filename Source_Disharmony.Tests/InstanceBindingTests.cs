namespace Disharmony.Tests;

public static class InstanceBindingPatches
{
    public static ClassMethodTargets? InstanceObserved;
    public static ClassMethodTargets? ReplacementInstance;
    public static ClassMethodTargets? CallerObserved;
    public static ClassMethodTargets? ReplacementCaller;
    public static int StructInstanceFieldObserved;

    [Prefix]
    [Target(typeof(ClassMethodTargets), nameof(ClassMethodTargets.Void))]
    public static void PrefixCanCapturePatchedMethodInstance(ClassMethodTargets __instance) => InstanceObserved = __instance;

    [Postfix]
    [Target(typeof(ClassMethodTargets), nameof(ClassMethodTargets.Void))]
    public static void PostfixCanCapturePatchedMethodInstance(ClassMethodTargets __instance) => InstanceObserved = __instance;

    [Prefix]
    [Target(typeof(ClassMethodTargets), nameof(ClassMethodTargets.Self))]
    public static void PrefixCanWritePatchedMethodInstanceByReference(ref ClassMethodTargets __instance) =>
        __instance = ReplacementInstance!;

    [Prefix]
    [Target(typeof(StructMethodTargets), nameof(StructMethodTargets.IntResult))]
    public static void PrefixCanCapturePatchedStructInstance(StructMethodTargets __instance) =>
        StructInstanceFieldObserved = __instance.foo;

    [Prefix]
    [Target(typeof(StructMethodTargets), nameof(StructMethodTargets.IntResult))]
    public static void PrefixCanWritePatchedStructInstanceByReference(ref StructMethodTargets __instance) =>
        __instance.foo = 42;

    [Postfix]
    [Target(typeof(ClassMethodTargets), nameof(ClassMethodTargets.Self))]
    public static void PostfixCanWritePatchedMethodInstanceByReference(
        ref ClassMethodTargets __instance,
        ref ClassMethodTargets __result)
    {
        __instance = ReplacementInstance!;
        __result = __instance;
    }

    [InnerPrefix(typeof(InnerStaticMethodTargets), nameof(InnerStaticMethodTargets.Void))]
    [Target(typeof(ClassMethodTargets), nameof(ClassMethodTargets.CallStaticVoid))]
    public static void InnerPrefixCanCaptureOuterMethodInstanceAsCaller(ClassMethodTargets __caller) => CallerObserved = __caller;

    [InnerPostfix(typeof(InnerStaticMethodTargets), nameof(InnerStaticMethodTargets.Void))]
    [Target(typeof(ClassMethodTargets), nameof(ClassMethodTargets.CallStaticVoid))]
    public static void InnerPostfixCanCaptureOuterMethodInstanceAsCaller(ClassMethodTargets __caller) => CallerObserved = __caller;

    [InnerPrefix(typeof(InnerStaticMethodTargets), nameof(InnerStaticMethodTargets.Void))]
    [Target(typeof(ClassMethodTargets), nameof(ClassMethodTargets.CallStaticVoidAndReturnValue))]
    public static void InnerPrefixCanWriteOuterMethodInstanceByReference(ref ClassMethodTargets __caller) =>
        __caller = ReplacementCaller!;

    [InnerPrefix(typeof(InstanceMethodTargetsWithoutFields), nameof(InstanceMethodTargetsWithoutFields.Void))]
    [Target(typeof(StructMethodTargets), nameof(StructMethodTargets.CallInnerWithoutField))]
    public static void InnerPrefixCanCaptureOuterStructMethodInstanceAsCaller(StructMethodTargets __caller) =>
        StructInstanceFieldObserved = __caller.foo;

    [InnerPrefix(typeof(InstanceMethodTargetsWithoutFields), nameof(InstanceMethodTargetsWithoutFields.Void))]
    [Target(typeof(StructMethodTargets), nameof(StructMethodTargets.CallInnerWithoutField))]
    public static void InnerPrefixCanWriteOuterStructMethodInstanceByReference(ref StructMethodTargets __caller) =>
        __caller.foo = 42;

    [InnerPostfix(typeof(InnerStaticMethodTargets), nameof(InnerStaticMethodTargets.Void))]
    [Target(typeof(ClassMethodTargets), nameof(ClassMethodTargets.CallStaticVoidAndReturnValue))]
    public static void InnerPostfixCanWriteOuterMethodInstanceByReference(ref ClassMethodTargets __caller) =>
        __caller = ReplacementCaller!;

    [Prefix]
    [Target(typeof(ClassMethodTargets), nameof(ClassMethodTargets.Void))]
    public static void InstanceAttributeBindsPatchedMethodInstance([Instance] ClassMethodTargets target) =>
        InstanceObserved = target;

    [Prefix]
    [Target(typeof(ClassMethodTargets), nameof(ClassMethodTargets.Self))]
    public static void InstanceAttributeCanWritePatchedReferenceTypeInstanceByReference(
        [Instance] ref ClassMethodTargets target) => target = ReplacementInstance!;

    [InnerPrefix(typeof(InnerInstanceMethodTargets), nameof(InnerInstanceMethodTargets.Void))]
    [Target(typeof(ClassMethodTargets), nameof(ClassMethodTargets.CallInnerWithField))]
    public static void InstanceAttributeCanSelectOuterInstance([Instance(Scope.Outer)] ClassMethodTargets target) =>
        InstanceObserved = target;

    [InnerPrefix(typeof(InstanceMethodTargetsWithoutFields), nameof(InstanceMethodTargetsWithoutFields.Void))]
    [Target(typeof(StructMethodTargets), nameof(StructMethodTargets.CallInnerWithoutField))]
    public static void InstanceAttributeCanReadOuterStructInstanceByValue(
        [Instance(Scope.Outer)] StructMethodTargets target) => StructInstanceFieldObserved = target.foo;

    [InnerPrefix(typeof(InstanceMethodTargetsWithoutFields), nameof(InstanceMethodTargetsWithoutFields.Void))]
    [Target(typeof(StructMethodTargets), nameof(StructMethodTargets.CallInnerWithoutField))]
    public static void InstanceAttributeCanWriteOuterStructInstanceByReference(
        [Instance(Scope.Outer)] ref StructMethodTargets target) => target.foo = 42;

    [Prefix]
    [Target(typeof(StructMethodTargets), nameof(StructMethodTargets.IntResult))]
    public static void InstanceAttributeCanWriteStructInstanceByReference([Instance] ref StructMethodTargets target) =>
        target.foo = 42;

    [Postfix]
    [Target(typeof(StructMethodTargets), nameof(StructMethodTargets.IntResult))]
    public static void InstanceAttributeCanReadStructInstanceByValue([Instance] StructMethodTargets target) =>
        StructInstanceFieldObserved = target.foo;
}

[TestFixture]
public sealed partial class InstanceBindingTests : PatchTestBase
{
    [Test]
    public void PrefixCanCapturePatchedMethodInstance()
    {
        InstanceBindingPatches.InstanceObserved = null;
        ApplyPatch(typeof(InstanceBindingPatches), nameof(InstanceBindingPatches.PrefixCanCapturePatchedMethodInstance));
        var instance = new ClassMethodTargets();

        instance.Void();

        Assert.That(InstanceBindingPatches.InstanceObserved, Is.SameAs(instance));
    }

    [Test]
    public void PostfixCanCapturePatchedMethodInstance()
    {
        InstanceBindingPatches.InstanceObserved = null;
        ApplyPatch(typeof(InstanceBindingPatches), nameof(InstanceBindingPatches.PostfixCanCapturePatchedMethodInstance));
        var instance = new ClassMethodTargets();

        instance.Void();

        Assert.That(InstanceBindingPatches.InstanceObserved, Is.SameAs(instance));
    }

    [Test]
    public void PrefixCanWritePatchedMethodInstanceByReference()
    {
        var original = new ClassMethodTargets();
        var replacement = new ClassMethodTargets();
        InstanceBindingPatches.ReplacementInstance = replacement;
        ApplyPatch(typeof(InstanceBindingPatches), nameof(InstanceBindingPatches.PrefixCanWritePatchedMethodInstanceByReference));

        ClassMethodTargets result = original.Self();

        Assert.That(result, Is.SameAs(replacement));
    }

    [Test]
    public void PrefixCanCapturePatchedStructInstance()
    {
        InstanceBindingPatches.StructInstanceFieldObserved = 0;
        ApplyPatch(typeof(InstanceBindingPatches), nameof(InstanceBindingPatches.PrefixCanCapturePatchedStructInstance));
        var target = new StructMethodTargets { foo = 42 };

        target.IntResult();

        Assert.That(InstanceBindingPatches.StructInstanceFieldObserved, Is.EqualTo(42));
    }

    [Test]
    public void PrefixCanWritePatchedStructInstanceByReference()
    {
        ApplyPatch(typeof(InstanceBindingPatches), nameof(InstanceBindingPatches.PrefixCanWritePatchedStructInstanceByReference));
        var target = new StructMethodTargets { foo = 1 };

        target.IntResult();

        Assert.That(target.foo, Is.EqualTo(42));
    }

    [Test]
    public void PostfixCanWritePatchedMethodInstanceByReference()
    {
        var original = new ClassMethodTargets();
        var replacement = new ClassMethodTargets();
        InstanceBindingPatches.ReplacementInstance = replacement;
        ApplyPatch(typeof(InstanceBindingPatches), nameof(InstanceBindingPatches.PostfixCanWritePatchedMethodInstanceByReference));

        ClassMethodTargets result = original.Self();

        Assert.That(result, Is.SameAs(replacement));
    }
}

[TestFixture]
public sealed partial class InstanceBindingTests
{
    [Test]
    public void InnerPrefixCanCaptureOuterMethodInstanceAsCaller()
    {
        InstanceBindingPatches.CallerObserved = null;
        ApplyPatch(typeof(InstanceBindingPatches), nameof(InstanceBindingPatches.InnerPrefixCanCaptureOuterMethodInstanceAsCaller));
        var outer = new ClassMethodTargets();

        outer.CallStaticVoid();

        Assert.That(InstanceBindingPatches.CallerObserved, Is.SameAs(outer));
    }

    [Test]
    public void InnerPostfixCanCaptureOuterMethodInstanceAsCaller()
    {
        InstanceBindingPatches.CallerObserved = null;
        ApplyPatch(typeof(InstanceBindingPatches), nameof(InstanceBindingPatches.InnerPostfixCanCaptureOuterMethodInstanceAsCaller));
        var outer = new ClassMethodTargets();

        outer.CallStaticVoid();

        Assert.That(InstanceBindingPatches.CallerObserved, Is.SameAs(outer));
    }

    [Test]
    public void InnerPrefixCanWriteOuterMethodInstanceByReference()
    {
        var original = new ClassMethodTargets();
        var replacement = new ClassMethodTargets();
        replacement.IntIdentity(42);
        InstanceBindingPatches.ReplacementCaller = replacement;
        ApplyPatch(typeof(InstanceBindingPatches), nameof(InstanceBindingPatches.InnerPrefixCanWriteOuterMethodInstanceByReference));

        int result = original.CallStaticVoidAndReturnValue();

        Assert.That(result, Is.EqualTo(42));
    }

    [Test]
    public void InnerPrefixCanCaptureOuterStructMethodInstanceAsCaller()
    {
        InstanceBindingPatches.StructInstanceFieldObserved = 0;
        ApplyPatch(typeof(InstanceBindingPatches), nameof(InstanceBindingPatches.InnerPrefixCanCaptureOuterStructMethodInstanceAsCaller));
        var outer = new StructMethodTargets { foo = 42 };

        outer.CallInnerWithoutField(new InstanceMethodTargetsWithoutFields());

        Assert.That(InstanceBindingPatches.StructInstanceFieldObserved, Is.EqualTo(42));
    }

    [Test]
    public void InnerPrefixCanWriteOuterStructMethodInstanceByReference()
    {
        ApplyPatch(typeof(InstanceBindingPatches), nameof(InstanceBindingPatches.InnerPrefixCanWriteOuterStructMethodInstanceByReference));
        var outer = new StructMethodTargets { foo = 1 };

        outer.CallInnerWithoutField(new InstanceMethodTargetsWithoutFields());

        Assert.That(outer.foo, Is.EqualTo(42));
    }

    [Test]
    public void InnerPostfixCanWriteOuterMethodInstanceByReference()
    {
        var original = new ClassMethodTargets();
        var replacement = new ClassMethodTargets();
        replacement.IntIdentity(42);
        InstanceBindingPatches.ReplacementCaller = replacement;
        ApplyPatch(typeof(InstanceBindingPatches), nameof(InstanceBindingPatches.InnerPostfixCanWriteOuterMethodInstanceByReference));

        int result = original.CallStaticVoidAndReturnValue();

        Assert.That(result, Is.EqualTo(42));
    }

    [Test]
    public void InstanceAttributeBindsPatchedMethodInstance()
    {
        InstanceBindingPatches.InstanceObserved = null;
        ApplyPatch(typeof(InstanceBindingPatches), nameof(InstanceBindingPatches.InstanceAttributeBindsPatchedMethodInstance));
        var target = new ClassMethodTargets();

        target.Void();

        Assert.That(InstanceBindingPatches.InstanceObserved, Is.SameAs(target));
    }

    [Test]
    public void InstanceAttributeCanWritePatchedReferenceTypeInstanceByReference()
    {
        var original = new ClassMethodTargets();
        var replacement = new ClassMethodTargets();
        InstanceBindingPatches.ReplacementInstance = replacement;
        ApplyPatch(typeof(InstanceBindingPatches), nameof(InstanceBindingPatches.InstanceAttributeCanWritePatchedReferenceTypeInstanceByReference));

        ClassMethodTargets result = original.Self();

        Assert.That(result, Is.SameAs(replacement));
    }

    [Test]
    public void InstanceAttributeCanSelectOuterInstance()
    {
        InstanceBindingPatches.InstanceObserved = null;
        ApplyPatch(typeof(InstanceBindingPatches), nameof(InstanceBindingPatches.InstanceAttributeCanSelectOuterInstance));
        var outer = new ClassMethodTargets();

        outer.CallInnerWithField(new InnerInstanceMethodTargets());

        Assert.That(InstanceBindingPatches.InstanceObserved, Is.SameAs(outer));
    }

    [Test]
    public void InstanceAttributeCanReadOuterStructInstanceByValue()
    {
        InstanceBindingPatches.StructInstanceFieldObserved = 0;
        ApplyPatch(typeof(InstanceBindingPatches), nameof(InstanceBindingPatches.InstanceAttributeCanReadOuterStructInstanceByValue));
        var outer = new StructMethodTargets { foo = 42 };

        outer.CallInnerWithoutField(new InstanceMethodTargetsWithoutFields());

        Assert.That(InstanceBindingPatches.StructInstanceFieldObserved, Is.EqualTo(42));
    }

    [Test]
    public void InstanceAttributeCanWriteOuterStructInstanceByReference()
    {
        ApplyPatch(typeof(InstanceBindingPatches), nameof(InstanceBindingPatches.InstanceAttributeCanWriteOuterStructInstanceByReference));
        var outer = new StructMethodTargets { foo = 1 };

        outer.CallInnerWithoutField(new InstanceMethodTargetsWithoutFields());

        Assert.That(outer.foo, Is.EqualTo(42));
    }

    [Test]
    public void InstanceAttributeCanWriteStructInstanceByReference()
    {
        ApplyPatch(typeof(InstanceBindingPatches), nameof(InstanceBindingPatches.InstanceAttributeCanWriteStructInstanceByReference));
        var target = new StructMethodTargets { foo = 1 };

        target.IntResult();

        Assert.That(target.foo, Is.EqualTo(42));
    }

    [Test]
    public void InstanceAttributeCanReadStructInstanceByValue()
    {
        InstanceBindingPatches.StructInstanceFieldObserved = 0;
        ApplyPatch(typeof(InstanceBindingPatches), nameof(InstanceBindingPatches.InstanceAttributeCanReadStructInstanceByValue));
        var target = new StructMethodTargets { foo = 42 };

        target.IntResult();

        Assert.That(InstanceBindingPatches.StructInstanceFieldObserved, Is.EqualTo(42));
    }
}
