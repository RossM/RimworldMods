namespace Disharmony.Tests;

public static class OptimizerPatches
{
    public static int PatchCalls;

    private static void RecordPatch() => PatchCalls++;

    [Prefix]
    [Target(typeof(OptimizerControlFlowTargets), nameof(OptimizerControlFlowTargets.ConditionalBranches))]
    [Optimize]
    public static void ConditionalBranches_PreservesEveryPath() => RecordPatch();

    [Prefix]
    [Target(typeof(OptimizerControlFlowTargets), nameof(OptimizerControlFlowTargets.DenseSwitch))]
    [Optimize]
    public static void DenseSwitch_PreservesCasesAndDefault() => RecordPatch();

    [Prefix]
    [Target(typeof(OptimizerControlFlowTargets), nameof(OptimizerControlFlowTargets.LoopWithBreakAndContinue))]
    [Optimize]
    public static void LoopWithBreakAndContinue_PreservesBackEdges() => RecordPatch();

    [Prefix]
    [Target(typeof(OptimizerControlFlowTargets), nameof(OptimizerControlFlowTargets.ConditionalInfiniteLoop))]
    [Optimize]
    public static void ConditionalInfiniteLoop_PreservesNonLoopingPath() => RecordPatch();

    [Prefix]
    [Target(typeof(OptimizerControlFlowTargets), nameof(OptimizerControlFlowTargets.ShortCircuit))]
    [Optimize]
    public static void ShortCircuit_PreservesSkippedRightOperand() => RecordPatch();

    [Prefix]
    [Target(typeof(OptimizerControlFlowTargets), nameof(OptimizerControlFlowTargets.RefLocalConditional))]
    [Optimize]
    public static void RefLocalConditional_PreservesManagedPointerBranches() => RecordPatch();

    [Prefix]
    [Target(typeof(OptimizerControlFlowTargets), nameof(OptimizerControlFlowTargets.PatternMatching))]
    [Optimize]
    public static void PatternMatching_PreservesTypePropertyGuardAndNullPatterns() => RecordPatch();

    [Prefix]
    [Target(typeof(OptimizerControlFlowTargets), nameof(OptimizerControlFlowTargets.NullPropagation))]
    [Optimize]
    public static void NullPropagation_PreservesNullAtEveryLink() => RecordPatch();

    [Prefix]
    [Target(typeof(OptimizerControlFlowTargets), nameof(OptimizerControlFlowTargets.NullCoalescingAssignment))]
    [Optimize]
    public static void NullCoalescingAssignment_PreservesNullAndNonNullValues() => RecordPatch();

    [Prefix]
    [Target(typeof(OptimizerControlFlowTargets), nameof(OptimizerControlFlowTargets.ForeachWithContinueAndEarlyReturn))]
    [Optimize]
    public static void ForeachWithContinueAndEarlyReturn_PreservesLoopAndEnumeratorCleanup() => RecordPatch();

    [Prefix]
    [Target(typeof(OptimizerExceptionTargets), nameof(OptimizerExceptionTargets.TryCatch))]
    [Optimize]
    public static void TryCatch_PreservesNormalAndExceptionalPaths() => RecordPatch();

    [Prefix]
    [Target(typeof(OptimizerExceptionTargets), nameof(OptimizerExceptionTargets.TryFinally))]
    [Optimize]
    public static void TryFinally_PreservesReturnsAndFinallyExecution() => RecordPatch();

    [Prefix]
    [Target(typeof(OptimizerExceptionTargets), nameof(OptimizerExceptionTargets.NestedTryFinallyAndCatch))]
    [Optimize]
    public static void NestedTryFinallyAndCatch_PreservesExceptionRegionControlFlow() => RecordPatch();

    [Prefix]
    [Target(typeof(OptimizerExceptionTargets), nameof(OptimizerExceptionTargets.ExceptionFilter))]
    [Optimize]
    public static void ExceptionFilter_PreservesFilterAndFallbackHandlers() => RecordPatch();

    [Prefix]
    [Target(typeof(OptimizerExceptionTargets), nameof(OptimizerExceptionTargets.UsingWithEarlyReturn))]
    [Optimize]
    public static void UsingWithEarlyReturn_PreservesCompilerGeneratedFinally() => RecordPatch();

