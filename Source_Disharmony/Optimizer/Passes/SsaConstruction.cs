namespace Disharmony.Optimizer.Passes;

/// <summary>
///     Incrementally renames newly promotable mutable variables into SSA values. Phi functions use
///     the shared IR's existing parallel edge assignments: every incoming edge supplies one source
///     for the same destination Variable at the target.
/// </summary>
internal sealed class SsaConstruction(Optimizer optimizer) : Pass
{
    private readonly Dictionary<BasicBlock, HashSet<Variable>> uses = [];
    private readonly Dictionary<BasicBlock, HashSet<Variable>> definitions = [];
    private readonly Dictionary<BasicBlock, HashSet<Variable>> liveIn = [];
    private readonly Dictionary<BasicBlock, Dictionary<Variable, Variable>> phis = [];
    private readonly Dictionary<Variable, int> nextVersion = [];
    private HashSet<Variable> candidates = [];
    private DominatorTree dominators = null!;

    public override void Run()
    {
        CheckPreconditions();
        optimizer.AddSsaEntryBlockIfNeeded();
        dominators = optimizer.ComputeDominatorTreeIfNeeded();
        candidates =
        [
            .. optimizer.variables.Where(variable =>
                variable.ssaOrigin == null &&
                (IsPromotableStackSlot(variable) || optimizer.IsEligibleForSsaPromotion(variable))),
        ];

        bool hasRemovableLiterals = optimizer.basicBlocks.SelectMany(block => block.ops)
            .Any(op => TryGetRemovableLiteral(op, out _));
        if (candidates.Count == 0 && !hasRemovableLiterals)
        {
            optimizer.Form = Optimizer.IrForm.Ssa;
            return;
        }

        if (candidates.Count != 0)
        {
            ComputeLiveness();
            ValidateExceptionEntryLiveness();
        }

        // The original mutable object is version zero and represents the value entering a
        // dominator root. Keeping it in the same family also makes later invocations recognize
        // that this storage has already been promoted. Do this only after validation, so a rejected
        // pass does not leave partially versioned variables behind.
        foreach (Variable candidate in candidates)
        {
            candidate.ssaOrigin = candidate;
            candidate.ssaVersion = 0;
            nextVersion.Add(candidate, 1);
        }
        PlacePhis();
        Rename();
        optimizer.Form = Optimizer.IrForm.Ssa;
    }

    // An imprecisely typed cross-block slot remains in the regular mutable representation. Phi
    // destruction may need to spill a predecessor-specific value, and the CLI has no local
    // signature that can represent a lattice marker or absent type metadata.
    // TODO: Trace why ConditionalStructCopy currently produces such a slot at its join. The goal is
    //       to preserve its concrete stack type and remove this exemption, not to spill AnyType.
    private static bool IsPromotableStackSlot(Variable variable) =>
        variable.kind == VariableKind.StackSlot &&
        variable.type != null &&
        !TypeLattice.IsSpecialType(variable.type);

    private void CheckPreconditions()
    {
        if (optimizer.Form is not (Optimizer.IrForm.Variables or Optimizer.IrForm.Ssa))
            throw new InvalidOperationException($"Cannot construct SSA from {optimizer.Form} form");

        foreach (ControlFlowEdge edge in optimizer.basicBlocks.SelectMany(block => block.outgoingEdges))
        {
            if (optimizer.Form == Optimizer.IrForm.Variables && edge.assignments.Count != 0)
                throw new InvalidOperationException("Regular Variables form contains SSA edge assignments");
            if (optimizer.Form == Optimizer.IrForm.Ssa && edge.assignments.Any(assignment =>
                    assignment.Destination.ssaOrigin == null))
            {
                throw new InvalidOperationException("SSA edge assignment has an unversioned destination");
            }
        }
    }

