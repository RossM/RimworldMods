namespace Disharmony.Tests.Unit.Optimizer;

[TestFixture]
public sealed class RewriteVisitorTests
{
    private static readonly ILInstruction Nop = new(OpCodes.Nop, null!, []);
    private static readonly ILInstruction Ret = new(OpCodes.Ret, null!, []);

    private sealed class RootRegionReplacingVisitor(RootRegion original, RootRegion replacement) : RewriteVisitor
    {
        public override Node Visit(RootRegion region) => ReferenceEquals(region, original) ? replacement : region;
    }

    [Test]
    public void ReplaceVisitor_LeafOp_ReturnsConfiguredReplacement()
    {
        StackSlot original = new(0, typeof(int), 0);
        StackSlot replacement = new(0, typeof(int), 1);
        ReplaceVisitor visitor = new();
        visitor.Replacements[original] = replacement;

        Assert.Multiple(() =>
        {
            Assert.That(original.Accept(visitor), Is.SameAs(replacement));
            Assert.That(replacement.Accept(visitor), Is.SameAs(replacement));
        });
    }

    [Test]
    public void Assignment_RewritesItsInputAndOutput()
    {
        StackSlot originalInput = new(0, typeof(int), 0);
        StackSlot replacementInput = new(0, typeof(int), 1);
        Temporary originalOutput = new(typeof(int), 0);
        Temporary replacementOutput = new(typeof(int), 1);
        AssignmentOp assignment = new(originalOutput, originalInput);
        ReplaceVisitor visitor = new();
        visitor.Replacements[originalInput] = replacementInput;
        visitor.Replacements[originalOutput] = replacementOutput;

        var rewritten = (AssignmentOp)assignment.Accept(visitor);

        Assert.Multiple(() =>
        {
            Assert.That(rewritten, Is.Not.SameAs(assignment));
            Assert.That(rewritten.Input, Is.SameAs(replacementInput));
            Assert.That(rewritten.Output, Is.SameAs(replacementOutput));
        });
    }

    [Test]
    public void ILOp_RewritesEachInput()
    {
        StackSlot original = new(0, typeof(int), 0);
        StackSlot replacement = new(0, typeof(int), 1);
        ILOp operation = new(new ILInstruction(OpCodes.Add, null!, []), [original, original], typeof(int));
        ReplaceVisitor visitor = new();
        visitor.Replacements[original] = replacement;

        var rewritten = (ILOp)operation.Accept(visitor);

        Assert.Multiple(() =>
        {
            Assert.That(rewritten, Is.Not.SameAs(operation));
            Assert.That(rewritten.Inputs, Is.EqualTo(new[] { replacement, replacement }));
            Assert.That(rewritten.IL, Is.SameAs(operation.IL));
            Assert.That(rewritten.Type, Is.EqualTo(typeof(int)));
        });
    }

    [Test]
    public void TerminalBranches_RewriteTheirValues()
    {
        StackSlot original = new(0, typeof(int), 0);
        StackSlot replacement = new(0, typeof(int), 1);
        ReplaceVisitor visitor = new();
        visitor.Replacements[original] = replacement;

        var rewrittenThrow = (Throw)new Throw(original).Accept(visitor);
        var rewrittenReturn = (Return)new Return(Ret, original).Accept(visitor);
        var rewrittenJump = (Jump)new Jump(original).Accept(visitor);

        Assert.Multiple(() =>
        {
            Assert.That(rewrittenThrow.Exception, Is.SameAs(replacement));
            Assert.That(rewrittenReturn.Value, Is.SameAs(replacement));
            Assert.That(rewrittenJump.Value, Is.SameAs(replacement));
        });
    }

    [Test]
    public void ConditionalBranch_RewritesEachInput()
    {
        StackSlot original = new(0, typeof(int), 0);
        StackSlot replacement = new(0, typeof(int), 1);
        ConditionalBranch branch = new(OpCodes.Beq, [original, original], [new BlockLabel(), new BlockLabel()]);
        ReplaceVisitor visitor = new();
        visitor.Replacements[original] = replacement;

        var rewritten = (ConditionalBranch)branch.Accept(visitor);

        Assert.Multiple(() =>
        {
            Assert.That(rewritten, Is.Not.SameAs(branch));
            Assert.That(rewritten.Inputs, Is.EqualTo(new[] { replacement, replacement }));
            Assert.That(rewritten.Labels, Is.SameAs(branch.Labels));
        });
    }

