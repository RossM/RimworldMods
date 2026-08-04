using System.Reflection.Emit;
using Disharmony.Optimizer;
using Disharmony.Optimizer.Passes;
using HarmonyLib;

namespace Disharmony.Tests;

[TestFixture]
public sealed class OptimizerPassTests
{
    private static readonly MethodInfo TargetMethod =
        typeof(StaticMethodTargets).GetMethod(nameof(StaticMethodTargets.Void))!;

    [Test]
    public void MakeBasicBlocksBuildsEdgesAndResolvesLabels()
    {
        Optimizer.Optimizer optimizer = CreateOptimizer(generator =>
        {
            Label target = generator.DefineLabel();
            return
            [
                new CodeInstruction(OpCodes.Ldc_I4_0),
                new CodeInstruction(OpCodes.Brfalse, target),
                new CodeInstruction(OpCodes.Ldc_I4_1),
                new CodeInstruction(OpCodes.Pop),
                new CodeInstruction(OpCodes.Ret).WithLabels(target),
            ];
        });

        optimizer.MakeBasicBlocks();

        Assert.That(optimizer.BasicBlocks, Has.Count.EqualTo(3));
        BasicBlock condition = optimizer.BasicBlocks[0];
        BasicBlock fallthrough = optimizer.BasicBlocks[1];
        BasicBlock target = optimizer.BasicBlocks[2];
        Assert.That(condition.Next, Is.SameAs(fallthrough));
        Assert.That(condition.fallthroughEdge, Is.Not.Null);
        Assert.That(condition.fallthroughEdge!.Target, Is.SameAs(fallthrough));
        Assert.That(condition.ops[^1].Operand, Is.TypeOf<ControlFlowEdge>());
        Assert.That(((ControlFlowEdge)condition.ops[^1].Operand!).Target, Is.SameAs(target));
        Assert.That(condition.Successors, Is.EqualTo(new[] { fallthrough, target }));
        Assert.That(fallthrough.Predecessors, Is.EqualTo(new[] { condition }));
        Assert.That(target.Predecessors, Is.EqualTo(new[] { condition, fallthrough }));
        Assert.That(condition.outgoingEdges, Has.All.Matches<ControlFlowEdge>(edge => edge.assignments.Count == 0));
    }

    [Test]
    public void NopEliminationRemovesEveryNop()
    {
        Optimizer.Optimizer optimizer = CreateOptimizer(_ =>
        [
            new CodeInstruction(OpCodes.Nop),
            new CodeInstruction(OpCodes.Nop),
            new CodeInstruction(OpCodes.Ret),
        ]);
        optimizer.MakeBasicBlocks();

        optimizer.NopElimination();

        Assert.That(OpCodesIn(optimizer.BasicBlocks.Single()), Is.EqualTo(new[] { OpCodes.Ret }));
    }

    [Test]
    public void JumpThreadingSkipsEmptyFallthroughBlock()
    {
        Optimizer.Optimizer optimizer = CreateOptimizer(generator =>
        {
            Label afterEmptyBlock = generator.DefineLabel();
            Label exit = generator.DefineLabel();
            return
            [
                new CodeInstruction(OpCodes.Ldc_I4_0),
                new CodeInstruction(OpCodes.Brfalse, exit),
                new CodeInstruction(OpCodes.Nop),
                new CodeInstruction(OpCodes.Ldc_I4_1).WithLabels(afterEmptyBlock),
                new CodeInstruction(OpCodes.Pop),
                new CodeInstruction(OpCodes.Ret).WithLabels(exit),
            ];
        });
        optimizer.MakeBasicBlocks();
        BasicBlock condition = optimizer.BasicBlocks[0];
        BasicBlock empty = optimizer.BasicBlocks[1];
        BasicBlock afterEmpty = optimizer.BasicBlocks[2];
        ControlFlowEdge fallthroughEdge = condition.fallthroughEdge!;
        optimizer.NopElimination();

        optimizer.JumpThreading();

        Assert.That(empty.ops, Is.Empty);
        Assert.That(condition.fallthroughEdge, Is.SameAs(fallthroughEdge));
        Assert.That(condition.Next, Is.SameAs(afterEmpty));
        Assert.That(condition.Successors, Does.Not.Contain(empty));
        Assert.That(empty.Predecessors, Is.Empty);
    }

    [Test]
    public void SimpleDeadCodeEliminationRemovesUnreachableBlock()
    {
        Optimizer.Optimizer optimizer = CreateOptimizer(generator =>
        {
            Label target = generator.DefineLabel();
            return
            [
                new CodeInstruction(OpCodes.Br, target),
                new CodeInstruction(OpCodes.Ldc_I4_1),
                new CodeInstruction(OpCodes.Pop),
                new CodeInstruction(OpCodes.Ret).WithLabels(target),
            ];
        });
        optimizer.MakeBasicBlocks();
        BasicBlock unreachable = optimizer.BasicBlocks[1];

        optimizer.SimpleDeadCodeElimination();

        Assert.That(optimizer.BasicBlocks, Has.Count.EqualTo(2));
        Assert.That(optimizer.BasicBlocks, Does.Not.Contain(unreachable));
    }

    [Test]
    public void DominatorTreeComputesImmediateDominatorsAcrossDiamond()
    {
        Optimizer.Optimizer optimizer = CreateOptimizer(generator =>
        {
            Label alternative = generator.DefineLabel();
            Label join = generator.DefineLabel();
            return
            [
                new CodeInstruction(OpCodes.Ldc_I4_0),
                new CodeInstruction(OpCodes.Brfalse, alternative),
                new CodeInstruction(OpCodes.Ldstr, "first"),
                new CodeInstruction(OpCodes.Pop),
                new CodeInstruction(OpCodes.Br, join),
                new CodeInstruction(OpCodes.Ldstr, "second").WithLabels(alternative),
                new CodeInstruction(OpCodes.Pop),
                new CodeInstruction(OpCodes.Ret).WithLabels(join),
            ];
        });
        optimizer.MakeBasicBlocks();
        BasicBlock entry = optimizer.BasicBlocks.Single(block =>
            block.ops.Any(op => op.Opcode == OpCodes.Brfalse));
        BasicBlock first = optimizer.BasicBlocks.Single(block =>
            block.ops.Any(op => Equals(op.Operand, "first")));
        BasicBlock second = optimizer.BasicBlocks.Single(block =>
            block.ops.Any(op => Equals(op.Operand, "second")));
        BasicBlock join = optimizer.BasicBlocks.Single(block =>
            block.ops.Count == 1 && block.ops[0].Opcode == OpCodes.Ret);

        DominatorTree dominators =
            DominatorTree.Compute(optimizer.BasicBlocks, [entry]);

        Assert.That(dominators.Roots, Is.EqualTo(new[] { entry }));
        Assert.That(dominators.GetImmediateDominator(entry), Is.Null);
        Assert.That(dominators.GetImmediateDominator(first), Is.SameAs(entry));
        Assert.That(dominators.GetImmediateDominator(second), Is.SameAs(entry));
        Assert.That(dominators.GetImmediateDominator(join), Is.SameAs(entry));
        Assert.That(dominators.Dominates(entry, join), Is.True);
        Assert.That(dominators.Dominates(first, join), Is.False);
        Assert.That(dominators.Dominates(second, join), Is.False);
        Assert.That(dominators.GetChildren(entry), Is.EquivalentTo(new[] { first, second, join }));
    }

    [Test]
    public void DominatorTreeHandlesLoopBackedge()
    {
        Optimizer.Optimizer optimizer = CreateOptimizer(generator =>
        {
            Label header = generator.DefineLabel();
            Label exit = generator.DefineLabel();
            return
            [
                new CodeInstruction(OpCodes.Nop),
                new CodeInstruction(OpCodes.Br, header),
                new CodeInstruction(OpCodes.Ldc_I4_0).WithLabels(header),
                new CodeInstruction(OpCodes.Brfalse, exit),
                new CodeInstruction(OpCodes.Ldstr, "body"),
                new CodeInstruction(OpCodes.Pop),
                new CodeInstruction(OpCodes.Br, header),
                new CodeInstruction(OpCodes.Ret).WithLabels(exit),
            ];
        });
        optimizer.MakeBasicBlocks();
        BasicBlock entry = optimizer.BasicBlocks.Single(block =>
            block.ops.Count == 1 && block.ops[0].Opcode == OpCodes.Nop);
        BasicBlock header = optimizer.BasicBlocks.Single(block =>
            block.ops.Any(op => op.Opcode == OpCodes.Brfalse));
        BasicBlock body = optimizer.BasicBlocks.Single(block =>
            block.ops.Any(op => Equals(op.Operand, "body")));
        BasicBlock exit = optimizer.BasicBlocks.Single(block =>
            block.ops.Count == 1 && block.ops[0].Opcode == OpCodes.Ret);

        DominatorTree dominators =
            DominatorTree.Compute(optimizer.BasicBlocks, [entry]);

        Assert.That(dominators.GetImmediateDominator(header), Is.SameAs(entry));
        Assert.That(dominators.GetImmediateDominator(body), Is.SameAs(header));
        Assert.That(dominators.GetImmediateDominator(exit), Is.SameAs(header));
        Assert.That(dominators.Dominates(header, body), Is.True);
        Assert.That(dominators.Dominates(body, header), Is.False);
    }

    [Test]
    public void DominatorTreeUsesArtificialRootForExceptionEntryAndSharedExit()
    {
        Optimizer.Optimizer optimizer = CreateTwoBlockTryOptimizer();
        optimizer.MakeBasicBlocks();
        Region root = optimizer.Regions.Single(region => region.parent == null);
        Region protectedRegion = optimizer.ExceptionEntryGroups.Single().ProtectedRegion;
        Region catchRegion = optimizer.ExceptionEntryGroups.Single().associatedRegions.Single();
        BasicBlock methodEntry = RecursiveEntry(root);
        BasicBlock trySecondBlock = optimizer.BasicBlocks.Single(block =>
            block.parent == protectedRegion && block != methodEntry);
        BasicBlock catchEntry = RecursiveEntry(catchRegion);
        BasicBlock exit = optimizer.BasicBlocks.Single(block =>
            block.ops.Count == 1 && block.ops[0].Opcode == OpCodes.Ret);

        DominatorTree dominators =
            DominatorTree.Compute(optimizer.BasicBlocks, [methodEntry, catchEntry]);

        Assert.That(dominators.GetImmediateDominator(trySecondBlock), Is.SameAs(methodEntry));
        Assert.That(dominators.GetImmediateDominator(catchEntry), Is.Null);
        Assert.That(dominators.GetImmediateDominator(exit), Is.Null);
        Assert.That(dominators.Roots, Is.EquivalentTo(new[] { methodEntry, catchEntry, exit }));
        Assert.That(dominators.Dominates(methodEntry, catchEntry), Is.False);
        Assert.That(dominators.Dominates(methodEntry, exit), Is.False);

        static BasicBlock RecursiveEntry(Region region)
        {
            RegionNode entry = region;
            while (entry is Region entryRegion)
                entry = entryRegion.entry!;
            return (BasicBlock)entry;
        }
    }

