using System.Reflection.Emit;

using Disharmony.Optimizer;
using HarmonyLib;

namespace Disharmony.Tests;

[TestFixture]
public sealed class ControlFlowGraphGeneratorTests
{
    private static readonly MethodInfo VoidMethod =
        typeof(ControlFlowGraphTargets).GetMethod(nameof(ControlFlowGraphTargets.ReturnVoid))!;

    private static readonly MethodInfo IntMethod =
        typeof(ControlFlowGraphTargets).GetMethod(nameof(ControlFlowGraphTargets.ReturnInt))!;

    private static readonly MethodInfo TwoArgumentMethod =
        typeof(ControlFlowGraphTargets).GetMethod(nameof(ControlFlowGraphTargets.Add))!;

    private static readonly MethodInfo VoidTwoArgumentMethod =
        typeof(ControlFlowGraphTargets).GetMethod(nameof(ControlFlowGraphTargets.Consume))!;

    private static readonly MethodInfo InstanceMethod =
        typeof(ControlFlowGraphInstanceTarget).GetMethod(nameof(ControlFlowGraphInstanceTarget.Add))!;

    private static readonly ConstructorInfo Constructor =
        typeof(ControlFlowGraphInstanceTarget).GetConstructor([typeof(int)])!;

    private static readonly FieldInfo InstanceField =
        typeof(ControlFlowGraphInstanceTarget).GetField(nameof(ControlFlowGraphInstanceTarget.Value))!;

    [Test]
    public void Metadata_StaticMethod_CreatesDeclaredArgumentsAndReturnType()
    {
        var generator = CreateGenerator(TwoArgumentMethod, ThrowTerminated());

        Assert.That(generator.Arguments.Keys, Is.EqualTo(new[] { 0, 1 }));
        Assert.That(generator.Arguments[0].Type, Is.EqualTo(typeof(int)));
        Assert.That(generator.Arguments[1].Type, Is.EqualTo(typeof(int)));
        Assert.That(generator.ReturnType, Is.EqualTo(typeof(int)));
    }

    [Test]
    public void Metadata_InstanceMethod_IncludesThisBeforeDeclaredArguments()
    {
        var generator = CreateGenerator(InstanceMethod, ThrowTerminated());

        Assert.That(generator.Arguments.Keys, Is.EqualTo(new[] { 0, 1 }));
        Assert.That(generator.Arguments[0].Type, Is.EqualTo(typeof(ControlFlowGraphInstanceTarget)));
        Assert.That(generator.Arguments[1].Type, Is.EqualTo(typeof(int)));
    }

    [Test]
    public void Locals_MethodBodyLocal_IsCreatedWithoutLocalBuilder()
    {
        MethodInfo method = typeof(ControlFlowGraphTargets)
            .GetMethod(nameof(ControlFlowGraphTargets.MethodWithLocal))!;
        LocalVariableInfo metadataLocal = method.GetMethodBody()!.LocalVariables[0];
        Assert.That(metadataLocal.LocalType, Is.EqualTo(typeof(int)));

        var generator = CreateGenerator(method, ThrowTerminated());

        Assert.That(generator.Locals[metadataLocal.LocalIndex].Type, Is.EqualTo(typeof(int)));
        Assert.That(generator.Locals[metadataLocal.LocalIndex].LocalBuilder, Is.Null);
    }

    [Test]
    public void Locals_LocalBuilderOnly_IsCreatedWithBuilder()
    {
        LocalBuilder builder = PatchProcessor.CreateILGenerator().DeclareLocal(typeof(string));

        var generator = CreateGenerator(
            VoidMethod,
            [new CodeInstruction(OpCodes.Ldloc_S, builder), new CodeInstruction(OpCodes.Pop), .. ThrowTerminated()]);

        Assert.That(generator.Locals[builder.LocalIndex].Type, Is.EqualTo(typeof(string)));
        Assert.That(generator.Locals[builder.LocalIndex].LocalBuilder, Is.SameAs(builder));
    }

    [Test]
    public void Locals_MetadataLocalAndMatchingBuilder_AreMerged()
    {
        MethodInfo method = typeof(ControlFlowGraphTargets)
            .GetMethod(nameof(ControlFlowGraphTargets.MethodWithLocal))!;
        LocalVariableInfo metadataLocal = method.GetMethodBody()!.LocalVariables[0];
        Assert.That(metadataLocal.LocalType, Is.EqualTo(typeof(int)));
        ILGenerator localGenerator = PatchProcessor.CreateILGenerator();
        LocalBuilder builder = null!;
        for (var index = 0; index <= metadataLocal.LocalIndex; index++)
            builder = localGenerator.DeclareLocal(typeof(int));

        var generator = CreateGenerator(
            method,
            [new CodeInstruction(OpCodes.Ldloc_S, builder), new CodeInstruction(OpCodes.Pop), .. ThrowTerminated()]);

        Assert.That(generator.Locals[metadataLocal.LocalIndex].LocalBuilder, Is.SameAs(builder));
    }