    // Computes pruned SSA liveness over only the newly promoted families. This avoids creating phi
    // destinations in joins from which the mutable value is never subsequently read.
    private void ComputeLiveness()
    {
        foreach (BasicBlock block in optimizer.basicBlocks)
        {
            HashSet<Variable> blockUses = [];
            HashSet<Variable> blockDefinitions = [];

            foreach (Variable variable in block.entryStackVariables)
                AddUse(variable);

            foreach (Op op in block.ops)
            {
                foreach (Variable variable in op.inputs)
                    AddUse(variable);

                foreach (Variable variable in op.outputs)
                {
                    if (!candidates.Contains(variable) || IsAliasedStackOutput(op, variable))
                        continue;
                    blockDefinitions.Add(variable);
                }
            }

            // An existing SSA family may already consume a newly promotable value on an edge.
            // Treat that transfer as a use in its source block so incremental construction keeps
            // the required definition live and subsequently renames the assignment source.
            foreach (ControlFlowEdge edge in block.outgoingEdges)
            foreach (VariableAssignment assignment in edge.assignments)
                AddUse(assignment.Source);

            uses.Add(block, blockUses);
            definitions.Add(block, blockDefinitions);
            liveIn.Add(block, [.. blockUses]);
            continue;

            void AddUse(Variable variable)
            {
                if (candidates.Contains(variable) && !blockDefinitions.Contains(variable))
                    blockUses.Add(variable);
            }
        }

        bool changed;
        do
        {
            changed = false;
            for (int index = optimizer.basicBlocks.Count - 1; index >= 0; index--)
            {
                BasicBlock block = optimizer.basicBlocks[index];
                HashSet<Variable> newLiveIn = [.. uses[block]];
                foreach (BasicBlock successor in block.Successors)
                    newLiveIn.UnionWith(liveIn[successor].Except(definitions[block]));

                if (liveIn[block].SetEquals(newLiveIn))
                    continue;
                liveIn[block] = newLiveIn;
                changed = true;
            }
        } while (changed);
    }

    private void PlacePhis()
    {
        foreach (BasicBlock block in optimizer.basicBlocks)
            phis.Add(block, []);

        foreach (Variable variable in candidates)
        {
            HashSet<BasicBlock> definitionBlocks =
            [
                .. optimizer.basicBlocks.Where(block => definitions[block].Contains(variable)),
                .. dominators.Roots,
            ];
            Queue<BasicBlock> work = new(definitionBlocks);
            HashSet<BasicBlock> queuedDefinitions = [.. definitionBlocks];

            while (work.Count > 0)
            {
                BasicBlock definition = work.Dequeue();
                foreach (BasicBlock frontier in dominators.GetDominanceFrontier(definition))
                {
                    if (!liveIn[frontier].Contains(variable) || phis[frontier].ContainsKey(variable))
                        continue;

                    // The phi result is distinct from version zero. At a looped method entry, the
                    // new entry block supplies version zero just like every other operand; no
                    // definition is hidden outside the CFG.
                    Variable destination = NewVersion(variable);
                    phis[frontier].Add(variable, destination);
                    if (queuedDefinitions.Add(frontier))
                        work.Enqueue(frontier);
                }
            }
        }
    }

    // Exceptional transfers do not identify the particular throwing instruction whose storage
    // state reaches a handler. Such storage must remain escaping until that dataflow is modeled;
    // silently treating a live argument/local as an ordinary dominator-root value would be wrong.
    private void ValidateExceptionEntryLiveness()
    {
        foreach (BasicBlock entry in optimizer.GetExceptionEntryBlocks())
        {
            bool hasNonescapingStorage = liveIn[entry].Any(variable =>
                variable.kind is VariableKind.Argument or VariableKind.Local);
            if (hasNonescapingStorage)
            {
                throw new InvalidOperationException(
                    $"Exception entry {entry.ID} has nonescaping storage live on entry");
            }

            // The runtime-supplied catch/filter stack value may be a root definition. It becomes
            // unsupported only if normal predecessors also turn the exception entry into a join;
            // unlike the method entry, exception entries must not gain synthetic predecessors.
            if (entry.incomingEdges.Count != 0 && liveIn[entry].Count != 0)
            {
                throw new InvalidOperationException(
                    $"Exception entry {entry.ID} has nonescaping values and normal predecessors");
            }
        }
    }