    [Test]
    public void DominatorTreeCacheIsReusedAndInvalidatedByEdgeMutation()
    {
        Optimizer.Optimizer optimizer = CreateOptimizer(generator =>
        {
            Label target = generator.DefineLabel();
            return
            [
                new CodeInstruction(OpCodes.Ldc_I4_0),
                new CodeInstruction(OpCodes.Brfalse, target),
                new CodeInstruction(OpCodes.Ret).WithLabels(target),
            ];
        });
        optimizer.MakeBasicBlocks();
        MethodInfo compute = typeof(Optimizer.Optimizer).GetMethod(
            "ComputeDominatorTreeIfNeeded",
            BindingFlags.Instance | BindingFlags.NonPublic)!;

        var first = (DominatorTree)compute.Invoke(optimizer, null)!;
        var second = (DominatorTree)compute.Invoke(optimizer, null)!;
        optimizer.BranchElimination();
        var afterMutation = (DominatorTree)compute.Invoke(optimizer, null)!;

        Assert.That(second, Is.SameAs(first));
        Assert.That(afterMutation, Is.Not.SameAs(first));
    }

    [Test]
    public void BranchEliminationReplacesRedundantConditionWithPop()
    {
        Optimizer.Optimizer optimizer = CreateOptimizer(generator =>
        {
            Label target = generator.DefineLabel();
            return
            [
                new CodeInstruction(OpCodes.Ldc_I4_0),
                new CodeInstruction(OpCodes.Brfalse, target),
                new CodeInstruction(OpCodes.Ret).WithLabels(target),
            ];
        });
        optimizer.MakeBasicBlocks();
        BasicBlock condition = optimizer.BasicBlocks[0];
        BasicBlock target = optimizer.BasicBlocks[1];

        optimizer.BranchElimination();

        Assert.That(OpCodesIn(condition), Is.EqualTo(new[] { OpCodes.Ldc_I4_0, OpCodes.Pop }));
        Assert.That(condition.Successors.Count(), Is.EqualTo(1));
        Assert.That(condition.Successors.Single(), Is.SameAs(condition.Next));
        Assert.That(target.incomingEdges, Is.EqualTo(new[] { condition.fallthroughEdge }));
    }

    [Test]
    public void BranchEliminationPreservesVariableInputsInPopOrder()
    {
        Optimizer.Optimizer optimizer = CreateOptimizer(generator =>
        {
            Label target = generator.DefineLabel();
            return
            [
                new CodeInstruction(OpCodes.Ldstr, "first"),
                new CodeInstruction(OpCodes.Ldstr, "second"),
                new CodeInstruction(OpCodes.Beq, target),
                new CodeInstruction(OpCodes.Ret).WithLabels(target),
            ];
        });
        optimizer.MakeBasicBlocks();
        new StackToVariableConversion(optimizer).Run();
        BasicBlock condition = optimizer.BasicBlocks[0];
        Variable first = condition.ops[0].outputs.Single();
        Variable second = condition.ops[1].outputs.Single();

        optimizer.BranchElimination();

        Op[] pops = [.. condition.ops.Where(op => op.Opcode == OpCodes.Pop)];
        Assert.That(pops, Has.Length.EqualTo(2));
        Assert.That(pops.Select(pop => pop.stackInputCount), Is.EqualTo(new[] { 1, 1 }));
        Assert.That(pops[0].inputs, Is.EqualTo(new[] { second }));
        Assert.That(pops[1].inputs, Is.EqualTo(new[] { first }));

        new VariableToStackConversion(optimizer).Run();
        Assert.That(OpCodesIn(condition), Is.EqualTo(new[]
        {
            OpCodes.Ldstr,
            OpCodes.Ldstr,
            OpCodes.Pop,
            OpCodes.Pop,
        }));
    }

    [Test]
    public void MergeBlocksAppendsSinglePredecessorSuccessor()
    {
        Optimizer.Optimizer optimizer = CreateOptimizer(generator =>
        {
            Label secondBlock = generator.DefineLabel();
            return
            [
                new CodeInstruction(OpCodes.Ldc_I4_1),
                new CodeInstruction(OpCodes.Pop).WithLabels(secondBlock),
                new CodeInstruction(OpCodes.Ret),
            ];
        });
        optimizer.MakeBasicBlocks();
        BasicBlock first = optimizer.BasicBlocks[0];
        BasicBlock merged = optimizer.BasicBlocks[1];

        optimizer.MergeBlocks();

        Assert.That(OpCodesIn(first), Is.EqualTo(new[] { OpCodes.Ldc_I4_1, OpCodes.Pop, OpCodes.Ret }));
        Assert.That(first.Next, Is.Null);
        Assert.That(first.Successors, Is.Empty);
        Assert.That(merged.Predecessors, Is.Empty);
    }

    [Test]
    public void MergeBlocksTransfersSuccessorFallthroughEdge()
    {
        Optimizer.Optimizer optimizer = CreateOptimizer(generator =>
        {
            Label condition = generator.DefineLabel();
            Label target = generator.DefineLabel();
            return
            [
                new CodeInstruction(OpCodes.Nop),
                new CodeInstruction(OpCodes.Ldc_I4_0).WithLabels(condition),
                new CodeInstruction(OpCodes.Brfalse, target),
                new CodeInstruction(OpCodes.Ret),
                new CodeInstruction(OpCodes.Ret).WithLabels(target),
            ];
        });
        optimizer.MakeBasicBlocks();
        BasicBlock first = optimizer.BasicBlocks[0];
        BasicBlock merged = optimizer.BasicBlocks[1];
        ControlFlowEdge fallthroughEdge = merged.fallthroughEdge!;

        optimizer.MergeBlocks();

        Assert.That(first.fallthroughEdge, Is.SameAs(fallthroughEdge));
        Assert.That(fallthroughEdge.Source, Is.SameAs(first));
        Assert.That(first.outgoingEdges, Does.Contain(fallthroughEdge));
        Assert.That(merged.fallthroughEdge, Is.Null);
        Assert.That(merged.outgoingEdges, Is.Empty);
    }

    [Test]
    public void AggressiveDeadCodeEliminationAndReorderFollowsControlFlowOrder()
    {
        Optimizer.Optimizer optimizer = CreateOptimizer(generator =>
        {
            Label target = generator.DefineLabel();
            return
            [
                new CodeInstruction(OpCodes.Br, target),
                new CodeInstruction(OpCodes.Ldc_I4_1),
                new CodeInstruction(OpCodes.Pop),
                new CodeInstruction(OpCodes.Ret),
                new CodeInstruction(OpCodes.Ret).WithLabels(target),
            ];
        });
        optimizer.MakeBasicBlocks();
        Region root = optimizer.Regions[0];
        BasicBlock entry = optimizer.BasicBlocks[0];
        BasicBlock unreachable = optimizer.BasicBlocks[1];
        BasicBlock target = optimizer.BasicBlocks[2];

        optimizer.AggressiveDeadCodeEliminationAndReorder();

        Assert.That(optimizer.BasicBlocks, Is.EqualTo(new[] { entry, target }));
        Assert.That(optimizer.BasicBlocks, Does.Not.Contain(unreachable));
        Assert.That(optimizer.Regions, Is.EqualTo(new[] { root }));
    }

    [Test]
    public void ConvertStackToVariablesPropagatesLocalTypeThroughJoin()
    {
        Optimizer.Optimizer optimizer = CreateOptimizer(generator =>
        {
            LocalBuilder local = generator.DeclareLocal(typeof(string));
            Label alternative = generator.DefineLabel();
            Label join = generator.DefineLabel();
            return
            [
                new CodeInstruction(OpCodes.Ldc_I4_0),
                new CodeInstruction(OpCodes.Brfalse, alternative),
                new CodeInstruction(OpCodes.Ldstr, "first"),
                new CodeInstruction(OpCodes.Stloc, local),
                new CodeInstruction(OpCodes.Br, join),
                new CodeInstruction(OpCodes.Ldstr, "second").WithLabels(alternative),
                new CodeInstruction(OpCodes.Stloc, local),
                new CodeInstruction(OpCodes.Ldloc, local).WithLabels(join),
                new CodeInstruction(OpCodes.Pop),
                new CodeInstruction(OpCodes.Ret),
            ];
        });
        optimizer.MakeBasicBlocks();
        BasicBlock join = optimizer.BasicBlocks.Single(block =>
            block.ops.Any(op => op.Opcode == OpCodes.Ldloc));

        new StackToVariableConversion(optimizer).Run();

        Op load = join.ops.Single(op => op.Opcode == OpCodes.Ldloc);
        Assert.That(load.outputs.Single().type, Is.EqualTo(typeof(string)));
    }

    [Test]
    public void ConvertStackToVariablesSeedsCatchEntryWithImplicitExceptionValue()
    {
        Optimizer.Optimizer optimizer = CreateOptimizer(generator =>
        {
            Label exit = generator.DefineLabel();
            var tryLeave = new CodeInstruction(OpCodes.Leave, exit);
            tryLeave.blocks.Add(new ExceptionBlock(ExceptionBlockType.BeginExceptionBlock));
            var catchPop = new CodeInstruction(OpCodes.Pop);
            catchPop.blocks.Add(new ExceptionBlock(ExceptionBlockType.BeginCatchBlock, typeof(Exception)));
            var catchLeave = new CodeInstruction(OpCodes.Leave, exit);
            catchLeave.blocks.Add(new ExceptionBlock(ExceptionBlockType.EndExceptionBlock));
            return
            [
                tryLeave,
                catchPop,
                catchLeave,
                new CodeInstruction(OpCodes.Ret).WithLabels(exit),
            ];
        });
        optimizer.MakeBasicBlocks();
        optimizer.SimpleDeadCodeElimination();
        Region catchRegion = optimizer.Regions.Single(region =>
            region.harmonyBlock?.blockType == ExceptionBlockType.BeginCatchBlock);
        BasicBlock catchEntry = (BasicBlock)catchRegion.entry!;
        ExceptionEntryGroup entryGroup = catchRegion.exceptionEntryGroup!;
        Assert.That(entryGroup.ProtectedRegion.harmonyBlock?.blockType,
            Is.EqualTo(ExceptionBlockType.BeginExceptionBlock));
        Assert.That(entryGroup.associatedRegions, Is.EqualTo(new[] { catchRegion }));

        new StackToVariableConversion(optimizer).Run();

        Assert.That(catchEntry.entryStackVariables, Has.Count.EqualTo(1));
        Assert.That(catchEntry.entryStackVariables[0].type, Is.EqualTo(typeof(Exception)));
        Assert.That(catchEntry.ops[0].inputs, Is.EqualTo(catchEntry.entryStackVariables));
    }

    [Test]
    public void MakeBasicBlocksGroupsProtectedRegionWithOrderedHandlers()
    {
        Optimizer.Optimizer optimizer = CreateOptimizer(generator =>
        {
            Label exit = generator.DefineLabel();
            var tryLeave = new CodeInstruction(OpCodes.Leave, exit);
            tryLeave.blocks.Add(new ExceptionBlock(ExceptionBlockType.BeginExceptionBlock));
            var firstCatch = new CodeInstruction(OpCodes.Pop);
            firstCatch.blocks.Add(new ExceptionBlock(ExceptionBlockType.BeginCatchBlock,
                typeof(InvalidOperationException)));
            var secondCatch = new CodeInstruction(OpCodes.Pop);
            secondCatch.blocks.Add(new ExceptionBlock(ExceptionBlockType.BeginCatchBlock, typeof(Exception)));
            var secondLeave = new CodeInstruction(OpCodes.Leave, exit);
            secondLeave.blocks.Add(new ExceptionBlock(ExceptionBlockType.EndExceptionBlock));
            return
            [
                tryLeave,
                firstCatch,
                new CodeInstruction(OpCodes.Leave, exit),
                secondCatch,
                secondLeave,
                new CodeInstruction(OpCodes.Ret).WithLabels(exit),
            ];
        });

        optimizer.MakeBasicBlocks();

        ExceptionEntryGroup entryGroup = optimizer.ExceptionEntryGroups.Single();
        Assert.That(entryGroup.ProtectedRegion.harmonyBlock?.blockType,
            Is.EqualTo(ExceptionBlockType.BeginExceptionBlock));
        Assert.That(entryGroup.associatedRegions.Select(region => region.harmonyBlock?.catchType),
            Is.EqualTo(new[] { typeof(InvalidOperationException), typeof(Exception) }));
        Assert.That(entryGroup.ProtectedRegion.Next,
            Is.SameAs(entryGroup.associatedRegions[0]));
        Assert.That(entryGroup.associatedRegions[0].Next,
            Is.SameAs(entryGroup.associatedRegions[1]));
        Assert.That(entryGroup.associatedRegions[1].Next, Is.Null);

        optimizer.AggressiveDeadCodeEliminationAndReorder();
        optimizer.Emit();
        Assert.That(optimizer.outputInstructions.instructions.SelectMany(instruction => instruction.blocks)
                .Select(block => block.blockType),
            Is.EqualTo(new[]
            {
                ExceptionBlockType.BeginExceptionBlock,
                ExceptionBlockType.BeginCatchBlock,
                ExceptionBlockType.BeginCatchBlock,
                ExceptionBlockType.EndExceptionBlock,
            }));
    }