    [Test]
    public void Locals_MetadataLocalAndConflictingBuilder_Throws()
    {
        MethodInfo method = typeof(ControlFlowGraphTargets)
            .GetMethod(nameof(ControlFlowGraphTargets.MethodWithLocal))!;
        LocalVariableInfo metadataLocal = method.GetMethodBody()!.LocalVariables[0];
        Assert.That(metadataLocal.LocalType, Is.EqualTo(typeof(int)));
        ILGenerator localGenerator = PatchProcessor.CreateILGenerator();
        LocalBuilder builder = null!;
        for (var index = 0; index <= metadataLocal.LocalIndex; index++)
            builder = localGenerator.DeclareLocal(typeof(string));
        var generator = new ControlFlowGraphGenerator(
            method,
            [new CodeInstruction(OpCodes.Ldloc_S, builder), new CodeInstruction(OpCodes.Pop), .. ThrowTerminated()]);

        Assert.Throws<InvalidOperationException>(() => generator.CreateControlFlowGraph());
    }

    [Test]
    public void StackBehaviourPop_EveryFixedFormCreatesExpectedInputs()
    {
        (StackBehaviour Behaviour, CodeInstruction Instruction, int InputCount)[] cases =
        [
            (StackBehaviour.Pop0, new CodeInstruction(OpCodes.Nop), 0),
            (StackBehaviour.Pop1, new CodeInstruction(OpCodes.Pop), 1),
            (StackBehaviour.Pop1_pop1, new CodeInstruction(OpCodes.Add), 2),
            (StackBehaviour.Popi, new CodeInstruction(OpCodes.Initobj, typeof(int)), 1),
            (StackBehaviour.Popi_pop1, new CodeInstruction(OpCodes.Stobj, typeof(int)), 2),
            (StackBehaviour.Popi_popi, new CodeInstruction(OpCodes.Stind_I4), 2),
            (StackBehaviour.Popi_popi8, new CodeInstruction(OpCodes.Stind_I8), 2),
            (StackBehaviour.Popi_popi_popi, new CodeInstruction(OpCodes.Cpblk), 3),
            (StackBehaviour.Popi_popr4, new CodeInstruction(OpCodes.Stind_R4), 2),
            (StackBehaviour.Popi_popr8, new CodeInstruction(OpCodes.Stind_R8), 2),
            (StackBehaviour.Popref, new CodeInstruction(OpCodes.Castclass, typeof(object)), 1),
            (StackBehaviour.Popref_pop1, new CodeInstruction(OpCodes.Stfld, InstanceField), 2),
            (StackBehaviour.Popref_popi, new CodeInstruction(OpCodes.Ldelem_I4), 2),
            (StackBehaviour.Popref_popi_popi, new CodeInstruction(OpCodes.Stelem_I4), 3),
            (StackBehaviour.Popref_popi_popi8, new CodeInstruction(OpCodes.Stelem_I8), 3),
            (StackBehaviour.Popref_popi_popr4, new CodeInstruction(OpCodes.Stelem_R4), 3),
            (StackBehaviour.Popref_popi_popr8, new CodeInstruction(OpCodes.Stelem_R8), 3),
            (StackBehaviour.Popref_popi_popref, new CodeInstruction(OpCodes.Stelem_Ref), 3),
            (StackBehaviour.Popref_popi_pop1, new CodeInstruction(OpCodes.Stelem, typeof(object)), 3),
        ];
        Assert.That(cases.Select(testCase => testCase.Behaviour).Distinct(), Is.EquivalentTo(
            Enum.GetValues(typeof(StackBehaviour)).Cast<StackBehaviour>()
                .Where(behaviour => behaviour != StackBehaviour.Varpop && behaviour.ToString().StartsWith("Pop"))));

        foreach (var testCase in cases)
        {
            List<CodeInstruction> instructions =
            [.. Enumerable.Repeat(new CodeInstruction(OpCodes.Ldc_I4_0), testCase.InputCount), testCase.Instruction, .. ThrowTerminated()];
            var generator = CreateGenerator(VoidMethod, instructions);
            ILOp operation = GetILOp(generator, testCase.Instruction.opcode);

            Assert.That(operation.Inputs, Has.Count.EqualTo(testCase.InputCount), testCase.Behaviour.ToString());
        }
    }

    [Test]
    public void StackBehaviourPop_VarpopHandlesStaticInstanceAndConstructorOperands()
    {
        (CodeInstruction Instruction, int InputCount)[] cases =
        [
            (new CodeInstruction(OpCodes.Call, TwoArgumentMethod), 2),
            (new CodeInstruction(OpCodes.Callvirt, InstanceMethod), 2),
            (new CodeInstruction(OpCodes.Newobj, Constructor), 1),
        ];

        foreach (var testCase in cases)
        {
            List<CodeInstruction> instructions =
            [.. Enumerable.Repeat(new CodeInstruction(OpCodes.Ldc_I4_0), testCase.InputCount), testCase.Instruction, .. ThrowTerminated()];
            var generator = CreateGenerator(VoidMethod, instructions);
            ILOp operation = GetILOp(generator, testCase.Instruction.opcode);

            Assert.That(operation.Inputs, Has.Count.EqualTo(testCase.InputCount), testCase.Instruction.opcode.Name);
        }
    }