    [Test]
    public void BasicBlock_RewritesOpsAndBranch()
    {
        RootRegion region = new(new BlockLabel());
        StackSlot original = new(0, typeof(int), 0);
        StackSlot replacement = new(0, typeof(int), 1);
        AssignmentOp assignment = new(new Temporary(typeof(int), 0), original);
        BasicBlock block = new(region.EntryLabel, [assignment], region, new Return(Ret, original));
        ReplaceVisitor visitor = new();
        visitor.Replacements[original] = replacement;

        var rewritten = (BasicBlock)block.Accept(visitor);

        Assert.Multiple(() =>
        {
            Assert.That(rewritten, Is.Not.SameAs(block));
            Assert.That(((AssignmentOp)rewritten.Ops.Single()).Input, Is.SameAs(replacement));
            Assert.That(((Return)rewritten.Branch).Value, Is.SameAs(replacement));
            Assert.That(rewritten.Region, Is.SameAs(region));
        });
    }

    [Test]
    public void Edge_RewritesAssignmentsAndRemovesIdentityAssignments()
    {
        StackSlot original = new(0, typeof(int), 0);
        StackSlot replacement = new(0, typeof(int), 1);
        Temporary retainedOutput = new(typeof(int), 0);
        AssignmentOp becomesIdentity = new(replacement, original);
        AssignmentOp retained = new(retainedOutput, original);
        Edge edge = new(new BlockLabel(), new BlockLabel(), [becomesIdentity, retained]);
        ReplaceVisitor visitor = new();
        visitor.Replacements[original] = replacement;

        var rewritten = (Edge)edge.Accept(visitor);

        Assert.Multiple(() =>
        {
            Assert.That(rewritten, Is.Not.SameAs(edge));
            Assert.That(rewritten.EdgeAssignments, Has.Count.EqualTo(1));
            Assert.That(rewritten.EdgeAssignments[0].Output, Is.SameAs(retainedOutput));
            Assert.That(rewritten.EdgeAssignments[0].Input, Is.SameAs(replacement));
        });
    }

    [Test]
    public void ExceptionGroup_RewritesCatchIncomingException()
    {
        RootRegion root = new(new BlockLabel());
        StackSlot original = new(0, typeof(Exception), 0);
        StackSlot replacement = new(0, typeof(Exception), 1);
        CatchRegion catchRegion = new(new BlockLabel(), root, original);
        FinallyRegion finallyRegion = new(new BlockLabel(), root);
        FaultRegion faultRegion = new(new BlockLabel(), root);
        ExceptionGroup group = new([catchRegion, finallyRegion, faultRegion]);
        ReplaceVisitor visitor = new();
        visitor.Replacements[original] = replacement;

        var rewritten = (ExceptionGroup)group.Accept(visitor);

        Assert.Multiple(() =>
        {
            Assert.That(rewritten, Is.Not.SameAs(group));
            Assert.That(((CatchRegion)rewritten.HandlerRegions[0]).IncomingException, Is.SameAs(replacement));
            Assert.That(rewritten.HandlerRegions[1], Is.SameAs(finallyRegion));
            Assert.That(rewritten.HandlerRegions[2], Is.SameAs(faultRegion));
        });
    }

    [Test]
    public void ExceptionRegions_RewriteTheirParentRegion()
    {
        RootRegion originalParent = new(new BlockLabel());
        RootRegion replacementParent = new(new BlockLabel());
        ProtectedRegion protectedRegion = new(new BlockLabel(), originalParent, new ExceptionGroup([]));
        CatchRegion catchRegion = new(new BlockLabel(), originalParent,
            new StackSlot(0, typeof(Exception), 0));
        FinallyRegion finallyRegion = new(new BlockLabel(), originalParent);
        FaultRegion faultRegion = new(new BlockLabel(), originalParent);
        RootRegionReplacingVisitor visitor = new(originalParent, replacementParent);

        var rewrittenProtected = (ProtectedRegion)protectedRegion.Accept(visitor);
        var rewrittenCatch = (CatchRegion)catchRegion.Accept(visitor);
        var rewrittenFinally = (FinallyRegion)finallyRegion.Accept(visitor);
        var rewrittenFault = (FaultRegion)faultRegion.Accept(visitor);

        Assert.Multiple(() =>
        {
            Assert.That(rewrittenProtected.Parent, Is.SameAs(replacementParent));
            Assert.That(rewrittenCatch.Parent, Is.SameAs(replacementParent));
            Assert.That(rewrittenFinally.Parent, Is.SameAs(replacementParent));
            Assert.That(rewrittenFault.Parent, Is.SameAs(replacementParent));
        });
    }

