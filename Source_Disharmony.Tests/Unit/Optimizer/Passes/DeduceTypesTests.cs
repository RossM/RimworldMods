using Disharmony.Optimizer.Passes;

namespace Disharmony.Tests.Unit.Optimizer.Passes;

[TestFixture]
[Timeout(1000)]
public sealed class DeduceTypesTests
{
    private static readonly MethodInfo ReturnIntMethod =
        typeof(ControlFlowGraphTargets).GetMethod(nameof(ControlFlowGraphTargets.ReturnInt))!;

    private static readonly ILInstruction Ret = new(OpCodes.Ret, null!, []);

    [Test]
    public void Constant_AssignmentAndReturnAreRewrittenWithDeducedType()
    {
        RootRegion root = new(new BlockLabel());
        StackSlot result = new(0, TypeLattice.Unknown, 0);
        ILOp constant = new(new ILInstruction(OpCodes.Ldc_I4_1, null!, []), [], TypeLattice.Unknown);
        BasicBlock block = new(root.EntryLabel, [new AssignmentOp(result, constant)], root,
            new Return(Ret, result));
        ControlFlowGraph graph = new(root, [block], []);
        global::Disharmony.Optimizer.Optimizer optimizer = new(
            ReturnIntMethod, [], PatchProcessor.CreateILGenerator(), false)
        {
            cfg = graph
        };

        new DeduceTypes(optimizer).RunInternal();

        BasicBlock rewrittenBlock = optimizer.cfg.GetBlock(block.Label);
        AssignmentOp rewrittenAssignment = rewrittenBlock.Ops.OfType<AssignmentOp>().Single();
        Assert.Multiple(() =>
        {
            Assert.That(rewrittenAssignment.Input.Type, Is.EqualTo(typeof(int)));
            Assert.That(rewrittenAssignment.Output.Type, Is.EqualTo(typeof(int)));
            Assert.That(((Return)rewrittenBlock.Branch).Value, Is.EqualTo(rewrittenAssignment.Output));
        });
    }

    [Test]
    public void ArgumentLoad_UsesDeclaredArgumentType()
    {
        RootRegion root = new(new BlockLabel());
        StackSlot result = new(0, TypeLattice.Unknown, 0);
        ILOp load = new(new ILInstruction(OpCodes.Ldarg_0, null!, []), [], TypeLattice.Unknown);
        BasicBlock block = new(root.EntryLabel, [new AssignmentOp(result, load)], root,
            new Return(Ret, result));
        ControlFlowGraph graph = new(root, [block], []);
        global::Disharmony.Optimizer.Optimizer optimizer = new(
            ReturnIntMethod, [], PatchProcessor.CreateILGenerator(), false)
        {
            cfg = graph
        };
        optimizer.arguments[0] = new Argument(0, typeof(int));

        new DeduceTypes(optimizer).RunInternal();

        AssignmentOp rewrittenAssignment = optimizer.cfg.GetBlock(block.Label).Ops.OfType<AssignmentOp>().Single();
        Assert.Multiple(() =>
        {
            Assert.That(rewrittenAssignment.Input.Type, Is.EqualTo(typeof(int)));
            Assert.That(rewrittenAssignment.Output.Type, Is.EqualTo(typeof(int)));
        });
    }

    [Test]
    public void LocalLoad_NormalizesDeclaredLocalType()
    {
        RootRegion root = new(new BlockLabel());
        StackSlot result = new(0, TypeLattice.Unknown, 0);
        ILOp load = new(new ILInstruction(OpCodes.Ldloc_0, null!, []), [], TypeLattice.Unknown);
        BasicBlock block = new(root.EntryLabel, [new AssignmentOp(result, load)], root,
            new Return(Ret, result));
        ControlFlowGraph graph = new(root, [block], []);
        global::Disharmony.Optimizer.Optimizer optimizer = new(
            ReturnIntMethod, [], PatchProcessor.CreateILGenerator(), false)
        {
            cfg = graph
        };
        optimizer.locals[0] = new Local(0, typeof(byte), null);

        new DeduceTypes(optimizer).RunInternal();

        AssignmentOp rewrittenAssignment = optimizer.cfg.GetBlock(block.Label).Ops.OfType<AssignmentOp>().Single();
        Assert.Multiple(() =>
        {
            Assert.That(rewrittenAssignment.Input.Type, Is.EqualTo(typeof(int)));
            Assert.That(rewrittenAssignment.Output.Type, Is.EqualTo(typeof(int)));
        });
    }

