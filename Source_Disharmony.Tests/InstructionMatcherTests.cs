using System.Reflection.Emit;
using HarmonyLib;

namespace Disharmony.Tests;

[TestFixture]
public sealed class InstructionMatcherTests
{
    private static readonly MethodInfo TargetMethod =
        typeof(StaticMethodTargets).GetMethod(nameof(StaticMethodTargets.Void))!;

    [Test]
    public void ReplaceSubstitutesMatchedInstructions()
    {
        var rule = new InstructionMatcher.Rule
        {
            mode = InstructionMatcher.OutputMode.Replace,
            pattern = [new CodeInstruction(OpCodes.Ldc_I4_1)],
            output = [new CodeInstruction(OpCodes.Ldc_I4_2)],
        };

        List<CodeInstruction> result = Run(
            [rule],
            [new CodeInstruction(OpCodes.Ldc_I4_1), new CodeInstruction(OpCodes.Pop), new CodeInstruction(OpCodes.Ret)]);

        Assert.That(MeaningfulOpCodes(result), Is.EqualTo(new[] { OpCodes.Ldc_I4_2, OpCodes.Pop, OpCodes.Ret }));
    }

    [Test]
    public void ReplacementPreservesExceptionBlockMarkersFromOutput()
    {
        var tryStart = new CodeInstruction(OpCodes.Nop);
        tryStart.blocks.Add(new ExceptionBlock(ExceptionBlockType.BeginExceptionBlock));
        var finallyStart = new CodeInstruction(OpCodes.Nop);
        finallyStart.blocks.Add(new ExceptionBlock(ExceptionBlockType.BeginFinallyBlock));
        var finallyEnd = new CodeInstruction(OpCodes.Endfinally);
        finallyEnd.blocks.Add(new ExceptionBlock(ExceptionBlockType.EndExceptionBlock));
        var rule = new InstructionMatcher.Rule
        {
            mode = InstructionMatcher.OutputMode.Replace,
            pattern = [new CodeInstruction(OpCodes.Ldc_I4_1)],
            output = [tryStart, finallyStart, finallyEnd],
        };

        List<CodeInstruction> result = Run(
            [rule],
            [new CodeInstruction(OpCodes.Ldc_I4_1), new CodeInstruction(OpCodes.Ret)]);

        Assert.That(
            result.SelectMany(instruction => instruction.blocks).Select(block => block.blockType),
            Is.EqualTo(new[]
            {
                ExceptionBlockType.BeginExceptionBlock,
                ExceptionBlockType.BeginFinallyBlock,
                ExceptionBlockType.EndExceptionBlock,
            }));
    }

    [Test]
    public void InsertBeforePreservesMatchedInstructions()
    {
        var rule = new InstructionMatcher.Rule
        {
            mode = InstructionMatcher.OutputMode.InsertBefore,
            pattern = [new CodeInstruction(OpCodes.Ret)],
            output = [new CodeInstruction(OpCodes.Nop)],
        };

        List<CodeInstruction> result = Run([rule], [new CodeInstruction(OpCodes.Ret)]);

        Assert.That(MeaningfulOpCodes(result), Is.EqualTo(new[] { OpCodes.Nop, OpCodes.Ret }));
    }

    [Test]
    public void InsertAfterPreservesMatchedInstructions()
    {
        var rule = new InstructionMatcher.Rule
        {
            mode = InstructionMatcher.OutputMode.InsertAfter,
            pattern = [new CodeInstruction(OpCodes.Nop)],
            output = [new CodeInstruction(OpCodes.Pop)],
        };

        List<CodeInstruction> result = Run(
            [rule],
            [new CodeInstruction(OpCodes.Ldnull), new CodeInstruction(OpCodes.Nop), new CodeInstruction(OpCodes.Ret)]);

        Assert.That(MeaningfulOpCodes(result), Is.EqualTo(new[] { OpCodes.Ldnull, OpCodes.Nop, OpCodes.Pop, OpCodes.Ret }));
    }

