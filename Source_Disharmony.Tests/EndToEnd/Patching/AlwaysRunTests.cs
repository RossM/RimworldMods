namespace Disharmony.Tests.EndToEnd.Patching;

public static class AlwaysRunPatches
{
    public static readonly List<string> events = [];
    public static Exception? observedException;
    public static Exception? replacementException;

    [Prefix]
    [Priority(PatchPriority.Low)]
    [PatchOptions(PatchOptions.AlwaysRun)]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.PriorityTarget))]
    public static void Ordering_AlwaysRunPatchesNestOutsideRegularPatches_AlwaysPrefix() =>
        StaticMethodTargets.PriorityEvents.Add("always-prefix");

    [Prefix]
    [Priority(PatchPriority.High)]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.PriorityTarget))]
    public static void Ordering_AlwaysRunPatchesNestOutsideRegularPatches_RegularPrefix() =>
        StaticMethodTargets.PriorityEvents.Add("regular-prefix");

    [Postfix]
    [Priority(PatchPriority.High)]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.PriorityTarget))]
    public static void Ordering_AlwaysRunPatchesNestOutsideRegularPatches_RegularPostfix() =>
        StaticMethodTargets.PriorityEvents.Add("regular-postfix");

    [Postfix]
    [Priority(PatchPriority.Low)]
    [PatchOptions(PatchOptions.AlwaysRun)]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.PriorityTarget))]
    public static void Ordering_AlwaysRunPatchesNestOutsideRegularPatches_AlwaysPostfix() =>
        StaticMethodTargets.PriorityEvents.Add("always-postfix");

    [Prefix]
    [PatchOptions(PatchOptions.AlwaysRun)]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.Void))]
    public static bool Validation_AlwaysRunPrefixReturningBoolIsRejected() => true;

    [Prefix]
    [PatchOptions(PatchOptions.AlwaysRun)]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.IntResult))]
    public static void Validation_AlwaysRunPrefixBindingResultIsRejected(int __result) { }

    [Prefix]
    [PatchOptions(PatchOptions.AlwaysRun)]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.Void))]
    public static void Validation_AlwaysRunPrefixBindingExceptionIsRejected(
        [ExceptionAttribute] Exception? exception) { }

    [Postfix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.Void))]
    public static void Validation_RegularPostfixBindingExceptionIsRejected(
        [ExceptionAttribute] Exception? exception) { }

    [Postfix]
    [PatchOptions(PatchOptions.AlwaysRun)]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.Void))]
    public static void ExceptionBinding_NoException_PassesNull(Exception? __exception) =>
        observedException = __exception;

    [Postfix]
    [PatchOptions(PatchOptions.AlwaysRun)]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.ThrowInvalidOperationException))]
    public static void ExceptionBinding_ThrownException_ReadByValueAndRethrown(Exception __exception) =>
        observedException = __exception;

    [Postfix]
    [PatchOptions(PatchOptions.AlwaysRun)]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.ThrowInvalidOperationException))]
    public static void ExceptionBinding_ThrownException_WriteReplacementByReference(
        [ExceptionAttribute] ref Exception? exception)
    {
        replacementException = new ArgumentException("replacement");
        exception = replacementException;
    }

    [Postfix]
    [PatchOptions(PatchOptions.AlwaysRun)]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.ThrowInvalidOperationException))]
    public static void ExceptionBinding_ThrownException_SuppressByReference(ref Exception? __exception)
    {
        observedException = __exception;
        __exception = null;
    }

    [Prefix]
    [PatchOptions(PatchOptions.AlwaysRun)]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.Void))]
    public static void RegularPrefixException_DoesNotPreventAlwaysRunPatches_AlwaysPrefix() =>
        events.Add("always-prefix");

    [Prefix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.Void))]
    public static void RegularPrefixException_DoesNotPreventAlwaysRunPatches_RegularPrefix()
    {
        events.Add("regular-prefix");
        throw new InvalidOperationException("regular-prefix");
    }

    [Postfix]
    [PatchOptions(PatchOptions.AlwaysRun)]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.Void))]
    public static void RegularPrefixException_DoesNotPreventAlwaysRunPatches_AlwaysPostfix()
    {
        events.Add("always-postfix");
    }

    [Prefix]
    [PatchOptions(PatchOptions.AlwaysRun)]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.Void))]
    public static void RegularPostfixException_DoesNotPreventAlwaysRunPatches_AlwaysPrefix() =>
        events.Add("always-prefix");

    [Postfix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.Void))]
    public static void RegularPostfixException_DoesNotPreventAlwaysRunPatches_RegularPostfix()
    {
        events.Add("regular-postfix");
        throw new InvalidOperationException("regular-postfix");
    }

    [Postfix]
    [PatchOptions(PatchOptions.AlwaysRun)]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.Void))]
    public static void RegularPostfixException_DoesNotPreventAlwaysRunPatches_AlwaysPostfix()
    {
        events.Add("always-postfix");
    }

    [Prefix]
    [Priority(PatchPriority.High)]
    [PatchOptions(PatchOptions.AlwaysRun)]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.Void))]
    public static void AlwaysRunPrefixException_PreventsOtherAlwaysRunPatches_ThrowingPrefix()
    {
        events.Add("throwing-always-prefix");
        throw new InvalidOperationException("always-prefix");
    }

    [Prefix]
    [Priority(PatchPriority.Low)]
    [PatchOptions(PatchOptions.AlwaysRun)]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.Void))]
    public static void AlwaysRunPrefixException_PreventsOtherAlwaysRunPatches_LaterPrefix() =>
        events.Add("later-always-prefix");

    [Postfix]
    [PatchOptions(PatchOptions.AlwaysRun)]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.Void))]
    public static void AlwaysRunPrefixException_PreventsOtherAlwaysRunPatches_Postfix() =>
        events.Add("always-postfix");

    [Postfix]
    [Priority(PatchPriority.Low)]
    [PatchOptions(PatchOptions.AlwaysRun)]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.Void))]
    public static void AlwaysRunPostfixException_PreventsLaterAlwaysRunPostfix_ThrowingPostfix()
    {
        events.Add("throwing-always-postfix");
        throw new InvalidOperationException("always-postfix");
    }

    [Postfix]
    [Priority(PatchPriority.High)]
    [PatchOptions(PatchOptions.AlwaysRun)]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.Void))]
    public static void AlwaysRunPostfixException_PreventsLaterAlwaysRunPostfix_LaterPostfix() =>
        events.Add("later-always-postfix");

    [Postfix]
    [PatchOptions(PatchOptions.AlwaysRun)]
    [Inner(typeof(InnerStaticMethodTargets), nameof(InnerStaticMethodTargets.ThrowInvalidOperationException))]
    [Target(typeof(OuterStaticMethodTargets), nameof(OuterStaticMethodTargets.CallThrowingVoid))]
    public static void InnerPostfix_ThrownException_SuppressByReference(ref Exception? __exception)
    {
        observedException = __exception;
        __exception = null;
    }
}