    [Test]
    public void AggressiveReorderPlacesEachRegionEntryFirst()
    {
        Optimizer.Optimizer optimizer = CreateTwoBlockTryOptimizer();
        optimizer.MakeBasicBlocks();
        optimizer.AggressiveDeadCodeEliminationAndReorder();

        foreach (var region in optimizer.Regions)
        {
            RegionNode entry = region.entry!;
            while (entry is Region nestedRegion)
                entry = nestedRegion.entry!;
            int entryIndex = optimizer.BasicBlocks.ToList().IndexOf((BasicBlock)entry);
            int firstRegionBlockIndex = optimizer.BasicBlocks
                .Select((block, index) => (block, index))
                .Where(item => item.block.HasAncestor(region))
                .Min(item => item.index);
            Assert.That(entryIndex, Is.EqualTo(firstRegionBlockIndex), region.ID);
        }
    }

    [Test]
    public void ConvertStackToVariablesDoesNotDependOnBasicBlockOrder()
    {
        // A backward-only edge carrying a stack value is an allowed intermediate optimizer shape,
        // although CIL emission requires another forward edge. The final aggressive reorder
        // restores that CIL invariant; variable conversion must not depend on it already holding.
        Optimizer.Optimizer optimizer = CreateOptimizer(generator =>
        {
            Label consumer = generator.DefineLabel();
            Label producer = generator.DefineLabel();
            return
            [
                new CodeInstruction(OpCodes.Br, producer),
                new CodeInstruction(OpCodes.Pop).WithLabels(consumer),
                new CodeInstruction(OpCodes.Ret),
                new CodeInstruction(OpCodes.Ldstr, "value").WithLabels(producer),
                new CodeInstruction(OpCodes.Br, consumer),
            ];
        });
        optimizer.MakeBasicBlocks();
        optimizer.SimpleDeadCodeElimination();
        BasicBlock consumer = optimizer.BasicBlocks.Single(block =>
            block.ops.Any(op => op.Opcode == OpCodes.Pop));
        BasicBlock producer = optimizer.BasicBlocks.Single(block =>
            block.ops.Any(op => op.Opcode == OpCodes.Ldstr));
        Assert.That(optimizer.BasicBlocks.ToList().IndexOf(consumer),
            Is.LessThan(optimizer.BasicBlocks.ToList().IndexOf(producer)));

        new StackToVariableConversion(optimizer).Run();

        Assert.That(consumer.entryStackVariables, Has.Count.EqualTo(1));
        Assert.That(consumer.entryStackVariables[0].type, Is.EqualTo(typeof(string)));
        Assert.That(consumer.ops[0].inputs, Is.EqualTo(consumer.entryStackVariables));
    }

    [Test]
    public void ConvertStackToVariablesUsesMutableVariablesForCrossBlockStack()
    {
        Optimizer.Optimizer optimizer = CreateOptimizer(generator =>
        {
            Label alternative = generator.DefineLabel();
            Label join = generator.DefineLabel();
            return
            [
                new CodeInstruction(OpCodes.Ldstr, "value"),
                new CodeInstruction(OpCodes.Ldc_I4_0),
                new CodeInstruction(OpCodes.Brfalse, alternative),
                new CodeInstruction(OpCodes.Br, join),
                new CodeInstruction(OpCodes.Nop).WithLabels(alternative),
                new CodeInstruction(OpCodes.Pop).WithLabels(join),
                new CodeInstruction(OpCodes.Ret),
            ];
        });
        optimizer.MakeBasicBlocks();
        int operationCount = optimizer.BasicBlocks.Sum(block => block.ops.Count);
        BasicBlock entry = optimizer.BasicBlocks[0];
        BasicBlock join = optimizer.BasicBlocks[3];
        Assert.That(join.incomingEdges, Has.Count.EqualTo(2));
        Assert.That(join.incomingEdges.SelectMany(edge => edge.assignments), Is.Empty);

        new StackToVariableConversion(optimizer).Run();

        Assert.That(optimizer.Form, Is.EqualTo(Optimizer.Optimizer.IrForm.Variables));
        Assert.That(entry.ops[2].inputs, Is.EqualTo(new[] { entry.ops[1].outputs.Single() }));
        Assert.That(optimizer.BasicBlocks.SelectMany(block => block.outgoingEdges)
            .SelectMany(edge => edge.assignments), Is.Empty);
        Assert.That(join.entryStackVariables, Has.Count.EqualTo(1));
        Assert.That(join.entryStackVariables[0], Is.SameAs(entry.ops[0].outputs.Single()));
        Assert.That(join.entryStackVariables[0].kind, Is.EqualTo(VariableKind.StackSlot));
        Assert.That(join.entryStackVariables[0].type, Is.EqualTo(typeof(string)));
        Assert.That(join.incomingEdges, Has.Count.EqualTo(2));
        Assert.That(optimizer.BasicBlocks.Sum(block => block.ops.Count), Is.EqualTo(operationCount));

        new VariableToStackConversion(optimizer).Run();
        optimizer.InsertBranches();
        optimizer.Emit();
        Assert.That(optimizer.outputInstructions.instructions, Has.None.Matches<CodeInstruction>(instruction =>
            instruction.IsLdloc() || instruction.IsStloc()));
    }

    [Test]
    public void ConvertStackToVariablesJoinsConcreteImplementationsAsTheirCommonInterface()
    {
        ConstructorInfo firstConstructor = typeof(OptimizerDataReader).GetConstructor([typeof(int)])!;
        ConstructorInfo secondConstructor = typeof(OptimizerAlternateDataReader).GetConstructor([typeof(int)])!;
        Optimizer.Optimizer optimizer = CreateOptimizer(generator =>
        {
            Label alternative = generator.DefineLabel();
            Label join = generator.DefineLabel();
            return
            [
                new CodeInstruction(OpCodes.Ldc_I4_0),
                new CodeInstruction(OpCodes.Brfalse, alternative),
                new CodeInstruction(OpCodes.Ldc_I4_7),
                new CodeInstruction(OpCodes.Newobj, firstConstructor),
                new CodeInstruction(OpCodes.Br, join),
                new CodeInstruction(OpCodes.Ldc_I4, 11).WithLabels(alternative),
                new CodeInstruction(OpCodes.Newobj, secondConstructor),
                new CodeInstruction(OpCodes.Pop).WithLabels(join),
                new CodeInstruction(OpCodes.Ret),
            ];
        });
        optimizer.MakeBasicBlocks();

        new StackToVariableConversion(optimizer).Run();

        BasicBlock join = optimizer.BasicBlocks.Single(block =>
            block.ops.Any(op => op.Opcode == OpCodes.Pop));
        Assert.That(join.entryStackVariables.Single().type, Is.EqualTo(typeof(IOptimizerDataReader)));
        Assert.That(join.incomingEdges.SelectMany(edge => edge.assignments), Is.Empty);
    }

    [Test]
    public void ConvertStackToVariablesUsesLocalBuilderIdentityAndDeclaredType()
    {
        LocalBuilder? declaredLocal = null;
        Optimizer.Optimizer optimizer = CreateOptimizer(generator =>
        {
            declaredLocal = generator.DeclareLocal(typeof(string));
            return
            [
                new CodeInstruction(OpCodes.Ldstr, "value"),
                new CodeInstruction(OpCodes.Stloc, declaredLocal),
                new CodeInstruction(OpCodes.Ldloc, declaredLocal),
                new CodeInstruction(OpCodes.Pop),
                new CodeInstruction(OpCodes.Ret),
            ];
        });
        optimizer.MakeBasicBlocks();
        new StackToVariableConversion(optimizer).Run();

        Variable local = optimizer.LocalVariables[declaredLocal!.LocalIndex];
        Op store = optimizer.BasicBlocks[0].ops[1];
        Op load = optimizer.BasicBlocks[0].ops[2];
        Assert.That(local.kind, Is.EqualTo(VariableKind.Local));
        Assert.That(local.type, Is.EqualTo(typeof(string)));
        Assert.That(local.localBuilder, Is.SameAs(declaredLocal));
        Assert.That(store.outputs, Is.EqualTo(new[] { local }));
        Assert.That(load.inputs, Is.EqualTo(new[] { local }));
    }

    [Test]
    public void ConvertStackToVariablesDoesNotGuessUnknownNumericLocalType()
    {
        Optimizer.Optimizer optimizer = CreateOptimizer(_ =>
        [
            new CodeInstruction(OpCodes.Ldc_I4_1),
            new CodeInstruction(OpCodes.Stloc_S, (byte)4),
            new CodeInstruction(OpCodes.Ldloc_S, (byte)4),
            new CodeInstruction(OpCodes.Pop),
            new CodeInstruction(OpCodes.Ret),
        ]);
        optimizer.MakeBasicBlocks();
        new StackToVariableConversion(optimizer).Run();

        Variable local = optimizer.LocalVariables[4];
        Assert.That(local.type, Is.Null);
        Assert.That(local.localBuilder, Is.Null);
        Assert.That(optimizer.BasicBlocks[0].ops[1].outputs, Is.EqualTo(new[] { local }));
        Assert.That(optimizer.BasicBlocks[0].ops[2].inputs, Is.EqualTo(new[] { local }));
    }

    [Test]
    public void ConvertStackToVariablesNormalizesSmallIntegerTypesToInt32StackType()
    {
        Type[] smallIntegerTypes =
        [
            typeof(sbyte),
            typeof(byte),
            typeof(bool),
            typeof(short),
            typeof(ushort),
            typeof(char),
            typeof(int),
            typeof(uint),
        ];
        Optimizer.Optimizer optimizer = CreateOptimizer(_ =>
        {
            List<CodeInstruction> instructions = [];
            foreach (Type type in smallIntegerTypes)
            {
                instructions.Add(new(OpCodes.Ldnull));
                instructions.Add(new(OpCodes.Unbox_Any, type));
                instructions.Add(new(OpCodes.Pop));
            }
            instructions.Add(new(OpCodes.Ret));
            return instructions;
        });
        optimizer.MakeBasicBlocks();

        new StackToVariableConversion(optimizer).Run();

        Assert.That(
            optimizer.BasicBlocks[0].ops
                .Where(op => op.Opcode == OpCodes.Unbox_Any)
                .Select(op => op.outputs.Single().type),
            Is.All.EqualTo(typeof(int)));
    }