    [Prefix]
    [Target(typeof(OptimizerExceptionTargets), nameof(OptimizerExceptionTargets.LockWithConditionalReturn))]
    [Optimize]
    public static void LockWithConditionalReturn_PreservesCompilerGeneratedFinally() => RecordPatch();

    [Prefix]
    [Target(typeof(OptimizerExceptionTargets), nameof(OptimizerExceptionTargets.CatchAndRethrow))]
    [Optimize]
    public static void CatchAndRethrow_PreservesHandledAndRethrownPaths() => RecordPatch();

    [Prefix]
    [Target(typeof(OptimizerPrefixTargets), nameof(OptimizerPrefixTargets.PrefixAlwaysFalseTarget))]
    [Optimize]
    public static bool Prefix_AlwaysFalse_SkipsTarget(ref int __result)
    {
        RecordPatch();
        __result = 42;
        return false;
    }

    [Prefix]
    [Target(typeof(OptimizerPrefixTargets), nameof(OptimizerPrefixTargets.PrefixAlwaysTrueTarget))]
    [Optimize]
    public static bool Prefix_AlwaysTrue_RunsTarget()
    {
        RecordPatch();
        return true;
    }

    [InnerPrefix(typeof(OptimizerPrefixTargets), nameof(OptimizerPrefixTargets.InnerAlwaysFalseTarget))]
    [Target(typeof(OptimizerPrefixTargets), nameof(OptimizerPrefixTargets.CallInnerAlwaysFalseTarget))]
    [Optimize]
    public static bool InnerPrefix_AlwaysFalse_SkipsInnerTarget(ref int __result)
    {
        RecordPatch();
        __result = 42;
        return false;
    }

    [InnerPrefix(typeof(OptimizerPrefixTargets), nameof(OptimizerPrefixTargets.InnerAlwaysTrueTarget))]
    [Target(typeof(OptimizerPrefixTargets), nameof(OptimizerPrefixTargets.CallInnerAlwaysTrueTarget))]
    [Optimize]
    public static bool InnerPrefix_AlwaysTrue_RunsInnerTarget()
    {
        RecordPatch();
        return true;
    }

    [Prefix]
    [Target(typeof(OptimizerPrefixTargets), nameof(OptimizerPrefixTargets.PrefixConditionallySkippedTarget))]
    [Optimize]
    public static bool Prefix_ArgumentControlsWhetherTargetIsSkipped(bool skip, ref int __result)
    {
        RecordPatch();
        __result = 42;
        return !skip;
    }

    [InnerPrefix(typeof(OptimizerPrefixTargets), nameof(OptimizerPrefixTargets.InnerConditionallySkippedTarget))]
    [Target(typeof(OptimizerPrefixTargets), nameof(OptimizerPrefixTargets.CallInnerConditionallySkippedTarget))]
    [Optimize]
    public static bool InnerPrefix_OuterArgumentControlsWhetherInnerTargetIsSkipped(
        [Parameter("skip", Scope.Outer)] bool skip,
        ref int __result)
    {
        RecordPatch();
        __result = 42;
        return !skip;
    }
}

[TestFixture]
public sealed class OptimizerTests : PatchTestBase
{
    [SetUp]
    public void EnableOptimizer()
    {
        Patcher.Instance.optimizerEnabled = true;
        OptimizerPatches.PatchCalls = 0;
        OptimizerControlFlowTargets.RightOperandCalls = 0;
        OptimizerExceptionTargets.FinallyExecutions = 0;
        OptimizerExceptionTargets.DisposalCount = 0;
        OptimizerPrefixTargets.PrefixTargetExecutions = 0;
        OptimizerPrefixTargets.InnerTargetExecutions = 0;
    }

    [TearDown]
    public void DisableOptimizer()
    {
        Patcher.Instance.optimizerEnabled = false;
        Autopatcher.UnpatchAll(typeof(OptimizerTests).Assembly);
    }

    [Test]
    public void ConditionalBranches_PreservesEveryPath()
    {
        ApplyPatch(typeof(OptimizerPatches), nameof(OptimizerPatches.ConditionalBranches_PreservesEveryPath));

        Assert.That(OptimizerControlFlowTargets.ConditionalBranches(-1), Is.EqualTo("negative"));
        Assert.That(OptimizerControlFlowTargets.ConditionalBranches(0), Is.EqualTo("zero"));
        Assert.That(OptimizerControlFlowTargets.ConditionalBranches(1), Is.EqualTo("positive"));
        Assert.That(OptimizerPatches.PatchCalls, Is.EqualTo(3));
    }

