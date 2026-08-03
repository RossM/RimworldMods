namespace Disharmony.Tests;

public static class OptimizerPatches
{
    public static int PatchCalls;

    private static void RecordPatch() => PatchCalls++;

    [Prefix]
    [Target(typeof(OptimizerControlFlowTargets), nameof(OptimizerControlFlowTargets.ConditionalBranches))]
    [PatchOptions(PatchOptions.Optimize)]
    public static void ConditionalBranches_PreservesEveryPath() => RecordPatch();

    [Prefix]
    [Target(typeof(OptimizerControlFlowTargets), nameof(OptimizerControlFlowTargets.DenseSwitch))]
    [PatchOptions(PatchOptions.Optimize)]
    public static void DenseSwitch_PreservesCasesAndDefault() => RecordPatch();

    [Prefix]
    [Target(typeof(OptimizerControlFlowTargets), nameof(OptimizerControlFlowTargets.LoopWithBreakAndContinue))]
    [PatchOptions(PatchOptions.Optimize)]
    public static void LoopWithBreakAndContinue_PreservesBackEdges() => RecordPatch();

    [Prefix]
    [Target(typeof(OptimizerControlFlowTargets), nameof(OptimizerControlFlowTargets.ConditionalInfiniteLoop))]
    [PatchOptions(PatchOptions.Optimize)]
    public static void ConditionalInfiniteLoop_PreservesNonLoopingPath() => RecordPatch();

    [Prefix]
    [Target(typeof(OptimizerControlFlowTargets), nameof(OptimizerControlFlowTargets.ShortCircuit))]
    [PatchOptions(PatchOptions.Optimize)]
    public static void ShortCircuit_PreservesSkippedRightOperand() => RecordPatch();

    [Prefix]
    [Target(typeof(OptimizerControlFlowTargets), nameof(OptimizerControlFlowTargets.RefLocalConditional))]
    [PatchOptions(PatchOptions.Optimize)]
    public static void RefLocalConditional_PreservesManagedPointerBranches() => RecordPatch();

    [Prefix]
    [Target(typeof(OptimizerControlFlowTargets), nameof(OptimizerControlFlowTargets.PatternMatching))]
    [PatchOptions(PatchOptions.Optimize)]
    public static void PatternMatching_PreservesTypePropertyGuardAndNullPatterns() => RecordPatch();

    [Prefix]
    [Target(typeof(OptimizerControlFlowTargets), nameof(OptimizerControlFlowTargets.NullPropagation))]
    [PatchOptions(PatchOptions.Optimize)]
    public static void NullPropagation_PreservesNullAtEveryLink() => RecordPatch();

    [Prefix]
    [Target(typeof(OptimizerControlFlowTargets), nameof(OptimizerControlFlowTargets.NullCoalescingAssignment))]
    [PatchOptions(PatchOptions.Optimize)]
    public static void NullCoalescingAssignment_PreservesNullAndNonNullValues() => RecordPatch();

    [Prefix]
    [Target(typeof(OptimizerControlFlowTargets), nameof(OptimizerControlFlowTargets.ForeachWithContinueAndEarlyReturn))]
    [PatchOptions(PatchOptions.Optimize)]
    public static void ForeachWithContinueAndEarlyReturn_PreservesLoopAndEnumeratorCleanup() => RecordPatch();

    [Prefix]
    [Target(typeof(OptimizerExceptionTargets), nameof(OptimizerExceptionTargets.TryCatch))]
    [PatchOptions(PatchOptions.Optimize)]
    public static void TryCatch_PreservesNormalAndExceptionalPaths() => RecordPatch();

    [Prefix]
    [Target(typeof(OptimizerExceptionTargets), nameof(OptimizerExceptionTargets.TryFinally))]
    [PatchOptions(PatchOptions.Optimize)]
    public static void TryFinally_PreservesReturnsAndFinallyExecution() => RecordPatch();

    [Prefix]
    [Target(typeof(OptimizerExceptionTargets), nameof(OptimizerExceptionTargets.NestedTryFinallyAndCatch))]
    [PatchOptions(PatchOptions.Optimize)]
    public static void NestedTryFinallyAndCatch_PreservesExceptionRegionControlFlow() => RecordPatch();

    [Prefix]
    [Target(typeof(OptimizerExceptionTargets), nameof(OptimizerExceptionTargets.ExceptionFilter))]
    [PatchOptions(PatchOptions.Optimize)]
    public static void ExceptionFilter_PreservesFilterAndFallbackHandlers() => RecordPatch();

    [Prefix]
    [Target(typeof(OptimizerExceptionTargets), nameof(OptimizerExceptionTargets.UsingWithEarlyReturn))]
    [PatchOptions(PatchOptions.Optimize)]
    public static void UsingWithEarlyReturn_PreservesCompilerGeneratedFinally() => RecordPatch();

    [Prefix]
    [Target(typeof(OptimizerExceptionTargets), nameof(OptimizerExceptionTargets.LockWithConditionalReturn))]
    [PatchOptions(PatchOptions.Optimize)]
    public static void LockWithConditionalReturn_PreservesCompilerGeneratedFinally() => RecordPatch();

    [Prefix]
    [Target(typeof(OptimizerExceptionTargets), nameof(OptimizerExceptionTargets.CatchAndRethrow))]
    [PatchOptions(PatchOptions.Optimize)]
    public static void CatchAndRethrow_PreservesHandledAndRethrownPaths() => RecordPatch();

    [Prefix]
    [Target(
        typeof(OptimizerDataTargets),
        nameof(OptimizerDataTargets.PrimitiveArithmeticAndNumericConversions))]
    [PatchOptions(PatchOptions.Optimize)]
    public static void PrimitiveArithmeticAndNumericConversions_PreserveResults() => RecordPatch();

    [Prefix]
    [Target(typeof(OptimizerDataTargets), nameof(OptimizerDataTargets.CheckedNumericConversion))]
    [PatchOptions(PatchOptions.Optimize)]
    public static void CheckedNumericConversion_PreservesValueAndOverflow() => RecordPatch();

    [Prefix]
    [Target(typeof(OptimizerDataTargets), nameof(OptimizerDataTargets.Arrays))]
    [PatchOptions(PatchOptions.Optimize)]
    public static void Arrays_PreserveConstructionLengthAndElementAccess() => RecordPatch();

    [Prefix]
    [Target(typeof(OptimizerDataTargets), nameof(OptimizerDataTargets.Objects))]
    [PatchOptions(PatchOptions.Optimize)]
    public static void Objects_PreserveConstructionFieldAndPropertyAccess() => RecordPatch();

    [Prefix]
    [Target(typeof(OptimizerDataTargets), nameof(OptimizerDataTargets.StructCopyAndMutation))]
    [PatchOptions(PatchOptions.Optimize)]
    public static void Structs_PreserveCopyAndIndependentMutation() => RecordPatch();

    [Prefix]
    [Target(typeof(OptimizerDataTargets), nameof(OptimizerDataTargets.BoxingAndUnboxing))]
    [PatchOptions(PatchOptions.Optimize)]
    public static void BoxingAndUnboxing_PreserveRuntimeTypeAndValue() => RecordPatch();

