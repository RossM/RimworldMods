namespace Disharmony.Optimizer;

internal class ReplaceVisitor : RewriteVisitor
{
    public Dictionary<Op, Op> Replacements { get; } = [];
    protected override Op DefaultVisit(Op op) => Replacements.TryGetValue(op, out var replacement) ? replacement : op;
}

internal class RewriteVisitor : Visitor
{
    protected virtual Op DefaultVisit(Op op) => op;

    public override Op Visit(AssignmentOp op)
    {
        var input = op.Input.Accept(this);
        var output = (Variable)op.Output.Accept(this);
        if (input == op.Input && output == op.Output)
            return DefaultVisit(op);
        return DefaultVisit(new AssignmentOp(output, input));
    }

    public override Op Visit(ILOp op)
    {
        var inputs = op.Inputs.Select(input => input.Accept(this)).ToList();
        if (inputs.SequenceEqual(op.Inputs))
            return DefaultVisit(op);
        return DefaultVisit(new ILOp(op.IL, inputs, op.Type));
    }

    public override Op Visit(StackSlot op) => DefaultVisit(op);
    public override Op Visit(Argument op) => DefaultVisit(op);
    public override Op Visit(Local op) => DefaultVisit(op);
    public override Op Visit(Temporary op) => DefaultVisit(op);
    public override Op Visit(VoidOp op) => DefaultVisit(op);
    public override Region Visit(RootRegion region) => region;

    public override Region Visit(ProtectedRegion region)
    {
        var parent = region.Parent.Accept(this);
        if (parent == region.Parent)
            return region;
        return new ProtectedRegion(region.EntryLabel, parent);
    }

    public override Region Visit(CatchRegion region)
    {
        var parent = region.Parent.Accept(this);
        var incomingException = (StackSlot)region.IncomingException.Accept(this);
        if (parent == region.Parent && incomingException == region.IncomingException)
            return region;
        return new CatchRegion(region.EntryLabel, parent, incomingException);
    }

    public override Region Visit(FinallyRegion region)
    {
        var parent = region.Parent.Accept(this);
        if (parent == region.Parent)
            return region;
        return new FinallyRegion(region.EntryLabel, parent);
    }

    public override Region Visit(FaultRegion region)
    {
        var parent = region.Parent.Accept(this);
        if (parent == region.Parent)
            return region;
        return new FaultRegion(region.EntryLabel, parent);
    }

    public override ExceptionGroup Visit(ExceptionGroup group)
    {
        var protectedRegion = (ProtectedRegion)group.ProtectedRegion.Accept(this);
        var handlerRegions = group.HandlerRegions.Select(region => (HandlerRegion)region.Accept(this)).ToList();
        if (protectedRegion == group.ProtectedRegion && handlerRegions.SequenceEqual(group.HandlerRegions))
            return group;
        return new ExceptionGroup(protectedRegion, handlerRegions);
    }

    public override Branch Visit(UnconditionalBranch branch) => branch;
    public override Branch Visit(Leave branch) => branch;

    public override Branch Visit(ConditionalBranch branch)
    {
        var inputs = branch.Inputs.Select(input => input.Accept(this)).ToList();
        if (inputs.SequenceEqual(branch.Inputs))
            return branch;
        return new ConditionalBranch(branch.OpCode, inputs, branch.Labels);
    }

    public override Branch Visit(Throw branch)
    {
        var exception = branch.Exception.Accept(this);
        if (exception == branch.Exception)
            return branch;
        return new Throw(exception);
    }

    public override Branch Visit(Rethrow branch) => branch;

    public override Branch Visit(Return branch)
    {
        var value = branch.Value.Accept(this);
        if (value == branch.Value)
            return branch;
        return new Return(branch.IL, value);
    }

    public override Branch Visit(Jump branch)
    {
        var value = branch.Value.Accept(this);
        if (value == branch.Value)
            return branch;
        return new Jump(value);
    }

    public override BasicBlock Visit(BasicBlock block)
    {
        var ops = block.Ops.Select(op => op.Accept(this)).ToList();
        var branch = block.Branch.Accept(this);
        var region = block.Region.Accept(this);
        if (ops.SequenceEqual(block.Ops) && branch == block.Branch && region == block.Region)
            return block;
        return new BasicBlock(block.Label, ops, region, branch);
    }

    public override Edge Visit(Edge edge)
    {
        var edgeAssignments = edge.EdgeAssignments.Select(op => (AssignmentOp)op.Accept(this)).Where(op => op.Input != op.Output).ToList();
        if (edgeAssignments.SequenceEqual(edge.EdgeAssignments))
            return edge;
        return new Edge(edge.Source, edge.Destination, edgeAssignments);
    }

    public void Visit(ControlFlowGraph controlFlowGraph)
    {
        foreach (var block in controlFlowGraph.BasicBlocks.ToList())
        {
            var newBlock = block.Accept(this);
            if (newBlock != block)
                controlFlowGraph.ReplaceBlock(newBlock);
        }

        foreach (var group in controlFlowGraph.ExceptionGroups.ToList())
        {
            var newGroup = group.Accept(this);
            if (newGroup != group)
                controlFlowGraph.ReplaceExceptionGroup(newGroup);
        }

        foreach (var edge in controlFlowGraph.Edges.ToList())
        {
            var newEdge = edge.Accept(this);
            if (newEdge != edge)
                controlFlowGraph.ReplaceEdge(newEdge);
        }

        controlFlowGraph.Validate();
    }
}