    [Test]
    public void DenseSwitch_PreservesCasesAndDefault()
    {
        ApplyPatch(typeof(OptimizerPatches), nameof(OptimizerPatches.DenseSwitch_PreservesCasesAndDefault));

        Assert.That(OptimizerControlFlowTargets.DenseSwitch(0), Is.EqualTo(10));
        Assert.That(OptimizerControlFlowTargets.DenseSwitch(1), Is.EqualTo(11));
        Assert.That(OptimizerControlFlowTargets.DenseSwitch(2), Is.EqualTo(12));
        Assert.That(OptimizerControlFlowTargets.DenseSwitch(3), Is.EqualTo(13));
        Assert.That(OptimizerControlFlowTargets.DenseSwitch(4), Is.EqualTo(99));
        Assert.That(OptimizerPatches.PatchCalls, Is.EqualTo(5));
    }

    [Test]
    public void LoopWithBreakAndContinue_PreservesBackEdges()
    {
        ApplyPatch(typeof(OptimizerPatches), nameof(OptimizerPatches.LoopWithBreakAndContinue_PreservesBackEdges));

        Assert.That(OptimizerControlFlowTargets.LoopWithBreakAndContinue(0), Is.Zero);
        Assert.That(OptimizerControlFlowTargets.LoopWithBreakAndContinue(4), Is.EqualTo(4));
        Assert.That(OptimizerControlFlowTargets.LoopWithBreakAndContinue(10), Is.EqualTo(16));
        Assert.That(OptimizerPatches.PatchCalls, Is.EqualTo(3));
    }

    [Test]
    public void ConditionalInfiniteLoop_PreservesNonLoopingPath()
    {
        ApplyPatch(
            typeof(OptimizerPatches),
            nameof(OptimizerPatches.ConditionalInfiniteLoop_PreservesNonLoopingPath));

        int result = OptimizerControlFlowTargets.ConditionalInfiniteLoop(false);

        Assert.That(result, Is.EqualTo(42));
        Assert.That(OptimizerPatches.PatchCalls, Is.EqualTo(1));
    }

    [Test]
    public void ShortCircuit_PreservesSkippedRightOperand()
    {
        ApplyPatch(typeof(OptimizerPatches), nameof(OptimizerPatches.ShortCircuit_PreservesSkippedRightOperand));

        Assert.That(OptimizerControlFlowTargets.ShortCircuit(false, true), Is.False);
        Assert.That(OptimizerControlFlowTargets.RightOperandCalls, Is.Zero);
        Assert.That(OptimizerControlFlowTargets.ShortCircuit(true, false), Is.False);
        Assert.That(OptimizerControlFlowTargets.ShortCircuit(true, true), Is.True);
        Assert.That(OptimizerControlFlowTargets.RightOperandCalls, Is.EqualTo(2));
        Assert.That(OptimizerPatches.PatchCalls, Is.EqualTo(3));
    }

    [Test]
    public void RefLocalConditional_PreservesManagedPointerBranches()
    {
        ApplyPatch(typeof(OptimizerPatches), nameof(OptimizerPatches.RefLocalConditional_PreservesManagedPointerBranches));

        Assert.That(OptimizerControlFlowTargets.RefLocalConditional(true), Is.EqualTo(42));
        Assert.That(OptimizerControlFlowTargets.RefLocalConditional(false), Is.EqualTo(42));
        Assert.That(OptimizerPatches.PatchCalls, Is.EqualTo(2));
    }

