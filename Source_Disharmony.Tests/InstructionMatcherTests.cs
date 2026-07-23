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
            Mode = InstructionMatcher.OutputMode.Replace,
            Pattern = [new CodeInstruction(OpCodes.Ldc_I4_1)],
            Output = [new CodeInstruction(OpCodes.Ldc_I4_2)],
        };

        List<CodeInstruction> result = Run(
            [rule],
            [new CodeInstruction(OpCodes.Ldc_I4_1), new CodeInstruction(OpCodes.Pop), new CodeInstruction(OpCodes.Ret)]);

        Assert.That(MeaningfulOpCodes(result), Is.EqualTo(new[] { OpCodes.Ldc_I4_2, OpCodes.Pop, OpCodes.Ret }));
    }

    [Test]
    public void InsertBeforePreservesMatchedInstructions()
    {
        var rule = new InstructionMatcher.Rule
        {
            Mode = InstructionMatcher.OutputMode.InsertBefore,
            Pattern = [new CodeInstruction(OpCodes.Ret)],
            Output = [new CodeInstruction(OpCodes.Nop)],
        };

        List<CodeInstruction> result = Run([rule], [new CodeInstruction(OpCodes.Ret)]);

        Assert.That(MeaningfulOpCodes(result), Is.EqualTo(new[] { OpCodes.Nop, OpCodes.Ret }));
    }

    [Test]
    public void InsertAfterPreservesMatchedInstructions()
    {
        var rule = new InstructionMatcher.Rule
        {
            Mode = InstructionMatcher.OutputMode.InsertAfter,
            Pattern = [new CodeInstruction(OpCodes.Nop)],
            Output = [new CodeInstruction(OpCodes.Pop)],
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
            Mode = InstructionMatcher.OutputMode.MethodPrefix,
            Output = [new CodeInstruction(OpCodes.Ldc_I4_1)],
        };
        var postfix = new InstructionMatcher.Rule
        {
            Mode = InstructionMatcher.OutputMode.MethodPostfix,
            Output = [new CodeInstruction(OpCodes.Pop)],
        };

        List<CodeInstruction> result = Run([prefix, postfix], [new CodeInstruction(OpCodes.Ret)]);

        Assert.That(MeaningfulOpCodes(result), Is.EqualTo(new[] { OpCodes.Ldc_I4_1, OpCodes.Ret, OpCodes.Pop }));
    }

    [Test]
    public void MatchOnlyCanCaptureLocalForLaterRule()
    {
        var captureLocal = new InstructionMatcher.Rule
        {
            Mode = InstructionMatcher.OutputMode.MatchOnly,
            Pattern = [CodeInstruction.StoreLocal(0)],
            SaveLocals = true,
        };
        var replaceMappedLoad = new InstructionMatcher.Rule
        {
            Mode = InstructionMatcher.OutputMode.Replace,
            Pattern = [CodeInstruction.LoadLocal(0)],
            Output = [CodeInstruction.LoadLocal(0)],
        };

        List<CodeInstruction> result = Run(
            [captureLocal, replaceMappedLoad],
            [CodeInstruction.StoreLocal(3), CodeInstruction.LoadLocal(3), new CodeInstruction(OpCodes.Pop)]);

        CodeInstruction localLoad = result.Single(instruction => instruction.IsLdloc());
        Assert.That(localLoad.LocalIndex(), Is.EqualTo(3));
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
            Mode = InstructionMatcher.OutputMode.Replace,
            Pattern = [CodeInstruction.StoreLocal(0), CodeInstruction.LoadLocal(0)],
            Output = [CodeInstruction.LoadLocal(0)],
        };

        List<CodeInstruction> result = Run(
            [rule],
            [CodeInstruction.StoreLocal(3), CodeInstruction.LoadLocal(3), new CodeInstruction(OpCodes.Pop)]);

        CodeInstruction localLoad = result.Single(instruction => instruction.IsLdloc());
        Assert.That(localLoad.LocalIndex(), Is.EqualTo(3));
    }

    private static List<CodeInstruction> Run(
        List<InstructionMatcher.Rule> rules,
        IEnumerable<CodeInstruction> instructions)
    {
        var dynamicMethod = new DynamicMethod("InstructionMatcherTest", typeof(void), Type.EmptyTypes);
        return InstructionMatcher.MatchAndReplace(rules, TargetMethod, instructions, dynamicMethod.GetILGenerator());
    }

    private static OpCode[] MeaningfulOpCodes(IEnumerable<CodeInstruction> instructions) =>
    [
        .. instructions
            .Where(instruction => instruction.opcode != OpCodes.Nop || instruction.operand is not string)
            .Select(instruction => instruction.opcode),
    ];
}