    [Test]
    public void IncomingEdges_MergeReferenceTypesAndRewriteEveryUse()
    {
        RootRegion root = new(new BlockLabel());
        BlockLabel secondLabel = new();
        BlockLabel targetLabel = new();
        StackSlot stringValue = new(0, typeof(string), 0);
        StackSlot classValue = new(0, typeof(OpcodeUtilitiesClass), 1);
        StackSlot mergedValue = new(0, TypeLattice.Unknown, 2);
        BasicBlock first = new(root.EntryLabel, [], root, new UnconditionalBranch(targetLabel));
        BasicBlock second = new(secondLabel, [], root, new UnconditionalBranch(targetLabel));
        BasicBlock target = new(targetLabel, [], root, new Return(Ret, mergedValue));
        ControlFlowGraph graph = new(root, [first, second, target],
        [
            new Edge(first.Label, target.Label, [new AssignmentOp(mergedValue, stringValue)]),
            new Edge(second.Label, target.Label, [new AssignmentOp(mergedValue, classValue)]),
        ]);
        global::Disharmony.Optimizer.Optimizer optimizer = new(
            ReturnIntMethod, [], PatchProcessor.CreateILGenerator(), false)
        {
            cfg = graph
        };

        new DeduceTypes(optimizer).RunInternal();

        ControlFlowGraph rewritten = optimizer.cfg;
        Op rewrittenReturnValue = ((Return)rewritten.GetBlock(target.Label).Branch).Value;
        AssignmentOp[] rewrittenAssignments = rewritten.IncomingEdges(target.Label)
            .SelectMany(edge => edge.EdgeAssignments).ToArray();
        Assert.Multiple(() =>
        {
            Assert.That(rewrittenReturnValue.Type, Is.EqualTo(typeof(object)));
            Assert.That(rewrittenAssignments.Select(assignment => assignment.Output.Type),
                Is.All.EqualTo(typeof(object)));
            Assert.That(rewrittenAssignments.Select(assignment => assignment.Output),
                Is.All.SameAs(rewrittenReturnValue));
        });
    }

    [Test]
    public void EdgeThenArithmetic_ReachesFixedPointBeforeRewritingGraph()
    {
        RootRegion root = new(new BlockLabel());
        BlockLabel targetLabel = new();
        StackSlot sourceValue = new(0, TypeLattice.Unknown, 0);
        StackSlot targetValue = new(0, TypeLattice.Unknown, 1);
        StackSlot result = new(0, TypeLattice.Unknown, 2);
        ILOp constant = new(new ILInstruction(OpCodes.Ldc_I4_1, null!, []), [], TypeLattice.Unknown);
        ILOp negate = new(new ILInstruction(OpCodes.Neg, null!, []), [targetValue], TypeLattice.Unknown);
        BasicBlock source = new(root.EntryLabel, [new AssignmentOp(sourceValue, constant)], root,
            new UnconditionalBranch(targetLabel));
        BasicBlock target = new(targetLabel, [new AssignmentOp(result, negate)], root,
            new Return(Ret, result));
        ControlFlowGraph graph = new(root, [source, target],
            [new Edge(source.Label, target.Label, [new AssignmentOp(targetValue, sourceValue)])]);
        global::Disharmony.Optimizer.Optimizer optimizer = new(
            ReturnIntMethod, [], PatchProcessor.CreateILGenerator(), false)
        {
            cfg = graph
        };

        new DeduceTypes(optimizer).RunInternal();

        BasicBlock rewrittenTarget = optimizer.cfg.GetBlock(target.Label);
        AssignmentOp rewrittenAssignment = rewrittenTarget.Ops.OfType<AssignmentOp>().Single();
        Assert.Multiple(() =>
        {
            Assert.That(((ILOp)rewrittenAssignment.Input).Inputs.Single().Type, Is.EqualTo(typeof(int)));
            Assert.That(rewrittenAssignment.Input.Type, Is.EqualTo(typeof(int)));
            Assert.That(rewrittenAssignment.Output.Type, Is.EqualTo(typeof(int)));
            Assert.That(((Return)rewrittenTarget.Branch).Value.Type, Is.EqualTo(typeof(int)));
        });
    }
}
