namespace Disharmony.Optimizer;

internal class ReplaceVisitor : RewriteVisitor
{
    public Mapping<Op> Replacements { get; init; } = [];

    protected override Op DefaultVisit(Op op) => Replacements[op];
}

internal class RewriteVisitor
{
    public Node Visit(Op op) => op switch
    {
        AssignmentOp assignmentOp => Visit(assignmentOp),
        Argument argument => Visit(argument),
        ILOp ilOp => Visit(ilOp),
        Local local => Visit(local),
        StackSlot stackSlot => Visit(stackSlot),
        Temporary temporary => Visit(temporary),
        VoidOp voidOp => Visit(voidOp),
        _ => throw new ArgumentOutOfRangeException(nameof(op), op, null),
    };

    public Node Visit(Branch branch) => branch switch
    {
        ConditionalBranch conditionalBranch => Visit(conditionalBranch),
        Jump jump => Visit(jump),
        Leave leave => Visit(leave),
        Rethrow rethrow => Visit(rethrow),
        Return @return => Visit(@return),
        Throw @throw => Visit(@throw),
        UnconditionalBranch unconditionalBranch => Visit(unconditionalBranch),
        _ => throw new ArgumentOutOfRangeException(nameof(branch)),
    };

    public Node Visit(Region region) => region switch
    {
        CatchRegion catchRegion => Visit(catchRegion),
        FaultRegion faultRegion => Visit(faultRegion),
        FinallyRegion finallyRegion => Visit(finallyRegion),
        HandlerRegion handlerRegion => Visit(handlerRegion),
        ProtectedRegion protectedRegion => Visit(protectedRegion),
        RootRegion rootRegion => Visit(rootRegion),
        _ => throw new ArgumentOutOfRangeException(nameof(region)),
    };

    protected virtual Op DefaultVisit(Op op) => op;

    public virtual Node Visit(AssignmentOp op)
    {
        var input = (Op)this.Visit(op.Input);
        var output = (Variable)this.Visit(op.Output);
        if (input == op.Input && output == op.Output)
            return DefaultVisit(op);
        return DefaultVisit(new AssignmentOp(output, input));
    }

    public virtual Node Visit(ILOp op)
    {
        var inputs = op.Inputs.Select(input => (Op)this.Visit(input)).ToList();
        if (inputs.SequenceEqual(op.Inputs))
            return DefaultVisit(op);
        return DefaultVisit(new ILOp(op.IL, inputs, op.Type));
    }

    public virtual Node Visit(StackSlot op) => DefaultVisit(op);
    public virtual Node Visit(Argument op) => DefaultVisit(op);
    public virtual Node Visit(Local op) => DefaultVisit(op);
    public virtual Node Visit(Temporary op) => DefaultVisit(op);
    public virtual Node Visit(VoidOp op) => DefaultVisit(op);
    public virtual Node Visit(RootRegion region) => region;

    public virtual Node Visit(ProtectedRegion region)
    {
        var parent = (Region)Visit(region.Parent);
        var group = (ExceptionGroup)this.Visit(region.Group);
        if (parent == region.Parent && group == region.Group)
            return region;
        return new ProtectedRegion(region.EntryLabel, parent, group);
    }

    public virtual Node Visit(CatchRegion region)
    {
        var parent = (Region)this.Visit(region.Parent);
        var incomingException = (StackSlot)this.Visit((Op)region.IncomingException);
        if (parent == region.Parent && incomingException == region.IncomingException)
            return region;
        return new CatchRegion(region.EntryLabel, parent, incomingException);
    }

    public virtual Node Visit(FinallyRegion region)
    {
        var parent = (Region)this.Visit(region.Parent);
        if (parent == region.Parent)
            return region;
        return new FinallyRegion(region.EntryLabel, parent);
    }

    public virtual Node Visit(FaultRegion region)
    {
        var parent = (Region)this.Visit(region.Parent);
        if (parent == region.Parent)
            return region;
        return new FaultRegion(region.EntryLabel, parent);
    }

    public virtual Node Visit(ExceptionGroup group)
    {
        var handlerRegions = group.HandlerRegions.Select(region => (HandlerRegion)this.Visit(region)).ToList();
        if (handlerRegions.SequenceEqual(group.HandlerRegions))
            return group;
        return new ExceptionGroup(handlerRegions);
    }

    public virtual Node Visit(UnconditionalBranch branch) => branch;
    public virtual Node Visit(Leave branch) => branch;

    public virtual Node Visit(ConditionalBranch branch)
    {
        var inputs = branch.Inputs.Select(input => (Op)this.Visit(input)).ToList();
        if (inputs.SequenceEqual(branch.Inputs))
            return branch;
        return new ConditionalBranch(branch.OpCode, inputs, branch.Labels);
    }

    public virtual Node Visit(Throw branch)
    {
        var exception = (Op)this.Visit(branch.Exception);
        if (exception == branch.Exception)
            return branch;
        return new Throw(exception);
    }

    public virtual Node Visit(Rethrow branch) => branch;

    public virtual Node Visit(Return branch)
    {
        var value = (Op)this.Visit(branch.Value);
        if (value == branch.Value)
            return branch;
        return new Return(branch.IL, value);
    }

    public virtual Node Visit(Jump branch)
    {
        var value = (Op)this.Visit(branch.Value);
        if (value == branch.Value)
            return branch;
        return new Jump(value);
    }

    public virtual Node Visit(BasicBlock block)
    {
        var ops = block.Ops.Select(op => (Op)this.Visit(op)).ToList();
        var branch = (Branch)this.Visit(block.Branch);
        var region = (Region)this.Visit(block.Region);
        if (ops.SequenceEqual(block.Ops) && branch == block.Branch && region == block.Region)
            return block;
        return new BasicBlock(block.Label, ops, region, branch);
    }

    public virtual Node Visit(Edge edge)
    {
        var edgeAssignments = edge.EdgeAssignments.Select(op => (AssignmentOp)this.Visit((Op)op)).Where(op => op.Input != op.Output)
            .ToList();
        if (edgeAssignments.SequenceEqual(edge.EdgeAssignments))
            return edge;
        return new Edge(edge.Source, edge.Destination, edgeAssignments);
    }

    public virtual Node Visit(ControlFlowGraph cfg)
    {
        var rootRegion = (RootRegion)this.Visit((Region)cfg.RootRegion);
        var blocks = cfg.BasicBlocks.Select(block => (BasicBlock)this.Visit(block)).ToList();
        var edges = cfg.Edges.Select(edge => (Edge)this.Visit(edge)).ToList();
        var arguments = cfg.Arguments.Select(argument => (Argument)this.Visit((Op)argument)).ToList();
        var locals = cfg.Locals.Select(local => (Local)this.Visit((Op)local)).ToList();

        if (rootRegion == cfg.RootRegion && blocks.SequenceEqual(cfg.BasicBlocks) && edges.SequenceEqual(cfg.Edges) &&
            arguments.SequenceEqual(cfg.Arguments) && locals.SequenceEqual(cfg.Locals))
            return cfg;

        return new ControlFlowGraph(rootRegion, blocks, edges, arguments, locals);
    }
}
