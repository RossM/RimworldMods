namespace Disharmony.Optimizer;

internal class RewriteVisitor : Visitor
{
    public override Op Visit(AssignmentOp op)
    {
        var input = op.Input.Accept(this);
        var output = (Variable)op.Output.Accept(this);
        if (input == op.Input && output == op.Output)
            return op;
        return new AssignmentOp(output, input);
    }

    public override Op Visit(ILOp op)
    {
        var inputs = op.Inputs.Select(input => input.Accept(this));
        if (inputs.SequenceEqual(op.Inputs))
            return op;
        return new ILOp(op.IL, op.Inputs, op.Type);
    }

    public override Op Visit(StackSlot op) => op;
    public override Op Visit(Argument op) => op;
    public override Op Visit(Local op) => op;
    public override Op Visit(Temporary op) => op;
    public override Op Visit(VoidOp op) => op;
    public override Region Visit(RootRegion region) => region;
    public override Region Visit(ProtectedRegion region) => region;
    public override Region Visit(CatchRegion region) => region;
    public override Region Visit(FinallyRegion region) => region;
    public override Region Visit(FaultRegion region) => region;
    public override ExceptionGroup Visit(ExceptionGroup group) => group;
    public override Branch Visit(UnconditionalBranch branch) => branch;
    public override Branch Visit(Leave branch) => branch;

    public override Branch Visit(ConditionalBranch branch)
    {
        var inputs = branch.Inputs.Select(input => input.Accept(this));
        if (inputs.SequenceEqual(branch.Inputs))
            return branch;
        return new ConditionalBranch(branch.OpCode, branch.Inputs, branch.Labels);
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

    public override Edge Visit(Edge edge) => edge;
}