    [Prefix]
    [Target(typeof(OptimizerDataTargets), nameof(OptimizerDataTargets.NullableValueOperations))]
    [PatchOptions(PatchOptions.Optimize)]
    public static void NullableValues_PreservePresentAndAbsentValues() => RecordPatch();

    [Prefix]
    [Target(typeof(OptimizerDataTargets), nameof(OptimizerDataTargets.GenericMethodCalls))]
    [PatchOptions(PatchOptions.Optimize)]
    public static void GenericCalls_PreservePrimitiveReferenceAndStructArguments() => RecordPatch();

    [Prefix]
    [Target(typeof(OptimizerDataTargets), nameof(OptimizerDataTargets.CapturingLambda))]
    [PatchOptions(PatchOptions.Optimize)]
    public static void CapturingLambda_PreservesClosureAndDelegateInvocation() => RecordPatch();

    [Prefix]
    [Target(typeof(OptimizerDataTargets), nameof(OptimizerDataTargets.StringInterpolation))]
    [PatchOptions(PatchOptions.Optimize)]
    public static void StringInterpolation_PreservesFormatting() => RecordPatch();

    [Prefix]
    [Target(typeof(OptimizerDataTargets), nameof(OptimizerDataTargets.TupleConstructionAndDeconstruction))]
    [PatchOptions(PatchOptions.Optimize)]
    public static void Tuples_PreserveConstructionAndDeconstruction() => RecordPatch();

    [Prefix]
    [Target(typeof(OptimizerInstanceDataTargets), nameof(OptimizerInstanceDataTargets.SetMembers))]
    [PatchOptions(PatchOptions.Optimize)]
    public static void InstanceMembers_PreserveFieldAndPropertyMutation() => RecordPatch();

    [Prefix]
    [Target(typeof(OptimizerDataTargets), nameof(OptimizerDataTargets.InterfaceDispatch))]
    [PatchOptions(PatchOptions.Optimize)]
    public static void InterfaceDispatch_PreservesImplementationCall() => RecordPatch();

    [Prefix]
    [Target(typeof(OptimizerMixedTargets), nameof(OptimizerMixedTargets.ConditionalReferenceType))]
    [PatchOptions(PatchOptions.Optimize)]
    public static void ConditionalReferenceType_PreservesConcreteTypeAndBaseMembers() => RecordPatch();

    [Prefix]
    [Target(typeof(OptimizerMixedTargets), nameof(OptimizerMixedTargets.ConditionalInterfaceImplementation))]
    [PatchOptions(PatchOptions.Optimize)]
    public static void ConditionalInterfaceImplementation_PreservesInterfaceDispatch() => RecordPatch();

    [Prefix]
    [Target(typeof(OptimizerMixedTargets), nameof(OptimizerMixedTargets.ConditionalBoxing))]
    [PatchOptions(PatchOptions.Optimize)]
    public static void ConditionalBoxing_PreservesValueAndReferenceAlternatives() => RecordPatch();

    [Prefix]
    [Target(typeof(OptimizerMixedTargets), nameof(OptimizerMixedTargets.ConditionalStructCopy))]
    [PatchOptions(PatchOptions.Optimize)]
    public static void ConditionalStructCopy_PreservesSelectionAndMutation() => RecordPatch();

    [Prefix]
    [Target(typeof(OptimizerMixedTargets), nameof(OptimizerMixedTargets.SwitchWithNumericConversions))]
    [PatchOptions(PatchOptions.Optimize)]
    public static void SwitchWithNumericConversions_PreservesEveryConvertedValue() => RecordPatch();

    [Prefix]
    [Target(typeof(OptimizerMixedTargets), nameof(OptimizerMixedTargets.LoopOverArray))]
    [PatchOptions(PatchOptions.Optimize)]
    public static void LoopOverArray_PreservesElementsAndObjectMemberUpdates() => RecordPatch();

    [Prefix]
    [Target(typeof(OptimizerMixedTargets), nameof(OptimizerMixedTargets.TryCatchWithReferenceLocal))]
    [PatchOptions(PatchOptions.Optimize)]
    public static void TryCatchWithReferenceLocal_PreservesNormalAndFallbackObjects() => RecordPatch();

    [Prefix]
    [Target(typeof(OptimizerMixedTargets), nameof(OptimizerMixedTargets.TryFinallyWithStructLocal))]
    [PatchOptions(PatchOptions.Optimize)]
    public static void TryFinallyWithStructLocal_PreservesBranchAndFinallyMutations() => RecordPatch();

    [Prefix]
    [Target(typeof(OptimizerMixedTargets), nameof(OptimizerMixedTargets.ConditionalDelegate))]
    [PatchOptions(PatchOptions.Optimize)]
    public static void ConditionalDelegate_PreservesCapturingAndStaticAlternatives() => RecordPatch();

    [Prefix]
    [Target(typeof(OptimizerMixedTargets), nameof(OptimizerMixedTargets.ConditionalRefToObjectField))]
    [PatchOptions(PatchOptions.Optimize)]
    public static void ConditionalRefToObjectField_PreservesSelectedFieldMutation() => RecordPatch();

    [Prefix]
    [Target(typeof(OptimizerMixedTargets), nameof(OptimizerMixedTargets.ExplicitReferenceCastOnLocal))]
    [PatchOptions(PatchOptions.Optimize)]
    public static void ExplicitReferenceCast_Local_SuccessAndFailure() => RecordPatch();

    [Prefix]
    [Target(typeof(OptimizerMixedTargets), nameof(OptimizerMixedTargets.ExplicitReferenceCastOnEvaluationStack))]
    [PatchOptions(PatchOptions.Optimize)]
    public static void ExplicitReferenceCast_EvaluationStack_SuccessAndFailure() => RecordPatch();

    [Prefix]
    [Target(typeof(OptimizerMixedTargets), nameof(OptimizerMixedTargets.ExplicitUnboxingCastOnLocal))]
    [PatchOptions(PatchOptions.Optimize)]
    public static void ExplicitUnboxingCast_Local_SuccessAndFailure() => RecordPatch();

    [Prefix]
    [Target(typeof(OptimizerMixedTargets), nameof(OptimizerMixedTargets.ExplicitUnboxingCastOnEvaluationStack))]
    [PatchOptions(PatchOptions.Optimize)]
    public static void ExplicitUnboxingCast_EvaluationStack_SuccessAndFailure() => RecordPatch();

    [Prefix]
    [Target(typeof(OptimizerMixedTargets), nameof(OptimizerMixedTargets.IsOperatorsOnLocal))]
    [PatchOptions(PatchOptions.Optimize)]
    public static void Is_Local_RecognizesEachRuntimeType() => RecordPatch();

    [Prefix]
    [Target(typeof(OptimizerMixedTargets), nameof(OptimizerMixedTargets.AsClassOnLocal))]
    [PatchOptions(PatchOptions.Optimize)]
    public static void AsClass_Local_SuccessAndFailure() => RecordPatch();