    [Test]
    public void StackBehaviourPush_EveryFixedFormProducesExpectedStackDepth()
    {
        (StackBehaviour Behaviour, CodeInstruction[] Instructions, int StackDepth)[] cases =
        [
            (StackBehaviour.Push0, [new CodeInstruction(OpCodes.Nop)], 0),
            (StackBehaviour.Push1, [new CodeInstruction(OpCodes.Ldarg_0)], 1),
            (StackBehaviour.Pushi, [new CodeInstruction(OpCodes.Ldc_I4_0)], 1),
            (StackBehaviour.Pushi8, [new CodeInstruction(OpCodes.Ldc_I8, 0L)], 1),
            (StackBehaviour.Pushr4, [new CodeInstruction(OpCodes.Ldc_R4, 0f)], 1),
            (StackBehaviour.Pushr8, [new CodeInstruction(OpCodes.Ldc_R8, 0d)], 1),
            (StackBehaviour.Pushref, [new CodeInstruction(OpCodes.Ldnull)], 1),
            (StackBehaviour.Push1_push1,
                [new CodeInstruction(OpCodes.Ldc_I4_0), new CodeInstruction(OpCodes.Dup)], 2),
        ];

        foreach (var testCase in cases)
        {
            var generator = CreateGenerator(TwoArgumentMethod, BranchToThrowTarget(testCase.Instructions));
            BasicBlock entry = FirstInstructionBlock(generator);

            Assert.That(generator.BlockStacks[entry.Label].OutgoingStack, Has.Count.EqualTo(testCase.StackDepth),
                testCase.Behaviour.ToString());
        }
    }

    [Test]
    public void StackBehaviourPush_VarpushUsesMethodReturnType()
    {
        var valueGenerator = CreateGenerator(
            VoidMethod,
            BranchToThrowTarget([new CodeInstruction(OpCodes.Call, IntMethod)]));
        var voidGenerator = CreateGenerator(
            VoidMethod,
            BranchToThrowTarget(
            [
                new CodeInstruction(OpCodes.Ldc_I4_0),
                new CodeInstruction(OpCodes.Ldc_I4_0),
                new CodeInstruction(OpCodes.Call, VoidTwoArgumentMethod),
            ]));

        BasicBlock valueEntry = FirstInstructionBlock(valueGenerator);
        BasicBlock voidEntry = FirstInstructionBlock(voidGenerator);
        Assert.That(valueGenerator.BlockStacks[valueEntry.Label].OutgoingStack, Has.Count.EqualTo(1));
        Assert.That(voidGenerator.BlockStacks[voidEntry.Label].OutgoingStack, Is.Empty);
    }

    [Test]
    public void Dup_ReusesSameStackValueTwiceWithoutCreatingAnILOp()
    {
        var generator = CreateGenerator(
            VoidMethod,
            BranchToThrowTarget([new CodeInstruction(OpCodes.Ldc_I4_1), new CodeInstruction(OpCodes.Dup)]));
        BasicBlock entry = FirstInstructionBlock(generator);
        List<StackSlot> outgoing = generator.BlockStacks[entry.Label].OutgoingStack;

        Assert.That(outgoing, Has.Count.EqualTo(2));
        Assert.That(outgoing[0], Is.SameAs(outgoing[1]));
        Assert.That(entry.Ops.SelectMany(Flatten).OfType<ILOp>().Any(operation => operation.IL.OpCode == OpCodes.Dup), Is.False);
    }

    [Test]
    public void Prefixes_AreAttachedInOrderToFollowingInstruction()
    {
        var generator = CreateGenerator(
            VoidMethod,
            [
                new CodeInstruction(OpCodes.Ldc_I4_0),
                new CodeInstruction(OpCodes.Unaligned, (byte)1),
                new CodeInstruction(OpCodes.Volatile),
                new CodeInstruction(OpCodes.Ldind_I4),
                new CodeInstruction(OpCodes.Pop),
                .. ThrowTerminated(),
            ]);

        ILOp load = GetILOp(generator, OpCodes.Ldind_I4);
        Assert.That(load.IL.Prefixes.Select(prefix => prefix.OpCode),
            Is.EqualTo(new[] { OpCodes.Unaligned, OpCodes.Volatile }));
        Assert.That(load.IL.Prefixes[0].Operand, Is.EqualTo((byte)1));
    }

    [Test]
    public void ControlFlow_Ret_VoidMethodCreatesReturnWithVoidValue()
    {
        var generator = CreateGenerator(VoidMethod, [new CodeInstruction(OpCodes.Ret)]);

        BasicBlock block = InstructionBlocks(generator).Single();
        Assert.That(block.Branch, Is.TypeOf<Return>());
        var branch = (Return)block.Branch;
        Assert.That(branch.IL.OpCode, Is.EqualTo(OpCodes.Ret));
        Assert.That(branch.Value, Is.TypeOf<VoidOp>());
        Assert.That(generator.BlockStacks[block.Label].OutgoingStack, Is.Empty);
    }