    [Test]
    public void PatternMatching_PreservesTypePropertyGuardAndNullPatterns()
    {
        Assert.That(OptimizerControlFlowTargets.PatternMatching(0), Is.EqualTo("non-positive integer"));

        ApplyPatch(typeof(OptimizerPatches), nameof(OptimizerPatches.PatternMatching_PreservesTypePropertyGuardAndNullPatterns));

        Assert.That(OptimizerControlFlowTargets.PatternMatching(null), Is.EqualTo("null"));
        Assert.That(OptimizerControlFlowTargets.PatternMatching(1), Is.EqualTo("positive integer"));
        Assert.That(OptimizerControlFlowTargets.PatternMatching(0), Is.EqualTo("non-positive integer"));
        Assert.That(OptimizerControlFlowTargets.PatternMatching(""), Is.EqualTo("empty string"));
        Assert.That(OptimizerControlFlowTargets.PatternMatching("text"), Is.EqualTo("text"));
        Assert.That(
            OptimizerControlFlowTargets.PatternMatching(new BindingReference { Value = 42 }),
            Is.EqualTo("reference with value 42"));
        Assert.That(OptimizerControlFlowTargets.PatternMatching(new object()), Is.EqualTo("other"));
        Assert.That(OptimizerPatches.PatchCalls, Is.EqualTo(7));
    }

    [Test]
    public void NullPropagation_PreservesNullAtEveryLink()
    {
        ApplyPatch(typeof(OptimizerPatches), nameof(OptimizerPatches.NullPropagation_PreservesNullAtEveryLink));
        var withoutNext = new OptimizerNullPropagationNode();
        var withNext = new OptimizerNullPropagationNode
        {
            Next = new OptimizerNullPropagationNode { Value = 42 },
        };

        Assert.That(OptimizerControlFlowTargets.NullPropagation(null), Is.Null);
        Assert.That(OptimizerControlFlowTargets.NullPropagation(withoutNext), Is.Null);
        Assert.That(OptimizerControlFlowTargets.NullPropagation(withNext), Is.EqualTo(42));
        Assert.That(OptimizerPatches.PatchCalls, Is.EqualTo(3));
    }

    [Test]
    public void NullCoalescingAssignment_PreservesNullAndNonNullValues()
    {
        ApplyPatch(typeof(OptimizerPatches), nameof(OptimizerPatches.NullCoalescingAssignment_PreservesNullAndNonNullValues));

        Assert.That(OptimizerControlFlowTargets.NullCoalescingAssignment(null), Is.EqualTo("fallback"));
        Assert.That(OptimizerControlFlowTargets.NullCoalescingAssignment("value"), Is.EqualTo("value"));
        Assert.That(OptimizerPatches.PatchCalls, Is.EqualTo(2));
    }

    [Test]
    public void ForeachWithContinueAndEarlyReturn_PreservesLoopAndEnumeratorCleanup()
    {
        Assert.That(OptimizerControlFlowTargets.ForeachWithContinueAndEarlyReturn([0, 1, 2]), Is.EqualTo(3));
        Assert.That(OptimizerControlFlowTargets.ForeachWithContinueAndEarlyReturn([1, -2, 3]), Is.EqualTo(-2));

        ApplyPatch(
            typeof(OptimizerPatches),
            nameof(OptimizerPatches.ForeachWithContinueAndEarlyReturn_PreservesLoopAndEnumeratorCleanup));

        Assert.That(OptimizerControlFlowTargets.ForeachWithContinueAndEarlyReturn([0, 1, 2]), Is.EqualTo(3));
        Assert.That(OptimizerControlFlowTargets.ForeachWithContinueAndEarlyReturn([1, -2, 3]), Is.EqualTo(-2));
        Assert.That(OptimizerPatches.PatchCalls, Is.EqualTo(2));
    }

    [Test]
    public void TryCatch_PreservesNormalAndExceptionalPaths()
    {
        ApplyPatch(typeof(OptimizerPatches), nameof(OptimizerPatches.TryCatch_PreservesNormalAndExceptionalPaths));

        Assert.That(OptimizerExceptionTargets.TryCatch(false), Is.EqualTo(1));
        Assert.That(OptimizerExceptionTargets.TryCatch(true), Is.EqualTo(2));
        Assert.That(OptimizerPatches.PatchCalls, Is.EqualTo(2));
    }

    [Test]
    public void TryFinally_PreservesReturnsAndFinallyExecution()
    {
        ApplyPatch(typeof(OptimizerPatches), nameof(OptimizerPatches.TryFinally_PreservesReturnsAndFinallyExecution));

        Assert.That(OptimizerExceptionTargets.TryFinally(true), Is.EqualTo(1));
        Assert.That(OptimizerExceptionTargets.TryFinally(false), Is.EqualTo(2));
        Assert.That(OptimizerExceptionTargets.FinallyExecutions, Is.EqualTo(2));
        Assert.That(OptimizerPatches.PatchCalls, Is.EqualTo(2));
    }

