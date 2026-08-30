namespace Disharmony.Optimizer.Passes;

internal class PromoteVariables(Optimizer optimizer) : Pass(optimizer)
{
    protected internal override void RunInternal()
    {
        var escapingVariables = new EscapingVariablesVisitor(ControlFlowGraph).GetEscapingVariables();

        var rewriter = new PromoteVariablesVisitor(ControlFlowGraph, escapingVariables);
        Optimizer.cfg = rewriter.Visit(ControlFlowGraph);
    }
}

internal class EscapingVariablesVisitor(ControlFlowGraph cfg) : RecursiveVisitor
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
        if (handlerDepth > 0)
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
        return OpCodeData.GetCanonicalOpcode(op.IL) switch
        {
            OpCodeValues.Ldarg => VisitLoad(op, cfg.GetArgument(op.IL)),
            OpCodeValues.Ldloc => VisitLoad(op, cfg.GetLocal(op.IL)),
            OpCodeValues.Starg => VisitStore(op, cfg.GetArgument(op.IL)),
            OpCodeValues.Stloc => VisitStore(op, cfg.GetLocal(op.IL)),
            _ => base.Visit(op)
        };
    }

    private Op VisitLoad(ILOp op, Variable variable)
    {
        if (escapingVariables.Contains(variable))
            return base.Visit(op);
        if (variable.Type != OpcodeUtilities.GetStackType(variable.Type))
            return new ConversionOp(variable, OpcodeUtilities.GetStackType(variable.Type));
        return variable;
    }

    private Op VisitStore(ILOp op, Variable variable)
    {
        if (escapingVariables.Contains(variable))
            return base.Visit(op);
        if (variable.Type != OpcodeUtilities.GetStackType(variable.Type))
            return new AssignmentOp(variable, new ConversionOp(op.Inputs[0], variable.Type));
        return new AssignmentOp(variable, op.Inputs[0]);
    }
}