using Disharmony.RulesEngine;

namespace Disharmony.Tests;

[TestFixture]
public sealed class RulesEngineTests
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
    public void Pattern_Call_Instruction_Callvirt_Matches()
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
    public void Pattern_Callvirt_Instruction_Call_DoesNotMatch()
    {
        MethodInfo method = typeof(object).GetMethod(nameof(ToString))!;
        var rule = new Rule
        {
            mode = OutputMode.Replace,
            pattern = [new CodeInstruction(OpCodes.Callvirt, method)],
            output = [new CodeInstruction(OpCodes.Nop)],
        };

        var exception = Assert.Throws<InvalidOperationException>(() =>
            Run([rule], [new CodeInstruction(OpCodes.Call, method)]));

        Assert.That(exception!.Message, Does.StartWith("Not enough matches found"));
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

    [Test]
    public void ReplacementRemapsBranchOperandAndTargetLabelTogether()
    {
        ILGenerator sourceGenerator = PatchProcessor.CreateILGenerator();
        Label sourceLabel = sourceGenerator.DefineLabel();
        var target = new CodeInstruction(OpCodes.Nop);
        target.labels.Add(sourceLabel);
        var rule = new Rule
        {
            mode = OutputMode.Replace,
            pattern = [new CodeInstruction(OpCodes.Ldc_I4_1)],
            output = [new CodeInstruction(OpCodes.Br, sourceLabel), target],
        };

        List<CodeInstruction> result = Run([rule], [new CodeInstruction(OpCodes.Ldc_I4_1)]);

        var branchTarget = (Label)result.Single(instruction => instruction.opcode == OpCodes.Br).operand;
        CodeInstruction emittedTarget = result.Single(instruction => instruction.labels.Contains(branchTarget));
        Assert.That(branchTarget, Is.Not.EqualTo(sourceLabel));
        Assert.That(emittedTarget.opcode, Is.EqualTo(OpCodes.Nop));
    }

    [Test]
    public void ReplacementRemapsSwitchOperandsAndTargetLabelsTogether()
    {
        ILGenerator sourceGenerator = PatchProcessor.CreateILGenerator();
        Label sourceCase0 = sourceGenerator.DefineLabel();
        Label sourceCase1 = sourceGenerator.DefineLabel();
        var case0Target = new CodeInstruction(OpCodes.Ldc_I4_0);
        case0Target.labels.Add(sourceCase0);
        var case1Target = new CodeInstruction(OpCodes.Ldc_I4_1);
        case1Target.labels.Add(sourceCase1);
        var rule = new Rule
        {
            mode = OutputMode.Replace,
            pattern = [new CodeInstruction(OpCodes.Nop)],
            output =
            [
                new CodeInstruction(OpCodes.Switch, new[] { sourceCase0, sourceCase1, sourceCase0 }),
                case0Target,
                case1Target,
            ],
        };

        List<CodeInstruction> result = Run([rule], [new CodeInstruction(OpCodes.Nop)]);

        var switchTargets = (Label[])result.Single(instruction => instruction.opcode == OpCodes.Switch).operand;
        Label emittedCase0 = result.Single(instruction => instruction.opcode == OpCodes.Ldc_I4_0).labels.Single();
        Label emittedCase1 = result.Single(instruction => instruction.opcode == OpCodes.Ldc_I4_1).labels.Single();
        Assert.That(switchTargets, Is.EqualTo(new[] { emittedCase0, emittedCase1, emittedCase0 }));
        Assert.That(emittedCase0, Is.Not.EqualTo(sourceCase0));
        Assert.That(emittedCase1, Is.Not.EqualTo(sourceCase1));
    }

    [Test]
    public void RepeatedMatchesUseIndependentReplacementLabels()
    {
        ILGenerator sourceGenerator = PatchProcessor.CreateILGenerator();
        Label sourceLabel = sourceGenerator.DefineLabel();
        var target = new CodeInstruction(OpCodes.Nop);
        target.labels.Add(sourceLabel);
        var rule = new Rule
        {
            min = 2,
            max = 0,
            mode = OutputMode.Replace,
            pattern = [new CodeInstruction(OpCodes.Ldc_I4_1)],
            output = [new CodeInstruction(OpCodes.Br, sourceLabel), target],
        };

        List<CodeInstruction> result = Run(
            [rule],
            [new CodeInstruction(OpCodes.Ldc_I4_1), new CodeInstruction(OpCodes.Ldc_I4_1)]);

        Label[] branchTargets =
        [
            .. result
                .Where(instruction => instruction.opcode == OpCodes.Br)
                .Select(instruction => (Label)instruction.operand),
        ];
        Assert.That(branchTargets, Has.Length.EqualTo(2));
        Assert.That(branchTargets[0], Is.Not.EqualTo(branchTargets[1]));
        Assert.That(result.Count(instruction => instruction.labels.Contains(branchTargets[0])), Is.EqualTo(1));
        Assert.That(result.Count(instruction => instruction.labels.Contains(branchTargets[1])), Is.EqualTo(1));
    }

    [Test]
    public void RepeatedMatchesUseIndependentSwitchLabels()
    {
        ILGenerator sourceGenerator = PatchProcessor.CreateILGenerator();
        Label sourceLabel = sourceGenerator.DefineLabel();
        var target = new CodeInstruction(OpCodes.Nop);
        target.labels.Add(sourceLabel);
        var rule = new Rule
        {
            min = 2,
            max = 0,
            mode = OutputMode.Replace,
            pattern = [new CodeInstruction(OpCodes.Ldc_I4_1)],
            output = [new CodeInstruction(OpCodes.Switch, new[] { sourceLabel, sourceLabel }), target],
        };

        List<CodeInstruction> result = Run(
            [rule],
            [new CodeInstruction(OpCodes.Ldc_I4_1), new CodeInstruction(OpCodes.Ldc_I4_1)]);

        Label[][] switchTargets =
        [
            .. result
                .Where(instruction => instruction.opcode == OpCodes.Switch)
                .Select(instruction => (Label[])instruction.operand),
        ];
        Assert.That(switchTargets, Has.Length.EqualTo(2));
        Assert.That(switchTargets[0][0], Is.EqualTo(switchTargets[0][1]));
        Assert.That(switchTargets[1][0], Is.EqualTo(switchTargets[1][1]));
        Assert.That(switchTargets[0][0], Is.Not.EqualTo(switchTargets[1][0]));
        Assert.That(result.Count(instruction => instruction.labels.Contains(switchTargets[0][0])), Is.EqualTo(1));
        Assert.That(result.Count(instruction => instruction.labels.Contains(switchTargets[1][0])), Is.EqualTo(1));
    }

    [Test]
    public void CrossRuleLabelIsReusedAcrossPhases()
    {
        var dynamicMethod = new DynamicMethod("InstructionMatcherCrossRuleLabelTest", typeof(void), Type.EmptyTypes);
        ILGenerator generator = dynamicMethod.GetILGenerator();
        generator.DefineLabel();
        Label sourceLabel = PatchProcessor.CreateILGenerator().DefineLabel();
        var target = new CodeInstruction(OpCodes.Nop);
        target.labels.Add(sourceLabel);
        var ruleset = new Ruleset(new List<Rule>
        {
            new()
            {
                phase = 1,
                mode = OutputMode.MethodPrefix,
                output = [new CodeInstruction(OpCodes.Br, sourceLabel)],
            },
            new()
            {
                phase = 2,
                mode = OutputMode.MethodPostfix,
                output = [target],
            },
        });
        ruleset.crossRuleLabels.Add(sourceLabel);
        List<CodeInstruction> instructions = [new CodeInstruction(OpCodes.Ret)];

        ruleset.MatchAndReplace(TargetMethod, ref instructions, generator);

        Label branchTarget = (Label)instructions.Single(instruction => instruction.opcode == OpCodes.Br).operand;
        Assert.That(instructions.Single(instruction => instruction.labels.Contains(branchTarget)), Is.Not.SameAs(target));
        Assert.That(branchTarget, Is.Not.EqualTo(sourceLabel));
    }

    [Test]
    public void CrossRuleSwitchLabelIsReusedAcrossPhases()
    {
        var dynamicMethod = new DynamicMethod("InstructionMatcherCrossRuleSwitchLabelTest", typeof(void), Type.EmptyTypes);
        ILGenerator generator = dynamicMethod.GetILGenerator();
        generator.DefineLabel();
        Label sourceLabel = PatchProcessor.CreateILGenerator().DefineLabel();
        var target = new CodeInstruction(OpCodes.Nop);
        target.labels.Add(sourceLabel);
        var ruleset = new Ruleset(new List<Rule>
        {
            new()
            {
                phase = 1,
                mode = OutputMode.MethodPrefix,
                output = [new CodeInstruction(OpCodes.Switch, new[] { sourceLabel })],
            },
            new()
            {
                phase = 2,
                mode = OutputMode.MethodPostfix,
                output = [target],
            },
        });
        ruleset.crossRuleLabels.Add(sourceLabel);
        List<CodeInstruction> instructions = [new CodeInstruction(OpCodes.Ret)];

        ruleset.MatchAndReplace(TargetMethod, ref instructions, generator);

        Label switchTarget = ((Label[])instructions.Single(instruction => instruction.opcode == OpCodes.Switch).operand).Single();
        Assert.That(instructions.Single(instruction => instruction.labels.Contains(switchTarget)), Is.Not.SameAs(target));
        Assert.That(switchTarget, Is.Not.EqualTo(sourceLabel));
    }

    [Test]
    public void ReplacementReusesLocalBuilderMatchedByPattern()
    {
        LocalBuilder patternLocal = PatchProcessor.CreateILGenerator().DeclareLocal(typeof(int));
        LocalBuilder targetLocal = PatchProcessor.CreateILGenerator().DeclareLocal(typeof(int));
        var rule = new Rule
        {
            mode = OutputMode.Replace,
            pattern = [new CodeInstruction(OpCodes.Stloc_S, patternLocal), new CodeInstruction(OpCodes.Ldloc_S, patternLocal)],
            output = [new CodeInstruction(OpCodes.Ldloc_S, patternLocal)],
        };

        List<CodeInstruction> result = Run(
            [rule],
            [new CodeInstruction(OpCodes.Stloc_S, targetLocal), new CodeInstruction(OpCodes.Ldloc_S, targetLocal)]);

        CodeInstruction localLoad = result.Single(instruction => instruction.IsLdloc());
        Assert.That(localLoad.operand, Is.SameAs(targetLocal));
    }

    [Test]
    public void ReplacementReusesLocalMatchedByAddressPattern()
    {
        LocalBuilder patternLocal = PatchProcessor.CreateILGenerator().DeclareLocal(typeof(int));
        LocalBuilder targetLocal = PatchProcessor.CreateILGenerator().DeclareLocal(typeof(int));
        var rule = new Rule
        {
            mode = OutputMode.Replace,
            pattern = [new CodeInstruction(OpCodes.Ldloca_S, patternLocal)],
            output = [new CodeInstruction(OpCodes.Ldloca_S, patternLocal)],
        };

        List<CodeInstruction> result = Run([rule], [new CodeInstruction(OpCodes.Ldloca_S, targetLocal)]);

        CodeInstruction addressLoad = result.Single(instruction => instruction.opcode == OpCodes.Ldloca_S);
        Assert.That(addressLoad.operand, Is.SameAs(targetLocal));
    }

    [Test]
    public void AddressPatternMatchesDifferentLocalOpcodeEncoding()
    {
        LocalBuilder patternLocal = PatchProcessor.CreateILGenerator().DeclareLocal(typeof(int));
        LocalBuilder targetLocal = PatchProcessor.CreateILGenerator().DeclareLocal(typeof(int));
        var rule = new Rule
        {
            mode = OutputMode.Replace,
            pattern = [new CodeInstruction(OpCodes.Ldloca_S, patternLocal)],
            output = [new CodeInstruction(OpCodes.Ldloca_S, patternLocal)],
        };

        List<CodeInstruction> result = Run([rule], [new CodeInstruction(OpCodes.Ldloca, targetLocal)]);

        CodeInstruction addressLoad = result.Single(instruction => instruction.opcode == OpCodes.Ldloca_S);
        Assert.That(addressLoad.operand, Is.SameAs(targetLocal));
    }

    [Test]
    public void Pattern_LdlocS_Instruction_Ldloc_Matches()
    {
        var rule = new Rule
        {
            mode = OutputMode.Replace,
            pattern = [new CodeInstruction(OpCodes.Ldloc_S, 4)],
            output = [new CodeInstruction(OpCodes.Nop)],
        };

        List<CodeInstruction> result = Run([rule], [new CodeInstruction(OpCodes.Ldloc, 4)]);

        Assert.That(MeaningfulOpCodes(result), Is.EqualTo(new[] { OpCodes.Nop }));
    }

    [Test]
    public void Pattern_Ldloc1_Instruction_LdlocS_Matches()
    {
        var rule = new Rule
        {
            mode = OutputMode.Replace,
            pattern = [new CodeInstruction(OpCodes.Ldloc_1)],
            output = [new CodeInstruction(OpCodes.Nop)],
        };

        List<CodeInstruction> result = Run([rule], [new CodeInstruction(OpCodes.Ldloc_S, 1)]);

        Assert.That(MeaningfulOpCodes(result), Is.EqualTo(new[] { OpCodes.Nop }));
    }

    [Test]
    public void Pattern_StlocS_Instruction_Stloc1_Matches()
    {
        var rule = new Rule
        {
            mode = OutputMode.Replace,
            pattern = [new CodeInstruction(OpCodes.Stloc_S, 1)],
            output = [new CodeInstruction(OpCodes.Nop)],
        };

        List<CodeInstruction> result = Run([rule], [new CodeInstruction(OpCodes.Stloc_1)]);

        Assert.That(MeaningfulOpCodes(result), Is.EqualTo(new[] { OpCodes.Nop }));
    }

    [Test]
    public void Pattern_LdargS_Instruction_Ldarg_Matches()
    {
        var rule = new Rule
        {
            mode = OutputMode.Replace,
            pattern = [new CodeInstruction(OpCodes.Ldarg_S, 4)],
            output = [new CodeInstruction(OpCodes.Nop)],
        };

        List<CodeInstruction> result = Run([rule], [new CodeInstruction(OpCodes.Ldarg, 4)]);

        Assert.That(MeaningfulOpCodes(result), Is.EqualTo(new[] { OpCodes.Nop }));
    }

    [Test]
    public void Pattern_Ldarg1_Instruction_LdargS_Matches()
    {
        var rule = new Rule
        {
            mode = OutputMode.Replace,
            pattern = [new CodeInstruction(OpCodes.Ldarg_1)],
            output = [new CodeInstruction(OpCodes.Nop)],
        };

        List<CodeInstruction> result = Run([rule], [new CodeInstruction(OpCodes.Ldarg_S, 1)]);

        Assert.That(MeaningfulOpCodes(result), Is.EqualTo(new[] { OpCodes.Nop }));
    }

    [Test]
    public void Pattern_LdargaS_Instruction_Ldarga_Matches()
    {
        var rule = new Rule
        {
            mode = OutputMode.Replace,
            pattern = [new CodeInstruction(OpCodes.Ldarga_S, 4)],
            output = [new CodeInstruction(OpCodes.Nop)],
        };

        List<CodeInstruction> result = Run([rule], [new CodeInstruction(OpCodes.Ldarga, 4)]);

        Assert.That(MeaningfulOpCodes(result), Is.EqualTo(new[] { OpCodes.Nop }));
    }

    [Test]
    public void Pattern_StargS_Instruction_Starg_Matches()
    {
        var rule = new Rule
        {
            mode = OutputMode.Replace,
            pattern = [new CodeInstruction(OpCodes.Starg_S, 4)],
            output = [new CodeInstruction(OpCodes.Nop)],
        };

        List<CodeInstruction> result = Run([rule], [new CodeInstruction(OpCodes.Starg, 4)]);

        Assert.That(MeaningfulOpCodes(result), Is.EqualTo(new[] { OpCodes.Nop }));
    }

    [Test]
    public void Pattern_LdcI4_1_Instruction_LdcI4_Matches()
    {
        var rule = new Rule
        {
            mode = OutputMode.Replace,
            pattern = [new CodeInstruction(OpCodes.Ldc_I4_1)],
            output = [new CodeInstruction(OpCodes.Nop)],
        };

        List<CodeInstruction> result = Run([rule], [new CodeInstruction(OpCodes.Ldc_I4, 1)]);

        Assert.That(MeaningfulOpCodes(result), Is.EqualTo(new[] { OpCodes.Nop }));
    }

    [Test]
    public void Pattern_LdcI4_Instruction_LdcI4_1_Matches()
    {
        var rule = new Rule
        {
            mode = OutputMode.Replace,
            pattern = [new CodeInstruction(OpCodes.Ldc_I4, 1)],
            output = [new CodeInstruction(OpCodes.Nop)],
        };

        List<CodeInstruction> result = Run([rule], [new CodeInstruction(OpCodes.Ldc_I4_1)]);

        Assert.That(MeaningfulOpCodes(result), Is.EqualTo(new[] { OpCodes.Nop }));
    }

    [Test]
    public void Pattern_LdcI4S_Instruction_LdcI4_Matches()
    {
        var rule = new Rule
        {
            mode = OutputMode.Replace,
            pattern = [new CodeInstruction(OpCodes.Ldc_I4_S, (sbyte)42)],
            output = [new CodeInstruction(OpCodes.Nop)],
        };

        List<CodeInstruction> result = Run([rule], [new CodeInstruction(OpCodes.Ldc_I4, 42)]);

        Assert.That(MeaningfulOpCodes(result), Is.EqualTo(new[] { OpCodes.Nop }));
    }

    [Test]
    public void Pattern_BrS_Instruction_Br_Matches()
    {
        Label target = PatchProcessor.CreateILGenerator().DefineLabel();
        var rule = new Rule
        {
            mode = OutputMode.Replace,
            pattern = [new CodeInstruction(OpCodes.Br_S, target)],
            output = [new CodeInstruction(OpCodes.Nop)],
        };

        List<CodeInstruction> result = Run([rule], [new CodeInstruction(OpCodes.Br, target)]);

        Assert.That(MeaningfulOpCodes(result), Is.EqualTo(new[] { OpCodes.Nop }));
    }

    [Test]
    public void Pattern_Br_Instruction_Br_WithSameLabel_Matches()
    {
        Label target = PatchProcessor.CreateILGenerator().DefineLabel();
        var rule = new Rule
        {
            mode = OutputMode.Replace,
            pattern = [new CodeInstruction(OpCodes.Br, target)],
            output = [new CodeInstruction(OpCodes.Nop)],
        };

        List<CodeInstruction> result = Run([rule], [new CodeInstruction(OpCodes.Br, target)]);

        Assert.That(MeaningfulOpCodes(result), Is.EqualTo(new[] { OpCodes.Nop }));
    }

    [Test]
    public void Pattern_Br_Instruction_Br_BindsEquivalentTargetLabel()
    {
        ILGenerator sourceGenerator = PatchProcessor.CreateILGenerator();
        Label patternTarget = sourceGenerator.DefineLabel();
        Label instructionTarget = sourceGenerator.DefineLabel();
        var targetInstruction = new CodeInstruction(OpCodes.Nop);
        targetInstruction.labels.Add(instructionTarget);
        var rule = new Rule
        {
            mode = OutputMode.Replace,
            pattern = [new CodeInstruction(OpCodes.Br, patternTarget)],
            output = [new CodeInstruction(OpCodes.Br, patternTarget)],
        };

        List<CodeInstruction> result = Run(
            [rule],
            [new CodeInstruction(OpCodes.Br, instructionTarget), targetInstruction]);

        Label emittedTarget = (Label)result.Single(instruction => instruction.opcode == OpCodes.Br).operand;
        Assert.That(result.Single(instruction => instruction.labels.Contains(emittedTarget)).opcode, Is.EqualTo(OpCodes.Nop));
    }

    [Test]
    public void Pattern_Switch_Instruction_Switch_BindsEquivalentTargetLabels()
    {
        ILGenerator sourceGenerator = PatchProcessor.CreateILGenerator();
        Label patternCase0 = sourceGenerator.DefineLabel();
        Label patternCase1 = sourceGenerator.DefineLabel();
        Label instructionCase0 = sourceGenerator.DefineLabel();
        Label instructionCase1 = sourceGenerator.DefineLabel();
        var case0Target = new CodeInstruction(OpCodes.Ldc_I4_0);
        case0Target.labels.Add(instructionCase0);
        var case1Target = new CodeInstruction(OpCodes.Ldc_I4_1);
        case1Target.labels.Add(instructionCase1);
        var rule = new Rule
        {
            mode = OutputMode.Replace,
            pattern = [new CodeInstruction(OpCodes.Switch, new[] { patternCase0, patternCase1, patternCase0 })],
            output = [new CodeInstruction(OpCodes.Switch, new[] { patternCase0, patternCase1, patternCase0 })],
        };

        List<CodeInstruction> result = Run(
            [rule],
            [
                new CodeInstruction(OpCodes.Switch, new[] { instructionCase0, instructionCase1, instructionCase0 }),
                case0Target,
                case1Target,
            ]);

        var emittedTargets = (Label[])result.Single(instruction => instruction.opcode == OpCodes.Switch).operand;
        Label emittedCase0 = result.Single(instruction => instruction.opcode == OpCodes.Ldc_I4_0).labels.Single();
        Label emittedCase1 = result.Single(instruction => instruction.opcode == OpCodes.Ldc_I4_1).labels.Single();
        Assert.That(emittedTargets, Is.EqualTo(new[] { emittedCase0, emittedCase1, emittedCase0 }));
    }

    [Test]
    public void Pattern_BrtrueS_Instruction_Brtrue_Matches()
    {
        Label target = PatchProcessor.CreateILGenerator().DefineLabel();
        var rule = new Rule
        {
            mode = OutputMode.Replace,
            pattern = [new CodeInstruction(OpCodes.Brtrue_S, target)],
            output = [new CodeInstruction(OpCodes.Nop)],
        };

        List<CodeInstruction> result = Run([rule], [new CodeInstruction(OpCodes.Brtrue, target)]);

        Assert.That(MeaningfulOpCodes(result), Is.EqualTo(new[] { OpCodes.Nop }));
    }

    [Test]
    public void Pattern_BeqS_Instruction_Beq_Matches()
    {
        Label target = PatchProcessor.CreateILGenerator().DefineLabel();
        var rule = new Rule
        {
            mode = OutputMode.Replace,
            pattern = [new CodeInstruction(OpCodes.Beq_S, target)],
            output = [new CodeInstruction(OpCodes.Nop)],
        };

        List<CodeInstruction> result = Run([rule], [new CodeInstruction(OpCodes.Beq, target)]);

        Assert.That(MeaningfulOpCodes(result), Is.EqualTo(new[] { OpCodes.Nop }));
    }

    [Test]
    public void Pattern_LeaveS_Instruction_Leave_Matches()
    {
        Label target = PatchProcessor.CreateILGenerator().DefineLabel();
        var rule = new Rule
        {
            mode = OutputMode.Replace,
            pattern = [new CodeInstruction(OpCodes.Leave_S, target)],
            output = [new CodeInstruction(OpCodes.Nop)],
        };

        List<CodeInstruction> result = Run([rule], [new CodeInstruction(OpCodes.Leave, target)]);

        Assert.That(MeaningfulOpCodes(result), Is.EqualTo(new[] { OpCodes.Nop }));
    }

    [Test]
    public void RepeatedMatchesUseIndependentReplacementLocals()
    {
        LocalBuilder sourceLocal = PatchProcessor.CreateILGenerator().DeclareLocal(typeof(int));
        var rule = new Rule
        {
            min = 2,
            max = 0,
            mode = OutputMode.Replace,
            pattern = [new CodeInstruction(OpCodes.Nop)],
            output = [new CodeInstruction(OpCodes.Stloc_S, sourceLocal)],
        };

        List<CodeInstruction> result = Run([rule], [new CodeInstruction(OpCodes.Nop), new CodeInstruction(OpCodes.Nop)]);

        LocalBuilder[] emittedLocals =
        [
            .. result
                .Where(instruction => instruction.IsStloc())
                .Select(instruction => (LocalBuilder)instruction.operand),
        ];
        Assert.That(emittedLocals, Has.Length.EqualTo(2));
        Assert.That(emittedLocals[0], Is.Not.SameAs(emittedLocals[1]));
    }

    [Test]
    public void CrossRuleLocalIsReusedAcrossPhases()
    {
        var dynamicMethod = new DynamicMethod("InstructionMatcherCrossRuleLocalTest", typeof(void), Type.EmptyTypes);
        ILGenerator generator = dynamicMethod.GetILGenerator();
        LocalBuilder sourceLocal = PatchProcessor.CreateILGenerator().DeclareLocal(typeof(int));
        var ruleset = new Ruleset(new List<Rule>
        {
            new()
            {
                phase = 1,
                mode = OutputMode.MethodPrefix,
                output = [new CodeInstruction(OpCodes.Stloc_S, sourceLocal)],
            },
            new()
            {
                phase = 2,
                mode = OutputMode.MethodPostfix,
                output = [new CodeInstruction(OpCodes.Ldloc_S, sourceLocal)],
            },
        });
        ruleset.crossRuleLocals.Add(sourceLocal);
        List<CodeInstruction> instructions = [new CodeInstruction(OpCodes.Ret)];

        ruleset.MatchAndReplace(TargetMethod, ref instructions, generator);

        LocalBuilder storedLocal = (LocalBuilder)instructions.Single(instruction => instruction.IsStloc()).operand;
        LocalBuilder loadedLocal = (LocalBuilder)instructions.Single(instruction => instruction.IsLdloc()).operand;
        Assert.That(loadedLocal, Is.SameAs(storedLocal));
        Assert.That(storedLocal, Is.Not.SameAs(sourceLocal));
    }

    [Test]
    public void ReplacementMovesInputLabelAndExceptionBoundariesAroundOutput()
    {
        Label entryLabel = PatchProcessor.CreateILGenerator().DefineLabel();
        var matchStart = new CodeInstruction(OpCodes.Ldc_I4_1);
        matchStart.labels.Add(entryLabel);
        matchStart.blocks.Add(new ExceptionBlock(ExceptionBlockType.BeginExceptionBlock));
        var matchEnd = new CodeInstruction(OpCodes.Pop);
        matchEnd.blocks.Add(new ExceptionBlock(ExceptionBlockType.EndExceptionBlock));
        var rule = new Rule
        {
            mode = OutputMode.Replace,
            pattern = [new CodeInstruction(OpCodes.Ldc_I4_1), new CodeInstruction(OpCodes.Pop)],
            output = [new CodeInstruction(OpCodes.Ldc_I4_2)],
        };

        List<CodeInstruction> result = Run([rule], [matchStart, matchEnd, new CodeInstruction(OpCodes.Ret)]);

        CodeInstruction entryCarrier = result.Single(instruction => instruction.labels.Contains(entryLabel));
        CodeInstruction beginCarrier = result.Single(instruction =>
            instruction.blocks.Any(block => block.blockType == ExceptionBlockType.BeginExceptionBlock));
        CodeInstruction endCarrier = result.Single(instruction =>
            instruction.blocks.Any(block => block.blockType == ExceptionBlockType.EndExceptionBlock));
        CodeInstruction replacement = result.Single(instruction => instruction.opcode == OpCodes.Ldc_I4_2);
        Assert.That(entryCarrier, Is.SameAs(beginCarrier));
        Assert.That(entryCarrier.opcode, Is.EqualTo(OpCodes.Nop));
        Assert.That(endCarrier.opcode, Is.EqualTo(OpCodes.Nop));
        Assert.That(result.IndexOf(entryCarrier), Is.LessThan(result.IndexOf(replacement)));
        Assert.That(result.IndexOf(endCarrier), Is.GreaterThan(result.IndexOf(replacement)));
    }

    [Test]
    public void InsertBeforeMovesInputLabelAndExceptionStartAheadOfOutput()
    {
        Label entryLabel = PatchProcessor.CreateILGenerator().DefineLabel();
        var matchedInstruction = new CodeInstruction(OpCodes.Ldc_I4_1);
        matchedInstruction.labels.Add(entryLabel);
        matchedInstruction.blocks.Add(new ExceptionBlock(ExceptionBlockType.BeginExceptionBlock));
        var rule = new Rule
        {
            mode = OutputMode.InsertBefore,
            pattern = [new CodeInstruction(OpCodes.Ldc_I4_1)],
            output = [new CodeInstruction(OpCodes.Ldc_I4_2)],
        };

        List<CodeInstruction> result = Run([rule], [matchedInstruction, new CodeInstruction(OpCodes.Ret)]);

        CodeInstruction entryCarrier = result.Single(instruction => instruction.labels.Contains(entryLabel));
        CodeInstruction inserted = result.Single(instruction => instruction.opcode == OpCodes.Ldc_I4_2);
        CodeInstruction original = result.Single(instruction => instruction.opcode == OpCodes.Ldc_I4_1);
        Assert.That(entryCarrier.opcode, Is.EqualTo(OpCodes.Nop));
        Assert.That(entryCarrier.blocks.Select(block => block.blockType),
            Does.Contain(ExceptionBlockType.BeginExceptionBlock));
        Assert.That(original.labels, Is.Empty);
        Assert.That(original.blocks.Select(block => block.blockType),
            Does.Not.Contain(ExceptionBlockType.BeginExceptionBlock));
        Assert.That(result.IndexOf(entryCarrier), Is.LessThan(result.IndexOf(inserted)));
        Assert.That(result.IndexOf(inserted), Is.LessThan(result.IndexOf(original)));
    }

    [Test]
    public void InsertAfterMovesInputExceptionEndBehindOutput()
    {
        var tryStart = new CodeInstruction(OpCodes.Nop);
        tryStart.blocks.Add(new ExceptionBlock(ExceptionBlockType.BeginExceptionBlock));
        var matchedInstruction = new CodeInstruction(OpCodes.Pop);
        matchedInstruction.blocks.Add(new ExceptionBlock(ExceptionBlockType.EndExceptionBlock));
        var rule = new Rule
        {
            mode = OutputMode.InsertAfter,
            pattern = [new CodeInstruction(OpCodes.Pop)],
            output = [new CodeInstruction(OpCodes.Ldc_I4_1)],
        };

        List<CodeInstruction> result = Run([rule], [tryStart, matchedInstruction, new CodeInstruction(OpCodes.Ret)]);

        CodeInstruction original = result.Single(instruction => instruction.opcode == OpCodes.Pop);
        CodeInstruction inserted = result.Single(instruction => instruction.opcode == OpCodes.Ldc_I4_1);
        CodeInstruction endCarrier = result.Single(instruction =>
            instruction.blocks.Any(block => block.blockType == ExceptionBlockType.EndExceptionBlock));
        Assert.That(original.blocks.Select(block => block.blockType),
            Does.Not.Contain(ExceptionBlockType.EndExceptionBlock));
        Assert.That(endCarrier.opcode, Is.EqualTo(OpCodes.Nop));
        Assert.That(result.IndexOf(original), Is.LessThan(result.IndexOf(inserted)));
        Assert.That(result.IndexOf(inserted), Is.LessThan(result.IndexOf(endCarrier)));
    }

    [Test]
    public void ReplacementDoesNotConsumeInteriorBranchTarget()
    {
        Label interiorLabel = PatchProcessor.CreateILGenerator().DefineLabel();
        var interiorInstruction = new CodeInstruction(OpCodes.Pop);
        interiorInstruction.labels.Add(interiorLabel);
        var rule = new Rule
        {
            mode = OutputMode.Replace,
            pattern = [new CodeInstruction(OpCodes.Ldc_I4_1), new CodeInstruction(OpCodes.Pop)],
            output = [new CodeInstruction(OpCodes.Nop)],
        };

        var exception = Assert.Throws<InvalidOperationException>(() => Run(
            [rule],
            [new CodeInstruction(OpCodes.Ldc_I4_1), interiorInstruction]));

        Assert.That(exception!.Message, Does.StartWith("Not enough matches found"));
    }

    [Test]
    public void ReplacementDoesNotConsumeInteriorExceptionStart()
    {
        var interiorInstruction = new CodeInstruction(OpCodes.Pop);
        interiorInstruction.blocks.Add(new ExceptionBlock(ExceptionBlockType.BeginExceptionBlock));
        var rule = new Rule
        {
            mode = OutputMode.Replace,
            pattern = [new CodeInstruction(OpCodes.Ldc_I4_1), new CodeInstruction(OpCodes.Pop)],
            output = [new CodeInstruction(OpCodes.Nop)],
        };

        var exception = Assert.Throws<InvalidOperationException>(() => Run(
            [rule],
            [new CodeInstruction(OpCodes.Ldc_I4_1), interiorInstruction]));

        Assert.That(exception!.Message, Does.StartWith("Not enough matches found"));
    }

    [Test]
    public void LaterPhaseTransformsOutputFromEarlierPhase()
    {
        var firstPhase = new Rule
        {
            phase = 1,
            mode = OutputMode.Replace,
            pattern = [new CodeInstruction(OpCodes.Ldc_I4_1)],
            output = [new CodeInstruction(OpCodes.Ldc_I4_2)],
        };
        var secondPhase = new Rule
        {
            phase = 2,
            mode = OutputMode.Replace,
            pattern = [new CodeInstruction(OpCodes.Ldc_I4_2)],
            output = [new CodeInstruction(OpCodes.Ldc_I4_3)],
        };

        List<CodeInstruction> result = Run(
            [secondPhase, firstPhase],
            [new CodeInstruction(OpCodes.Ldc_I4_1), new CodeInstruction(OpCodes.Pop)]);

        Assert.That(MeaningfulOpCodes(result), Is.EqualTo(new[] { OpCodes.Ldc_I4_3, OpCodes.Pop }));
    }

    [Test]
    public void ReplacementLabelRemainsAttachedWhenLocalOperandIsTransformed()
    {
        ILGenerator sourceGenerator = PatchProcessor.CreateILGenerator();
        Label sourceLabel = sourceGenerator.DefineLabel();
        LocalBuilder sourceLocal = sourceGenerator.DeclareLocal(typeof(int));
        var labelledLocalLoad = new CodeInstruction(OpCodes.Ldloc_S, sourceLocal);
        labelledLocalLoad.labels.Add(sourceLabel);
        var rule = new Rule
        {
            mode = OutputMode.Replace,
            pattern = [new CodeInstruction(OpCodes.Nop)],
            output = [new CodeInstruction(OpCodes.Br, sourceLabel), labelledLocalLoad],
        };

        List<CodeInstruction> result = Run([rule], [new CodeInstruction(OpCodes.Nop)]);

        Label branchTarget = (Label)result.Single(instruction => instruction.opcode == OpCodes.Br).operand;
        CodeInstruction emittedLocalLoad = result.Single(instruction => instruction.IsLdloc());
        Assert.That(emittedLocalLoad.labels, Does.Contain(branchTarget));
        Assert.That(emittedLocalLoad.operand, Is.TypeOf<LocalBuilder>());
    }

    [Test]
    public void ReplacementExceptionBlockRemainsAttachedWhenLocalOperandIsTransformed()
    {
        LocalBuilder sourceLocal = PatchProcessor.CreateILGenerator().DeclareLocal(typeof(int));
        var localLoad = new CodeInstruction(OpCodes.Ldloc_S, sourceLocal);
        localLoad.blocks.Add(new ExceptionBlock(ExceptionBlockType.BeginExceptionBlock));
        var exceptionEnd = new CodeInstruction(OpCodes.Nop);
        exceptionEnd.blocks.Add(new ExceptionBlock(ExceptionBlockType.EndExceptionBlock));
        var rule = new Rule
        {
            mode = OutputMode.Replace,
            pattern = [new CodeInstruction(OpCodes.Nop)],
            output = [localLoad, exceptionEnd],
        };

        List<CodeInstruction> result = Run([rule], [new CodeInstruction(OpCodes.Nop)]);

        CodeInstruction emittedLocalLoad = result.Single(instruction => instruction.IsLdloc());
        Assert.That(emittedLocalLoad.blocks.Select(block => block.blockType),
            Does.Contain(ExceptionBlockType.BeginExceptionBlock));
        Assert.That(result.SelectMany(instruction => instruction.blocks).Select(block => block.blockType),
            Does.Contain(ExceptionBlockType.EndExceptionBlock));
    }

    [Test]
    public void MethodPrefixesAndPostfixesUseDescendingPriority()
    {
        var lowPriorityPrefix = new Rule
        {
            priority = 1,
            mode = OutputMode.MethodPrefix,
            output = [new CodeInstruction(OpCodes.Ldc_I4_1)],
        };
        var highPriorityPrefix = new Rule
        {
            priority = 2,
            mode = OutputMode.MethodPrefix,
            output = [new CodeInstruction(OpCodes.Ldc_I4_2)],
        };
        var lowPriorityPostfix = new Rule
        {
            priority = 1,
            mode = OutputMode.MethodPostfix,
            output = [new CodeInstruction(OpCodes.Ldc_I4_3)],
        };
        var highPriorityPostfix = new Rule
        {
            priority = 2,
            mode = OutputMode.MethodPostfix,
            output = [new CodeInstruction(OpCodes.Ldc_I4_4)],
        };

        List<CodeInstruction> result = Run(
            [lowPriorityPrefix, highPriorityPrefix, lowPriorityPostfix, highPriorityPostfix],
            [new CodeInstruction(OpCodes.Ret)]);

        Assert.That(MeaningfulOpCodes(result), Is.EqualTo(new[]
        {
            OpCodes.Ldc_I4_2,
            OpCodes.Ldc_I4_1,
            OpCodes.Ret,
            OpCodes.Ldc_I4_4,
            OpCodes.Ldc_I4_3,
        }));
    }

    [Test]
    public void ReplacementHonorsMaximumMatchCount()
    {
        var rule = new Rule
        {
            min = 2,
            max = 2,
            mode = OutputMode.Replace,
            pattern = [new CodeInstruction(OpCodes.Ldc_I4_1)],
            output = [new CodeInstruction(OpCodes.Ldc_I4_2)],
        };

        List<CodeInstruction> result = Run(
            [rule],
            [
                new CodeInstruction(OpCodes.Ldc_I4_1),
                new CodeInstruction(OpCodes.Ldc_I4_1),
                new CodeInstruction(OpCodes.Ldc_I4_1),
            ]);

        Assert.That(MeaningfulOpCodes(result), Is.EqualTo(new[]
        {
            OpCodes.Ldc_I4_2,
            OpCodes.Ldc_I4_2,
            OpCodes.Ldc_I4_1,
        }));
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
        ILGenerator generator = dynamicMethod.GetILGenerator();
        for (var i = 0; i < 16; i++)
            generator.DefineLabel();
        return Ruleset.MatchAndReplace(rules, TargetMethod, instructions, generator);
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