    // Traverses the dominator forest while maintaining one current-definition stack per newly
    // promoted family. Existing SSA families are deliberately ignored, which is what makes this
    // pass safe to invoke incrementally.
    private void Rename()
    {
        Dictionary<Variable, Stack<Variable>> current = candidates.ToDictionary(
            variable => variable,
            variable => new Stack<Variable>([variable]));
        Dictionary<Variable, Variable> replacements = [];

        foreach (BasicBlock root in dominators.Roots)
            RenameSubtree(root);

        void RenameSubtree(BasicBlock block)
        {
            List<Variable> pushed = [];
            foreach (KeyValuePair<Variable, Variable> phi in phis[block])
                Push(phi.Key, phi.Value);

            ReplaceUses(block.entryStackVariables);
            List<Op> retainedOperations = [];
            foreach (Op op in block.ops)
            {
                if (TryGetRemovableLiteral(op, out ConstantValue? literal))
                {
                    Variable output = op.outputs[0];
                    Variable constant = optimizer.NewConstantVariable(literal);
                    if (candidates.Contains(output))
                        Push(output, constant);
                    else
                        replacements[output] = constant;
                    continue;
                }

                Op.StorageAccess? storageAccess = op.GetStorageAccess();

                // Promoted storage operations no longer access physical memory. Loads disappear,
                // while each write remains as a logical copy defining a fresh SSA name. Keeping
                // that definition distinct from its source is required by SSA; copy elimination
                // can remove it independently.
                if (storageAccess is { } access && candidates.Contains(access.Variable))
                {
                    switch (access.Kind)
                    {
                        case Op.VariableAccessKind.Read:
                        {
                            if (op.stackInputCount != 0 || op.stackOutputCount != 1)
                                throw new InvalidOperationException("Promoted storage read has an invalid stack shape");
                            Variable value = Current(access.Variable);
                            Variable loadedValue = op.outputs[0];
                            if (loadedValue.kind == VariableKind.StackSlot &&
                                !candidates.Contains(loadedValue))
                            {
                                // An imprecisely typed cross-block slot is deliberately not in SSA.
                                // Keep this path-specific logical copy; replacing the shared slot
                                // globally would make the last visited predecessor win the join.
                                op.inputs[op.stackInputCount] = value;
                                retainedOperations.Add(op);
                                continue;
                            }
                            if (candidates.Contains(loadedValue))
                                Push(loadedValue, value);
                            else
                                replacements[loadedValue] = value;
                            continue;
                        }
                        case Op.VariableAccessKind.Write:
                        {
                            if (op.stackInputCount != 1 || op.stackOutputCount != 0)
                                throw new InvalidOperationException("Promoted storage write has an invalid stack shape");
                            op.inputs[0] = Resolve(op.inputs[0]);
                            Variable version = NewVersion(access.Variable);
                            op.outputs[op.stackOutputCount] = version;
                            Push(access.Variable, version);
                            retainedOperations.Add(op);
                            continue;
                        }
                        case Op.VariableAccessKind.Address:
                            throw new InvalidOperationException("Address-taken storage was promoted to SSA");
                        default: throw new ArgumentOutOfRangeException();
                    }
                }

                ReplaceUses(op.inputs);

                Dictionary<Variable, Variable> operationDefinitions = [];
                for (int index = 0; index < op.outputs.Count; index++)
                {
                    Variable output = op.outputs[index];
                    if (!candidates.Contains(output))
                        continue;
                    if (IsAliasedStackOutput(op, output))
                    {
                        op.outputs[index] = Current(output);
                        continue;
                    }

                    if (!operationDefinitions.TryGetValue(output, out Variable? version))
                    {
                        version = NewVersion(output);
                        operationDefinitions.Add(output, version);
                        Push(output, version);
                    }
                    op.outputs[index] = version;
                }
                retainedOperations.Add(op);
            }
            block.ops.Clear();
            block.ops.AddRange(retainedOperations);

            foreach (ControlFlowEdge edge in block.outgoingEdges)
            {
                for (int index = 0; index < edge.assignments.Count; index++)
                {
                    VariableAssignment assignment = edge.assignments[index];
                    Variable source = Resolve(assignment.Source);
                    if (source != assignment.Source)
                        edge.assignments[index] = new(source, assignment.Destination);
                }

                foreach (KeyValuePair<Variable, Variable> phi in phis[edge.Target])
                    edge.assignments.Add(new VariableAssignment(Current(phi.Key), phi.Value));
            }

            foreach (BasicBlock child in dominators.GetChildren(block))
                RenameSubtree(child);

            for (int index = pushed.Count - 1; index >= 0; index--)
                current[pushed[index]].Pop();

            return;

            void ReplaceUses(List<Variable> variables)
            {
                for (int index = 0; index < variables.Count; index++)
                    variables[index] = Resolve(variables[index]);
            }

            void Push(Variable origin, Variable version)
            {
                current[origin].Push(version);
                pushed.Add(origin);
            }
        }

        Variable Current(Variable origin) => current[origin].Peek();

        Variable Resolve(Variable variable)
        {
            HashSet<Variable>? visited = null;
            bool replaced = false;
            while (replacements.TryGetValue(variable, out Variable? replacement))
            {
                visited ??= [];
                if (!visited.Add(variable))
                    throw new InvalidOperationException("Cyclic SSA value replacement");
                variable = replacement;
                replaced = true;
            }

            // A replacement captures the value which a removed load produced at that point. Its
            // result may be another family's version-zero object; looking that object up again as
            // a mutable name would incorrectly substitute a later definition of the other family.
            return !replaced && candidates.Contains(variable) ? Current(variable) : variable;
        }
    }

