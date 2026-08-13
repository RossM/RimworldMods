using System.Reflection.Emit;
using Disharmony.Optimizer;
using HarmonyLib;

namespace Disharmony.Tests;

[TestFixture]
public sealed class OptimizerMergeStackSlotsTests
{
    private static readonly MethodInfo VoidMethod =
        typeof(ControlFlowGraphTargets).GetMethod(nameof(ControlFlowGraphTargets.ReturnVoid))!;

    private static readonly ILInstruction Ret = new(OpCodes.Ret, null!, []);

    [Test]
    public void MergeStackSlots_NoEdgeAssignments_PreservesTheGraph()
    {
        ControlFlowGraph graph = new();
        BasicBlock block = new(graph.RootRegion.EntryLabel, [], graph.RootRegion,
            new Return(Ret, new VoidOp()));
        graph.AddBlock(block);
        Disharmony.Optimizer.Optimizer optimizer = new(VoidMethod, [], PatchProcessor.CreateILGenerator(), false)
        {
            cfg = graph
        };

        optimizer.MergeStackSlots();

        Assert.That(graph.GetBlock(block.Label), Is.SameAs(block));
    }

    [Test]
    public void MergeStackSlots_OneEdge_RewritesTheTargetAndRemovesTheCopy()
    {
        ControlFlowGraph graph = new();
        BlockLabel destination = new();
        StackSlot sourceSlot = new(0, typeof(int), 0);
        StackSlot targetSlot = new(0, typeof(int), 1);
        BasicBlock source = new(graph.RootRegion.EntryLabel, [], graph.RootRegion,
            new UnconditionalBranch(destination));
        BasicBlock target = new(destination, [], graph.RootRegion, new Return(Ret, targetSlot));
        graph.AddBlock(source);
        graph.AddBlock(target);
        graph.AddEdge(new Edge(source.Label, target.Label, [new AssignmentOp(targetSlot, sourceSlot)]));
        Disharmony.Optimizer.Optimizer optimizer = new(VoidMethod, [], PatchProcessor.CreateILGenerator(), false)
        {
            cfg = graph
        };

        optimizer.MergeStackSlots();

        Assert.Multiple(() =>
        {
            Assert.That(((Return)graph.GetBlock(target.Label).Branch).Value, Is.SameAs(sourceSlot));
            Assert.That(graph.GetEdge(source.Label, target.Label).EdgeAssignments, Is.Empty);
        });
    }

    [Test]
    public void MergeStackSlots_ChainOfEdges_RewritesEverySlotToTheOriginalSource()
    {
        ControlFlowGraph graph = new();
        BlockLabel middleLabel = new();
        BlockLabel targetLabel = new();
        StackSlot sourceSlot = new(0, typeof(int), 0);
        StackSlot middleSlot = new(0, typeof(int), 1);
        StackSlot targetSlot = new(0, typeof(int), 2);
        BasicBlock source = new(graph.RootRegion.EntryLabel, [], graph.RootRegion,
            new UnconditionalBranch(middleLabel));
        BasicBlock middle = new(middleLabel, [], graph.RootRegion, new UnconditionalBranch(targetLabel));
        BasicBlock target = new(targetLabel, [], graph.RootRegion, new Return(Ret, targetSlot));
        graph.AddBlock(source);
        graph.AddBlock(middle);
        graph.AddBlock(target);
        graph.AddEdge(new Edge(source.Label, middle.Label, [new AssignmentOp(middleSlot, sourceSlot)]));
        graph.AddEdge(new Edge(middle.Label, target.Label, [new AssignmentOp(targetSlot, middleSlot)]));
        Disharmony.Optimizer.Optimizer optimizer = new(VoidMethod, [], PatchProcessor.CreateILGenerator(), false)
        {
            cfg = graph
        };

        optimizer.MergeStackSlots();

        Assert.Multiple(() =>
        {
            Assert.That(((Return)graph.GetBlock(target.Label).Branch).Value, Is.SameAs(sourceSlot));
            Assert.That(graph.GetEdge(source.Label, middle.Label).EdgeAssignments, Is.Empty);
            Assert.That(graph.GetEdge(middle.Label, target.Label).EdgeAssignments, Is.Empty);
        });
    }