    [Test]
    public void NestedTryFinallyAndCatch_PreservesExceptionRegionControlFlow()
    {
        ApplyPatch(
            typeof(OptimizerPatches),
            nameof(OptimizerPatches.NestedTryFinallyAndCatch_PreservesExceptionRegionControlFlow));

        Assert.That(OptimizerExceptionTargets.NestedTryFinallyAndCatch(0), Is.EqualTo(10));
        Assert.That(OptimizerExceptionTargets.NestedTryFinallyAndCatch(1), Is.EqualTo(20));
        Assert.That(OptimizerExceptionTargets.NestedTryFinallyAndCatch(2), Is.EqualTo(30));
        Assert.That(OptimizerExceptionTargets.FinallyExecutions, Is.EqualTo(3));
        Assert.That(OptimizerPatches.PatchCalls, Is.EqualTo(3));
    }

    [Test]
    [Ignore("Harmony cannot transpile methods containing exception filters")]
    public void ExceptionFilter_PreservesFilterAndFallbackHandlers()
    {
        Assert.That(OptimizerExceptionTargets.ExceptionFilter(true), Is.EqualTo(1));
        Assert.That(OptimizerExceptionTargets.ExceptionFilter(false), Is.EqualTo(2));

        ApplyPatch(typeof(OptimizerPatches), nameof(OptimizerPatches.ExceptionFilter_PreservesFilterAndFallbackHandlers));

        Assert.That(OptimizerExceptionTargets.ExceptionFilter(true), Is.EqualTo(1));
        Assert.That(OptimizerExceptionTargets.ExceptionFilter(false), Is.EqualTo(2));
        Assert.That(OptimizerPatches.PatchCalls, Is.EqualTo(2));
    }

    [Test]
    public void UsingWithEarlyReturn_PreservesCompilerGeneratedFinally()
    {
        ApplyPatch(typeof(OptimizerPatches), nameof(OptimizerPatches.UsingWithEarlyReturn_PreservesCompilerGeneratedFinally));

        Assert.That(OptimizerExceptionTargets.UsingWithEarlyReturn(true), Is.EqualTo(1));
        Assert.That(OptimizerExceptionTargets.UsingWithEarlyReturn(false), Is.EqualTo(2));
        Assert.That(OptimizerExceptionTargets.DisposalCount, Is.EqualTo(2));
        Assert.That(OptimizerPatches.PatchCalls, Is.EqualTo(2));
    }

    [Test]
    public void LockWithConditionalReturn_PreservesCompilerGeneratedFinally()
    {
        ApplyPatch(
            typeof(OptimizerPatches),
            nameof(OptimizerPatches.LockWithConditionalReturn_PreservesCompilerGeneratedFinally));

        Assert.That(OptimizerExceptionTargets.LockWithConditionalReturn(true), Is.EqualTo(1));
        Assert.That(OptimizerExceptionTargets.LockWithConditionalReturn(false), Is.EqualTo(2));
        Assert.That(OptimizerPatches.PatchCalls, Is.EqualTo(2));
    }

    [Test]
    public void CatchAndRethrow_PreservesHandledAndRethrownPaths()
    {
        Assert.That(OptimizerExceptionTargets.CatchAndRethrow(false), Is.EqualTo(42));
        var originalException = Assert.Throws<InvalidOperationException>(() => OptimizerExceptionTargets.CatchAndRethrow(true));
        Assert.That(originalException!.Message, Is.EqualTo("original"));

        ApplyPatch(typeof(OptimizerPatches), nameof(OptimizerPatches.CatchAndRethrow_PreservesHandledAndRethrownPaths));

        Assert.That(OptimizerExceptionTargets.CatchAndRethrow(false), Is.EqualTo(42));
        var exception = Assert.Throws<InvalidOperationException>(() => OptimizerExceptionTargets.CatchAndRethrow(true));
        Assert.That(exception!.Message, Is.EqualTo("original"));
        Assert.That(OptimizerPatches.PatchCalls, Is.EqualTo(2));
    }

