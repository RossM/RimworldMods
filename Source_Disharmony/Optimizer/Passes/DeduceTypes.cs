namespace Disharmony.Optimizer.Passes;

internal class DeduceTypes(Optimizer optimizer) : Pass(optimizer)
{
    protected internal override void RunInternal()
    {
        var visitor = new TypeVisitor(Optimizer);
        Optimizer.cfg = (ControlFlowGraph)Optimizer.cfg.Accept(visitor);
    }
}

internal class TypeVisitor(Optimizer optimizer) : Visitor
{
    private class TypeMap
    {
        private readonly Dictionary<Op, Type> map = [];
        public bool Dirty { get; set; } = true;

        public Type this[Op op]
        {
            get => map.TryGetValue(op, out var type) ? type : op.Type;
            set
            {
                if (value == this[op])
                    return;
                map[op] = value;
                Dirty = true;
            }
        }

        public Dictionary<Op, Op> GetReplacements()
        {
            return map.ToDictionary(kvp => kvp.Key, kvp => kvp.Key with { Type = kvp.Value });
        }
    }

    private readonly TypeMap Types = new();

    public override Node Visit(AssignmentOp op)
    {
        op.Input.Accept(this);
        if (op.Output is StackSlot)
            Types[op.Output] = TypeLattice.Merge(Types[op.Output], Types[op.Input]);
        return op;
    }

    public override Node Visit(ILOp op)
    {
        var data = OpCodeData.Get(op.IL.OpCode);

        IEnumerable<Type> inputTypes = op.Inputs.Select(inputOp => ((Op)inputOp.Accept(this)).Type);

        Type[] types;
        if (data.flags.HasFlag(OpCodeFlags.Argument))
            types = [optimizer.arguments[OpCodeData.GetIntOperand(op.IL)].Type, .. inputTypes];
        else if (data.flags.HasFlag(OpCodeFlags.Local))
            types = [optimizer.locals[OpCodeData.GetIntOperand(op.IL)].Type, .. inputTypes];
        else
            types = [.. inputTypes];

        Types[op] = OpcodeUtilities.GetOutputType(op.IL, types);
        return op;
    }

    public override Node Visit(ConditionalBranch branch)
    {
        foreach (var op in branch.Inputs)
            op.Accept(this);
        return branch;
    }

    public override Node Visit(Throw branch)
    {
        branch.Exception.Accept(this);
        return branch;
    }

    public override Node Visit(Return branch)
    {
        branch.Value.Accept(this);
        return branch;
    }

    public override Node Visit(Jump branch)
    {
        branch.Value.Accept(this);
        return branch;
    }

    public override Node Visit(BasicBlock block)
    {
        foreach (var op in block.Ops)
            op.Accept(this);
        return block;
    }

    public override Node Visit(Edge edge)
    {
        foreach (var assignment in edge.EdgeAssignments)
            assignment.Accept(this);
        return edge;
    }

    public override Node Visit(ControlFlowGraph cfg)
    {
        while (Types.Dirty)
        {
            Types.Dirty = false;

            foreach (var block in cfg.BasicBlocks)
                block.Accept(this);
            foreach (var edge in cfg.Edges)
                edge.Accept(this);
        }

        ReplaceVisitor replaceVisitor = new ReplaceVisitor { Replacements = Types.GetReplacements() };
        return replaceVisitor.Visit(cfg);
    }
}