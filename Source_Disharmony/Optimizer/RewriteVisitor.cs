namespace Disharmony.Optimizer;

internal class ReplaceVisitor : RewriteVisitor
{
    public Dictionary<Op, Op> Replacements { get; } = [];
    protected override Op DefaultVisit(Op op) => Replacements.TryGetValue(op, out var replacement) ? replacement : op;
}

internal class RewriteVisitor : Visitor
{
    protected virtual Op DefaultVisit(Op op) => op;

    public override Node Visit(AssignmentOp op)
    {
        var input = (Op)op.Input.Accept(this);
        var output = (Variable)op.Output.Accept(this);
        if (input == op.Input && output == op.Output)
            return DefaultVisit(op);
        return DefaultVisit(new AssignmentOp(output, input));
    }

    public override Node Visit(ILOp op)
    {
        var inputs = op.Inputs.Select(input => (Op)input.Accept(this)).ToList();
        if (inputs.SequenceEqual(op.Inputs))
            return DefaultVisit(op);
        return DefaultVisit(new ILOp(op.IL, inputs, op.Type));
    }

    public override Node Visit(StackSlot op) => DefaultVisit(op);
    public override Node Visit(Argument op) => DefaultVisit(op);
    public override Node Visit(Local op) => DefaultVisit(op);
    public override Node Visit(Temporary op) => DefaultVisit(op);
    public override Node Visit(VoidOp op) => DefaultVisit(op);
    public override Node Visit(RootRegion region) => region;

    public override Node Visit(ProtectedRegion region)
    {
        var parent = (Region)region.Parent.Accept(this);
        var group = (ExceptionGroup)region.Group.Accept(this);
        if (parent == region.Parent && group == region.Group)
            return region;
        return new ProtectedRegion(region.EntryLabel, parent, group);
    }

    public override Node Visit(CatchRegion region)
    {
        var parent = (Region)region.Parent.Accept(this);
        var incomingException = (StackSlot)region.IncomingException.Accept(this);
        if (parent == region.Parent && incomingException == region.IncomingException)
            return region;
        return new CatchRegion(region.EntryLabel, parent, incomingException);
    }

    public override Node Visit(FinallyRegion region)
    {
        var parent = (Region)region.Parent.Accept(this);
        if (parent == region.Parent)
            return region;
        return new FinallyRegion(region.EntryLabel, parent);
    }

    public override Node Visit(FaultRegion region)
    {
        var parent = (Region)region.Parent.Accept(this);
        if (parent == region.Parent)
            return region;
        return new FaultRegion(region.EntryLabel, parent);
    }

    public override Node Visit(ExceptionGroup group)
    {
        var handlerRegions = group.HandlerRegions.Select(region => (HandlerRegion)region.Accept(this)).ToList();
        if (handlerRegions.SequenceEqual(group.HandlerRegions))
            return group;
        return new ExceptionGroup(handlerRegions);
    }

    public override Node Visit(UnconditionalBranch branch) => branch;
    public override Node Visit(Leave branch) => branch;

    public override Node Visit(ConditionalBranch branch)
    {
        var inputs = branch.Inputs.Select(input => (Op)input.Accept(this)).ToList();
        if (inputs.SequenceEqual(branch.Inputs))
            return branch;
        return new ConditionalBranch(branch.OpCode, inputs, branch.Labels);
    }

    public override Node Visit(Throw branch)
    {
        var exception = (Op)branch.Exception.Accept(this);
        if (exception == branch.Exception)
            return branch;
        return new Throw(exception);
    }

    public override Node Visit(Rethrow branch) => branch;

    public override Node Visit(Return branch)
    {
        var value = (Op)branch.Value.Accept(this);
        if (value == branch.Value)
            return branch;
        return new Return(branch.IL, value);
    }

    public override Node Visit(Jump branch)
    {
        var value = (Op)branch.Value.Accept(this);
        if (value == branch.Value)
            return branch;
        return new Jump(value);
    }

    public override Node Visit(BasicBlock block)
    {
        var ops = block.Ops.Select(op => (Op)op.Accept(this)).ToList();
        var branch = (Branch)block.Branch.Accept(this);
        var region = (Region)block.Region.Accept(this);
        if (ops.SequenceEqual(block.Ops) && branch == block.Branch && region == block.Region)
            return block;
        return new BasicBlock(block.Label, ops, region, branch);
    }

    public override Node Visit(Edge edge)
    {
        var edgeAssignments = edge.EdgeAssignments.Select(op => (AssignmentOp)op.Accept(this)).Where(op => op.Input != op.Output).ToList();
        if (edgeAssignments.SequenceEqual(edge.EdgeAssignments))
            return edge;
        return new Edge(edge.Source, edge.Destination, edgeAssignments);
    }

    public override Node Visit(ControlFlowGraph cfg)
    {
        var blocks = cfg.BasicBlocks.Select(block => (BasicBlock)block.Accept(this)).ToList();
        var edges = cfg.Edges.Select(edge => (Edge)edge.Accept(this)).ToList();
        var rootRegion = (RootRegion)cfg.RootRegion.Accept(this);

        if (blocks.SequenceEqual(cfg.BasicBlocks) && edges.SequenceEqual(cfg.Edges) && rootRegion == cfg.RootRegion)
            return cfg;

        return new ControlFlowGraph(rootRegion, blocks, edges);
    }
}