    [Prefix]
    [Target(typeof(OptimizerMixedTargets), nameof(OptimizerMixedTargets.AsInterfaceOnLocal))]
    [PatchOptions(PatchOptions.Optimize)]
    public static void AsInterface_Local_SuccessAndFailure() => RecordPatch();

}

[TestFixture]
public sealed class OptimizerTests : PatchTestBase
{
    [SetUp]
    public void EnableOptimizer()
    {
        Patcher.Instance.optimizerEnabled = true;
        OptimizerPatches.PatchCalls = 0;
        OptimizerInlinePatches.PatchCalls = 0;
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

    private static void ApplyInlinePatch(
        string patchMethodName,
        PatchType patchType,
        MethodBase target,
        MemberInfo? innerTarget = null)
    {
        MethodInfo patch = typeof(OptimizerInlinePatches).GetMethod(patchMethodName)!;
        Autopatcher.Patch(
            patch,
            patchType,
            innerTarget: innerTarget,
            options: PatchOptions.Inline | PatchOptions.Optimize,
            targets: [target]);
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
        ApplyInlinePatch(
            nameof(OptimizerInlinePatches.Prefix_AlwaysFalse_SkipsTarget),
            PatchType.Prefix,
            typeof(OptimizerPrefixTargets).GetMethod(nameof(OptimizerPrefixTargets.PrefixAlwaysFalseTarget))!);

        Assert.That(OptimizerPrefixTargets.PrefixAlwaysFalseTarget(-1), Is.EqualTo(42));
        Assert.That(OptimizerPrefixTargets.PrefixAlwaysFalseTarget(1), Is.EqualTo(42));
        Assert.That(OptimizerPrefixTargets.PrefixTargetExecutions, Is.Zero);
        Assert.That(OptimizerInlinePatches.PatchCalls, Is.EqualTo(2));
    }

    [Test]
    public void Prefix_AlwaysTrue_RunsTarget()
    {
        ApplyInlinePatch(
            nameof(OptimizerInlinePatches.Prefix_AlwaysTrue_RunsTarget),
            PatchType.Prefix,
            typeof(OptimizerPrefixTargets).GetMethod(nameof(OptimizerPrefixTargets.PrefixAlwaysTrueTarget))!);

        Assert.That(OptimizerPrefixTargets.PrefixAlwaysTrueTarget(-1), Is.EqualTo(-1));
        Assert.That(OptimizerPrefixTargets.PrefixAlwaysTrueTarget(1), Is.EqualTo(1));
        Assert.That(OptimizerPrefixTargets.PrefixTargetExecutions, Is.EqualTo(2));
        Assert.That(OptimizerInlinePatches.PatchCalls, Is.EqualTo(2));
    }

    [Test]
    public void InnerPrefix_AlwaysFalse_SkipsInnerTarget()
    {
        ApplyInlinePatch(
            nameof(OptimizerInlinePatches.InnerPrefix_AlwaysFalse_SkipsInnerTarget),
            PatchType.InnerPrefix,
            typeof(OptimizerPrefixTargets).GetMethod(nameof(OptimizerPrefixTargets.CallInnerAlwaysFalseTarget))!,
            typeof(OptimizerPrefixTargets).GetMethod(nameof(OptimizerPrefixTargets.InnerAlwaysFalseTarget))!);

        Assert.That(OptimizerPrefixTargets.CallInnerAlwaysFalseTarget(-1), Is.EqualTo(-1));
        Assert.That(OptimizerPrefixTargets.CallInnerAlwaysFalseTarget(1), Is.EqualTo(42));
        Assert.That(OptimizerPrefixTargets.InnerTargetExecutions, Is.Zero);
        Assert.That(OptimizerInlinePatches.PatchCalls, Is.EqualTo(1));
    }

    [Test]
    public void InnerPrefix_AlwaysTrue_RunsInnerTarget()
    {
        ApplyInlinePatch(
            nameof(OptimizerInlinePatches.InnerPrefix_AlwaysTrue_RunsInnerTarget),
            PatchType.InnerPrefix,
            typeof(OptimizerPrefixTargets).GetMethod(nameof(OptimizerPrefixTargets.CallInnerAlwaysTrueTarget))!,
            typeof(OptimizerPrefixTargets).GetMethod(nameof(OptimizerPrefixTargets.InnerAlwaysTrueTarget))!);

        Assert.That(OptimizerPrefixTargets.CallInnerAlwaysTrueTarget(-1), Is.EqualTo(-1));
        Assert.That(OptimizerPrefixTargets.CallInnerAlwaysTrueTarget(1), Is.EqualTo(1));
        Assert.That(OptimizerPrefixTargets.InnerTargetExecutions, Is.EqualTo(1));
        Assert.That(OptimizerInlinePatches.PatchCalls, Is.EqualTo(1));
    }

    [Test]
    public void Prefix_ArgumentControlsWhetherTargetIsSkipped()
    {
        ApplyInlinePatch(
            nameof(OptimizerInlinePatches.Prefix_ArgumentControlsWhetherTargetIsSkipped),
            PatchType.Prefix,
            typeof(OptimizerPrefixTargets).GetMethod(nameof(OptimizerPrefixTargets.PrefixConditionallySkippedTarget))!);

        Assert.That(OptimizerPrefixTargets.PrefixConditionallySkippedTarget(false), Is.EqualTo(1));
        Assert.That(OptimizerPrefixTargets.PrefixConditionallySkippedTarget(true), Is.EqualTo(42));
        Assert.That(OptimizerPrefixTargets.PrefixTargetExecutions, Is.EqualTo(1));
        Assert.That(OptimizerInlinePatches.PatchCalls, Is.EqualTo(2));
    }

    [Test]
    public void InnerPrefix_OuterArgumentControlsWhetherInnerTargetIsSkipped()
    {
        ApplyInlinePatch(
            nameof(OptimizerInlinePatches.InnerPrefix_OuterArgumentControlsWhetherInnerTargetIsSkipped),
            PatchType.InnerPrefix,
            typeof(OptimizerPrefixTargets).GetMethod(nameof(OptimizerPrefixTargets.CallInnerConditionallySkippedTarget))!,
            typeof(OptimizerPrefixTargets).GetMethod(nameof(OptimizerPrefixTargets.InnerConditionallySkippedTarget))!);

        Assert.That(OptimizerPrefixTargets.CallInnerConditionallySkippedTarget(false), Is.EqualTo(1));
        Assert.That(OptimizerPrefixTargets.CallInnerConditionallySkippedTarget(true), Is.EqualTo(42));
        Assert.That(OptimizerPrefixTargets.InnerTargetExecutions, Is.EqualTo(1));
        Assert.That(OptimizerInlinePatches.PatchCalls, Is.EqualTo(2));
    }

    [Test]
    public void PrimitiveArithmeticAndNumericConversions_PreserveResults()
    {
        ApplyPatch(
            typeof(OptimizerPatches),
            nameof(OptimizerPatches.PrimitiveArithmeticAndNumericConversions_PreserveResults));

        var result = OptimizerDataTargets.PrimitiveArithmeticAndNumericConversions(300, 4);

        Assert.That(result.Sum, Is.EqualTo(304));
        Assert.That(result.Product, Is.EqualTo(1200L));
        Assert.That(result.Quotient, Is.EqualTo(75.0));
        Assert.That(result.Narrowed, Is.EqualTo(44));
        Assert.That(OptimizerPatches.PatchCalls, Is.EqualTo(1));
    }

    [Test]
    public void CheckedNumericConversion_PreservesValueAndOverflow()
    {
        ApplyPatch(
            typeof(OptimizerPatches),
            nameof(OptimizerPatches.CheckedNumericConversion_PreservesValueAndOverflow));

        Assert.That(OptimizerDataTargets.CheckedNumericConversion(42L), Is.EqualTo(42));
        Assert.Throws<OverflowException>(() => OptimizerDataTargets.CheckedNumericConversion(long.MaxValue));
        Assert.That(OptimizerPatches.PatchCalls, Is.EqualTo(2));
    }

    [Test]
    public void Arrays_PreserveConstructionLengthAndElementAccess()
    {
        ApplyPatch(typeof(OptimizerPatches), nameof(OptimizerPatches.Arrays_PreserveConstructionLengthAndElementAccess));

        int[] result = OptimizerDataTargets.Arrays(7, 11);

        Assert.That(result, Has.Length.EqualTo(2));
        Assert.That(result[0], Is.EqualTo(11));
        Assert.That(result[1], Is.EqualTo(7));
        Assert.That(OptimizerPatches.PatchCalls, Is.EqualTo(1));
    }

    [Test]
    public void Objects_PreserveConstructionFieldAndPropertyAccess()
    {
        ApplyPatch(
            typeof(OptimizerPatches),
            nameof(OptimizerPatches.Objects_PreserveConstructionFieldAndPropertyAccess));

        OptimizerDataObject result = OptimizerDataTargets.Objects(42, "text");

        Assert.That(result.Number, Is.EqualTo(42));
        Assert.That(result.Text, Is.EqualTo("text"));
        Assert.That(OptimizerPatches.PatchCalls, Is.EqualTo(1));
    }

    [Test]
    public void Structs_PreserveCopyAndIndependentMutation()
    {
        ApplyPatch(typeof(OptimizerPatches), nameof(OptimizerPatches.Structs_PreserveCopyAndIndependentMutation));

        var result = OptimizerDataTargets.StructCopyAndMutation(7, "original");

        Assert.That(result.Original.Number, Is.EqualTo(7));
        Assert.That(result.Original.Text, Is.EqualTo("original"));
        Assert.That(result.Copy.Number, Is.EqualTo(42));
        Assert.That(result.Copy.Text, Is.EqualTo("copy"));
        Assert.That(OptimizerPatches.PatchCalls, Is.EqualTo(1));
    }

    [Test]
    public void BoxingAndUnboxing_PreserveRuntimeTypeAndValue()
    {
        ApplyPatch(typeof(OptimizerPatches), nameof(OptimizerPatches.BoxingAndUnboxing_PreserveRuntimeTypeAndValue));

        var result = OptimizerDataTargets.BoxingAndUnboxing(42);

        Assert.That(result.Boxed, Is.TypeOf<int>());
        Assert.That(result.Boxed, Is.EqualTo(42));
        Assert.That(result.Unboxed, Is.EqualTo(42));
        Assert.That(OptimizerPatches.PatchCalls, Is.EqualTo(1));
    }

    [Test]
    public void NullableValues_PreservePresentAndAbsentValues()
    {
        ApplyPatch(
            typeof(OptimizerPatches),
            nameof(OptimizerPatches.NullableValues_PreservePresentAndAbsentValues));

        var present = OptimizerDataTargets.NullableValueOperations(42);
        var absent = OptimizerDataTargets.NullableValueOperations(null);

        Assert.That(present.HasValue, Is.True);
        Assert.That(present.Value, Is.EqualTo(42));
        Assert.That(absent.HasValue, Is.False);
        Assert.That(absent.Value, Is.Zero);
        Assert.That(OptimizerPatches.PatchCalls, Is.EqualTo(2));
    }

    [Test]
    public void GenericCalls_PreservePrimitiveReferenceAndStructArguments()
    {
        ApplyPatch(
            typeof(OptimizerPatches),
            nameof(OptimizerPatches.GenericCalls_PreservePrimitiveReferenceAndStructArguments));
        var structure = new OptimizerDataStruct
        {
            Number = 42,
            Text = "structure",
        };

        var result = OptimizerDataTargets.GenericMethodCalls(7, "reference", structure);

        Assert.That(result.Primitive, Is.EqualTo(7));
        Assert.That(result.Reference, Is.EqualTo("reference"));
        Assert.That(result.Structure.Number, Is.EqualTo(42));
        Assert.That(result.Structure.Text, Is.EqualTo("structure"));
        Assert.That(OptimizerPatches.PatchCalls, Is.EqualTo(1));
    }

    [Test]
    public void CapturingLambda_PreservesClosureAndDelegateInvocation()
    {
        ApplyPatch(
            typeof(OptimizerPatches),
            nameof(OptimizerPatches.CapturingLambda_PreservesClosureAndDelegateInvocation));

        Assert.That(OptimizerDataTargets.CapturingLambda(40, 2), Is.EqualTo(42));
        Assert.That(OptimizerPatches.PatchCalls, Is.EqualTo(1));
    }

    [Test]
    public void StringInterpolation_PreservesFormatting()
    {
        ApplyPatch(typeof(OptimizerPatches), nameof(OptimizerPatches.StringInterpolation_PreservesFormatting));

        Assert.That(OptimizerDataTargets.StringInterpolation("value", 42), Is.EqualTo("value: 0042"));
        Assert.That(OptimizerPatches.PatchCalls, Is.EqualTo(1));
    }

    [Test]
    public void Tuples_PreserveConstructionAndDeconstruction()
    {
        ApplyPatch(
            typeof(OptimizerPatches),
            nameof(OptimizerPatches.Tuples_PreserveConstructionAndDeconstruction));

        var result = OptimizerDataTargets.TupleConstructionAndDeconstruction(42, "text");

        Assert.That(result.Text, Is.EqualTo("text"));
        Assert.That(result.Number, Is.EqualTo(42));
        Assert.That(OptimizerPatches.PatchCalls, Is.EqualTo(1));
    }

    [Test]
    public void InstanceMembers_PreserveFieldAndPropertyMutation()
    {
        ApplyPatch(typeof(OptimizerPatches), nameof(OptimizerPatches.InstanceMembers_PreserveFieldAndPropertyMutation));
        var target = new OptimizerInstanceDataTargets();

        var result = target.SetMembers(42, "text");

        Assert.That(target.Number, Is.EqualTo(42));
        Assert.That(target.Text, Is.EqualTo("text"));
        Assert.That(result.Number, Is.EqualTo(42));
        Assert.That(result.Text, Is.EqualTo("text"));
        Assert.That(OptimizerPatches.PatchCalls, Is.EqualTo(1));
    }

    [Test]
    public void InterfaceDispatch_PreservesImplementationCall()
    {
        ApplyPatch(typeof(OptimizerPatches), nameof(OptimizerPatches.InterfaceDispatch_PreservesImplementationCall));

        Assert.That(OptimizerDataTargets.InterfaceDispatch(42), Is.EqualTo(42));
        Assert.That(OptimizerPatches.PatchCalls, Is.EqualTo(1));
    }

    [Test]
    public void ConditionalReferenceType_PreservesConcreteTypeAndBaseMembers()
    {
        ApplyPatch(
            typeof(OptimizerPatches),
            nameof(OptimizerPatches.ConditionalReferenceType_PreservesConcreteTypeAndBaseMembers));

        OptimizerBranchValue first = OptimizerMixedTargets.ConditionalReferenceType(true);
        OptimizerBranchValue second = OptimizerMixedTargets.ConditionalReferenceType(false);

        Assert.That(first, Is.TypeOf<OptimizerFirstBranchValue>());
        Assert.That(first.Number, Is.EqualTo(7));
        Assert.That(second, Is.TypeOf<OptimizerSecondBranchValue>());
        Assert.That(second.Number, Is.EqualTo(11));
        Assert.That(OptimizerPatches.PatchCalls, Is.EqualTo(2));
    }

    [Test]
    public void ConditionalInterfaceImplementation_PreservesInterfaceDispatch()
    {
        ApplyPatch(
            typeof(OptimizerPatches),
            nameof(OptimizerPatches.ConditionalInterfaceImplementation_PreservesInterfaceDispatch));

        Assert.That(OptimizerMixedTargets.ConditionalInterfaceImplementation(true), Is.EqualTo(7));
        Assert.That(OptimizerMixedTargets.ConditionalInterfaceImplementation(false), Is.EqualTo(11));
        Assert.That(OptimizerPatches.PatchCalls, Is.EqualTo(2));
    }

    [Test]
    public void ConditionalBoxing_PreservesValueAndReferenceAlternatives()
    {
        ApplyPatch(
            typeof(OptimizerPatches),
            nameof(OptimizerPatches.ConditionalBoxing_PreservesValueAndReferenceAlternatives));

        object number = OptimizerMixedTargets.ConditionalBoxing(true);
        object text = OptimizerMixedTargets.ConditionalBoxing(false);

        Assert.That(number, Is.TypeOf<int>());
        Assert.That(number, Is.EqualTo(42));
        Assert.That(text, Is.TypeOf<string>());
        Assert.That(text, Is.EqualTo("text"));
        Assert.That(OptimizerPatches.PatchCalls, Is.EqualTo(2));
    }

    [Test]
    public void ConditionalStructCopy_PreservesSelectionAndMutation()
    {
        ApplyPatch(
            typeof(OptimizerPatches),
            nameof(OptimizerPatches.ConditionalStructCopy_PreservesSelectionAndMutation));

        OptimizerDataStruct first = OptimizerMixedTargets.ConditionalStructCopy(true);
        OptimizerDataStruct second = OptimizerMixedTargets.ConditionalStructCopy(false);

        Assert.That(first.Number, Is.EqualTo(8));
        Assert.That(first.Text, Is.EqualTo("FIRST"));
        Assert.That(second.Number, Is.EqualTo(12));
        Assert.That(second.Text, Is.EqualTo("SECOND"));
        Assert.That(OptimizerPatches.PatchCalls, Is.EqualTo(2));
    }

    [Test]
    public void SwitchWithNumericConversions_PreservesEveryConvertedValue()
    {
        ApplyPatch(
            typeof(OptimizerPatches),
            nameof(OptimizerPatches.SwitchWithNumericConversions_PreservesEveryConvertedValue));

        Assert.That(OptimizerMixedTargets.SwitchWithNumericConversions(0, 8), Is.EqualTo(8.0));
        Assert.That(OptimizerMixedTargets.SwitchWithNumericConversions(1, 8), Is.EqualTo(16.0));
        Assert.That(OptimizerMixedTargets.SwitchWithNumericConversions(2, 8), Is.EqualTo(4.0));
        Assert.That(OptimizerMixedTargets.SwitchWithNumericConversions(3, 8), Is.EqualTo(2.0));
        Assert.That(OptimizerPatches.PatchCalls, Is.EqualTo(4));
    }

    [Test]
    public void LoopOverArray_PreservesElementsAndObjectMemberUpdates()
    {
        ApplyPatch(
            typeof(OptimizerPatches),
            nameof(OptimizerPatches.LoopOverArray_PreservesElementsAndObjectMemberUpdates));

        OptimizerDataObject result = OptimizerMixedTargets.LoopOverArray([1, 2, 3]);

        Assert.That(result.Number, Is.EqualTo(6));
        Assert.That(result.Text, Is.EqualTo("2: 3"));
        Assert.That(OptimizerPatches.PatchCalls, Is.EqualTo(1));
    }

    [Test]
    public void TryCatchWithReferenceLocal_PreservesNormalAndFallbackObjects()
    {
        ApplyPatch(
            typeof(OptimizerPatches),
            nameof(OptimizerPatches.TryCatchWithReferenceLocal_PreservesNormalAndFallbackObjects));

        OptimizerDataObject parsed = OptimizerMixedTargets.TryCatchWithReferenceLocal("42");
        OptimizerDataObject fallback = OptimizerMixedTargets.TryCatchWithReferenceLocal("not a number");

        Assert.That(parsed.Number, Is.EqualTo(42));
        Assert.That(parsed.Text, Is.EqualTo("parsed"));
        Assert.That(fallback.Number, Is.EqualTo(-1));
        Assert.That(fallback.Text, Is.EqualTo("fallback"));
        Assert.That(OptimizerPatches.PatchCalls, Is.EqualTo(2));
    }

    [Test]
    public void TryFinallyWithStructLocal_PreservesBranchAndFinallyMutations()
    {
        ApplyPatch(
            typeof(OptimizerPatches),
            nameof(OptimizerPatches.TryFinallyWithStructLocal_PreservesBranchAndFinallyMutations));

        OptimizerDataStruct first = OptimizerMixedTargets.TryFinallyWithStructLocal(true);
        OptimizerDataStruct second = OptimizerMixedTargets.TryFinallyWithStructLocal(false);

        Assert.That(first.Number, Is.EqualTo(8));
        Assert.That(first.Text, Is.EqualTo("FIRST"));
        Assert.That(second.Number, Is.EqualTo(12));
        Assert.That(second.Text, Is.EqualTo("SECOND"));
        Assert.That(OptimizerPatches.PatchCalls, Is.EqualTo(2));
    }

    [Test]
    public void ConditionalDelegate_PreservesCapturingAndStaticAlternatives()
    {
        ApplyPatch(
            typeof(OptimizerPatches),
            nameof(OptimizerPatches.ConditionalDelegate_PreservesCapturingAndStaticAlternatives));

        Assert.That(OptimizerMixedTargets.ConditionalDelegate(true, 40), Is.EqualTo(42));
        Assert.That(OptimizerMixedTargets.ConditionalDelegate(false, 21), Is.EqualTo(42));
        Assert.That(OptimizerPatches.PatchCalls, Is.EqualTo(2));
    }

    [Test]
    public void ConditionalRefToObjectField_PreservesSelectedFieldMutation()
    {
        ApplyPatch(
            typeof(OptimizerPatches),
            nameof(OptimizerPatches.ConditionalRefToObjectField_PreservesSelectedFieldMutation));

        var first = OptimizerMixedTargets.ConditionalRefToObjectField(true);
        var second = OptimizerMixedTargets.ConditionalRefToObjectField(false);

        Assert.That(first.First, Is.EqualTo(42));
        Assert.That(first.Second, Is.EqualTo(11));
        Assert.That(second.First, Is.EqualTo(7));
        Assert.That(second.Second, Is.EqualTo(42));
        Assert.That(OptimizerPatches.PatchCalls, Is.EqualTo(2));
    }

    [Test]
    public void ExplicitReferenceCast_Local_SuccessAndFailure()
    {
        ApplyPatch(
            typeof(OptimizerPatches),
            nameof(OptimizerPatches.ExplicitReferenceCast_Local_SuccessAndFailure));

        Assert.That(OptimizerMixedTargets.ExplicitReferenceCastOnLocal(true), Is.EqualTo(7));
        Assert.Throws<InvalidCastException>(() => OptimizerMixedTargets.ExplicitReferenceCastOnLocal(false));
        Assert.That(OptimizerPatches.PatchCalls, Is.EqualTo(2));
    }

    [Test]
    public void ExplicitReferenceCast_EvaluationStack_SuccessAndFailure()
    {
        ApplyPatch(
            typeof(OptimizerPatches),
            nameof(OptimizerPatches.ExplicitReferenceCast_EvaluationStack_SuccessAndFailure));

        Assert.That(OptimizerMixedTargets.ExplicitReferenceCastOnEvaluationStack(true), Is.EqualTo(7));
        Assert.Throws<InvalidCastException>(
            () => OptimizerMixedTargets.ExplicitReferenceCastOnEvaluationStack(false));
        Assert.That(OptimizerPatches.PatchCalls, Is.EqualTo(2));
    }

    [Test]
    public void ExplicitUnboxingCast_Local_SuccessAndFailure()
    {
        ApplyPatch(
            typeof(OptimizerPatches),
            nameof(OptimizerPatches.ExplicitUnboxingCast_Local_SuccessAndFailure));

        Assert.That(OptimizerMixedTargets.ExplicitUnboxingCastOnLocal(true), Is.EqualTo(42));
        Assert.Throws<InvalidCastException>(() => OptimizerMixedTargets.ExplicitUnboxingCastOnLocal(false));
        Assert.That(OptimizerPatches.PatchCalls, Is.EqualTo(2));
    }

    [Test]
    public void ExplicitUnboxingCast_EvaluationStack_SuccessAndFailure()
    {
        ApplyPatch(
            typeof(OptimizerPatches),
            nameof(OptimizerPatches.ExplicitUnboxingCast_EvaluationStack_SuccessAndFailure));

        Assert.That(OptimizerMixedTargets.ExplicitUnboxingCastOnEvaluationStack(true), Is.EqualTo(42));
        Assert.Throws<InvalidCastException>(
            () => OptimizerMixedTargets.ExplicitUnboxingCastOnEvaluationStack(false));
        Assert.That(OptimizerPatches.PatchCalls, Is.EqualTo(2));
    }

    [Test]
    public void Is_Local_RecognizesEachRuntimeType()
    {
        ApplyPatch(typeof(OptimizerPatches), nameof(OptimizerPatches.Is_Local_RecognizesEachRuntimeType));

        var text = OptimizerMixedTargets.IsOperatorsOnLocal(0);
        var number = OptimizerMixedTargets.IsOperatorsOnLocal(1);
        var dataObject = OptimizerMixedTargets.IsOperatorsOnLocal(2);

        Assert.That(text.IsString, Is.True);
        Assert.That(text.IsInt, Is.False);
        Assert.That(text.IsDataObject, Is.False);
        Assert.That(number.IsString, Is.False);
        Assert.That(number.IsInt, Is.True);
        Assert.That(number.IsDataObject, Is.False);
        Assert.That(dataObject.IsString, Is.False);
        Assert.That(dataObject.IsInt, Is.False);
        Assert.That(dataObject.IsDataObject, Is.True);
        Assert.That(OptimizerPatches.PatchCalls, Is.EqualTo(3));
    }

    [Test]
    public void AsClass_Local_SuccessAndFailure()
    {
        ApplyPatch(typeof(OptimizerPatches), nameof(OptimizerPatches.AsClass_Local_SuccessAndFailure));

        Assert.That(OptimizerMixedTargets.AsClassOnLocal(true), Is.EqualTo(7));
        Assert.That(OptimizerMixedTargets.AsClassOnLocal(false), Is.Null);
        Assert.That(OptimizerPatches.PatchCalls, Is.EqualTo(2));
    }

    [Test]
    public void AsInterface_Local_SuccessAndFailure()
    {
        ApplyPatch(typeof(OptimizerPatches), nameof(OptimizerPatches.AsInterface_Local_SuccessAndFailure));

        Assert.That(OptimizerMixedTargets.AsInterfaceOnLocal(true), Is.EqualTo(7));
        Assert.That(OptimizerMixedTargets.AsInterfaceOnLocal(false), Is.Null);
        Assert.That(OptimizerPatches.PatchCalls, Is.EqualTo(2));
    }

    [Test]
    public void InlinePrefix_Is_Local_KnownSuccess()
    {
        ApplyInlinePatch(
            nameof(OptimizerInlinePatches.InlinePrefix_Is_Local_KnownSuccess),
            PatchType.Prefix,
            typeof(OptimizerMixedTargets).GetMethod(nameof(OptimizerMixedTargets.IsOperatorOnLocal))!);

        Assert.That(OptimizerMixedTargets.IsOperatorOnLocal(false), Is.True);
        Assert.That(OptimizerMixedTargets.IsOperatorOnLocal(true), Is.True);
        Assert.That(OptimizerInlinePatches.PatchCalls, Is.EqualTo(2));
    }

    [Test]
    public void InlinePrefix_Is_Local_KnownFailure()
    {
        ApplyInlinePatch(
            nameof(OptimizerInlinePatches.InlinePrefix_Is_Local_KnownFailure),
            PatchType.Prefix,
            typeof(OptimizerMixedTargets).GetMethod(nameof(OptimizerMixedTargets.IsOperatorOnLocal))!);

        Assert.That(OptimizerMixedTargets.IsOperatorOnLocal(false), Is.False);
        Assert.That(OptimizerMixedTargets.IsOperatorOnLocal(true), Is.False);
        Assert.That(OptimizerInlinePatches.PatchCalls, Is.EqualTo(2));
    }

    [Test]
    public void InlinePrefix_Is_EvaluationStack_KnownSuccess()
    {
        ApplyInlinePatch(
            nameof(OptimizerInlinePatches.InlinePrefix_Is_EvaluationStack_KnownSuccess),
            PatchType.Prefix,
            typeof(OptimizerMixedTargets).GetMethod(nameof(OptimizerMixedTargets.IsOperatorOnEvaluationStack))!);

        Assert.That(OptimizerMixedTargets.IsOperatorOnEvaluationStack(false), Is.True);
        Assert.That(OptimizerMixedTargets.IsOperatorOnEvaluationStack(true), Is.True);
        Assert.That(OptimizerInlinePatches.PatchCalls, Is.EqualTo(2));
    }

    [Test]
    public void InlinePrefix_Is_EvaluationStack_KnownFailure()
    {
        ApplyInlinePatch(
            nameof(OptimizerInlinePatches.InlinePrefix_Is_EvaluationStack_KnownFailure),
            PatchType.Prefix,
            typeof(OptimizerMixedTargets).GetMethod(nameof(OptimizerMixedTargets.IsOperatorOnEvaluationStack))!);

        Assert.That(OptimizerMixedTargets.IsOperatorOnEvaluationStack(false), Is.False);
        Assert.That(OptimizerMixedTargets.IsOperatorOnEvaluationStack(true), Is.False);
        Assert.That(OptimizerInlinePatches.PatchCalls, Is.EqualTo(2));
    }

    [Test]
    public void InlinePrefix_As_Local_KnownSuccess()
    {
        ApplyInlinePatch(
            nameof(OptimizerInlinePatches.InlinePrefix_As_Local_KnownSuccess),
            PatchType.Prefix,
            typeof(OptimizerMixedTargets).GetMethod(nameof(OptimizerMixedTargets.AsOperatorOnLocal))!);

        Assert.That(OptimizerMixedTargets.AsOperatorOnLocal(false), Is.EqualTo(7));
        Assert.That(OptimizerMixedTargets.AsOperatorOnLocal(true), Is.EqualTo(7));
        Assert.That(OptimizerInlinePatches.PatchCalls, Is.EqualTo(2));
    }

    [Test]
    public void InlinePrefix_As_Local_KnownFailure()
    {
        ApplyInlinePatch(
            nameof(OptimizerInlinePatches.InlinePrefix_As_Local_KnownFailure),
            PatchType.Prefix,
            typeof(OptimizerMixedTargets).GetMethod(nameof(OptimizerMixedTargets.AsOperatorOnLocal))!);

        Assert.That(OptimizerMixedTargets.AsOperatorOnLocal(false), Is.Null);
        Assert.That(OptimizerMixedTargets.AsOperatorOnLocal(true), Is.Null);
        Assert.That(OptimizerInlinePatches.PatchCalls, Is.EqualTo(2));
    }

    [Test]
    public void InlinePrefix_As_EvaluationStack_KnownSuccess()
    {
        ApplyInlinePatch(
            nameof(OptimizerInlinePatches.InlinePrefix_As_EvaluationStack_KnownSuccess),
            PatchType.Prefix,
            typeof(OptimizerMixedTargets).GetMethod(nameof(OptimizerMixedTargets.AsOperatorOnEvaluationStack))!);

        Assert.That(OptimizerMixedTargets.AsOperatorOnEvaluationStack(false), Is.EqualTo(7));
        Assert.That(OptimizerMixedTargets.AsOperatorOnEvaluationStack(true), Is.EqualTo(7));
        Assert.That(OptimizerInlinePatches.PatchCalls, Is.EqualTo(2));
    }

    [Test]
    public void InlinePrefix_As_EvaluationStack_KnownFailure()
    {
        ApplyInlinePatch(
            nameof(OptimizerInlinePatches.InlinePrefix_As_EvaluationStack_KnownFailure),
            PatchType.Prefix,
            typeof(OptimizerMixedTargets).GetMethod(nameof(OptimizerMixedTargets.AsOperatorOnEvaluationStack))!);

        Assert.That(OptimizerMixedTargets.AsOperatorOnEvaluationStack(false), Is.Null);
        Assert.That(OptimizerMixedTargets.AsOperatorOnEvaluationStack(true), Is.Null);
        Assert.That(OptimizerInlinePatches.PatchCalls, Is.EqualTo(2));
    }

    [Test]
    public void InlinePrefix_Argument_Primitive_ReadWriteByReference()
    {
        OptimizerInlinePatches.PrimitiveObserved = 0;
        ApplyInlinePatch(
            nameof(OptimizerInlinePatches.InlinePrefix_Argument_Primitive_ReadWriteByReference),
            PatchType.Prefix,
            typeof(OptimizerInlineTargets).GetMethod(nameof(OptimizerInlineTargets.PrimitiveIdentity))!);

        int result = OptimizerInlineTargets.PrimitiveIdentity(7);

        Assert.That(OptimizerInlinePatches.PrimitiveObserved, Is.EqualTo(7));
        Assert.That(result, Is.EqualTo(42));
        Assert.That(OptimizerInlinePatches.PatchCalls, Is.EqualTo(1));
    }

    [Test]
    public void InlinePrefix_Argument_ReferenceType_ReadWriteByReference()
    {
        OptimizerInlinePatches.ReferenceObserved = null;
        var original = new OptimizerDataObject { Number = 7, Text = "original" };
        ApplyInlinePatch(
            nameof(OptimizerInlinePatches.InlinePrefix_Argument_ReferenceType_ReadWriteByReference),
            PatchType.Prefix,
            typeof(OptimizerInlineTargets).GetMethod(nameof(OptimizerInlineTargets.ReferenceIdentity))!);

        OptimizerDataObject result = OptimizerInlineTargets.ReferenceIdentity(original);

        Assert.That(OptimizerInlinePatches.ReferenceObserved, Is.SameAs(original));
        Assert.That(original.Number, Is.EqualTo(7));
        Assert.That(original.Text, Is.EqualTo("original"));
        Assert.That(result, Is.Not.SameAs(original));
        Assert.That(result.Number, Is.EqualTo(42));
        Assert.That(result.Text, Is.EqualTo("patched"));
        Assert.That(OptimizerInlinePatches.PatchCalls, Is.EqualTo(1));
    }

    [Test]
    public void InlinePrefix_Argument_Struct_ReadWriteByReference()
    {
        OptimizerInlinePatches.StructObserved = default;
        var original = new OptimizerDataStruct { Number = 7, Text = "original" };
        ApplyInlinePatch(
            nameof(OptimizerInlinePatches.InlinePrefix_Argument_Struct_ReadWriteByReference),
            PatchType.Prefix,
            typeof(OptimizerInlineTargets).GetMethod(nameof(OptimizerInlineTargets.StructIdentity))!);

        OptimizerDataStruct result = OptimizerInlineTargets.StructIdentity(original);

        Assert.That(OptimizerInlinePatches.StructObserved.Number, Is.EqualTo(7));
        Assert.That(OptimizerInlinePatches.StructObserved.Text, Is.EqualTo("original"));
        Assert.That(original.Number, Is.EqualTo(7));
        Assert.That(original.Text, Is.EqualTo("original"));
        Assert.That(result.Number, Is.EqualTo(42));
        Assert.That(result.Text, Is.EqualTo("patched"));
        Assert.That(OptimizerInlinePatches.PatchCalls, Is.EqualTo(1));
    }

    [Test]
    public void InlinePostfix_Result_Primitive_ReadWriteByReference()
    {
        OptimizerInlinePatches.PrimitiveObserved = 0;
        ApplyInlinePatch(
            nameof(OptimizerInlinePatches.InlinePostfix_Result_Primitive_ReadWriteByReference),
            PatchType.Postfix,
            typeof(OptimizerInlineTargets).GetMethod(nameof(OptimizerInlineTargets.PrimitiveResult))!);

        int result = OptimizerInlineTargets.PrimitiveResult();

        Assert.That(OptimizerInlinePatches.PrimitiveObserved, Is.EqualTo(7));
        Assert.That(result, Is.EqualTo(42));
        Assert.That(OptimizerInlinePatches.PatchCalls, Is.EqualTo(1));
    }

    [Test]
    public void InlinePostfix_Result_ReferenceType_ReadWriteByReference()
    {
        OptimizerInlinePatches.ReferenceObserved = null;
        ApplyInlinePatch(
            nameof(OptimizerInlinePatches.InlinePostfix_Result_ReferenceType_ReadWriteByReference),
            PatchType.Postfix,
            typeof(OptimizerInlineTargets).GetMethod(nameof(OptimizerInlineTargets.ReferenceResult))!);

        OptimizerDataObject result = OptimizerInlineTargets.ReferenceResult();

        Assert.That(OptimizerInlinePatches.ReferenceObserved, Is.Not.Null);
        Assert.That(OptimizerInlinePatches.ReferenceObserved!.Number, Is.EqualTo(7));
        Assert.That(OptimizerInlinePatches.ReferenceObserved.Text, Is.EqualTo("original"));
        Assert.That(result, Is.Not.SameAs(OptimizerInlinePatches.ReferenceObserved));
        Assert.That(result.Number, Is.EqualTo(42));
        Assert.That(result.Text, Is.EqualTo("patched"));
        Assert.That(OptimizerInlinePatches.PatchCalls, Is.EqualTo(1));
    }

    [Test]
    public void InlinePostfix_Result_Struct_ReadWriteByReference()
    {
        OptimizerInlinePatches.StructObserved = default;
        ApplyInlinePatch(
            nameof(OptimizerInlinePatches.InlinePostfix_Result_Struct_ReadWriteByReference),
            PatchType.Postfix,
            typeof(OptimizerInlineTargets).GetMethod(nameof(OptimizerInlineTargets.StructResult))!);

        OptimizerDataStruct result = OptimizerInlineTargets.StructResult();

        Assert.That(OptimizerInlinePatches.StructObserved.Number, Is.EqualTo(7));
        Assert.That(OptimizerInlinePatches.StructObserved.Text, Is.EqualTo("original"));
        Assert.That(result.Number, Is.EqualTo(42));
        Assert.That(result.Text, Is.EqualTo("patched"));
        Assert.That(OptimizerInlinePatches.PatchCalls, Is.EqualTo(1));
    }

    [Test]
    public void InlinePrefix_TargetRefArgument_Primitive_ReadWriteByReference()
    {
        OptimizerInlinePatches.PrimitiveObserved = 0;
        int value = 7;
        ApplyInlinePatch(
            nameof(OptimizerInlinePatches.InlinePrefix_TargetRefArgument_Primitive_ReadWriteByReference),
            PatchType.Prefix,
            typeof(OptimizerInlineTargets).GetMethod(nameof(OptimizerInlineTargets.RefPrimitiveIdentity))!);

        int result = OptimizerInlineTargets.RefPrimitiveIdentity(ref value);

        Assert.That(OptimizerInlinePatches.PrimitiveObserved, Is.EqualTo(7));
        Assert.That(value, Is.EqualTo(43));
        Assert.That(result, Is.EqualTo(43));
        Assert.That(OptimizerInlinePatches.PatchCalls, Is.EqualTo(1));
    }

    [Test]
    public void InlinePrefix_ControlFlow_MultipleReturns()
    {
        ApplyInlinePatch(
            nameof(OptimizerInlinePatches.InlinePrefix_ControlFlow_MultipleReturns),
            PatchType.Prefix,
            typeof(OptimizerInlineTargets).GetMethod(nameof(OptimizerInlineTargets.PrimitiveIdentity))!);

        Assert.That(OptimizerInlineTargets.PrimitiveIdentity(-10), Is.EqualTo(-1));
        Assert.That(OptimizerInlineTargets.PrimitiveIdentity(0), Is.EqualTo(7));
        Assert.That(OptimizerInlineTargets.PrimitiveIdentity(1), Is.EqualTo(42));
        Assert.That(OptimizerInlinePatches.PatchCalls, Is.EqualTo(3));
    }

    [Test]
    public void InlinePrefix_ExceptionHandling_TryFinally()
    {
        OptimizerInlinePatches.FinallyExecutions = 0;
        ApplyInlinePatch(
            nameof(OptimizerInlinePatches.InlinePrefix_ExceptionHandling_TryFinally),
            PatchType.Prefix,
            typeof(OptimizerInlineTargets).GetMethod(nameof(OptimizerInlineTargets.PrimitiveIdentity))!);

        int result = OptimizerInlineTargets.PrimitiveIdentity(7);

        Assert.That(result, Is.EqualTo(42));
        Assert.That(OptimizerInlinePatches.FinallyExecutions, Is.EqualTo(1));
        Assert.That(OptimizerInlinePatches.PatchCalls, Is.EqualTo(1));
    }

    [Test]
    public void InlinePrefixPostfix_StateAndResult_PreservesValues()
    {
        OptimizerInlinePatches.StateObserved = 0;
        OptimizerInlinePatches.ResultObserved = 0;
        ApplyInlinePatch(
            nameof(OptimizerInlinePatches.InlinePrefixPostfix_StateAndResult_PreservesValues_Prefix),
            PatchType.Prefix,
            typeof(OptimizerInlineTargets).GetMethod(nameof(OptimizerInlineTargets.PrimitiveIdentity))!);
        ApplyInlinePatch(
            nameof(OptimizerInlinePatches.InlinePrefixPostfix_StateAndResult_PreservesValues_Postfix),
            PatchType.Postfix,
            typeof(OptimizerInlineTargets).GetMethod(nameof(OptimizerInlineTargets.PrimitiveIdentity))!);

        int result = OptimizerInlineTargets.PrimitiveIdentity(7);

        Assert.That(OptimizerInlinePatches.StateObserved, Is.EqualTo(7));
        Assert.That(OptimizerInlinePatches.ResultObserved, Is.EqualTo(7));
        Assert.That(result, Is.EqualTo(42));
        Assert.That(OptimizerInlinePatches.PatchCalls, Is.EqualTo(2));
    }
}
