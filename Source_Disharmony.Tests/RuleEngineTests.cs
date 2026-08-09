using System.Reflection.Emit;
using Disharmony.RuleEngine;
using HarmonyLib;

namespace Disharmony.Tests;

[TestFixture]
public sealed class RuleEngineTests
{
    private static readonly MethodInfo TargetMethod =
        typeof(StaticMethodTargets).GetMethod(nameof(StaticMethodTargets.Void))!;

    [Test]
    public void ReplaceSubstitutesMatchedInstructions()
    {
        var rule = new Rule
        {
            mode = OutputMode.Replace,
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
        var rule = new Rule
        {
            mode = OutputMode.Replace,
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
        var rule = new Rule
        {
            mode = OutputMode.InsertBefore,
            pattern = [new CodeInstruction(OpCodes.Ret)],
            output = [new CodeInstruction(OpCodes.Nop)],
        };

        List<CodeInstruction> result = Run([rule], [new CodeInstruction(OpCodes.Ret)]);

        Assert.That(MeaningfulOpCodes(result), Is.EqualTo(new[] { OpCodes.Nop, OpCodes.Ret }));
    }

    [Test]
    public void InsertAfterPreservesMatchedInstructions()
    {
        var rule = new Rule
        {
            mode = OutputMode.InsertAfter,
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
        var prefix = new Rule
        {
            mode = OutputMode.MethodPrefix,
            output = [new CodeInstruction(OpCodes.Ldc_I4_1)],
        };
        var postfix = new Rule
        {
            mode = OutputMode.MethodPostfix,
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
        Rule rule = new Rule
        {
            min = 1,
            max = 0,
            mode = OutputMode.Replace,
            pattern = [new(OpCodes.Call, oldMethod)],
            output = [new(OpCodes.Call, newMethod)],
            name = oldMethod.FullName,
        };

        List<CodeInstruction> result = Run(
            [rule],
            [new CodeInstruction(OpCodes.Ldnull), new CodeInstruction(OpCodes.Callvirt, oldMethod), new CodeInstruction(OpCodes.Pop)]);

        CodeInstruction redirectedCall = result.Single(instruction => instruction.opcode == OpCodes.Call);
        Assert.That(redirectedCall.operand, Is.SameAs(newMethod));
    }

    [Test]
    public void ReplacementReusesLocalMatchedByPattern()
    {
        var rule = new Rule
        {
            mode = OutputMode.Replace,
            pattern = [CodeInstruction.StoreLocal(0), CodeInstruction.LoadLocal(0)],
            output = [CodeInstruction.LoadLocal(0)],
        };

        List<CodeInstruction> result = Run(
            [rule],
            [CodeInstruction.StoreLocal(3), CodeInstruction.LoadLocal(3), new CodeInstruction(OpCodes.Pop)]);

        CodeInstruction localLoad = result.Single(instruction => instruction.IsLdloc());
        Assert.That(localLoad.LocalIndex(), Is.EqualTo(3));
    }

    [TestCaseSource(nameof(NewLocalReplacementCases))]
    public void ReplacementForNewLocalUsesExpectedOpcodeAndBuilder(
        OpCode replacementOpcode,
        int precedingLocalCount,
        OpCode expectedOpcode)
    {
        List<CodeInstruction> result = RunWithNewLocal([replacementOpcode], precedingLocalCount);

        CodeInstruction replacement = result.Single(instruction => instruction.operand is LocalBuilder);
        Assert.That(replacement.opcode, Is.EqualTo(expectedOpcode));
        AssertLocalBuilderOperand(replacement, expectedIndex: precedingLocalCount);
    }

    [Test]
    public void ReplacementOperationsForNewLocalReuseSameLocalBuilder()
    {
        List<CodeInstruction> result = RunWithNewLocal(
            [OpCodes.Stloc_S, OpCodes.Ldloc_S, OpCodes.Ldloca_S],
            precedingLocalCount: 4);

        LocalBuilder[] localBuilders =
        [
            .. result
                .Where(instruction => instruction.operand is LocalBuilder)
                .Select(instruction => (LocalBuilder)instruction.operand),
        ];
        Assert.That(localBuilders, Has.Length.EqualTo(3));
        Assert.That(localBuilders, Has.All.SameAs(localBuilders[0]));
    }

    private static IEnumerable<TestCaseData> NewLocalReplacementCases()
    {
        yield return NewLocalReplacementCase(
            "ReplacementForNewLocal_Store_ZeroIndex_UsesExpectedOpcodeAndBuilder",
            OpCodes.Stloc_S, 0, OpCodes.Stloc_S);
        yield return NewLocalReplacementCase(
            "ReplacementForNewLocal_Load_ZeroIndex_UsesExpectedOpcodeAndBuilder",
            OpCodes.Ldloc_S, 0, OpCodes.Ldloc_S);
        yield return NewLocalReplacementCase(
            "ReplacementForNewLocal_AddressLoad_ZeroIndex_UsesExpectedOpcodeAndBuilder",
            OpCodes.Ldloca_S, 0, OpCodes.Ldloca_S);
        yield return NewLocalReplacementCase(
            "ReplacementForNewLocal_Store_ShortIndex_UsesExpectedOpcodeAndBuilder",
            OpCodes.Stloc_S, 4, OpCodes.Stloc_S);
        yield return NewLocalReplacementCase(
            "ReplacementForNewLocal_Load_ShortIndex_UsesExpectedOpcodeAndBuilder",
            OpCodes.Ldloc_S, 4, OpCodes.Ldloc_S);
        yield return NewLocalReplacementCase(
            "ReplacementForNewLocal_AddressLoad_ShortIndex_UsesExpectedOpcodeAndBuilder",
            OpCodes.Ldloca_S, 4, OpCodes.Ldloca_S);
        yield return NewLocalReplacementCase(
            "ReplacementForNewLocal_Store_MaximumShortIndex_UsesExpectedOpcodeAndBuilder",
            OpCodes.Stloc_S, byte.MaxValue, OpCodes.Stloc_S);
        yield return NewLocalReplacementCase(
            "ReplacementForNewLocal_Load_MaximumShortIndex_UsesExpectedOpcodeAndBuilder",
            OpCodes.Ldloc_S, byte.MaxValue, OpCodes.Ldloc_S);
        yield return NewLocalReplacementCase(
            "ReplacementForNewLocal_AddressLoad_MaximumShortIndex_UsesExpectedOpcodeAndBuilder",
            OpCodes.Ldloca_S, byte.MaxValue, OpCodes.Ldloca_S);
        yield return NewLocalReplacementCase(
            "ReplacementForNewLocal_Store_LongIndex_UsesExpectedOpcodeAndBuilder",
            OpCodes.Stloc_S, 256, OpCodes.Stloc);
        yield return NewLocalReplacementCase(
            "ReplacementForNewLocal_Load_LongIndex_UsesExpectedOpcodeAndBuilder",
            OpCodes.Ldloc_S, 256, OpCodes.Ldloc);
        yield return NewLocalReplacementCase(
            "ReplacementForNewLocal_AddressLoad_LongIndex_UsesExpectedOpcodeAndBuilder",
            OpCodes.Ldloca_S, 256, OpCodes.Ldloca);
    }

    private static TestCaseData NewLocalReplacementCase(
        string name,
        OpCode replacementOpcode,
        int precedingLocalCount,
        OpCode expectedOpcode) =>
        new TestCaseData(replacementOpcode, precedingLocalCount, expectedOpcode)
            .SetName(name);

    private static List<CodeInstruction> Run(
        List<Rule> rules,
        IEnumerable<CodeInstruction> instructions)
    {
        var dynamicMethod = new DynamicMethod("InstructionMatcherTest", typeof(void), Type.EmptyTypes);
        return Ruleset.MatchAndReplace(rules, TargetMethod, instructions, dynamicMethod.GetILGenerator());
    }

    private static List<CodeInstruction> RunWithNewLocal(
        IEnumerable<OpCode> replacementOpCodes,
        int precedingLocalCount)
    {
        var dynamicMethod = new DynamicMethod("InstructionMatcherLocalTest", typeof(void), Type.EmptyTypes);
        ILGenerator generator = dynamicMethod.GetILGenerator();
        for (int i = 0; i < precedingLocalCount; i++)
            generator.DeclareLocal(typeof(int));

        LocalBuilder local = PatchProcessor.CreateILGenerator().DeclareLocal(typeof(string));
        var ruleset = new Ruleset(new List<Rule>
        {
            new()
            {
                mode = OutputMode.MethodPrefix,
                output = replacementOpCodes.Select(opcode => new CodeInstruction(opcode, local)).ToArray(),
            },
        });
        ruleset.crossRuleLocals.Add(local);

        List<CodeInstruction> instructions = [new CodeInstruction(OpCodes.Ret)];
        ruleset.MatchAndReplace(TargetMethod, ref instructions, generator);
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