    [Test]
    public void MethodPrefixAndPostfixWrapInstructionSequence()
    {
        var prefix = new InstructionMatcher.Rule
        {
            mode = InstructionMatcher.OutputMode.MethodPrefix,
            output = [new CodeInstruction(OpCodes.Ldc_I4_1)],
        };
        var postfix = new InstructionMatcher.Rule
        {
            mode = InstructionMatcher.OutputMode.MethodPostfix,
            output = [new CodeInstruction(OpCodes.Pop)],
        };

        List<CodeInstruction> result = Run([prefix, postfix], [new CodeInstruction(OpCodes.Ret)]);

        Assert.That(MeaningfulOpCodes(result), Is.EqualTo(new[] { OpCodes.Ldc_I4_1, OpCodes.Ret, OpCodes.Pop }));
    }

    [Test]
    public void MakeRedirectRuleMatchesCallvirtAndEmitsCall()
    {
        MethodInfo oldMethod = typeof(object).GetMethod(nameof(ToString))!;
        MethodInfo newMethod = typeof(Convert).GetMethod(nameof(Convert.ToString), [typeof(object)])!;
        InstructionMatcher.Rule rule = InstructionMatcher.MakeRedirectRule(oldMethod, newMethod);

        List<CodeInstruction> result = Run(
            [rule],
            [new CodeInstruction(OpCodes.Ldnull), new CodeInstruction(OpCodes.Callvirt, oldMethod), new CodeInstruction(OpCodes.Pop)]);

        CodeInstruction redirectedCall = result.Single(instruction => instruction.opcode == OpCodes.Call);
        Assert.That(redirectedCall.operand, Is.SameAs(newMethod));
    }

    [Test]
    public void ReplacementReusesLocalMatchedByPattern()
    {
        var rule = new InstructionMatcher.Rule
        {
            mode = InstructionMatcher.OutputMode.Replace,
            pattern = [CodeInstruction.StoreLocal(0), CodeInstruction.LoadLocal(0)],
            output = [CodeInstruction.LoadLocal(0)],
        };

        List<CodeInstruction> result = Run(
            [rule],
            [CodeInstruction.StoreLocal(3), CodeInstruction.LoadLocal(3), new CodeInstruction(OpCodes.Pop)]);

        CodeInstruction localLoad = result.Single(instruction => instruction.IsLdloc());
        Assert.That(localLoad.LocalIndex(), Is.EqualTo(3));
    }

    [Test]
    public void ReplacementStoreForNewLocalAtShortIndexRetainsLocalBuilderOperand()
    {
        List<CodeInstruction> result = RunWithNewLocal(CodeInstruction.StoreLocal(0));

        CodeInstruction localStore = result.Single(instruction => instruction.IsStloc());
        Assert.That(localStore.opcode, Is.EqualTo(OpCodes.Stloc_S));
        AssertLocalBuilderOperand(localStore, expectedIndex: 4);
    }

    [Test]
    public void ReplacementLoadForNewLocalAtShortIndexRetainsLocalBuilderOperand()
    {
        List<CodeInstruction> result = RunWithNewLocal(CodeInstruction.LoadLocal(0));

        CodeInstruction localLoad = result.Single(instruction =>
            instruction.IsLdloc() && instruction.opcode != OpCodes.Ldloca && instruction.opcode != OpCodes.Ldloca_S);
        Assert.That(localLoad.opcode, Is.EqualTo(OpCodes.Ldloc_S));
        AssertLocalBuilderOperand(localLoad, expectedIndex: 4);
    }

    [Test]
    public void ReplacementAddressLoadForNewLocalAtShortIndexRetainsLocalBuilderOperand()
    {
        List<CodeInstruction> result = RunWithNewLocal(CodeInstruction.LoadLocal(0, true));

        CodeInstruction localAddressLoad = result.Single(instruction =>
            instruction.opcode == OpCodes.Ldloca || instruction.opcode == OpCodes.Ldloca_S);
        Assert.That(localAddressLoad.opcode, Is.EqualTo(OpCodes.Ldloca_S));
        AssertLocalBuilderOperand(localAddressLoad, expectedIndex: 4);
    }