    [Test]
    public void ControlFlow_Ret_ValueMethodConsumesReturnValue()
    {
        var generator = CreateGenerator(
            IntMethod,
            [new CodeInstruction(OpCodes.Ldc_I4_1), new CodeInstruction(OpCodes.Ret)]);

        BasicBlock block = InstructionBlocks(generator).Single();
        Assert.That(block.Branch, Is.TypeOf<Return>());
        var branch = (Return)block.Branch;
        Assert.That(branch.IL.OpCode, Is.EqualTo(OpCodes.Ret));
        Assert.That(branch.Value, Is.TypeOf<StackSlot>());
        Assert.That(generator.BlockStacks[block.Label].OutgoingStack, Is.Empty);
    }

    [Test]
    public void ControlFlow_Throw_ConsumesExceptionAndHasNoSuccessor()
    {
        var generator = CreateGenerator(
            VoidMethod,
            [new CodeInstruction(OpCodes.Ldnull), new CodeInstruction(OpCodes.Throw)]);

        BasicBlock block = InstructionBlocks(generator).Single();
        Assert.That(block.Branch, Is.TypeOf<Throw>());
        Assert.That(((Throw)block.Branch).Exception, Is.TypeOf<StackSlot>());
        Assert.That(block.Branch.Labels, Is.Empty);
        Assert.That(generator.BlockStacks[block.Label].OutgoingStack, Is.Empty);
    }

    [Test]
    public void ControlFlow_Rethrow_HasNoInputAndNoSuccessor()
    {
        var generator = CreateGenerator(VoidMethod, [new CodeInstruction(OpCodes.Rethrow)]);

        BasicBlock block = InstructionBlocks(generator).Single();
        Assert.That(block.Branch, Is.TypeOf<Rethrow>());
        Assert.That(block.Branch.Labels, Is.Empty);
        Assert.That(generator.BlockStacks[block.Label].OutgoingStack, Is.Empty);
    }

    [Test]
    public void ControlFlow_UnconditionalBranch_ForwardWithoutCarriedStackCreatesEdge()
    {
        Label targetLabel = PatchProcessor.CreateILGenerator().DefineLabel();
        var target = new CodeInstruction(OpCodes.Ret);
        target.labels.Add(targetLabel);
        var generator = CreateGenerator(
            VoidMethod,
            [new CodeInstruction(OpCodes.Br, targetLabel), target]);

        BasicBlock source = InstructionBlocks(generator).First();
        BasicBlock destination = InstructionBlocks(generator).Last();
        Assert.That(source.Branch, Is.TypeOf<UnconditionalBranch>());
        Assert.That(((UnconditionalBranch)source.Branch).Label, Is.EqualTo(destination.Label));
        Edge edge = generator.ControlFlowGraph.Edges.Single(candidate => candidate.Source == source.Label);
        Assert.That(edge.Source, Is.EqualTo(source.Label));
        Assert.That(edge.Destination, Is.EqualTo(destination.Label));
        Assert.That(edge.EdgeAssignments, Is.Empty);
    }

    [Test]
    public void ControlFlow_UnconditionalBranch_ForwardWithCarriedStackCreatesAssignment()
    {
        Label targetLabel = PatchProcessor.CreateILGenerator().DefineLabel();
        var target = new CodeInstruction(OpCodes.Pop);
        target.labels.Add(targetLabel);
        var generator = CreateGenerator(
            VoidMethod,
            [new CodeInstruction(OpCodes.Ldc_I4_1), new CodeInstruction(OpCodes.Br, targetLabel), target,
                new CodeInstruction(OpCodes.Ret)]);

        BasicBlock source = InstructionBlocks(generator).First();
        BasicBlock destination = InstructionBlocks(generator).Last();
        Edge edge = generator.ControlFlowGraph.Edges.Single(candidate => candidate.Source == source.Label);
        Assert.That(generator.BlockStacks[source.Label].OutgoingStack, Has.Count.EqualTo(1));
        Assert.That(generator.BlockStacks[destination.Label].IncomingStack, Has.Count.EqualTo(1));
        Assert.That(edge.EdgeAssignments, Has.Count.EqualTo(1));
        Assert.That(edge.EdgeAssignments[0].Output,
            Is.SameAs(generator.BlockStacks[destination.Label].IncomingStack[0]));
        Assert.That(edge.EdgeAssignments[0].Input,
            Is.SameAs(generator.BlockStacks[source.Label].OutgoingStack[0]));
    }

    [Test]
    public void ControlFlow_UnconditionalBranch_BackwardWithoutCarriedStackCreatesEdge()
    {
        Label loopLabel = PatchProcessor.CreateILGenerator().DefineLabel();
        var loop = new CodeInstruction(OpCodes.Br, loopLabel);
        loop.labels.Add(loopLabel);
        var generator = CreateGenerator(
            VoidMethod,
            [new CodeInstruction(OpCodes.Br, loopLabel), loop]);

        BasicBlock loopBlock = InstructionBlocks(generator).Last();
        Assert.That(((UnconditionalBranch)loopBlock.Branch).Label, Is.EqualTo(loopBlock.Label));
        Assert.That(generator.ControlFlowGraph.Edges.Any(edge =>
            edge.Source == loopBlock.Label && edge.Destination == loopBlock.Label), Is.True);
    }