    [Test]
    public void ConvertStackToVariablesNormalizesOtherNumericTypesToClrStackTypes()
    {
        Optimizer.Optimizer optimizer = CreateOptimizer(_ =>
        [
            new CodeInstruction(OpCodes.Ldnull),
            new CodeInstruction(OpCodes.Unbox_Any, typeof(ulong)),
            new CodeInstruction(OpCodes.Pop),
            new CodeInstruction(OpCodes.Ldnull),
            new CodeInstruction(OpCodes.Unbox_Any, typeof(float)),
            new CodeInstruction(OpCodes.Pop),
            new CodeInstruction(OpCodes.Ldnull),
            new CodeInstruction(OpCodes.Unbox_Any, typeof(UIntPtr)),
            new CodeInstruction(OpCodes.Pop),
            new CodeInstruction(OpCodes.Ret),
        ]);
        optimizer.MakeBasicBlocks();

        new StackToVariableConversion(optimizer).Run();

        Op[] unboxes = [.. optimizer.BasicBlocks[0].ops.Where(op => op.Opcode == OpCodes.Unbox_Any)];
        Assert.That(unboxes.Select(op => op.outputs.Single().type), Is.EqualTo(new[]
        {
            typeof(long),
            typeof(double),
            typeof(IntPtr),
        }));
    }

    [Test]
    public void ConvertStackToVariablesNormalizesUnsignedNativeIntegerOperationsToIntPtrStackType()
    {
        Optimizer.Optimizer optimizer = CreateOptimizer(_ =>
        [
            new CodeInstruction(OpCodes.Ldc_I4_1),
            new CodeInstruction(OpCodes.Conv_U),
            new CodeInstruction(OpCodes.Pop),
            new CodeInstruction(OpCodes.Ldc_I4_1),
            new CodeInstruction(OpCodes.Conv_Ovf_U),
            new CodeInstruction(OpCodes.Pop),
            new CodeInstruction(OpCodes.Ldc_I4_1),
            new CodeInstruction(OpCodes.Conv_Ovf_U_Un),
            new CodeInstruction(OpCodes.Pop),
            new CodeInstruction(OpCodes.Ldnull),
            new CodeInstruction(OpCodes.Ldlen),
            new CodeInstruction(OpCodes.Pop),
            new CodeInstruction(OpCodes.Ret),
        ]);
        optimizer.MakeBasicBlocks();

        new StackToVariableConversion(optimizer).Run();

        BasicBlock block = optimizer.BasicBlocks[0];
        Assert.That(new[]
        {
            block.ops[1].outputs.Single().type,
            block.ops[4].outputs.Single().type,
            block.ops[7].outputs.Single().type,
            block.ops[10].outputs.Single().type,
        }, Is.All.EqualTo(typeof(IntPtr)));
    }

    [Test]
    public void ConvertStackToVariablesPreservesClrStackTypesThroughArithmetic()
    {
        Optimizer.Optimizer optimizer = CreateOptimizer(_ =>
        [
            new CodeInstruction(OpCodes.Ldnull),
            new CodeInstruction(OpCodes.Unbox_Any, typeof(uint)),
            new CodeInstruction(OpCodes.Ldnull),
            new CodeInstruction(OpCodes.Unbox_Any, typeof(uint)),
            new CodeInstruction(OpCodes.Add),
            new CodeInstruction(OpCodes.Pop),
            new CodeInstruction(OpCodes.Ldnull),
            new CodeInstruction(OpCodes.Unbox_Any, typeof(ulong)),
            new CodeInstruction(OpCodes.Ldnull),
            new CodeInstruction(OpCodes.Unbox_Any, typeof(ulong)),
            new CodeInstruction(OpCodes.Mul),
            new CodeInstruction(OpCodes.Pop),
            new CodeInstruction(OpCodes.Ldnull),
            new CodeInstruction(OpCodes.Unbox_Any, typeof(float)),
            new CodeInstruction(OpCodes.Ldnull),
            new CodeInstruction(OpCodes.Unbox_Any, typeof(float)),
            new CodeInstruction(OpCodes.Div),
            new CodeInstruction(OpCodes.Pop),
            new CodeInstruction(OpCodes.Ldc_I4_1),
            new CodeInstruction(OpCodes.Conv_U),
            new CodeInstruction(OpCodes.Ldc_I4_2),
            new CodeInstruction(OpCodes.Conv_U),
            new CodeInstruction(OpCodes.Xor),
            new CodeInstruction(OpCodes.Pop),
            new CodeInstruction(OpCodes.Ret),
        ]);
        optimizer.MakeBasicBlocks();

        new StackToVariableConversion(optimizer).Run();

        BasicBlock block = optimizer.BasicBlocks[0];
        Assert.That(block.ops.Single(op => op.Opcode == OpCodes.Add).outputs.Single().type,
            Is.EqualTo(typeof(int)));
        Assert.That(block.ops.Single(op => op.Opcode == OpCodes.Mul).outputs.Single().type,
            Is.EqualTo(typeof(long)));
        Assert.That(block.ops.Single(op => op.Opcode == OpCodes.Div).outputs.Single().type,
            Is.EqualTo(typeof(double)));
        Assert.That(block.ops.Single(op => op.Opcode == OpCodes.Xor).outputs.Single().type,
            Is.EqualTo(typeof(IntPtr)));
    }

    [Test]
    public void ConvertStackToVariablesPreservesUnknownTypeThroughArithmetic()
    {
        Optimizer.Optimizer optimizer = CreateOptimizer(_ =>
        [
            new CodeInstruction(OpCodes.Ldloc_S, (byte)4),
            new CodeInstruction(OpCodes.Ldc_I4_1),
            new CodeInstruction(OpCodes.Add),
            new CodeInstruction(OpCodes.Pop),
            new CodeInstruction(OpCodes.Ret),
        ]);
        optimizer.MakeBasicBlocks();

        new StackToVariableConversion(optimizer).Run();

        BasicBlock block = optimizer.BasicBlocks[0];
        Assert.That(block.ops[2].outputs.Single().type, Is.SameAs(block.ops[0].outputs.Single().type));
    }

    [Test]
    public void ConvertStackToVariablesInfersUnsignedOverflowPointerArithmeticTypes()
    {
        LocalBuilder? local = null;
        Optimizer.Optimizer optimizer = CreateOptimizer(generator =>
        {
            local = generator.DeclareLocal(typeof(int));
            return
            [
                new CodeInstruction(OpCodes.Ldloca, local),
                new CodeInstruction(OpCodes.Ldc_I4_1),
                new CodeInstruction(OpCodes.Add_Ovf_Un),
                new CodeInstruction(OpCodes.Pop),
                new CodeInstruction(OpCodes.Ldc_I4_1),
                new CodeInstruction(OpCodes.Ldloca, local),
                new CodeInstruction(OpCodes.Add_Ovf_Un),
                new CodeInstruction(OpCodes.Pop),
                new CodeInstruction(OpCodes.Ldloca, local),
                new CodeInstruction(OpCodes.Ldc_I4_1),
                new CodeInstruction(OpCodes.Sub_Ovf_Un),
                new CodeInstruction(OpCodes.Pop),
                new CodeInstruction(OpCodes.Ldloca, local),
                new CodeInstruction(OpCodes.Ldloca, local),
                new CodeInstruction(OpCodes.Sub_Ovf_Un),
                new CodeInstruction(OpCodes.Pop),
                new CodeInstruction(OpCodes.Ret),
            ];
        });
        optimizer.MakeBasicBlocks();

        new StackToVariableConversion(optimizer).Run();

        BasicBlock block = optimizer.BasicBlocks[0];
        Type pointerType = block.ops[0].outputs.Single().type!;
        Assert.That(pointerType, Is.EqualTo(typeof(int).MakeByRefType()));
        Assert.That(block.ops[2].outputs.Single().type, Is.SameAs(pointerType));
        Assert.That(block.ops[6].outputs.Single().type, Is.SameAs(pointerType));
        Assert.That(block.ops[10].outputs.Single().type, Is.SameAs(pointerType));
        Assert.That(block.ops[14].outputs.Single().type, Is.EqualTo(typeof(IntPtr)));
    }

    [Test]
    public void ConvertStackToVariablesPreservesPointerCategoryForLocalWithoutMetadata()
    {
        Optimizer.Optimizer optimizer = CreateOptimizer(_ =>
        [
            new CodeInstruction(OpCodes.Ldloca_S, (byte)4),
            new CodeInstruction(OpCodes.Ldc_I4_1),
            new CodeInstruction(OpCodes.Add_Ovf_Un),
            new CodeInstruction(OpCodes.Pop),
            new CodeInstruction(OpCodes.Ldloca_S, (byte)4),
            new CodeInstruction(OpCodes.Ldloca_S, (byte)4),
            new CodeInstruction(OpCodes.Sub_Ovf_Un),
            new CodeInstruction(OpCodes.Pop),
            new CodeInstruction(OpCodes.Ret),
        ]);
        optimizer.MakeBasicBlocks();

        new StackToVariableConversion(optimizer).Run();

        BasicBlock block = optimizer.BasicBlocks[0];
        Type anyType = typeof(Optimizer.Optimizer).GetNestedType("AnyType", BindingFlags.NonPublic)!;
        Type pointerType = block.ops[0].outputs.Single().type!;
        Assert.That(pointerType.IsByRef, Is.True);
        Assert.That(pointerType.GetElementType(), Is.SameAs(anyType));
        Assert.That(block.ops[2].outputs.Single().type, Is.SameAs(pointerType));
        Assert.That(block.ops[6].outputs.Single().type, Is.EqualTo(typeof(IntPtr)));
    }

    [Test]
    public void MakeBasicBlocksBundlesPrefixesWithTheirOperation()
    {
        FieldInfo field = typeof(OptimizerPatches).GetField(nameof(OptimizerPatches.PatchCalls))!;
        Optimizer.Optimizer optimizer = CreateOptimizer(_ =>
        [
            new CodeInstruction(OpCodes.Volatile),
            new CodeInstruction(OpCodes.Ldsfld, field),
            new CodeInstruction(OpCodes.Pop),
            new CodeInstruction(OpCodes.Ret),
        ]);
        optimizer.MakeBasicBlocks();

        Op fieldLoad = optimizer.BasicBlocks[0].ops[0];
        Assert.That(fieldLoad.Opcode, Is.EqualTo(OpCodes.Ldsfld));
        Assert.That(fieldLoad.Prefixes, Is.EqualTo(new[] { OpCodes.Volatile }));

        new StackToVariableConversion(optimizer).Run();
        new VariableToStackConversion(optimizer).Run();
        optimizer.Emit();
        Assert.That(optimizer.outputInstructions.instructions.Select(instruction => instruction.opcode), Is.EqualTo(new[]
        {
            OpCodes.Volatile,
            OpCodes.Ldsfld,
            OpCodes.Pop,
            OpCodes.Ret,
        }));
    }

    [Test]
    public void ConvertStackToVariablesMakesReturnValueAnExplicitInput()
    {
        MethodInfo targetMethod = typeof(StaticMethodTargets).GetMethod(nameof(StaticMethodTargets.IntResult))!;
        Optimizer.Optimizer optimizer = CreateOptimizer(targetMethod, _ =>
        [
            new CodeInstruction(OpCodes.Ldc_I4_1),
            new CodeInstruction(OpCodes.Ret),
        ]);
        optimizer.MakeBasicBlocks();
        new StackToVariableConversion(optimizer).Run();

        Op value = optimizer.BasicBlocks[0].ops[0];
        Op returnOperation = optimizer.BasicBlocks[0].ops[1];
        Assert.That(returnOperation.inputs, Is.EqualTo(new[] { value.outputs.Single() }));
        Assert.That(optimizer.BasicBlocks[0].outgoingEdges, Is.Empty);
    }