[TestFixture]
public sealed class AlwaysRunTests : PatchTestBase
{
    [Test]
    public void Ordering_AlwaysRunPatchesNestOutsideRegularPatches()
    {
        StaticMethodTargets.PriorityEvents.Clear();
        ApplyPatch(typeof(AlwaysRunPatches),
            nameof(AlwaysRunPatches.Ordering_AlwaysRunPatchesNestOutsideRegularPatches_RegularPostfix));
        ApplyPatch(typeof(AlwaysRunPatches),
            nameof(AlwaysRunPatches.Ordering_AlwaysRunPatchesNestOutsideRegularPatches_AlwaysPrefix));
        ApplyPatch(typeof(AlwaysRunPatches),
            nameof(AlwaysRunPatches.Ordering_AlwaysRunPatchesNestOutsideRegularPatches_AlwaysPostfix));
        ApplyPatch(typeof(AlwaysRunPatches),
            nameof(AlwaysRunPatches.Ordering_AlwaysRunPatchesNestOutsideRegularPatches_RegularPrefix));

        StaticMethodTargets.PriorityTarget();

        Assert.That(StaticMethodTargets.PriorityEvents, Is.EqualTo(new[]
        {
            "always-prefix",
            "regular-prefix",
            "target",
            "regular-postfix",
            "always-postfix",
        }));
    }