    [Test]
    public void ControlFlow_UnconditionalBranch_BackwardWithCarriedStackCreatesAssignment()
    {
        Label loopLabel = PatchProcessor.CreateILGenerator().DefineLabel();
        var loop = new CodeInstruction(OpCodes.Pop);
        loop.labels.Add(loopLabel);
        var generator = CreateGenerator(
            VoidMethod,
            [
                new CodeInstruction(OpCodes.Ldc_I4_0),
                new CodeInstruction(OpCodes.Br, loopLabel),
                loop,
                new CodeInstruction(OpCodes.Ldc_I4_1),
                new CodeInstruction(OpCodes.Br, loopLabel),
            ]);

        BasicBlock loopBlock = InstructionBlocks(generator).Last();
        Edge backEdge = generator.ControlFlowGraph.Edges.Single(edge => edge.Source == loopBlock.Label);
        Assert.That(generator.BlockStacks[loopBlock.Label].IncomingStack, Has.Count.EqualTo(1));
        Assert.That(generator.BlockStacks[loopBlock.Label].OutgoingStack, Has.Count.EqualTo(1));
        Assert.That(backEdge.EdgeAssignments, Has.Count.EqualTo(1));
    }

    [Test]
    public void ControlFlow_ConditionalBranches_EachOpcodeCreatesTakenAndFallthroughEdges()
    {
        (OpCode OpCode, int InputCount)[] cases =
        [
            (OpCodes.Brfalse, 1), (OpCodes.Brfalse_S, 1), (OpCodes.Brtrue, 1), (OpCodes.Brtrue_S, 1),
            (OpCodes.Beq, 2), (OpCodes.Beq_S, 2), (OpCodes.Bge, 2), (OpCodes.Bge_S, 2),
            (OpCodes.Bge_Un, 2), (OpCodes.Bge_Un_S, 2), (OpCodes.Bgt, 2), (OpCodes.Bgt_S, 2),
            (OpCodes.Bgt_Un, 2), (OpCodes.Bgt_Un_S, 2), (OpCodes.Ble, 2), (OpCodes.Ble_S, 2),
            (OpCodes.Ble_Un, 2), (OpCodes.Ble_Un_S, 2), (OpCodes.Blt, 2), (OpCodes.Blt_S, 2),
            (OpCodes.Blt_Un, 2), (OpCodes.Blt_Un_S, 2), (OpCodes.Bne_Un, 2), (OpCodes.Bne_Un_S, 2),
        ];

        foreach (var testCase in cases)
        {
            ILGenerator il = PatchProcessor.CreateILGenerator();
            Label targetLabel = il.DefineLabel();
            Label endLabel = il.DefineLabel();
            var target = new CodeInstruction(OpCodes.Br, endLabel);
            target.labels.Add(targetLabel);
            var end = new CodeInstruction(OpCodes.Ret);
            end.labels.Add(endLabel);
            List<CodeInstruction> instructions =
            [
                .. Enumerable.Repeat(new CodeInstruction(OpCodes.Ldc_I4_0), testCase.InputCount),
                new CodeInstruction(testCase.OpCode, targetLabel),
                new CodeInstruction(OpCodes.Br, endLabel),
                target,
                end,
            ];

            var generator = CreateGenerator(VoidMethod, instructions);
            BasicBlock source = FirstInstructionBlock(generator);
            Assert.That(source.Branch, Is.TypeOf<ConditionalBranch>(), testCase.OpCode.Name);
            var branch = (ConditionalBranch)source.Branch;
            Assert.That(branch.OpCode, Is.EqualTo(testCase.OpCode), testCase.OpCode.Name);
            Assert.That(branch.Inputs, Has.Count.EqualTo(testCase.InputCount), testCase.OpCode.Name);
            Assert.That(branch.Labels, Has.Count.EqualTo(2), testCase.OpCode.Name);
            Assert.That(generator.ControlFlowGraph.OutgoingEdges(source), Has.Exactly(2).Items, testCase.OpCode.Name);
        }
    }

    [Test]
    public void ControlFlow_Switch_CreatesFallthroughAndOneEdgePerDistinctTargetWithCarriedStack()
    {
        ILGenerator il = PatchProcessor.CreateILGenerator();
        Label firstTargetLabel = il.DefineLabel();
        Label secondTargetLabel = il.DefineLabel();
        Label endLabel = il.DefineLabel();
        var firstTarget = new CodeInstruction(OpCodes.Pop);
        firstTarget.labels.Add(firstTargetLabel);
        var secondTarget = new CodeInstruction(OpCodes.Pop);
        secondTarget.labels.Add(secondTargetLabel);
        var end = new CodeInstruction(OpCodes.Ret);
        end.labels.Add(endLabel);
        var generator = CreateGenerator(
            VoidMethod,
            [
                new CodeInstruction(OpCodes.Ldc_I4, 42),
                new CodeInstruction(OpCodes.Ldc_I4_0),
                new CodeInstruction(OpCodes.Switch, new[] { firstTargetLabel, secondTargetLabel }),
                new CodeInstruction(OpCodes.Pop),
                new CodeInstruction(OpCodes.Br, endLabel),
                firstTarget,
                new CodeInstruction(OpCodes.Br, endLabel),
                secondTarget,
                new CodeInstruction(OpCodes.Br, endLabel),
                end,
            ]);

        BasicBlock source = FirstInstructionBlock(generator);
        Assert.That(source.Branch, Is.TypeOf<ConditionalBranch>());
        var branch = (ConditionalBranch)source.Branch;
        Assert.That(branch.OpCode, Is.EqualTo(OpCodes.Switch));
        Assert.That(branch.Inputs, Has.Count.EqualTo(1));
        Assert.That(branch.Labels, Has.Count.EqualTo(3));
        Assert.That(generator.ControlFlowGraph.OutgoingEdges(source), Has.Exactly(3).Items);
        Assert.That(generator.ControlFlowGraph.OutgoingEdges(source),
            Has.All.Matches<Edge>(edge => edge.EdgeAssignments.Count == 1));
    }