    private Variable NewVersion(Variable origin)
    {
        Type? type = origin.kind is VariableKind.Argument or VariableKind.Local
            ? TypeLattice.ToStackType(origin.type!)
            : origin.type;
        Variable version = optimizer.NewVariable(VariableKind.Temporary, type);
        version.ssaOrigin = origin;
        version.ssaVersion = nextVersion[origin]++;
        PreferOriginalStorage(version, origin);
        return version;
    }

    // The first logical value derived from promoted storage can reclaim that slot if lowering
    // needs a spill. A value shared by several promoted variables keeps its first compatible hint.
    private static void PreferOriginalStorage(Variable value, Variable origin)
    {
        if (origin.kind is not (VariableKind.Argument or VariableKind.Local) ||
            value.kind is VariableKind.Argument or VariableKind.Local or VariableKind.Constant ||
            value.preferredStorage != null)
        {
            return;
        }

        // The original well-formed stloc is the compatibility evidence even when symbolic stack
        // analysis could not recover the producer's exact type. Eligibility has already ruled out
        // storage whose store/load boundary performs a narrowing conversion.
        value.preferredStorage = origin;
    }

    // Dup's output is another name for its input, not a new definition. Stack-slot merging can
    // make that shared value a promotion candidate when the duplicate crosses a block boundary.
    private static bool IsAliasedStackOutput(Op op, Variable variable) =>
        op.Opcode == OpCodes.Dup &&
        op.outputs.Take(op.stackOutputCount).Contains(variable);

    // Well-formed literal loads have no prefixes and exactly one new stack result. Keeping a
    // prefixed literal operation is the conservative choice for malformed-but-processable input.
    private static bool TryGetRemovableLiteral(
        Op op,
        [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out ConstantValue? literal)
    {
        literal = null;
        if (op.Prefixes.Count != 0 || !op.TryGetLiteral(out ConstantValue? value))
            return false;
        if (op.stackInputCount != 0 || op.inputs.Count != 0 ||
            op.stackOutputCount != 1 || op.outputs.Count != 1)
        {
            throw new InvalidOperationException($"Literal {op.Opcode} has an invalid variable shape");
        }

        literal = value;
        return true;
    }
}
