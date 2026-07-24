namespace Disharmony.Tests;

public sealed class OptimizerNullPropagationNode
{
    public OptimizerNullPropagationNode? Next;
    public int Value;
}

public static class OptimizerControlFlowTargets
{
    public static int RightOperandCalls;

    public static string ConditionalBranches(int value)
    {
        if (value < 0)
            return "negative";
        if (value == 0)
            return "zero";
        return "positive";
    }

    public static int DenseSwitch(int value)
    {
        switch (value)
        {
            case 0: return 10;
            case 1: return 11;
            case 2: return 12;
            case 3: return 13;
            default: return 99;
        }
    }

    public static int LoopWithBreakAndContinue(int limit)
    {
        int total = 0;
        for (int value = 0; value < limit; value++)
        {
            if (value % 2 == 0)
                continue;
            if (value > 7)
                break;
            total += value;
        }

        return total;
    }

    public static bool ShortCircuit(bool left, bool right) =>
        left && EvaluateRight(right);

    private static bool EvaluateRight(bool value)
    {
        RightOperandCalls++;
        return value;
    }

    public static int RefLocalConditional(bool selectFirst)
    {
        int first = 1;
        int second = 2;
        ref int selected = ref (selectFirst ? ref first : ref second);
        selected = 42;
        return selectFirst ? first : second;
    }

    public static string PatternMatching(object? value) =>
        value switch
        {
            null => "null",
            int number when number > 0 => "positive integer",
            int => "non-positive integer",
            string { Length: 0 } => "empty string",
            string text => text,
            BindingReference { Value: 42 } => "reference with value 42",
            _ => "other",
        };

    public static int? NullPropagation(OptimizerNullPropagationNode? node) =>
        node?.Next?.Value;

    public static string NullCoalescingAssignment(string? value)
    {
        value ??= "fallback";
        return value;
    }

    public static int ForeachWithContinueAndEarlyReturn(IEnumerable<int> values)
    {
        int total = 0;
        foreach (int value in values)
        {
            if (value == 0)
                continue;
            if (value < 0)
                return value;
            total += value;
        }

        return total;
    }
}

public static class OptimizerExceptionTargets
{
    private sealed class TestDisposable : IDisposable
    {
        public void Dispose() => DisposalCount++;
    }

    private static readonly object SyncRoot = new();

    public static int FinallyExecutions;
    public static int DisposalCount;

    public static int TryCatch(bool throwException)
    {
        try
        {
            if (throwException)
                throw new InvalidOperationException();
            return 1;
        }
        catch (InvalidOperationException)
        {
            return 2;
        }
    }

    public static int TryFinally(bool returnEarly)
    {
        try
        {
            if (returnEarly)
                return 1;
            return 2;
        }
        finally
        {
            FinallyExecutions++;
        }
    }

    public static int NestedTryFinallyAndCatch(int mode)
    {
        try
        {
            try
            {
                if (mode == 0)
                    return 10;
                if (mode == 1)
                    throw new InvalidOperationException();
                throw new ArgumentException();
            }
            finally
            {
                FinallyExecutions++;
            }
        }
        catch (InvalidOperationException)
        {
            return 20;
        }
        catch (ArgumentException)
        {
            return 30;
        }
    }

    public static int ExceptionFilter(bool filterMatches)
    {
        try
        {
            throw new InvalidOperationException(filterMatches ? "match" : "other");
        }
        catch (InvalidOperationException exception) when (exception.Message == "match")
        {
            return 1;
        }
        catch (InvalidOperationException)
        {
            return 2;
        }
    }

    public static int UsingWithEarlyReturn(bool returnEarly)
    {
        using (new TestDisposable())
        {
            if (returnEarly)
                return 1;
            return 2;
        }
    }

    public static int LockWithConditionalReturn(bool returnEarly)
    {
        lock (SyncRoot)
        {
            if (returnEarly)
                return 1;
            return 2;
        }
    }

    public static int CatchAndRethrow(bool rethrow)
    {
        try
        {
            throw new InvalidOperationException("original");
        }
        catch (InvalidOperationException)
        {
            if (rethrow)
                throw;
            return 42;
        }
    }
}