    [Test]
    public void ControlFlow_Fallthrough_WithoutCarriedStackCreatesImplicitEdge()
    {
        Label targetLabel = PatchProcessor.CreateILGenerator().DefineLabel();
        var target = new CodeInstruction(OpCodes.Ret);
        target.labels.Add(targetLabel);
        var generator = CreateGenerator(VoidMethod, [new CodeInstruction(OpCodes.Nop), target]);

        BasicBlock source = InstructionBlocks(generator).First();
        BasicBlock destination = InstructionBlocks(generator).Last();
        Assert.That(source.Branch, Is.TypeOf<UnconditionalBranch>());
        Assert.That(((UnconditionalBranch)source.Branch).Label, Is.EqualTo(destination.Label));
        Assert.That(generator.ControlFlowGraph.Edges.Single(edge => edge.Source == source.Label).EdgeAssignments, Is.Empty);
    }

    [Test]
    public void ControlFlow_Fallthrough_WithCarriedStackCreatesAssignment()
    {
        Label targetLabel = PatchProcessor.CreateILGenerator().DefineLabel();
        var target = new CodeInstruction(OpCodes.Pop);
        target.labels.Add(targetLabel);
        var generator = CreateGenerator(
            VoidMethod,
            [new CodeInstruction(OpCodes.Ldc_I4_1), target, new CodeInstruction(OpCodes.Ret)]);

        BasicBlock source = InstructionBlocks(generator).First();
        BasicBlock destination = InstructionBlocks(generator).Last();
        Assert.That(generator.BlockStacks[source.Label].OutgoingStack, Has.Count.EqualTo(1));
        Assert.That(generator.BlockStacks[destination.Label].IncomingStack, Has.Count.EqualTo(1));
        Assert.That(generator.ControlFlowGraph.Edges.Single(edge => edge.Source == source.Label).EdgeAssignments,
            Has.Count.EqualTo(1));
    }

    [Test]
    public void ControlFlow_LastInstructionWithoutTerminalControlFlow_Throws()
    {
        var generator = new ControlFlowGraphGenerator(VoidMethod, [new CodeInstruction(OpCodes.Nop)]);

        Assert.Throws<InvalidOperationException>(() => generator.CreateControlFlowGraph());
    }

    [Test]
    public void Regions_SyntheticEntryAndOrdinaryBlocksBelongToGraphRoot()
    {
        var generator = CreateGenerator(VoidMethod, [new CodeInstruction(OpCodes.Ret)]);

        RootRegion root = generator.ControlFlowGraph.RootRegion;
        BasicBlock syntheticEntry = generator.ControlFlowGraph.GetBlock(root.EntryLabel);
        BasicBlock instructionEntry = FirstInstructionBlock(generator);
        Assert.That(syntheticEntry.Region, Is.SameAs(root));
        Assert.That(syntheticEntry.Label, Is.SameAs(root.EntryLabel));
        Assert.That(syntheticEntry.Ops, Is.Empty);
        Assert.That(syntheticEntry.Branch, Is.TypeOf<UnconditionalBranch>());
        Assert.That(((UnconditionalBranch)syntheticEntry.Branch).Label, Is.SameAs(instructionEntry.Label));
        Assert.That(instructionEntry.Region, Is.SameAs(root));
        Assert.That(generator.ControlFlowGraph.Edges.Any(edge =>
            edge.Source == syntheticEntry.Label && edge.Destination == instructionEntry.Label), Is.True);
    }

