namespace Disharmony.Optimizer.Passes;

internal class PromoteVariables(Optimizer optimizer) : Pass(optimizer)
{
    protected internal override void RunInternal()
    {
        var escapingVariables = new FindEscapingVariablesVisitor(ControlFlowGraph).GetEscapingVariables();

        throw new NotImplementedException();
    }
}

internal class FindEscapingVariablesVisitor(ControlFlowGraph cfg) : RecursiveVisitor
{
    public HashSet<Variable> EscapingVariables { get; } = [];
    private int handlerDepth = 0;

    protected override void Visit(ILOp op)
    {
        base.Visit(op);

        switch (OpCodeData.GetCanonicalOpcode(op.IL))
        {
            case OpCodeValues.Ldloca: EscapingVariables.Add(cfg.GetLocal(op.IL)); break;
            case OpCodeValues.Ldarga: EscapingVariables.Add(cfg.GetArgument(op.IL)); break;
        }
    }

    protected override void Visit(Argument op)
    {
        base.Visit(op);
        CheckEscape(op);
    }

    protected override void Visit(Local op)
    {
        base.Visit(op);
        CheckEscape(op);
    }

    private void CheckEscape(Variable op)
    {
        if (handlerDepth > 0 || op.Type != OpcodeUtilities.GetStackType(op.Type))
            EscapingVariables.Add(op);
    }

    public override void Visit(BasicBlock block)
    {
        int oldHandlerDepth = handlerDepth;
        for (ExceptionRegion? region = block.Region as ExceptionRegion; region != null; region = region.Parent as ExceptionRegion)
        {
            if (region is HandlerRegion)
                handlerDepth++;
        }
        base.Visit(block);
        handlerDepth = oldHandlerDepth;
    }

    public HashSet<Variable> GetEscapingVariables()
    {
        Visit(cfg);
        return EscapingVariables;
    }
}