    [Test]
    public void Prefix_AlwaysFalse_SkipsTarget()
    {
        ApplyPatch(typeof(OptimizerPatches), nameof(OptimizerPatches.Prefix_AlwaysFalse_SkipsTarget));

        Assert.That(OptimizerPrefixTargets.PrefixAlwaysFalseTarget(-1), Is.EqualTo(42));
        Assert.That(OptimizerPrefixTargets.PrefixAlwaysFalseTarget(1), Is.EqualTo(42));
        Assert.That(OptimizerPrefixTargets.PrefixTargetExecutions, Is.Zero);
        Assert.That(OptimizerPatches.PatchCalls, Is.EqualTo(2));
    }

    [Test]
    public void Prefix_AlwaysTrue_RunsTarget()
    {
        ApplyPatch(typeof(OptimizerPatches), nameof(OptimizerPatches.Prefix_AlwaysTrue_RunsTarget));

        Assert.That(OptimizerPrefixTargets.PrefixAlwaysTrueTarget(-1), Is.EqualTo(-1));
        Assert.That(OptimizerPrefixTargets.PrefixAlwaysTrueTarget(1), Is.EqualTo(1));
        Assert.That(OptimizerPrefixTargets.PrefixTargetExecutions, Is.EqualTo(2));
        Assert.That(OptimizerPatches.PatchCalls, Is.EqualTo(2));
    }

    [Test]
    public void InnerPrefix_AlwaysFalse_SkipsInnerTarget()
    {
        ApplyPatch(typeof(OptimizerPatches), nameof(OptimizerPatches.InnerPrefix_AlwaysFalse_SkipsInnerTarget));

        Assert.That(OptimizerPrefixTargets.CallInnerAlwaysFalseTarget(-1), Is.EqualTo(-1));
        Assert.That(OptimizerPrefixTargets.CallInnerAlwaysFalseTarget(1), Is.EqualTo(42));
        Assert.That(OptimizerPrefixTargets.InnerTargetExecutions, Is.Zero);
        Assert.That(OptimizerPatches.PatchCalls, Is.EqualTo(1));
    }

    [Test]
    public void InnerPrefix_AlwaysTrue_RunsInnerTarget()
    {
        ApplyPatch(typeof(OptimizerPatches), nameof(OptimizerPatches.InnerPrefix_AlwaysTrue_RunsInnerTarget));

        Assert.That(OptimizerPrefixTargets.CallInnerAlwaysTrueTarget(-1), Is.EqualTo(-1));
        Assert.That(OptimizerPrefixTargets.CallInnerAlwaysTrueTarget(1), Is.EqualTo(1));
        Assert.That(OptimizerPrefixTargets.InnerTargetExecutions, Is.EqualTo(1));
        Assert.That(OptimizerPatches.PatchCalls, Is.EqualTo(1));
    }

    [Test]
    public void Prefix_ArgumentControlsWhetherTargetIsSkipped()
    {
        ApplyPatch(typeof(OptimizerPatches), nameof(OptimizerPatches.Prefix_ArgumentControlsWhetherTargetIsSkipped));

        Assert.That(OptimizerPrefixTargets.PrefixConditionallySkippedTarget(false), Is.EqualTo(1));
        Assert.That(OptimizerPrefixTargets.PrefixConditionallySkippedTarget(true), Is.EqualTo(42));
        Assert.That(OptimizerPrefixTargets.PrefixTargetExecutions, Is.EqualTo(1));
        Assert.That(OptimizerPatches.PatchCalls, Is.EqualTo(2));
    }

    [Test]
    public void InnerPrefix_OuterArgumentControlsWhetherInnerTargetIsSkipped()
    {
        ApplyPatch(
            typeof(OptimizerPatches),
            nameof(OptimizerPatches.InnerPrefix_OuterArgumentControlsWhetherInnerTargetIsSkipped));

        Assert.That(OptimizerPrefixTargets.CallInnerConditionallySkippedTarget(false), Is.EqualTo(1));
        Assert.That(OptimizerPrefixTargets.CallInnerConditionallySkippedTarget(true), Is.EqualTo(42));
        Assert.That(OptimizerPrefixTargets.InnerTargetExecutions, Is.EqualTo(1));
        Assert.That(OptimizerPatches.PatchCalls, Is.EqualTo(2));
    }
}
