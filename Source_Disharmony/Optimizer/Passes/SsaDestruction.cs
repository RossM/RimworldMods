namespace Disharmony.Optimizer.Passes;

/// <summary>
///     Converts phi edge assignments into ordinary variable-form storage operations. The generated
///     loads and stores are intentionally mundane: the stack scheduler already knows how to keep a
///     logical value on the evaluation stack or spill it only when required.
/// </summary>
internal sealed class SsaDestruction(Optimizer optimizer) : Pass
{
    public override void Run()
    {
        CheckPreconditions();

        foreach (ControlFlowEdge edge in optimizer.Edges)
        {
            if (edge.Source.isSyntheticMethodEntry)
            {
                // Version zero already occupies the original argument/local slot on invocation.
                // The entry phi retains that slot as its lowering preference, so materializing its
                // conceptual initial assignment would only add a redundant load/store pair.
                edge.assignments.Clear();
                continue;
            }
            if (edge.assignments.Count != 0)
                MaterializeAssignments(edge);
        }

        // The family relationship is no longer canonical, but retaining the physical storage as a
        // lowering preference lets one compatible spill reclaim a slot made free by promotion.
        foreach (Variable variable in optimizer.variables)
        {
            Variable? origin = variable.ssaOrigin;
            if (origin != null && origin != variable &&
                origin.kind is VariableKind.Argument or VariableKind.Local)
            {
                variable.preferredStorage = origin;
            }
            variable.ssaOrigin = null;
            variable.ssaVersion = -1;
        }

        optimizer.Form = Optimizer.IrForm.Variables;
    }

    private void CheckPreconditions()
    {
        if (optimizer.Form != Optimizer.IrForm.Ssa)
            throw new InvalidOperationException($"Cannot destruct SSA in {optimizer.Form} form");

        foreach (ControlFlowEdge edge in optimizer.basicBlocks.SelectMany(block => block.outgoingEdges))
        {
            HashSet<Variable> stackDestinations = [.. edge.Target.entryStackVariables];
            bool hasStackAssignments = edge.assignments.Any(assignment =>
                stackDestinations.Contains(assignment.Destination));
            if (hasStackAssignments && edge.Source.outgoingEdges.Count != 1)
            {
                throw new InvalidOperationException(
                    $"SSA edge {edge.Source.ID} => {edge.Target.ID} requires edge splitting");
            }
        }
    }

    private void MaterializeAssignments(ControlFlowEdge edge)
    {
        Dictionary<Variable, Variable> sourcesByDestination = [];
        foreach (VariableAssignment assignment in edge.assignments)
        {
            if (sourcesByDestination.ContainsKey(assignment.Destination))
                throw new InvalidOperationException("An SSA edge assigns the same phi destination more than once");
            sourcesByDestination.Add(assignment.Destination, assignment.Source);
        }

        HashSet<Variable> stackDestinations = [.. edge.Target.entryStackVariables];
        List<VariableAssignment> storageAssignments =
        [
            .. edge.assignments.Where(assignment =>
                !stackDestinations.Contains(assignment.Destination) &&
                assignment.Source != assignment.Destination),
        ];

        List<Op> copies = [];

        // Load every source before writing any destination, thereby preserving the parallel-copy
        // semantics even when later SSA optimizations create a copy cycle.
        List<(VariableAssignment Assignment, Variable Value)> loadedAssignments = [];
        foreach (VariableAssignment assignment in storageAssignments)
        {
            Variable value = optimizer.CreateTemporary(assignment.Source.type);
            copies.Add(MakeLoad(assignment.Source, value));
            loadedAssignments.Add((assignment, value));
        }
        for (int index = loadedAssignments.Count - 1; index >= 0; index--)
        {
            (VariableAssignment assignment, Variable value) = loadedAssignments[index];
            copies.Add(MakeStore(value, assignment.Destination));
        }

        List<Variable> sourceStack =
        [
            .. edge.Target.entryStackVariables.Select(destination =>
                sourcesByDestination.TryGetValue(destination, out Variable? source) ? source : destination),
        ];
        bool stackChanges = !sourceStack.SequenceEqual(edge.Target.entryStackVariables);
        if (stackChanges)
        {
            // Empty and rebuild the complete cross-block stack. This turns predecessor-specific
            // phi sources into the one regular entry-stack identity required by stack lowering.
            for (int index = sourceStack.Count - 1; index >= 0; index--)
            {
                Variable source = sourceStack[index];
                Variable destination = edge.Target.entryStackVariables[index];
                copies.Add(destination.type == typeof(TypeLattice.NullType)
                    ? MakePop(source)
                    : MakeStore(source, destination));
            }
            foreach (Variable destination in edge.Target.entryStackVariables)
                copies.Add(MakeLoad(destination, destination));
        }

        BasicBlock sourceBlock = edge.Source;
        int insertionIndex = sourceBlock.ops.Count;
        if (insertionIndex > 0 && sourceBlock.ops[^1].CanBranch)
            insertionIndex--;
        sourceBlock.ops.InsertRange(insertionIndex, copies);
        edge.assignments.Clear();
    }

    private static Op MakeLoad(Variable source, Variable stackValue)
    {
        var load = new Op(OpCodes.Ldloc, 0, []);
        load.inputs.Add(source);
        load.outputs.Add(stackValue);
        load.stackOutputCount = 1;
        return load;
    }

    private static Op MakeStore(Variable value, Variable destination)
    {
        var store = new Op(OpCodes.Stloc, 0, []);
        store.inputs.Add(value);
        store.outputs.Add(destination);
        store.stackInputCount = 1;
        return store;
    }

    private static Op MakePop(Variable value)
    {
        var pop = new Op(OpCodes.Pop);
        pop.inputs.Add(value);
        pop.stackInputCount = 1;
        return pop;
    }
}
