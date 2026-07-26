namespace Disharmony.Tests;

public sealed class OptimizerNullPropagationNode
{
    public OptimizerNullPropagationNode? Next;
    public int Value;
}

public static class OptimizerControlFlowTargets
{
    public static int RightOperandCalls;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static string ConditionalBranches(int value)
    {
        if (value < 0)
            return "negative";
        if (value == 0)
            return "zero";
        return "positive";
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
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

    [MethodImpl(MethodImplOptions.NoInlining)]
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

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int ConditionalInfiniteLoop(bool loopForever)
    {
        if (loopForever)
        {
            while (true) { }
        }

        return 42;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool ShortCircuit(bool left, bool right) =>
        left && EvaluateRight(right);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool EvaluateRight(bool value)
    {
        RightOperandCalls++;
        return value;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int RefLocalConditional(bool selectFirst)
    {
        int first = 1;
        int second = 2;
        ref int selected = ref (selectFirst ? ref first : ref second);
        selected = 42;
        return selectFirst ? first : second;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
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

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int? NullPropagation(OptimizerNullPropagationNode? node) =>
        node?.Next?.Value;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static string NullCoalescingAssignment(string? value)
    {
        value ??= "fallback";
        return value;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
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
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void Dispose() => DisposalCount++;
    }

    private static readonly object SyncRoot = new();

    public static int FinallyExecutions;
    public static int DisposalCount;

    [MethodImpl(MethodImplOptions.NoInlining)]
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

    [MethodImpl(MethodImplOptions.NoInlining)]
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

    [MethodImpl(MethodImplOptions.NoInlining)]
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

    [MethodImpl(MethodImplOptions.NoInlining)]
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

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int UsingWithEarlyReturn(bool returnEarly)
    {
        using (new TestDisposable())
        {
            if (returnEarly)
                return 1;
            return 2;
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int LockWithConditionalReturn(bool returnEarly)
    {
        lock (SyncRoot)
        {
            if (returnEarly)
                return 1;
            return 2;
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
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

public static class OptimizerPrefixTargets
{
    public static int PrefixTargetExecutions;
    public static int InnerTargetExecutions;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int PrefixAlwaysFalseTarget(int value)
    {
        PrefixTargetExecutions++;
        if (value < 0)
            return -1;
        return 1;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int PrefixAlwaysTrueTarget(int value)
    {
        PrefixTargetExecutions++;
        if (value < 0)
            return -1;
        return 1;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int CallInnerAlwaysFalseTarget(int value)
    {
        if (value < 0)
            return -1;
        return InnerAlwaysFalseTarget(value);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int CallInnerAlwaysTrueTarget(int value)
    {
        if (value < 0)
            return -1;
        return InnerAlwaysTrueTarget(value);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int InnerAlwaysFalseTarget(int value)
    {
        InnerTargetExecutions++;
        return value;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int InnerAlwaysTrueTarget(int value)
    {
        InnerTargetExecutions++;
        return value;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int PrefixConditionallySkippedTarget(bool skip)
    {
        PrefixTargetExecutions++;
        return 1;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int CallInnerConditionallySkippedTarget(bool skip) =>
        InnerConditionallySkippedTarget();

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int InnerConditionallySkippedTarget()
    {
        InnerTargetExecutions++;
        return 1;
    }
}

