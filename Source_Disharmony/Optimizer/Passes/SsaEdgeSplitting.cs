namespace Disharmony.Optimizer.Passes;

/// <summary>
///     Gives every edge carrying an evaluation-stack SSA assignment a source block with no other
///     successor. Storage assignments need no split because promoted storage is nonescaping and a
///     speculative write to a target-specific phi destination is unobservable.
/// </summary>
internal sealed class SsaEdgeSplitting(Optimizer optimizer) : Pass
{
    public override void Run()
    {
        if (optimizer.Form != Optimizer.IrForm.Ssa)
            throw new InvalidOperationException($"Cannot split SSA edges in {optimizer.Form} form");

        List<ControlFlowEdge> edges =
        [
            .. optimizer.Edges
                .Where(edge => HasStackAssignments(edge) && edge.Source.outgoingEdges.Count > 1),
        ];

        foreach (ControlFlowEdge edge in edges)
        {
            BasicBlock source = edge.Source;
            if (source.ops.LastOrDefault()?.IsLeave == true)
                throw new InvalidOperationException("A leave block cannot have multiple successors");

            List<Variable> enteringStack = GetTransferredStack(edge);
            HashSet<Variable> stackDestinations = [.. edge.Target.entryStackVariables];
            List<VariableAssignment> speculativeStorageAssignments =
            [
                .. edge.assignments.Where(assignment =>
                    !stackDestinations.Contains(assignment.Destination)),
            ];
            ControlFlowEdge assignedEdge = optimizer.SplitControlFlowEdge(edge);
            assignedEdge.Source.entryStackVariables.AddRange(enteringStack);

            // Storage copies have no observable effect because promoted storage cannot escape.
            // Keep them on the original edge so out-of-SSA can schedule an arbitrary temporary
            // source in its defining predecessor, before control chooses a successor. Only stack
            // transfers require the new edge-specific block.
            foreach (VariableAssignment assignment in speculativeStorageAssignments)
            {
                assignedEdge.assignments.Remove(assignment);
                edge.assignments.Add(assignment);
            }
        }
    }

    private static bool HasStackAssignments(ControlFlowEdge edge)
    {
        HashSet<Variable> stackDestinations = [.. edge.Target.entryStackVariables];
        return edge.assignments.Any(assignment => stackDestinations.Contains(assignment.Destination));
    }

    // Before the split, an assignment to a target entry-stack variable is that edge's source stack
    // value. Positions without a phi already have the same variable on both sides of the edge.
    private static List<Variable> GetTransferredStack(ControlFlowEdge edge)
    {
        Dictionary<Variable, Variable> sourcesByDestination = [];
        foreach (VariableAssignment assignment in edge.assignments)
        {
            if (sourcesByDestination.ContainsKey(assignment.Destination))
                throw new InvalidOperationException("An SSA edge assigns the same phi destination more than once");
            sourcesByDestination.Add(assignment.Destination, assignment.Source);
        }

        return
        [
            .. edge.Target.entryStackVariables.Select(destination =>
                sourcesByDestination.TryGetValue(destination, out Variable? source) ? source : destination),
        ];
    }
}