    [Test]
    public void ConvertStackToVariablesTracksMutableArgumentIdentity()
    {
        MethodInfo targetMethod = typeof(StaticMethodTargets).GetMethod(nameof(StaticMethodTargets.IntArgument))!;
        Optimizer.Optimizer optimizer = CreateOptimizer(targetMethod, _ =>
        [
            new CodeInstruction(OpCodes.Ldc_I4_1),
            new CodeInstruction(OpCodes.Starg_S, (byte)0),
            new CodeInstruction(OpCodes.Ldarg_0),
            new CodeInstruction(OpCodes.Pop),
            new CodeInstruction(OpCodes.Ret),
        ]);
        optimizer.MakeBasicBlocks();
        new StackToVariableConversion(optimizer).Run();

        Variable argument = optimizer.ArgumentVariables[0];
        Assert.That(argument.type, Is.EqualTo(typeof(int)));
        Assert.That(optimizer.BasicBlocks[0].ops[1].outputs, Is.EqualTo(new[] { argument }));
        Assert.That(optimizer.BasicBlocks[0].ops[2].inputs, Is.EqualTo(new[] { argument }));
    }

    [Test]
    public void ConservativeConstantPropagationReplacesLocalLdobjWithDirectLoad()
    {
        LocalBuilder? target = null;
        LocalBuilder? reference = null;
        Optimizer.Optimizer optimizer = CreateOptimizer(generator =>
        {
            target = generator.DeclareLocal(typeof(int));
            reference = generator.DeclareLocal(typeof(int).MakeByRefType());
            return
            [
                new CodeInstruction(OpCodes.Ldloca, target),
                new CodeInstruction(OpCodes.Stloc, reference),
                new CodeInstruction(OpCodes.Ldloc, reference),
                new CodeInstruction(OpCodes.Ldobj, typeof(int)),
                new CodeInstruction(OpCodes.Pop),
                new CodeInstruction(OpCodes.Ret),
            ];
        });
        optimizer.MakeBasicBlocks();
        new StackToVariableConversion(optimizer).Run();

        optimizer.ConservativeConstantPropagation();

        BasicBlock block = optimizer.BasicBlocks.Single();
        Assert.That(OpCodesIn(block), Is.EqualTo(new[] { OpCodes.Ldloc, OpCodes.Pop, OpCodes.Ret }));
        Assert.That(block.ops[0].inputs, Is.EqualTo(new[] { optimizer.LocalVariables[target!.LocalIndex] }));
        Assert.That(optimizer.LocalVariables[target.LocalIndex].addressTaken, Is.False);
        Assert.That(block.ops.SelectMany(op => op.inputs.Concat(op.outputs)),
            Does.Not.Contain(optimizer.LocalVariables[reference!.LocalIndex]));
    }

    [Test]
    public void ConservativeConstantPropagationHandlesInterleavedPatchArgumentSetup()
    {
        MethodInfo targetMethod = typeof(StaticMethodTargets).GetMethod(nameof(StaticMethodTargets.IntArgument))!;
        LocalBuilder? copiedArgument = null;
        LocalBuilder? reference = null;
        Optimizer.Optimizer optimizer = CreateOptimizer(targetMethod, generator =>
        {
            copiedArgument = generator.DeclareLocal(typeof(int));
            reference = generator.DeclareLocal(typeof(int).MakeByRefType());
            return
            [
                // Multiple patch arguments can be pushed before their temporary stores. The
                // ordinary value argument remains between the address producer and its store.
                new CodeInstruction(OpCodes.Ldarga, 0),
                new CodeInstruction(OpCodes.Ldarg, 0),
                new CodeInstruction(OpCodes.Stloc, copiedArgument),
                new CodeInstruction(OpCodes.Stloc, reference),
                new CodeInstruction(OpCodes.Ldloc, reference),
                new CodeInstruction(OpCodes.Ldobj, typeof(int)),
                new CodeInstruction(OpCodes.Pop),
                new CodeInstruction(OpCodes.Ret),
            ];
        });
        optimizer.MakeBasicBlocks();
        new StackToVariableConversion(optimizer).Run();

        optimizer.ConservativeConstantPropagation();

        BasicBlock block = optimizer.BasicBlocks.Single();
        Assert.That(OpCodesIn(block), Is.EqualTo(new[]
        {
            OpCodes.Ldarg,
            OpCodes.Stloc,
            OpCodes.Ldarg,
            OpCodes.Pop,
            OpCodes.Ret,
        }));
        Assert.Multiple(() =>
        {
            Assert.That(block.ops[2].inputs,
                Is.EqualTo(new[] { optimizer.ArgumentVariables[0] }));
            Assert.That(optimizer.ArgumentVariables[0].addressTaken, Is.False);
            Assert.That(block.ops.SelectMany(op => op.inputs.Concat(op.outputs)),
                Does.Not.Contain(optimizer.LocalVariables[reference!.LocalIndex]));
        });
    }

    [Test]
    public void ConservativeConstantPropagationReplacesLocalStobjWithDirectStore()
    {
        LocalBuilder? target = null;
        LocalBuilder? reference = null;
        Optimizer.Optimizer optimizer = CreateOptimizer(generator =>
        {
            target = generator.DeclareLocal(typeof(int));
            reference = generator.DeclareLocal(typeof(int).MakeByRefType());
            return
            [
                new CodeInstruction(OpCodes.Ldloca, target),
                new CodeInstruction(OpCodes.Stloc, reference),
                new CodeInstruction(OpCodes.Ldloc, reference),
                new CodeInstruction(OpCodes.Ldc_I4, 42),
                new CodeInstruction(OpCodes.Stobj, typeof(int)),
                new CodeInstruction(OpCodes.Ret),
            ];
        });
        optimizer.MakeBasicBlocks();
        new StackToVariableConversion(optimizer).Run();

        optimizer.ConservativeConstantPropagation();

        BasicBlock block = optimizer.BasicBlocks.Single();
        Assert.That(OpCodesIn(block), Is.EqualTo(new[] { OpCodes.Ldc_I4, OpCodes.Stloc, OpCodes.Ret }));
        Assert.That(block.ops[1].outputs, Is.EqualTo(new[] { optimizer.LocalVariables[target!.LocalIndex] }));
        Assert.That(optimizer.LocalVariables[target.LocalIndex].addressTaken, Is.False);
    }

    [Test]
    public void ConservativeConstantPropagationReplacesArgumentObjectAccesses()
    {
        MethodInfo targetMethod = typeof(StaticMethodTargets).GetMethod(nameof(StaticMethodTargets.IntArgument))!;
        LocalBuilder? reference = null;
        Optimizer.Optimizer optimizer = CreateOptimizer(targetMethod, generator =>
        {
            reference = generator.DeclareLocal(typeof(int).MakeByRefType());
            return
            [
                new CodeInstruction(OpCodes.Ldarga, 0),
                new CodeInstruction(OpCodes.Stloc, reference),
                new CodeInstruction(OpCodes.Ldloc, reference),
                new CodeInstruction(OpCodes.Ldobj, typeof(int)),
                new CodeInstruction(OpCodes.Pop),
                new CodeInstruction(OpCodes.Ldloc, reference),
                new CodeInstruction(OpCodes.Ldc_I4, 42),
                new CodeInstruction(OpCodes.Stobj, typeof(int)),
                new CodeInstruction(OpCodes.Ret),
            ];
        });
        optimizer.MakeBasicBlocks();
        new StackToVariableConversion(optimizer).Run();

        optimizer.ConservativeConstantPropagation();

        BasicBlock block = optimizer.BasicBlocks.Single();
        Assert.That(OpCodesIn(block), Is.EqualTo(new[]
        {
            OpCodes.Ldarg,
            OpCodes.Pop,
            OpCodes.Ldc_I4,
            OpCodes.Starg,
            OpCodes.Ret,
        }));
        Assert.That(block.ops[0].inputs, Is.EqualTo(new[] { optimizer.ArgumentVariables[0] }));
        Assert.That(block.ops[3].outputs, Is.EqualTo(new[] { optimizer.ArgumentVariables[0] }));
        Assert.That(optimizer.ArgumentVariables[0].addressTaken, Is.False);
    }

    [Test]
    public void ConservativeConstantPropagationHandlesCompilerIndirectOpcodes()
    {
        MethodInfo targetMethod = typeof(StaticMethodTargets).GetMethod(nameof(StaticMethodTargets.IntArgument))!;
        LocalBuilder? reference = null;
        Optimizer.Optimizer optimizer = CreateOptimizer(targetMethod, generator =>
        {
            reference = generator.DeclareLocal(typeof(int).MakeByRefType());
            return
            [
                new CodeInstruction(OpCodes.Ldarga, 0),
                new CodeInstruction(OpCodes.Stloc, reference),
                new CodeInstruction(OpCodes.Ldloc, reference),
                new CodeInstruction(OpCodes.Ldind_I4),
                new CodeInstruction(OpCodes.Pop),
                new CodeInstruction(OpCodes.Ldloc, reference),
                new CodeInstruction(OpCodes.Ldc_I4, 42),
                new CodeInstruction(OpCodes.Stind_I4),
                new CodeInstruction(OpCodes.Ret),
            ];
        });
        optimizer.MakeBasicBlocks();
        new StackToVariableConversion(optimizer).Run();

        optimizer.ConservativeConstantPropagation();

        Assert.That(OpCodesIn(optimizer.BasicBlocks.Single()), Is.EqualTo(new[]
        {
            OpCodes.Ldarg,
            OpCodes.Pop,
            OpCodes.Ldc_I4,
            OpCodes.Starg,
            OpCodes.Ret,
        }));
    }

    [Test]
    public void ConservativeConstantPropagationIgnoresSignednessForFourByteIndirectLoad()
    {
        LocalBuilder? target = null;
        LocalBuilder? reference = null;
        Optimizer.Optimizer optimizer = CreateOptimizer(generator =>
        {
            target = generator.DeclareLocal(typeof(uint));
            reference = generator.DeclareLocal(typeof(uint).MakeByRefType());
            return
            [
                new CodeInstruction(OpCodes.Ldloca, target),
                new CodeInstruction(OpCodes.Stloc, reference),
                new CodeInstruction(OpCodes.Ldloc, reference),
                new CodeInstruction(OpCodes.Ldind_I4),
                new CodeInstruction(OpCodes.Pop),
                new CodeInstruction(OpCodes.Ret),
            ];
        });
        optimizer.MakeBasicBlocks();
        new StackToVariableConversion(optimizer).Run();

        optimizer.ConservativeConstantPropagation();

        BasicBlock block = optimizer.BasicBlocks.Single();
        Assert.That(OpCodesIn(block), Is.EqualTo(new[] { OpCodes.Ldloc, OpCodes.Pop, OpCodes.Ret }));
        Assert.That(block.ops[0].inputs,
            Is.EqualTo(new[] { optimizer.LocalVariables[target!.LocalIndex] }));
    }

    [Test]
    public void ConservativeConstantPropagationPreservesSmallIntegerSignExtension()
    {
        LocalBuilder? target = null;
        LocalBuilder? reference = null;
        Optimizer.Optimizer optimizer = CreateOptimizer(generator =>
        {
            target = generator.DeclareLocal(typeof(byte));
            reference = generator.DeclareLocal(typeof(byte).MakeByRefType());
            return
            [
                new CodeInstruction(OpCodes.Ldloca, target),
                new CodeInstruction(OpCodes.Stloc, reference),
                new CodeInstruction(OpCodes.Ldloc, reference),
                new CodeInstruction(OpCodes.Ldind_I1),
                new CodeInstruction(OpCodes.Pop),
                new CodeInstruction(OpCodes.Ret),
            ];
        });
        optimizer.MakeBasicBlocks();
        new StackToVariableConversion(optimizer).Run();

        optimizer.ConservativeConstantPropagation();

        Assert.That(OpCodesIn(optimizer.BasicBlocks.Single()),
            Is.EqualTo(new[] { OpCodes.Ldloca, OpCodes.Ldind_I1, OpCodes.Pop, OpCodes.Ret }));
    }