    [Test]
    public void Validation_AlwaysRunPrefixReturningBoolIsRejected()
    {
        Assert.Throws<PatchException>(() => ApplyPatch(
            typeof(AlwaysRunPatches),
            nameof(AlwaysRunPatches.Validation_AlwaysRunPrefixReturningBoolIsRejected)));
    }

    [Test]
    public void Validation_AlwaysRunPrefixBindingResultIsRejected()
    {
        Assert.Throws<PatchException>(() => ApplyPatch(
            typeof(AlwaysRunPatches),
            nameof(AlwaysRunPatches.Validation_AlwaysRunPrefixBindingResultIsRejected)));
    }

    [Test]
    public void Validation_AlwaysRunPrefixBindingExceptionIsRejected()
    {
        Assert.Throws<PatchException>(() => ApplyPatch(
            typeof(AlwaysRunPatches),
            nameof(AlwaysRunPatches.Validation_AlwaysRunPrefixBindingExceptionIsRejected)));
    }

    [Test]
    public void Validation_RegularPostfixBindingExceptionIsRejected()
    {
        Assert.Throws<PatchException>(() => ApplyPatch(
            typeof(AlwaysRunPatches),
            nameof(AlwaysRunPatches.Validation_RegularPostfixBindingExceptionIsRejected)));
    }

    [Test]
    public void ExceptionBinding_NoException_PassesNull()
    {
        AlwaysRunPatches.observedException = new Exception("sentinel");
        ApplyPatch(typeof(AlwaysRunPatches), nameof(AlwaysRunPatches.ExceptionBinding_NoException_PassesNull));

        StaticMethodTargets.Void();

        Assert.That(AlwaysRunPatches.observedException, Is.Null);
    }

    [Test]
    public void ExceptionBinding_ThrownException_ReadByValueAndRethrown()
    {
        AlwaysRunPatches.observedException = null;
        ApplyPatch(typeof(AlwaysRunPatches),
            nameof(AlwaysRunPatches.ExceptionBinding_ThrownException_ReadByValueAndRethrown));

        var exception = Assert.Throws<InvalidOperationException>(StaticMethodTargets.ThrowInvalidOperationException);

        Assert.That(exception!.Message, Is.EqualTo("target"));
        Assert.That(AlwaysRunPatches.observedException, Is.SameAs(exception));
    }

    [Test]
    public void ExceptionBinding_ThrownException_WriteReplacementByReference()
    {
        AlwaysRunPatches.replacementException = null;
        ApplyPatch(typeof(AlwaysRunPatches),
            nameof(AlwaysRunPatches.ExceptionBinding_ThrownException_WriteReplacementByReference));

        var exception = Assert.Throws<ArgumentException>(StaticMethodTargets.ThrowInvalidOperationException);

        Assert.That(exception, Is.SameAs(AlwaysRunPatches.replacementException));
        Assert.That(exception!.Message, Is.EqualTo("replacement"));
    }

    [Test]
    public void ExceptionBinding_ThrownException_SuppressByReference()
    {
        AlwaysRunPatches.observedException = null;
        ApplyPatch(typeof(AlwaysRunPatches),
            nameof(AlwaysRunPatches.ExceptionBinding_ThrownException_SuppressByReference));

        Assert.DoesNotThrow(StaticMethodTargets.ThrowInvalidOperationException);
        Assert.That(AlwaysRunPatches.observedException, Is.TypeOf<InvalidOperationException>()
            .With.Message.EqualTo("target"));
    }

    [Test]
    public void RegularPrefixException_DoesNotPreventAlwaysRunPatches()
    {
        AlwaysRunPatches.events.Clear();
        ApplyPatch(typeof(AlwaysRunPatches),
            nameof(AlwaysRunPatches.RegularPrefixException_DoesNotPreventAlwaysRunPatches_RegularPrefix));
        ApplyPatch(typeof(AlwaysRunPatches),
            nameof(AlwaysRunPatches.RegularPrefixException_DoesNotPreventAlwaysRunPatches_AlwaysPostfix));
        ApplyPatch(typeof(AlwaysRunPatches),
            nameof(AlwaysRunPatches.RegularPrefixException_DoesNotPreventAlwaysRunPatches_AlwaysPrefix));

        var exception = Assert.Throws<InvalidOperationException>(StaticMethodTargets.Void);
        Assert.That(exception!.Message, Is.EqualTo("regular-prefix"));
        Assert.That(AlwaysRunPatches.events, Is.EqualTo(new[]
        {
            "always-prefix",
            "regular-prefix",
            "always-postfix",
        }));
    }