    [Test]
    public void UnchangedCompositeNodes_PreserveTheirInstances()
    {
        RootRegion root = new(new BlockLabel());
        StackSlot value = new(0, typeof(int), 0);
        AssignmentOp assignment = new(new Temporary(typeof(int), 0), value);
        ILOp operation = new(Nop, [value], typeof(void));
        ConditionalBranch conditional = new(OpCodes.Brtrue, [value], [new BlockLabel(), new BlockLabel()]);
        BasicBlock block = new(root.EntryLabel, [assignment, operation], root, conditional);
        Edge edge = new(block.Label, conditional.Labels[0], [assignment]);
        RewriteVisitor visitor = new();

        Assert.Multiple(() =>
        {
            Assert.That(assignment.Accept(visitor), Is.SameAs(assignment));
            Assert.That(operation.Accept(visitor), Is.SameAs(operation));
            Assert.That(conditional.Accept(visitor), Is.SameAs(conditional));
            Assert.That(block.Accept(visitor), Is.SameAs(block));
            Assert.That(edge.Accept(visitor), Is.SameAs(edge));
            Assert.That(root.Accept(visitor), Is.SameAs(root));
            Assert.That(new UnconditionalBranch(new BlockLabel()).Accept(visitor),
                Is.TypeOf<UnconditionalBranch>());
            Assert.That(new Leave(new BlockLabel()).Accept(visitor), Is.TypeOf<Leave>());
            Assert.That(new Rethrow().Accept(visitor), Is.TypeOf<Rethrow>());
        });
    }

    [Test]
    public void ControlFlowGraph_ReplacesChangedBlocksEdgesAndExceptionGroups()
    {
        RootRegion root = new(new BlockLabel());
        BlockLabel destination = new();
        StackSlot original = new(0, typeof(Exception), 0);
        StackSlot replacement = new(0, typeof(Exception), 1);
        CatchRegion catchRegion = new(new BlockLabel(), root, original);
        ExceptionGroup group = new([catchRegion]);
        ProtectedRegion protectedRegion = new(root.EntryLabel, root, group);
        BasicBlock source = new(root.EntryLabel, [], protectedRegion,
            new UnconditionalBranch(destination));
        BasicBlock target = new(destination, [], root, new Return(Ret, original));
        Edge edge = new(source.Label, target.Label, [new AssignmentOp(replacement, original)]);
        ControlFlowGraph graph = new(root, [source, target], [edge], [], []);
        ReplaceVisitor visitor = new();
        visitor.Replacements[original] = replacement;

        var rewritten = (ControlFlowGraph)visitor.Visit(graph);

        Assert.Multiple(() =>
        {
            Assert.That(rewritten.GetBlock(source.Label), Is.Not.SameAs(source));
            Assert.That(rewritten.GetBlock(target.Label), Is.Not.SameAs(target));
            Assert.That(((Return)rewritten.GetBlock(target.Label).Branch).Value, Is.SameAs(replacement));
            Assert.That(rewritten.GetEdge(source.Label, target.Label).EdgeAssignments, Is.Empty);
            Assert.That(((CatchRegion)rewritten.ExceptionGroups.Single().HandlerRegions.Single()).IncomingException,
                Is.SameAs(replacement));
        });
    }

    [Test]
    public void ControlFlowGraph_RewritesArgumentsAndLocalsWithoutLosingUnchangedMetadata()
    {
        RootRegion root = new(new BlockLabel());
        BasicBlock block = new(root.EntryLabel, [], root, new Return(Ret, new VoidOp()));
        Argument originalArgument = new(0, typeof(int));
        Argument replacementArgument = new(0, typeof(long));
        Argument unchangedArgument = new(1, typeof(string));
        Local originalLocal = new(typeof(int), 0);
        Local replacementLocal = new(typeof(long), 0);
        Local unchangedLocal = new(typeof(string), 1);
        ControlFlowGraph graph = new(root, [block], [],
            [originalArgument, unchangedArgument], [originalLocal, unchangedLocal]);
        ReplaceVisitor visitor = new();
        visitor.Replacements[originalArgument] = replacementArgument;
        visitor.Replacements[originalLocal] = replacementLocal;

        var rewritten = (ControlFlowGraph)visitor.Visit(graph);

        Assert.Multiple(() =>
        {
            Assert.That(rewritten, Is.Not.SameAs(graph));
            Assert.That(rewritten.Arguments, Is.EqualTo(new[] { replacementArgument, unchangedArgument }));
            Assert.That(rewritten.Arguments[0], Is.SameAs(replacementArgument));
            Assert.That(rewritten.Arguments[1], Is.SameAs(unchangedArgument));
            Assert.That(rewritten.Locals, Is.EqualTo(new[] { replacementLocal, unchangedLocal }));
            Assert.That(rewritten.Locals[0], Is.SameAs(replacementLocal));
            Assert.That(rewritten.Locals[1], Is.SameAs(unchangedLocal));
            Assert.That(rewritten.RootRegion, Is.SameAs(root));
            Assert.That(rewritten.GetBlock(block.Label), Is.SameAs(block));
        });
    }
}