    [Test]
    public void ConservativeConstantPropagationRematerializesReferenceForUnsupportedUse()
    {
        LocalBuilder? target = null;
        LocalBuilder? reference = null;
        MethodInfo consumeReference = typeof(OptimizerPassTests).GetMethod(
            nameof(ConsumeReference), BindingFlags.Static | BindingFlags.NonPublic)!;
        Optimizer.Optimizer optimizer = CreateOptimizer(generator =>
        {
            target = generator.DeclareLocal(typeof(int));
            reference = generator.DeclareLocal(typeof(int).MakeByRefType());
            return
            [
                new CodeInstruction(OpCodes.Ldloca, target),
                new CodeInstruction(OpCodes.Stloc, reference),
                new CodeInstruction(OpCodes.Ldloc, reference),
                new CodeInstruction(OpCodes.Call, consumeReference),
                new CodeInstruction(OpCodes.Ret),
            ];
        });
        optimizer.MakeBasicBlocks();
        new StackToVariableConversion(optimizer).Run();

        optimizer.ConservativeConstantPropagation();

        BasicBlock block = optimizer.BasicBlocks.Single();
        Assert.That(OpCodesIn(block), Is.EqualTo(new[] { OpCodes.Ldloca, OpCodes.Call, OpCodes.Ret }));
        Assert.That(block.ops[0].inputs, Is.EqualTo(new[] { optimizer.LocalVariables[target!.LocalIndex] }));
        Assert.That(optimizer.LocalVariables[target.LocalIndex].addressTaken, Is.True);
        Assert.That(block.ops.SelectMany(op => op.inputs.Concat(op.outputs)),
            Does.Not.Contain(optimizer.LocalVariables[reference!.LocalIndex]));
    }

    [Test]
    public void ConservativeConstantPropagationRematerializesPrimitiveConstant()
    {
        LocalBuilder? local = null;
        Optimizer.Optimizer optimizer = CreateOptimizer(generator =>
        {
            local = generator.DeclareLocal(typeof(int));
            return
            [
                new CodeInstruction(OpCodes.Ldc_I4, 42),
                new CodeInstruction(OpCodes.Stloc, local),
                new CodeInstruction(OpCodes.Ldloc, local),
                new CodeInstruction(OpCodes.Pop),
                new CodeInstruction(OpCodes.Ret),
            ];
        });
        optimizer.MakeBasicBlocks();
        new StackToVariableConversion(optimizer).Run();

        optimizer.ConservativeConstantPropagation();

        Assert.That(OpCodesIn(optimizer.BasicBlocks.Single()),
            Is.EqualTo(new[] { OpCodes.Ldc_I4, OpCodes.Pop, OpCodes.Ret }));
    }

    [Test]
    public void ConservativeConstantPropagationRematerializesNullThroughManagedReferenceLocal()
    {
        LocalBuilder? reference = null;
        Optimizer.Optimizer optimizer = CreateOptimizer(generator =>
        {
            reference = generator.DeclareLocal(typeof(int).MakeByRefType());
            return
            [
                new CodeInstruction(OpCodes.Ldnull),
                new CodeInstruction(OpCodes.Stloc, reference),
                new CodeInstruction(OpCodes.Ldloc, reference),
                new CodeInstruction(OpCodes.Pop),
                new CodeInstruction(OpCodes.Ret),
            ];
        });
        optimizer.MakeBasicBlocks();
        new StackToVariableConversion(optimizer).Run();

        optimizer.ConservativeConstantPropagation();

        Assert.That(OpCodesIn(optimizer.BasicBlocks.Single()),
            Is.EqualTo(new[] { OpCodes.Ldnull, OpCodes.Pop, OpCodes.Ret }));
    }

    [Test]
    public void ConservativeConstantPropagationHandlesDominatingDefinitionInAnotherBlock()
    {
        LocalBuilder? target = null;
        LocalBuilder? reference = null;
        Optimizer.Optimizer optimizer = CreateOptimizer(generator =>
        {
            target = generator.DeclareLocal(typeof(int));
            reference = generator.DeclareLocal(typeof(int).MakeByRefType());
            Label use = generator.DefineLabel();
            return
            [
                new CodeInstruction(OpCodes.Ldloca, target),
                new CodeInstruction(OpCodes.Stloc, reference),
                new CodeInstruction(OpCodes.Br, use),
                new CodeInstruction(OpCodes.Ldloc, reference).WithLabels(use),
                new CodeInstruction(OpCodes.Ldobj, typeof(int)),
                new CodeInstruction(OpCodes.Pop),
                new CodeInstruction(OpCodes.Ret),
            ];
        });
        optimizer.MakeBasicBlocks();
        new StackToVariableConversion(optimizer).Run();

        optimizer.ConservativeConstantPropagation();

        Assert.That(optimizer.BasicBlocks.SelectMany(block => block.ops).Select(op => op.Opcode),
            Is.EqualTo(new[] { OpCodes.Ldloc, OpCodes.Pop, OpCodes.Ret }));
        Assert.That(optimizer.LocalVariables[target!.LocalIndex].addressTaken, Is.False);
    }

    [Test]
    public void ConservativeConstantPropagationTreatsProtectedRegionEntryAsNormalControlFlow()
    {
        LocalBuilder? target = null;
        LocalBuilder? reference = null;
        Optimizer.Optimizer optimizer = CreateOptimizer(generator =>
        {
            target = generator.DeclareLocal(typeof(int));
            reference = generator.DeclareLocal(typeof(int).MakeByRefType());
            Label exit = generator.DefineLabel();
            var tryLoad = new CodeInstruction(OpCodes.Ldloc, reference);
            tryLoad.blocks.Add(new ExceptionBlock(ExceptionBlockType.BeginExceptionBlock));
            var catchPop = new CodeInstruction(OpCodes.Pop);
            catchPop.blocks.Add(new ExceptionBlock(ExceptionBlockType.BeginCatchBlock, typeof(Exception)));
            var catchLeave = new CodeInstruction(OpCodes.Leave, exit);
            catchLeave.blocks.Add(new ExceptionBlock(ExceptionBlockType.EndExceptionBlock));
            return
            [
                new CodeInstruction(OpCodes.Ldloca, target),
                new CodeInstruction(OpCodes.Stloc, reference),
                tryLoad,
                new CodeInstruction(OpCodes.Ldobj, typeof(int)),
                new CodeInstruction(OpCodes.Pop),
                new CodeInstruction(OpCodes.Leave, exit),
                catchPop,
                catchLeave,
                new CodeInstruction(OpCodes.Ret).WithLabels(exit),
            ];
        });
        optimizer.MakeBasicBlocks();
        optimizer.SimpleDeadCodeElimination();
        new StackToVariableConversion(optimizer).Run();

        optimizer.ConservativeConstantPropagation();

        Assert.That(optimizer.BasicBlocks.SelectMany(block => block.ops).Select(op => op.Opcode),
            Does.Not.Contain(OpCodes.Stloc));
        Assert.That(optimizer.BasicBlocks.SelectMany(block => block.ops).Select(op => op.Opcode),
            Does.Not.Contain(OpCodes.Ldobj));
        Assert.That(optimizer.BasicBlocks.SelectMany(block => block.ops).Select(op => op.Opcode),
            Does.Contain(OpCodes.Ldloc));
        Assert.That(optimizer.LocalVariables[target!.LocalIndex].addressTaken, Is.False);
    }

    [Test]
    public void ConservativeConstantPropagationDoesNotPropagateTryDefinitionIntoCatch()
    {
        LocalBuilder? target = null;
        LocalBuilder? reference = null;
        Optimizer.Optimizer optimizer = CreateOptimizer(generator =>
        {
            target = generator.DeclareLocal(typeof(int));
            reference = generator.DeclareLocal(typeof(int).MakeByRefType());
            Label exit = generator.DefineLabel();
            var call = new CodeInstruction(OpCodes.Call, TargetMethod);
            call.blocks.Add(new ExceptionBlock(ExceptionBlockType.BeginExceptionBlock));
            var catchPop = new CodeInstruction(OpCodes.Pop);
            catchPop.blocks.Add(new ExceptionBlock(ExceptionBlockType.BeginCatchBlock, typeof(Exception)));
            var catchLeave = new CodeInstruction(OpCodes.Leave, exit);
            catchLeave.blocks.Add(new ExceptionBlock(ExceptionBlockType.EndExceptionBlock));
            return
            [
                call,
                new CodeInstruction(OpCodes.Ldloca, target),
                new CodeInstruction(OpCodes.Stloc, reference),
                new CodeInstruction(OpCodes.Leave, exit),
                catchPop,
                new CodeInstruction(OpCodes.Ldloc, reference),
                new CodeInstruction(OpCodes.Ldobj, typeof(int)),
                new CodeInstruction(OpCodes.Pop),
                catchLeave,
                new CodeInstruction(OpCodes.Ret).WithLabels(exit),
            ];
        });
        optimizer.MakeBasicBlocks();
        optimizer.SimpleDeadCodeElimination();
        new StackToVariableConversion(optimizer).Run();

        optimizer.ConservativeConstantPropagation();

        Assert.That(optimizer.BasicBlocks.SelectMany(block => block.ops).Select(op => op.Opcode),
            Does.Contain(OpCodes.Stloc));
        Assert.That(optimizer.BasicBlocks.SelectMany(block => block.ops).Select(op => op.Opcode),
            Does.Contain(OpCodes.Ldobj));
    }

    [Test]
    public void ConservativeConstantPropagationDoesNotUseDefinitionBypassedByBranch()
    {
        LocalBuilder? target = null;
        LocalBuilder? reference = null;
        Optimizer.Optimizer optimizer = CreateOptimizer(generator =>
        {
            target = generator.DeclareLocal(typeof(int));
            reference = generator.DeclareLocal(typeof(int).MakeByRefType());
            Label use = generator.DefineLabel();
            return
            [
                new CodeInstruction(OpCodes.Ldc_I4_0),
                new CodeInstruction(OpCodes.Brtrue, use),
                new CodeInstruction(OpCodes.Ldloca, target),
                new CodeInstruction(OpCodes.Stloc, reference),
                new CodeInstruction(OpCodes.Ldloc, reference).WithLabels(use),
                new CodeInstruction(OpCodes.Ldobj, typeof(int)),
                new CodeInstruction(OpCodes.Pop),
                new CodeInstruction(OpCodes.Ret),
            ];
        });
        optimizer.MakeBasicBlocks();
        new StackToVariableConversion(optimizer).Run();

        optimizer.ConservativeConstantPropagation();

        Assert.That(optimizer.BasicBlocks.SelectMany(block => block.ops).Select(op => op.Opcode),
            Does.Contain(OpCodes.Stloc));
        Assert.That(optimizer.BasicBlocks.SelectMany(block => block.ops).Select(op => op.Opcode),
            Does.Contain(OpCodes.Ldobj));
    }

    [Test]
    public void ConservativeConstantPropagationDoesNotUseDefinitionAfterEarlierRead()
    {
        LocalBuilder? local = null;
        Optimizer.Optimizer optimizer = CreateOptimizer(generator =>
        {
            local = generator.DeclareLocal(typeof(int));
            return
            [
                new CodeInstruction(OpCodes.Ldloc, local),
                new CodeInstruction(OpCodes.Pop),
                new CodeInstruction(OpCodes.Ldc_I4, 42),
                new CodeInstruction(OpCodes.Stloc, local),
                new CodeInstruction(OpCodes.Ret),
            ];
        });
        optimizer.MakeBasicBlocks();
        new StackToVariableConversion(optimizer).Run();

        optimizer.ConservativeConstantPropagation();

        Assert.That(OpCodesIn(optimizer.BasicBlocks.Single()), Is.EqualTo(new[]
        {
            OpCodes.Ldloc,
            OpCodes.Pop,
            OpCodes.Ldc_I4,
            OpCodes.Stloc,
            OpCodes.Ret,
        }));
    }