    [Test]
    public void RegularPostfixException_DoesNotPreventAlwaysRunPatches()
    {
        AlwaysRunPatches.events.Clear();
        ApplyPatch(typeof(AlwaysRunPatches),
            nameof(AlwaysRunPatches.RegularPostfixException_DoesNotPreventAlwaysRunPatches_AlwaysPostfix));
        ApplyPatch(typeof(AlwaysRunPatches),
            nameof(AlwaysRunPatches.RegularPostfixException_DoesNotPreventAlwaysRunPatches_AlwaysPrefix));
        ApplyPatch(typeof(AlwaysRunPatches),
            nameof(AlwaysRunPatches.RegularPostfixException_DoesNotPreventAlwaysRunPatches_RegularPostfix));

        var exception = Assert.Throws<InvalidOperationException>(StaticMethodTargets.Void);
        Assert.That(exception!.Message, Is.EqualTo("regular-postfix"));
        Assert.That(AlwaysRunPatches.events, Is.EqualTo(new[]
        {
            "always-prefix",
            "regular-postfix",
            "always-postfix",
        }));
    }

    [Test]
    public void AlwaysRunPrefixException_PreventsOtherAlwaysRunPatches()
    {
        AlwaysRunPatches.events.Clear();
        ApplyPatch(typeof(AlwaysRunPatches),
            nameof(AlwaysRunPatches.AlwaysRunPrefixException_PreventsOtherAlwaysRunPatches_LaterPrefix));
        ApplyPatch(typeof(AlwaysRunPatches),
            nameof(AlwaysRunPatches.AlwaysRunPrefixException_PreventsOtherAlwaysRunPatches_Postfix));
        ApplyPatch(typeof(AlwaysRunPatches),
            nameof(AlwaysRunPatches.AlwaysRunPrefixException_PreventsOtherAlwaysRunPatches_ThrowingPrefix));

        var exception = Assert.Throws<InvalidOperationException>(StaticMethodTargets.Void);

        Assert.That(exception!.Message, Is.EqualTo("always-prefix"));
        Assert.That(AlwaysRunPatches.events, Is.EqualTo(new[] { "throwing-always-prefix" }));
    }

    [Test]
    public void AlwaysRunPostfixException_PreventsLaterAlwaysRunPostfix()
    {
        AlwaysRunPatches.events.Clear();
        ApplyPatch(typeof(AlwaysRunPatches),
            nameof(AlwaysRunPatches.AlwaysRunPostfixException_PreventsLaterAlwaysRunPostfix_LaterPostfix));
        ApplyPatch(typeof(AlwaysRunPatches),
            nameof(AlwaysRunPatches.AlwaysRunPostfixException_PreventsLaterAlwaysRunPostfix_ThrowingPostfix));

        var exception = Assert.Throws<InvalidOperationException>(StaticMethodTargets.Void);

        Assert.That(exception!.Message, Is.EqualTo("always-postfix"));
        Assert.That(AlwaysRunPatches.events, Is.EqualTo(new[] { "throwing-always-postfix" }));
    }

    [Test]
    public void InnerPostfix_ThrownException_SuppressByReference()
    {
        AlwaysRunPatches.observedException = null;
        ApplyPatch(typeof(AlwaysRunPatches), nameof(AlwaysRunPatches.InnerPostfix_ThrownException_SuppressByReference));

        int result = OuterStaticMethodTargets.CallThrowingVoid();

        Assert.That(result, Is.EqualTo(42));
        Assert.That(AlwaysRunPatches.observedException, Is.TypeOf<InvalidOperationException>()
            .With.Message.EqualTo("inner"));
    }
}
