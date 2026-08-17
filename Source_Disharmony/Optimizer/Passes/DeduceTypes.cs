namespace Disharmony.Optimizer.Passes;

internal class DeduceTypes(Optimizer optimizer) : Pass(optimizer)
{
    protected internal override void RunInternal()
    {
        throw new NotImplementedException();
    }
}

internal class TypeVisitor : Visitor
{
    private class TypeMap
    {
        private readonly Dictionary<Op, Type> map = [];
        public bool Dirty { get; set; }

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
    }

    private readonly TypeMap Types = new();

    public override Node Visit(AssignmentOp op)
    {
        op.Input.Accept(this);
        if (op.Output is StackSlot)
            Types[op.Output] = TypeLattice.Merge(Types[op.Output], Types[op.Input]);
        return op;
    }
}