    [Test]
    public void ConservativeConstantPropagationDoesNotUseMultiplyAssignedLocal()
    {
        LocalBuilder? local = null;
        Optimizer.Optimizer optimizer = CreateOptimizer(generator =>
        {
            local = generator.DeclareLocal(typeof(int));
            return
            [
                new CodeInstruction(OpCodes.Ldc_I4_1),
                new CodeInstruction(OpCodes.Stloc, local),
                new CodeInstruction(OpCodes.Ldc_I4_2),
                new CodeInstruction(OpCodes.Stloc, local),
                new CodeInstruction(OpCodes.Ldloc, local),
                new CodeInstruction(OpCodes.Pop),
                new CodeInstruction(OpCodes.Ret),
            ];
        });
        optimizer.MakeBasicBlocks();
        new StackToVariableConversion(optimizer).Run();

        optimizer.ConservativeConstantPropagation();

        Assert.That(OpCodesIn(optimizer.BasicBlocks.Single()), Is.EqualTo(new[]
        {
            OpCodes.Ldc_I4_1,
            OpCodes.Stloc,
            OpCodes.Ldc_I4_2,
            OpCodes.Stloc,
            OpCodes.Ldloc,
            OpCodes.Pop,
            OpCodes.Ret,
        }));
    }

    [Test]
    public void ConservativeConstantPropagationPreservesPrefixedIndirectAccess()
    {
        LocalBuilder? target = null;
        LocalBuilder? reference = null;
        Optimizer.Optimizer optimizer = CreateOptimizer(generator =>
        {
            target = generator.DeclareLocal(typeof(int));
            reference = generator.DeclareLocal(typeof(int).MakeByRefType());
            return
            [
                new CodeInstruction(OpCodes.Ldloca, target),
                new CodeInstruction(OpCodes.Stloc, reference),
                new CodeInstruction(OpCodes.Ldloc, reference),
                new CodeInstruction(OpCodes.Volatile),
                new CodeInstruction(OpCodes.Ldobj, typeof(int)),
                new CodeInstruction(OpCodes.Pop),
                new CodeInstruction(OpCodes.Ret),
            ];
        });
        optimizer.MakeBasicBlocks();
        new StackToVariableConversion(optimizer).Run();

        optimizer.ConservativeConstantPropagation();

        BasicBlock block = optimizer.BasicBlocks.Single();
        Assert.That(OpCodesIn(block), Is.EqualTo(new[] { OpCodes.Ldloca, OpCodes.Ldobj, OpCodes.Pop, OpCodes.Ret }));
        Assert.That(block.ops[1].Prefixes, Is.EqualTo(new[] { OpCodes.Volatile }));
        Assert.That(optimizer.LocalVariables[target!.LocalIndex].addressTaken, Is.True);
    }

    [Test]
    public void ConservativeConstantPropagationRejectsStackForm()
    {
        Optimizer.Optimizer optimizer = CreateOptimizer(_ => [new CodeInstruction(OpCodes.Ret)]);
        optimizer.MakeBasicBlocks();

        Assert.That(
            () => optimizer.ConservativeConstantPropagation(),
            Throws.InvalidOperationException.With.Message.Contains("regular variable form"));
    }

    [Test]
    public void ConservativeConstantPropagationRejectsSsaEdgeAssignments()
    {
        MethodInfo targetMethod = typeof(StaticMethodTargets).GetMethod(nameof(StaticMethodTargets.IntArgument))!;
        Optimizer.Optimizer optimizer = CreateOptimizer(targetMethod, generator =>
        {
            Label target = generator.DefineLabel();
            return
            [
                new CodeInstruction(OpCodes.Br, target),
                new CodeInstruction(OpCodes.Ret).WithLabels(target),
            ];
        });
        optimizer.MakeBasicBlocks();
        new StackToVariableConversion(optimizer).Run();
        ControlFlowEdge edge = optimizer.BasicBlocks[0].outgoingEdges.Single();
        Variable argument = optimizer.ArgumentVariables[0];
        edge.assignments.Add(new VariableAssignment(argument, argument));

        Assert.That(
            () => optimizer.ConservativeConstantPropagation(),
            Throws.InvalidOperationException.With.Message.Contains("requires empty edge"));
    }

    [Test]
    public void ConvertStackToVariablesUsesSymbolicStackAliasingForDup()
    {
        Optimizer.Optimizer optimizer = CreateOptimizer(_ =>
        [
            new CodeInstruction(OpCodes.Ldstr, "value"),
            new CodeInstruction(OpCodes.Dup),
            new CodeInstruction(OpCodes.Pop),
            new CodeInstruction(OpCodes.Pop),
            new CodeInstruction(OpCodes.Ret),
        ]);
        optimizer.MakeBasicBlocks();
        new StackToVariableConversion(optimizer).Run();

        Op load = optimizer.BasicBlocks[0].ops[0];
        Op duplicate = optimizer.BasicBlocks[0].ops[1];
        Assert.That(duplicate.inputs, Is.EqualTo(new[] { load.outputs.Single() }));
        Assert.That(duplicate.outputs, Is.EqualTo(new[] { load.outputs.Single(), load.outputs.Single() }));

        new VariableToStackConversion(optimizer).Run();
        Assert.That(optimizer.BasicBlocks[0].ops.Select(op => op.Opcode), Is.EqualTo(new[]
        {
            OpCodes.Ldstr,
            OpCodes.Dup,
            OpCodes.Pop,
            OpCodes.Pop,
            OpCodes.Ret,
        }));
    }

    [Test]
    public void ConvertVariablesToStackUsesCanonicalStorageVariable()
    {
        LocalBuilder? firstLocal = null;
        LocalBuilder? secondLocal = null;
        Optimizer.Optimizer optimizer = CreateOptimizer(generator =>
        {
            firstLocal = generator.DeclareLocal(typeof(string));
            secondLocal = generator.DeclareLocal(typeof(string));
            return
            [
                new CodeInstruction(OpCodes.Ldstr, "first"),
                new CodeInstruction(OpCodes.Stloc, firstLocal),
                new CodeInstruction(OpCodes.Ldstr, "second"),
                new CodeInstruction(OpCodes.Stloc, secondLocal),
                new CodeInstruction(OpCodes.Ldloc, secondLocal),
                new CodeInstruction(OpCodes.Pop),
                new CodeInstruction(OpCodes.Ret),
            ];
        });
        optimizer.MakeBasicBlocks();
        new StackToVariableConversion(optimizer).Run();

        Op load = optimizer.BasicBlocks[0].ops[4];
        load.inputs[load.stackInputCount] = optimizer.LocalVariables[firstLocal!.LocalIndex];

        new VariableToStackConversion(optimizer).Run();

        Op loweredLoad = optimizer.BasicBlocks[0].ops[4];
        Assert.That(optimizer.Form, Is.EqualTo(Optimizer.Optimizer.IrForm.Stack));
        Assert.That(loweredLoad.Opcode, Is.EqualTo(OpCodes.Ldloc));
        Assert.That(loweredLoad.Index, Is.EqualTo(firstLocal.LocalIndex));
        Assert.That(loweredLoad.Index, Is.Not.EqualTo(secondLocal!.LocalIndex));
    }

    [Test]
    public void ConvertVariablesToStackSpillsOnlyWhenCanonicalInputsRequireReordering()
    {
        MethodInfo concat = typeof(string).GetMethod(nameof(string.Concat), [typeof(string), typeof(string)])!;
        Optimizer.Optimizer optimizer = CreateOptimizer(_ =>
        [
            new CodeInstruction(OpCodes.Ldstr, "first"),
            new CodeInstruction(OpCodes.Ldstr, "second"),
            new CodeInstruction(OpCodes.Call, concat),
            new CodeInstruction(OpCodes.Pop),
            new CodeInstruction(OpCodes.Ret),
        ]);
        optimizer.MakeBasicBlocks();
        new StackToVariableConversion(optimizer).Run();

        Op call = optimizer.BasicBlocks[0].ops[2];
        (call.inputs[0], call.inputs[1]) = (call.inputs[1], call.inputs[0]);

        new VariableToStackConversion(optimizer).Run();

        Op[] operations = [.. optimizer.BasicBlocks[0].ops];
        Assert.That(operations.Select(op => op.Opcode), Is.EqualTo(new[]
        {
            OpCodes.Ldstr,
            OpCodes.Ldstr,
            OpCodes.Stloc,
            OpCodes.Stloc,
            OpCodes.Ldloc,
            OpCodes.Ldloc,
            OpCodes.Call,
            OpCodes.Pop,
            OpCodes.Ret,
        }));
        Assert.That(operations[4].Operand, Is.SameAs(operations[2].Operand));
        Assert.That(operations[5].Operand, Is.SameAs(operations[3].Operand));
    }

    [Test]
    public void ConvertVariablesToStackCanSpillOpcodeTypedIntegerTemporaries()
    {
        Optimizer.Optimizer optimizer = CreateOptimizer(_ =>
        [
            new CodeInstruction(OpCodes.Ldc_I4_1),
            new CodeInstruction(OpCodes.Ldc_I4_2),
            new CodeInstruction(OpCodes.Sub),
            new CodeInstruction(OpCodes.Pop),
            new CodeInstruction(OpCodes.Ret),
        ]);
        optimizer.MakeBasicBlocks();
        new StackToVariableConversion(optimizer).Run();

        Op subtract = optimizer.BasicBlocks[0].ops[2];
        Assert.That(subtract.inputs.Select(variable => variable.type), Is.All.EqualTo(typeof(int)));
        Assert.That(subtract.outputs.Single().type, Is.EqualTo(typeof(int)));
        (subtract.inputs[0], subtract.inputs[1]) = (subtract.inputs[1], subtract.inputs[0]);

        new VariableToStackConversion(optimizer).Run();

        LocalBuilder[] spills =
        [
            .. optimizer.BasicBlocks[0].ops
                .Where(op => op.Opcode == OpCodes.Stloc)
                .Select(op => (LocalBuilder)op.Operand!),
        ];
        Assert.That(spills, Has.Length.EqualTo(2));
        Assert.That(spills.Select(local => local.LocalType), Is.All.EqualTo(typeof(int)));
    }

    [Test]
    public void ConvertStackToVariablesInfersIsinstResultTypeFromOperand()
    {
        Optimizer.Optimizer optimizer = CreateOptimizer(_ =>
        [
            new CodeInstruction(OpCodes.Ldstr, "value"),
            new CodeInstruction(OpCodes.Isinst, typeof(string)),
            new CodeInstruction(OpCodes.Pop),
            new CodeInstruction(OpCodes.Ret),
        ]);
        optimizer.MakeBasicBlocks();

        new StackToVariableConversion(optimizer).Run();

        Assert.That(optimizer.BasicBlocks[0].ops[1].outputs.Single().type, Is.EqualTo(typeof(string)));
    }

    [Test]
    public void ConvertStackToVariablesTracksLdnullAsTheTransientNullType()
    {
        Optimizer.Optimizer optimizer = CreateOptimizer(_ =>
        [
            new CodeInstruction(OpCodes.Ldnull),
            new CodeInstruction(OpCodes.Pop),
            new CodeInstruction(OpCodes.Ret),
        ]);
        optimizer.MakeBasicBlocks();

        new StackToVariableConversion(optimizer).Run();

        Assert.That(optimizer.BasicBlocks[0].ops[0].outputs.Single().type,
            Is.EqualTo(typeof(Optimizer.Optimizer.NullType)));
    }