    [Test]
    public void ReplacementStoreForNewLocalAtIndexZeroUsesOperandBearingShortOpcode()
    {
        List<CodeInstruction> result = RunWithNewLocal(CodeInstruction.StoreLocal(0), precedingLocalCount: 0);

        CodeInstruction localStore = result.Single(instruction => instruction.IsStloc());
        Assert.That(localStore.opcode, Is.EqualTo(OpCodes.Stloc_S));
        AssertLocalBuilderOperand(localStore, expectedIndex: 0);
    }

    [Test]
    public void ReplacementLoadForNewLocalAtIndexZeroUsesOperandBearingShortOpcode()
    {
        List<CodeInstruction> result = RunWithNewLocal(CodeInstruction.LoadLocal(0), precedingLocalCount: 0);

        CodeInstruction localLoad = result.Single(instruction => instruction.IsLdloc());
        Assert.That(localLoad.opcode, Is.EqualTo(OpCodes.Ldloc_S));
        AssertLocalBuilderOperand(localLoad, expectedIndex: 0);
    }

    [Test]
    public void ReplacementAddressLoadForNewLocalAtIndexZeroRetainsLocalBuilderOperand()
    {
        List<CodeInstruction> result = RunWithNewLocal(CodeInstruction.LoadLocal(0, true), precedingLocalCount: 0);

        CodeInstruction localAddressLoad = result.Single(instruction =>
            instruction.opcode == OpCodes.Ldloca || instruction.opcode == OpCodes.Ldloca_S);
        Assert.That(localAddressLoad.opcode, Is.EqualTo(OpCodes.Ldloca_S));
        AssertLocalBuilderOperand(localAddressLoad, expectedIndex: 0);
    }

    [Test]
    public void ReplacementStoreForNewLocalAtMaximumShortIndexRetainsLocalBuilderOperand()
    {
        List<CodeInstruction> result = RunWithNewLocal(CodeInstruction.StoreLocal(0), precedingLocalCount: byte.MaxValue);

        CodeInstruction localStore = result.Single(instruction => instruction.IsStloc());
        Assert.That(localStore.opcode, Is.EqualTo(OpCodes.Stloc_S));
        AssertLocalBuilderOperand(localStore, expectedIndex: byte.MaxValue);
    }

    [Test]
    public void ReplacementLoadForNewLocalAtMaximumShortIndexRetainsLocalBuilderOperand()
    {
        List<CodeInstruction> result = RunWithNewLocal(CodeInstruction.LoadLocal(0), precedingLocalCount: byte.MaxValue);

        CodeInstruction localLoad = result.Single(instruction => instruction.IsLdloc());
        Assert.That(localLoad.opcode, Is.EqualTo(OpCodes.Ldloc_S));
        AssertLocalBuilderOperand(localLoad, expectedIndex: byte.MaxValue);
    }

    [Test]
    public void ReplacementAddressLoadForNewLocalAtMaximumShortIndexRetainsLocalBuilderOperand()
    {
        List<CodeInstruction> result = RunWithNewLocal(CodeInstruction.LoadLocal(0, true), precedingLocalCount: byte.MaxValue);

        CodeInstruction localAddressLoad = result.Single(instruction =>
            instruction.opcode == OpCodes.Ldloca || instruction.opcode == OpCodes.Ldloca_S);
        Assert.That(localAddressLoad.opcode, Is.EqualTo(OpCodes.Ldloca_S));
        AssertLocalBuilderOperand(localAddressLoad, expectedIndex: byte.MaxValue);
    }

    [Test]
    public void ReplacementStoreForNewLocalAtLongIndexRetainsLocalBuilderOperand()
    {
        List<CodeInstruction> result = RunWithNewLocal(CodeInstruction.StoreLocal(0), precedingLocalCount: 256);

        CodeInstruction localStore = result.Single(instruction => instruction.IsStloc());
        Assert.That(localStore.opcode, Is.EqualTo(OpCodes.Stloc));
        AssertLocalBuilderOperand(localStore, expectedIndex: 256);
    }