    [Test]
    public void ExceptionRegions_Catch_ReceivesImplicitExceptionOnStack()
    {
        Label endLabel = PatchProcessor.CreateILGenerator().DefineLabel();
        var tryStart = new CodeInstruction(OpCodes.Nop);
        tryStart.blocks.Add(new ExceptionBlock(ExceptionBlockType.BeginExceptionBlock));
        var catchStart = new CodeInstruction(OpCodes.Pop);
        catchStart.blocks.Add(new ExceptionBlock(ExceptionBlockType.BeginCatchBlock, typeof(InvalidOperationException)));
        var catchLeave = new CodeInstruction(OpCodes.Leave, endLabel);
        catchLeave.blocks.Add(new ExceptionBlock(ExceptionBlockType.EndExceptionBlock));
        var end = new CodeInstruction(OpCodes.Ret);
        end.labels.Add(endLabel);

        var generator = CreateGenerator(
            VoidMethod,
            [
                tryStart,
                new CodeInstruction(OpCodes.Leave, endLabel),
                catchStart,
                catchLeave,
                end,
            ]);

        ExceptionGroup group = generator.ControlFlowGraph.ExceptionGroups.Single();
        Assert.That(group.HandlerRegions, Has.Count.EqualTo(1));
        Assert.That(group.HandlerRegions[0], Is.TypeOf<CatchRegion>());
        var catchRegion = (CatchRegion)group.HandlerRegions[0];
        BasicBlock syntheticEntry = generator.ControlFlowGraph.GetBlock(catchRegion.EntryLabel);
        BasicBlock catchBlock = generator.ControlFlowGraph.GetBlock(((UnconditionalBranch)syntheticEntry.Branch).Label);
        Edge entryEdge = generator.ControlFlowGraph.OutgoingEdges(syntheticEntry).Single();
        Assert.That(catchRegion.ExceptionType, Is.EqualTo(typeof(InvalidOperationException)));
        Assert.That(generator.BlockStacks[syntheticEntry.Label].IncomingStack,
            Is.EqualTo(new[] { catchRegion.IncomingException }));
        Assert.That(generator.BlockStacks[syntheticEntry.Label].OutgoingStack,
            Is.EqualTo(new[] { catchRegion.IncomingException }));
        Assert.That(generator.BlockStacks[catchBlock.Label].IncomingStack, Has.Count.EqualTo(1));
        Assert.That(generator.BlockStacks[catchBlock.Label].IncomingStack[0], Is.Not.EqualTo(catchRegion.IncomingException));
        Assert.That(entryEdge.EdgeAssignments, Has.Count.EqualTo(1));
        Assert.That(entryEdge.EdgeAssignments[0].Input, Is.EqualTo(catchRegion.IncomingException));
        Assert.That(entryEdge.EdgeAssignments[0].Output,
            Is.EqualTo(generator.BlockStacks[catchBlock.Label].IncomingStack[0]));
        Assert.That(generator.BlockStacks[catchBlock.Label].OutgoingStack, Is.Empty);
    }

    [Test]
    public void ExceptionRegions_Finally_HasEmptySyntheticEntryAndEndsWithEndfinally()
    {
        Label endLabel = PatchProcessor.CreateILGenerator().DefineLabel();
        var tryStart = new CodeInstruction(OpCodes.Nop);
        tryStart.blocks.Add(new ExceptionBlock(ExceptionBlockType.BeginExceptionBlock));
        var finallyStart = new CodeInstruction(OpCodes.Nop);
        finallyStart.blocks.Add(new ExceptionBlock(ExceptionBlockType.BeginFinallyBlock));
        var endfinally = new CodeInstruction(OpCodes.Endfinally);
        endfinally.blocks.Add(new ExceptionBlock(ExceptionBlockType.EndExceptionBlock));
        var end = new CodeInstruction(OpCodes.Ret);
        end.labels.Add(endLabel);

        var generator = CreateGenerator(
            VoidMethod,
            [tryStart, new CodeInstruction(OpCodes.Leave, endLabel), finallyStart, endfinally, end]);

        ExceptionGroup group = generator.ControlFlowGraph.ExceptionGroups.Single();
        Assert.That(group.HandlerRegions.Single(), Is.TypeOf<FinallyRegion>());
        var region = (FinallyRegion)group.HandlerRegions.Single();
        BasicBlock syntheticEntry = generator.ControlFlowGraph.GetBlock(region.EntryLabel);
        BasicBlock finallyBlock = generator.ControlFlowGraph.GetBlock(((UnconditionalBranch)syntheticEntry.Branch).Label);
        Assert.That(generator.BlockStacks[syntheticEntry.Label].IncomingStack, Is.Empty);
        Assert.That(generator.BlockStacks[syntheticEntry.Label].OutgoingStack, Is.Empty);
        Assert.That(generator.ControlFlowGraph.OutgoingEdges(syntheticEntry).Single().EdgeAssignments, Is.Empty);
        Assert.That(finallyBlock.Region, Is.SameAs(region));
        Assert.That(finallyBlock.Branch, Is.TypeOf<Return>());
        Assert.That(((Return)finallyBlock.Branch).IL.OpCode, Is.EqualTo(OpCodes.Endfinally));
        Assert.That(((Return)finallyBlock.Branch).Value, Is.TypeOf<VoidOp>());
    }

