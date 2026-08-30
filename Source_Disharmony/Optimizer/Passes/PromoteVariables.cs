namespace Disharmony.Optimizer.Passes;

internal class PromoteVariables(Optimizer optimizer) : Pass(optimizer)
{
    protected internal override void RunInternal()
    {
        var escapingVariables = new FindEscapingVariablesVisitor(ControlFlowGraph).GetEscapingVariables();

        var rewriter = new PromoteVariablesVisitor(ControlFlowGraph, escapingVariables);
        Optimizer.cfg = rewriter.Visit(ControlFlowGraph);
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

internal class PromoteVariablesVisitor(ControlFlowGraph cfg, HashSet<Variable> escapingVariables) : RewriteVisitor
{
    protected override Op Visit(ILOp op)
    {
        switch (OpCodeData.GetCanonicalOpcode(op.IL))
        {
            case OpCodeValues.Ldarg:
            {
                var variable = cfg.GetArgument(op.IL);
                if (escapingVariables.Contains(variable))
                    return base.Visit(op);
                return variable;
            }
            case OpCodeValues.Ldloc:
            {
                var variable = cfg.GetLocal(op.IL);
                if (escapingVariables.Contains(variable))
                    return base.Visit(op);
                return variable;
            }
            case OpCodeValues.Starg:
            {
                var variable = cfg.GetArgument(op.IL);
                if (escapingVariables.Contains(variable))
                    return base.Visit(op);
                return new AssignmentOp(variable, op.Inputs[0]);
            }
            case OpCodeValues.Stloc:
            {
                var variable = cfg.GetLocal(op.IL);
                if (escapingVariables.Contains(variable))
                    return base.Visit(op);
                return new AssignmentOp(variable, op.Inputs[0]);
            }
            default: return base.Visit(op);
        }
    }
}