    [Test]
    public void ReplacementLoadForNewLocalAtLongIndexRetainsLocalBuilderOperand()
    {
        List<CodeInstruction> result = RunWithNewLocal(CodeInstruction.LoadLocal(0), precedingLocalCount: 256);

        CodeInstruction localLoad = result.Single(instruction => instruction.IsLdloc());
        Assert.That(localLoad.opcode, Is.EqualTo(OpCodes.Ldloc));
        AssertLocalBuilderOperand(localLoad, expectedIndex: 256);
    }

    [Test]
    public void ReplacementAddressLoadForNewLocalAtLongIndexRetainsLocalBuilderOperand()
    {
        List<CodeInstruction> result = RunWithNewLocal(CodeInstruction.LoadLocal(0, true), precedingLocalCount: 256);

        CodeInstruction localAddressLoad = result.Single(instruction =>
            instruction.opcode == OpCodes.Ldloca || instruction.opcode == OpCodes.Ldloca_S);
        Assert.That(localAddressLoad.opcode, Is.EqualTo(OpCodes.Ldloca));
        AssertLocalBuilderOperand(localAddressLoad, expectedIndex: 256);
    }

    [Test]
    public void ReplacementOperationsForNewLocalReuseSameLocalBuilder()
    {
        List<CodeInstruction> result = RunWithNewLocal(
            CodeInstruction.StoreLocal(0),
            CodeInstruction.LoadLocal(0),
            CodeInstruction.LoadLocal(0, true));

        LocalBuilder[] localBuilders =
        [
            .. result
                .Where(instruction => instruction.IsStloc() || instruction.IsLdloc())
                .Select(instruction => instruction.operand)
                .Cast<LocalBuilder>(),
        ];
        Assert.That(localBuilders, Has.Length.EqualTo(3));
        Assert.That(localBuilders, Has.All.SameAs(localBuilders[0]));
    }

    private static List<CodeInstruction> Run(
        List<InstructionMatcher.Rule> rules,
        IEnumerable<CodeInstruction> instructions)
    {
        var dynamicMethod = new DynamicMethod("InstructionMatcherTest", typeof(void), Type.EmptyTypes);
        return InstructionMatcher.MatchAndReplace(rules, TargetMethod, instructions, dynamicMethod.GetILGenerator());
    }

    private static List<CodeInstruction> RunWithNewLocal(CodeInstruction replacement, int precedingLocalCount = 4) =>
        RunWithNewLocal(precedingLocalCount, replacement);

    private static List<CodeInstruction> RunWithNewLocal(params CodeInstruction[] replacements) =>
        RunWithNewLocal(4, replacements);

    private static List<CodeInstruction> RunWithNewLocal(int precedingLocalCount, params CodeInstruction[] replacements)
    {
        var dynamicMethod = new DynamicMethod("InstructionMatcherLocalTest", typeof(void), Type.EmptyTypes);
        ILGenerator generator = dynamicMethod.GetILGenerator();
        for (int i = 0; i < precedingLocalCount; i++)
            generator.DeclareLocal(typeof(int));

        var matcher = new InstructionMatcher(new InstructionMatcher.Rule
        {
            mode = InstructionMatcher.OutputMode.MethodPrefix,
            output = replacements,
        });
        matcher.crossRuleLocalTypes.Add(typeof(string));

        List<CodeInstruction> instructions = [new CodeInstruction(OpCodes.Ret)];
        matcher.MatchAndReplace(TargetMethod, ref instructions, generator);
        return instructions;
    }

    private static void AssertLocalBuilderOperand(CodeInstruction instruction, int expectedIndex)
    {
        Assert.That(instruction.operand, Is.TypeOf<LocalBuilder>());
        var localBuilder = (LocalBuilder)instruction.operand;
        Assert.That(localBuilder.LocalIndex, Is.EqualTo(expectedIndex));
        Assert.That(localBuilder.LocalType, Is.EqualTo(typeof(string)));
    }

    private static OpCode[] MeaningfulOpCodes(IEnumerable<CodeInstruction> instructions) =>
    [
        .. instructions
            .Where(instruction => instruction.opcode != OpCodes.Nop || instruction.operand is not string)
            .Select(instruction => instruction.opcode),
    ];
}
