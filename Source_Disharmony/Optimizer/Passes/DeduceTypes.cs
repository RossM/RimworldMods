namespace Disharmony.Optimizer.Passes;

internal class DeduceTypes(Optimizer optimizer) : Pass(optimizer)
{
    protected internal override void RunInternal()
    {
        var visitor = new TypeVisitor(Optimizer);
        Optimizer.cfg = (ControlFlowGraph)Optimizer.cfg.Accept(visitor);
    }
}

internal class TypeVisitor(Optimizer optimizer) : RewriteVisitor
{
    private bool dirty = true;

    private readonly Dictionary<int, StackSlot> stackSlots = [];

    public override Node Visit(AssignmentOp op)
    {
        var input = (Op)op.Input.Accept(this);

        if (op.Output is StackSlot stackSlot)
        {
            StackSlot replacement = GetReplacement(stackSlot);
            var mergedType = TypeLattice.Merge(replacement.Type, input.Type);
            if (replacement.Type != mergedType)
            {
                dirty = true;
                stackSlots[stackSlot.Id] = stackSlot with { Type = mergedType };
            }
        }

        var output = (Variable)op.Output.Accept(this);

        if (input == op.Input && output == op.Output)
            return DefaultVisit(op);
        return DefaultVisit(new AssignmentOp(output, input));
    }

    public override Node Visit(ILOp op)
    {
        var inputs = op.Inputs.Select(input => (Op)input.Accept(this)).ToList();

        var data = OpCodeData.Get(op.IL.OpCode);

        IEnumerable<Type> inputTypes = inputs.Select(input => input.Type);
        
        Type[] types;
        if (data.flags.HasFlag(OpCodeFlags.Argument))
            types = [optimizer.cfg.Arguments[OpCodeData.GetIntOperand(op.IL)].Type, .. inputTypes];
        else if (data.flags.HasFlag(OpCodeFlags.Local))
            types = [optimizer.cfg.Locals[LocalTracker.IndexFrom(op.IL)].Type, .. inputTypes];
        else
            types = [.. inputTypes];

        var resultType = OpcodeUtilities.GetOutputType(op.IL, types);

        if (inputs.SequenceEqual(op.Inputs) && op.Type == resultType)
            return DefaultVisit(op);
        return DefaultVisit(new ILOp(op.IL, inputs, resultType));
    }

    public override Node Visit(StackSlot op) => GetReplacement(op) ;

    private StackSlot GetReplacement(StackSlot op) => stackSlots.TryGetValue(op.Id, out StackSlot replacement) ? replacement : op;

    public override Node Visit(ControlFlowGraph cfg)
    {
        while (true)
        {
            dirty = false;

            var blocks = cfg.BasicBlocks.Select(block => (BasicBlock)block.Accept(this)).ToList();
            var edges = cfg.Edges.Select(edge => (Edge)edge.Accept(this)).ToList();

            if (!dirty)
                return new ControlFlowGraph(cfg.RootRegion, blocks, edges, cfg.Arguments, cfg.Locals);
        }
    }
}