    [Test]
    public void ConvertVariablesToStackPreservesAValueUsedAfterItsStackCopyIsConsumed_WithoutDup()
    {
        MethodInfo concat = typeof(string).GetMethod(nameof(string.Concat), [typeof(string), typeof(string)])!;
        Optimizer.Optimizer optimizer = CreateOptimizer(_ =>
        [
            new CodeInstruction(OpCodes.Ldstr, "unused"),
            new CodeInstruction(OpCodes.Ldstr, "reused"),
            new CodeInstruction(OpCodes.Ldstr, "suffix"),
            new CodeInstruction(OpCodes.Call, concat),
            new CodeInstruction(OpCodes.Pop),
            new CodeInstruction(OpCodes.Pop),
            new CodeInstruction(OpCodes.Ret),
        ]);
        optimizer.MakeBasicBlocks();
        new StackToVariableConversion(optimizer).Run();

        BasicBlock block = optimizer.BasicBlocks[0];
        block.ops[5].inputs[0] = block.ops[1].outputs[0];

        new VariableToStackConversion(optimizer).Run();

        Assert.That(block.ops.Select(op => op.Opcode), Is.EqualTo(new[]
        {
            OpCodes.Ldstr,
            OpCodes.Ldstr,
            OpCodes.Ldstr,
            OpCodes.Stloc,
            OpCodes.Stloc,
            OpCodes.Ldloc,
            OpCodes.Ldloc,
            OpCodes.Call,
            OpCodes.Pop,
            OpCodes.Pop,
            OpCodes.Ldloc,
            OpCodes.Pop,
            OpCodes.Ret,
        }));
        Assert.That(block.ops[5].Operand, Is.SameAs(block.ops[4].Operand));
        Assert.That(block.ops[6].Operand, Is.SameAs(block.ops[3].Operand));
        Assert.That(block.ops[10].Operand, Is.SameAs(block.ops[4].Operand));
    }

    [Test]
    public void ConvertVariablesToStackPreservesAValueUsedAfterItsStackCopyIsConsumed_WithDup()
    {
        Optimizer.Optimizer optimizer = CreateOptimizer(_ =>
        [
            new CodeInstruction(OpCodes.Ldstr, "unused"),
            new CodeInstruction(OpCodes.Ldstr, "reused"),
            new CodeInstruction(OpCodes.Pop),
            new CodeInstruction(OpCodes.Pop),
            new CodeInstruction(OpCodes.Ret),
        ]);
        optimizer.MakeBasicBlocks();
        new StackToVariableConversion(optimizer).Run();

        BasicBlock block = optimizer.BasicBlocks[0];
        block.ops[3].inputs[0] = block.ops[1].outputs[0];

        new VariableToStackConversion(optimizer).Run();

        Assert.That(block.ops.Select(op => op.Opcode), Is.EqualTo(new[]
        {
            OpCodes.Ldstr,
            OpCodes.Ldstr,
            OpCodes.Dup,
            OpCodes.Pop,
            OpCodes.Pop,
            OpCodes.Pop,
            OpCodes.Ret,
        }));
        Assert.That(block.ops, Has.None.Matches<Op>(op =>
            op.Opcode == OpCodes.Ldloc || op.Opcode == OpCodes.Stloc));
    }

    [Test]
    public void ConvertVariablesToStackRematerializesNullInsteadOfSpillingIt()
    {
        Optimizer.Optimizer optimizer = CreateOptimizer(_ =>
        [
            new CodeInstruction(OpCodes.Ldnull),
            new CodeInstruction(OpCodes.Ldstr, "discarded"),
            new CodeInstruction(OpCodes.Pop),
            new CodeInstruction(OpCodes.Pop),
            new CodeInstruction(OpCodes.Ret),
        ]);
        optimizer.MakeBasicBlocks();
        new StackToVariableConversion(optimizer).Run();

        BasicBlock block = optimizer.BasicBlocks[0];
        Variable nullValue = block.ops[0].outputs.Single();
        block.ops[2].inputs[0] = nullValue;
        block.ops[3].inputs[0] = nullValue;

        new VariableToStackConversion(optimizer).Run();

        Assert.That(block.ops.Count(op => op.Opcode == OpCodes.Ldnull), Is.EqualTo(2));
        Assert.That(block.ops, Has.None.Matches<Op>(op =>
            op.Opcode == OpCodes.Ldloc || op.Opcode == OpCodes.Stloc));
    }

    [Test]
    public void ConvertVariablesToStackRejectsSsaEdgeAssignments()
    {
        Optimizer.Optimizer optimizer = CreateOptimizer(generator =>
        {
            Label target = generator.DefineLabel();
            return
            [
                new CodeInstruction(OpCodes.Ldc_I4_1),
                new CodeInstruction(OpCodes.Br, target),
                new CodeInstruction(OpCodes.Pop).WithLabels(target),
                new CodeInstruction(OpCodes.Ret),
            ];
        });
        optimizer.MakeBasicBlocks();
        new StackToVariableConversion(optimizer).Run();

        ControlFlowEdge edge = optimizer.BasicBlocks[0].outgoingEdges.Single();
        Variable stackSlot = edge.Target.entryStackVariables.Single();
        edge.assignments.Add(new VariableAssignment(stackSlot, stackSlot));

        Assert.That(
            () => new VariableToStackConversion(optimizer).Run(),
            Throws.InvalidOperationException.With.Message.Contains("still has SSA assignments"));
    }

    [Test]
    public void EmitRejectsCanonicalVariableForm()
    {
        Optimizer.Optimizer optimizer = CreateOptimizer(_ => [new CodeInstruction(OpCodes.Ret)]);
        optimizer.MakeBasicBlocks();
        new StackToVariableConversion(optimizer).Run();

        Assert.That(
            () => optimizer.Emit(),
            Throws.InvalidOperationException.With.Message.Contains("convert it to stack form"));
    }

    [Test]
    public void BranchInversionMakesSinglePredecessorTargetFallThrough()
    {
        Optimizer.Optimizer optimizer = CreateOptimizer(generator =>
        {
            Label shared = generator.DefineLabel();
            Label unique = generator.DefineLabel();
            return
            [
                new CodeInstruction(OpCodes.Ldc_I4_0),
                new CodeInstruction(OpCodes.Brfalse, unique),
                new CodeInstruction(OpCodes.Ret).WithLabels(shared),
                new CodeInstruction(OpCodes.Br, shared).WithLabels(unique),
            ];
        });
        optimizer.MakeBasicBlocks();
        BasicBlock condition = optimizer.BasicBlocks[0];
        BasicBlock shared = optimizer.BasicBlocks[1];
        BasicBlock unique = optimizer.BasicBlocks[2];

        optimizer.BranchInversion();

        Assert.That(condition.ops[^1].Opcode, Is.EqualTo(OpCodes.Brtrue_S));
        Assert.That(condition.ops[^1].Operand, Is.TypeOf<ControlFlowEdge>());
        Assert.That(((ControlFlowEdge)condition.ops[^1].Operand!).Target, Is.SameAs(shared));
        Assert.That(condition.Next, Is.SameAs(unique));
    }

    [Test]
    public void InsertBranchesRestoresNonAdjacentFallThroughBranch()
    {
        Optimizer.Optimizer optimizer = CreateOptimizer(generator =>
        {
            Label target = generator.DefineLabel();
            return
            [
                new CodeInstruction(OpCodes.Br, target),
                new CodeInstruction(OpCodes.Ret),
                new CodeInstruction(OpCodes.Ret).WithLabels(target),
            ];
        });
        optimizer.MakeBasicBlocks();
        BasicBlock entry = optimizer.BasicBlocks[0];
        BasicBlock target = optimizer.BasicBlocks[2];
        ControlFlowEdge fallthroughEdge = entry.fallthroughEdge!;

        optimizer.InsertBranches();

        Assert.That(entry.Next, Is.Null);
        Assert.That(entry.ops, Has.Count.EqualTo(1));
        Assert.That(entry.ops[0].Opcode, Is.EqualTo(OpCodes.Br_S));
        Assert.That(entry.ops[0].Operand, Is.SameAs(fallthroughEdge));
        Assert.That(entry.ops[0].Operand, Is.TypeOf<ControlFlowEdge>());
        Assert.That(((ControlFlowEdge)entry.ops[0].Operand!).Target, Is.SameAs(target));
    }

    [Test]
    public void EmitConvertsBlockTargetsBackToLabels()
    {
        Optimizer.Optimizer optimizer = CreateOptimizer(generator =>
        {
            Label target = generator.DefineLabel();
            return
            [
                new CodeInstruction(OpCodes.Br, target),
                new CodeInstruction(OpCodes.Ret),
                new CodeInstruction(OpCodes.Ret).WithLabels(target),
            ];
        });
        optimizer.MakeBasicBlocks();
        optimizer.InsertBranches();
        BasicBlock target = optimizer.BasicBlocks[2];

        optimizer.Emit();

        CodeInstruction branch = optimizer.outputInstructions.instructions[0];
        Assert.That(branch.opcode, Is.EqualTo(OpCodes.Br_S));
        Assert.That(branch.operand, Is.TypeOf<Label>());
        Assert.That(branch.operand, Is.EqualTo(target.label));
        CodeInstruction emittedTarget = optimizer.outputInstructions.instructions.Single(instruction =>
            instruction.labels.Contains((Label)branch.operand));
        Assert.That(emittedTarget.opcode, Is.EqualTo(OpCodes.Ret));
    }

    private static Optimizer.Optimizer CreateOptimizer(Func<ILGenerator, List<CodeInstruction>> createInstructions)
        => CreateOptimizer(TargetMethod, createInstructions);

    private static Optimizer.Optimizer CreateTwoBlockTryOptimizer() => CreateOptimizer(generator =>
    {
        Label secondTryBlock = generator.DefineLabel();
        Label exit = generator.DefineLabel();
        var tryStart = new CodeInstruction(OpCodes.Nop);
        tryStart.blocks.Add(new ExceptionBlock(ExceptionBlockType.BeginExceptionBlock));
        var catchStart = new CodeInstruction(OpCodes.Pop);
        catchStart.blocks.Add(new ExceptionBlock(ExceptionBlockType.BeginCatchBlock, typeof(Exception)));
        var catchLeave = new CodeInstruction(OpCodes.Leave, exit);
        catchLeave.blocks.Add(new ExceptionBlock(ExceptionBlockType.EndExceptionBlock));
        return
        [
            tryStart,
            new CodeInstruction(OpCodes.Br, secondTryBlock),
            new CodeInstruction(OpCodes.Leave, exit).WithLabels(secondTryBlock),
            catchStart,
            catchLeave,
            new CodeInstruction(OpCodes.Ret).WithLabels(exit),
        ];
    });

    private static Optimizer.Optimizer CreateOptimizer(
        MethodBase targetMethod,
        Func<ILGenerator, List<CodeInstruction>> createInstructions)
    {
        var dynamicMethod = new DynamicMethod("OptimizerPassTest", typeof(void), Type.EmptyTypes);
        ILGenerator generator = dynamicMethod.GetILGenerator();
        return new Optimizer.Optimizer(targetMethod, createInstructions(generator), generator, debug: false);
    }

    private static void ConsumeReference(ref int value) { }

    private static OpCode[] OpCodesIn(BasicBlock block) => [.. block.ops.Select(op => op.Opcode)];
}