public static class OptimizerPatches
{
    public static int PatchCalls;

    private static void RecordPatch() => PatchCalls++;

    [Prefix]
    [Target(typeof(OptimizerControlFlowTargets), nameof(OptimizerControlFlowTargets.ConditionalBranches))]
    public static void ConditionalBranches_PreservesEveryPath() => RecordPatch();

    [Prefix]
    [Target(typeof(OptimizerControlFlowTargets), nameof(OptimizerControlFlowTargets.DenseSwitch))]
    public static void DenseSwitch_PreservesCasesAndDefault() => RecordPatch();

    [Prefix]
    [Target(typeof(OptimizerControlFlowTargets), nameof(OptimizerControlFlowTargets.LoopWithBreakAndContinue))]
    public static void LoopWithBreakAndContinue_PreservesBackEdges() => RecordPatch();

    [Prefix]
    [Target(typeof(OptimizerControlFlowTargets), nameof(OptimizerControlFlowTargets.ShortCircuit))]
    public static void ShortCircuit_PreservesSkippedRightOperand() => RecordPatch();

    [Prefix]
    [Target(typeof(OptimizerControlFlowTargets), nameof(OptimizerControlFlowTargets.RefLocalConditional))]
    public static void RefLocalConditional_PreservesManagedPointerBranches() => RecordPatch();

    [Prefix]
    [Target(typeof(OptimizerControlFlowTargets), nameof(OptimizerControlFlowTargets.PatternMatching))]
    public static void PatternMatching_PreservesTypePropertyGuardAndNullPatterns() => RecordPatch();

    [Prefix]
    [Target(typeof(OptimizerControlFlowTargets), nameof(OptimizerControlFlowTargets.NullPropagation))]
    public static void NullPropagation_PreservesNullAtEveryLink() => RecordPatch();

    [Prefix]
    [Target(typeof(OptimizerControlFlowTargets), nameof(OptimizerControlFlowTargets.NullCoalescingAssignment))]
    public static void NullCoalescingAssignment_PreservesNullAndNonNullValues() => RecordPatch();

    [Prefix]
    [Target(typeof(OptimizerControlFlowTargets), nameof(OptimizerControlFlowTargets.ForeachWithContinueAndEarlyReturn))]
    public static void ForeachWithContinueAndEarlyReturn_PreservesLoopAndEnumeratorCleanup() => RecordPatch();

    [Prefix]
    [Target(typeof(OptimizerExceptionTargets), nameof(OptimizerExceptionTargets.TryCatch))]
    public static void TryCatch_PreservesNormalAndExceptionalPaths() => RecordPatch();

    [Prefix]
    [Target(typeof(OptimizerExceptionTargets), nameof(OptimizerExceptionTargets.TryFinally))]
    public static void TryFinally_PreservesReturnsAndFinallyExecution() => RecordPatch();

    [Prefix]
    [Target(typeof(OptimizerExceptionTargets), nameof(OptimizerExceptionTargets.NestedTryFinallyAndCatch))]
    public static void NestedTryFinallyAndCatch_PreservesExceptionRegionControlFlow() => RecordPatch();

    [Prefix]
    [Target(typeof(OptimizerExceptionTargets), nameof(OptimizerExceptionTargets.ExceptionFilter))]
    public static void ExceptionFilter_PreservesFilterAndFallbackHandlers() => RecordPatch();

    [Prefix]
    [Target(typeof(OptimizerExceptionTargets), nameof(OptimizerExceptionTargets.UsingWithEarlyReturn))]
    public static void UsingWithEarlyReturn_PreservesCompilerGeneratedFinally() => RecordPatch();

    [Prefix]
    [Target(typeof(OptimizerExceptionTargets), nameof(OptimizerExceptionTargets.LockWithConditionalReturn))]
    public static void LockWithConditionalReturn_PreservesCompilerGeneratedFinally() => RecordPatch();

    [Prefix]
    [Target(typeof(OptimizerExceptionTargets), nameof(OptimizerExceptionTargets.CatchAndRethrow))]
    public static void CatchAndRethrow_PreservesHandledAndRethrownPaths() => RecordPatch();
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
}