    [Test]
    public void ExceptionRegions_Fault_HasEmptySyntheticEntryAndEndsWithEndfinally()
    {
        Label endLabel = PatchProcessor.CreateILGenerator().DefineLabel();
        var tryStart = new CodeInstruction(OpCodes.Nop);
        tryStart.blocks.Add(new ExceptionBlock(ExceptionBlockType.BeginExceptionBlock));
        var faultStart = new CodeInstruction(OpCodes.Nop);
        faultStart.blocks.Add(new ExceptionBlock(ExceptionBlockType.BeginFaultBlock));
        var endfinally = new CodeInstruction(OpCodes.Endfinally);
        endfinally.blocks.Add(new ExceptionBlock(ExceptionBlockType.EndExceptionBlock));
        var end = new CodeInstruction(OpCodes.Ret);
        end.labels.Add(endLabel);

        var generator = CreateGenerator(
            VoidMethod,
            [tryStart, new CodeInstruction(OpCodes.Leave, endLabel), faultStart, endfinally, end]);

        ExceptionGroup group = generator.ControlFlowGraph.ExceptionGroups.Single();
        Assert.That(group.HandlerRegions.Single(), Is.TypeOf<FaultRegion>());
        var region = (FaultRegion)group.HandlerRegions.Single();
        BasicBlock syntheticEntry = generator.ControlFlowGraph.GetBlock(region.EntryLabel);
        BasicBlock faultBlock = generator.ControlFlowGraph.GetBlock(((UnconditionalBranch)syntheticEntry.Branch).Label);
        Assert.That(generator.BlockStacks[syntheticEntry.Label].IncomingStack, Is.Empty);
        Assert.That(generator.BlockStacks[syntheticEntry.Label].OutgoingStack, Is.Empty);
        Assert.That(generator.ControlFlowGraph.OutgoingEdges(syntheticEntry).Single().EdgeAssignments, Is.Empty);
        Assert.That(faultBlock.Region, Is.SameAs(region));
        Assert.That(faultBlock.Branch, Is.TypeOf<Return>());
        Assert.That(((Return)faultBlock.Branch).IL.OpCode, Is.EqualTo(OpCodes.Endfinally));
        Assert.That(((Return)faultBlock.Branch).Value, Is.TypeOf<VoidOp>());
    }

    [Test]
    public void ControlFlow_Leave_LongAndShortFormsCreateLeaveWithEmptyStackEdge()
    {
        foreach (OpCode opcode in new[] { OpCodes.Leave, OpCodes.Leave_S })
        {
            Label targetLabel = PatchProcessor.CreateILGenerator().DefineLabel();
            var target = new CodeInstruction(OpCodes.Ret);
            target.labels.Add(targetLabel);
            var generator = CreateGenerator(VoidMethod, [new CodeInstruction(opcode, targetLabel), target]);

            BasicBlock source = FirstInstructionBlock(generator);
            Assert.That(source.Branch, Is.TypeOf<Leave>(), opcode.Name);
            Assert.That(((Leave)source.Branch).Label, Is.EqualTo(target.labels.Select(label => generator.BlockLabels[label]).Single()),
                opcode.Name);
            Assert.That(generator.ControlFlowGraph.OutgoingEdges(source).Single().EdgeAssignments, Is.Empty, opcode.Name);
        }
    }

    [Test]
    public void ControlFlow_Jmp_CreatesTerminalTransferWithoutSuccessor()
    {
        var generator = CreateGenerator(VoidMethod, [new CodeInstruction(OpCodes.Jmp, VoidMethod)]);

        BasicBlock block = FirstInstructionBlock(generator);
        Assert.That(block.Branch.Labels, Is.Empty);
        Assert.That(generator.ControlFlowGraph.OutgoingEdges(block), Is.Empty);
        Assert.That(generator.BlockStacks[block.Label].OutgoingStack, Is.Empty);
    }

    private static ControlFlowGraphGenerator CreateGenerator(MethodBase method, List<CodeInstruction> instructions)
    {
        var generator = new ControlFlowGraphGenerator(method, instructions);
        generator.CreateControlFlowGraph();
        return generator;
    }

    private static BasicBlock FirstInstructionBlock(ControlFlowGraphGenerator generator) =>
        generator.ControlFlowGraph.GetBlock(
            ((UnconditionalBranch)generator.ControlFlowGraph.GetBlock(generator.ControlFlowGraph.RootRegion.EntryLabel).Branch).Label);

    private static IEnumerable<BasicBlock> InstructionBlocks(ControlFlowGraphGenerator generator) =>
        generator.ControlFlowGraph.BasicBlocks.Where(block => block.Label != generator.ControlFlowGraph.RootRegion.EntryLabel);

    private static List<CodeInstruction> ThrowTerminated() =>
        [new CodeInstruction(OpCodes.Ldnull), new CodeInstruction(OpCodes.Throw)];

    private static List<CodeInstruction> BranchToThrowTarget(IEnumerable<CodeInstruction> body)
    {
        Label label = PatchProcessor.CreateILGenerator().DefineLabel();
        var target = new CodeInstruction(OpCodes.Ldnull);
        target.labels.Add(label);
        return [.. body, new CodeInstruction(OpCodes.Br, label), target, new CodeInstruction(OpCodes.Throw)];
    }

    private static ILOp GetILOp(ControlFlowGraphGenerator generator, OpCode opcode) =>
        generator.ControlFlowGraph.BasicBlocks
            .SelectMany(block => block.Ops)
            .SelectMany(Flatten)
            .OfType<ILOp>()
            .Single(operation => operation.IL.OpCode == opcode);

    private static IEnumerable<Op> Flatten(Op operation)
    {
        yield return operation;
        switch (operation)
        {
            case AssignmentOp assignment:
                foreach (var nested in Flatten(assignment.Input))
                    yield return nested;
                break;
            case ILOp il:
                foreach (var input in il.Inputs)
                foreach (var nested in Flatten(input))
                    yield return nested;
                break;
        }
    }
}