    [Test]
    public void MergeStackSlots_RewritesSlotsUsedByILOperations()
    {
        ControlFlowGraph graph = new();
        BlockLabel destination = new();
        StackSlot sourceSlot = new(0, typeof(int), 0);
        StackSlot targetSlot = new(0, typeof(int), 1);
        ILOp pop = new(new ILInstruction(OpCodes.Pop, null!, []), [targetSlot], typeof(void));
        BasicBlock source = new(graph.RootRegion.EntryLabel, [], graph.RootRegion,
            new UnconditionalBranch(destination));
        BasicBlock target = new(destination, [pop], graph.RootRegion, new Return(Ret, new VoidOp()));
        graph.AddBlock(source);
        graph.AddBlock(target);
        graph.AddEdge(new Edge(source.Label, target.Label, [new AssignmentOp(targetSlot, sourceSlot)]));
        Disharmony.Optimizer.Optimizer optimizer = new(VoidMethod, [], PatchProcessor.CreateILGenerator(), false)
        {
            cfg = graph
        };

        optimizer.MergeStackSlots();

        var rewritten = (ILOp)graph.GetBlock(target.Label).Ops.Single();
        Assert.That(rewritten.Inputs.Single(), Is.SameAs(sourceSlot));
    }

    [Test]
    public void MergeStackSlots_RewritesSlotsUsedByConditionalBranches()
    {
        ControlFlowGraph graph = new();
        BlockLabel conditionLabel = new();
        BlockLabel fallthroughLabel = new();
        BlockLabel takenLabel = new();
        StackSlot sourceSlot = new(0, typeof(int), 0);
        StackSlot conditionSlot = new(0, typeof(int), 1);
        BasicBlock source = new(graph.RootRegion.EntryLabel, [], graph.RootRegion,
            new UnconditionalBranch(conditionLabel));
        BasicBlock condition = new(conditionLabel, [], graph.RootRegion,
            new ConditionalBranch(OpCodes.Brtrue, [conditionSlot], [fallthroughLabel, takenLabel]));
        BasicBlock fallthrough = new(fallthroughLabel, [], graph.RootRegion, new Return(Ret, new VoidOp()));
        BasicBlock taken = new(takenLabel, [], graph.RootRegion, new Return(Ret, new VoidOp()));
        graph.AddBlock(source);
        graph.AddBlock(condition);
        graph.AddBlock(fallthrough);
        graph.AddBlock(taken);
        graph.AddEdge(new Edge(source.Label, condition.Label, [new AssignmentOp(conditionSlot, sourceSlot)]));
        graph.AddEdge(new Edge(condition.Label, fallthrough.Label, []));
        graph.AddEdge(new Edge(condition.Label, taken.Label, []));
        Disharmony.Optimizer.Optimizer optimizer = new(VoidMethod, [], PatchProcessor.CreateILGenerator(), false)
        {
            cfg = graph
        };

        optimizer.MergeStackSlots();

        var rewritten = (ConditionalBranch)graph.GetBlock(condition.Label).Branch;
        Assert.That(rewritten.Inputs.Single(), Is.SameAs(sourceSlot));
    }

    [Test]
    public void MergeStackSlots_DisconnectedSets_RemainDistinct()
    {
        ControlFlowGraph graph = new();
        BlockLabel secondLabel = new();
        BlockLabel thirdLabel = new();
        StackSlot firstSource = new(0, typeof(int), 0);
        StackSlot firstTarget = new(0, typeof(int), 1);
        StackSlot secondSource = new(1, typeof(int), 2);
        StackSlot secondTarget = new(1, typeof(int), 3);
        BasicBlock first = new(graph.RootRegion.EntryLabel, [], graph.RootRegion,
            new UnconditionalBranch(secondLabel));
        BasicBlock second = new(secondLabel, [], graph.RootRegion, new UnconditionalBranch(thirdLabel));
        ILOp add = new(new ILInstruction(OpCodes.Add, null!, []), [firstTarget, secondTarget], typeof(int));
        BasicBlock third = new(thirdLabel, [add], graph.RootRegion, new Return(Ret, new VoidOp()));
        graph.AddBlock(first);
        graph.AddBlock(second);
        graph.AddBlock(third);
        graph.AddEdge(new Edge(first.Label, second.Label, [new AssignmentOp(firstTarget, firstSource)]));
        graph.AddEdge(new Edge(second.Label, third.Label, [new AssignmentOp(secondTarget, secondSource)]));
        Disharmony.Optimizer.Optimizer optimizer = new(VoidMethod, [], PatchProcessor.CreateILGenerator(), false)
        {
            cfg = graph
        };

        optimizer.MergeStackSlots();

        var rewritten = (ILOp)graph.GetBlock(third.Label).Ops.Single();
        Assert.Multiple(() =>
        {
            Assert.That(rewritten.Inputs[0], Is.SameAs(firstSource));
            Assert.That(rewritten.Inputs[1], Is.SameAs(secondSource));
            Assert.That(rewritten.Inputs[0], Is.Not.SameAs(rewritten.Inputs[1]));
        });
    }
}
