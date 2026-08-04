namespace Disharmony.Optimizer.Passes;

internal class VariableToStackConversion(Optimizer optimizer) : Pass
{
    private readonly Dictionary<Variable, Storage> spillStorage = [];
    private readonly HashSet<Variable> occupiedPreferredStorage = [];
    private readonly HashSet<Variable> storedVariables = [];
    private readonly HashSet<Variable> crossBlockSpills = [];

    // Replaces canonical variable operands with an executable CIL stack schedule, then
    // invalidates the variable-form data that no longer describes the operations.
    public override void Run()
    {
        Dictionary<BasicBlock, List<Variable>> exitStacks = CheckPreconditions();
        FindStoredVariables();
        ReserveLiveOriginalStorage();
        FindCrossBlockUses();

        foreach (var block in optimizer.basicBlocks)
            ConvertBlock(block, exitStacks[block]);

        // Variable information is no longer canonical once the operations have been stackified.
        // Clear it so later stack-form passes cannot accidentally consume stale dataflow.
        foreach (var block in optimizer.basicBlocks)
            block.entryStackVariables.Clear();

        optimizer.variables.Clear();
        optimizer.argumentVariables.Clear();
        optimizer.localVariables.Clear();
        optimizer.nextVariableId = 0;
        optimizer.Form = Optimizer.IrForm.Stack;
    }

    // A logical variable named by a storage write has storage even if block conversion has not yet
    // visited that defining write. Recording this up front keeps lowering independent of block order.
    private void FindStoredVariables()
    {
        foreach (Op op in optimizer.basicBlocks.SelectMany(block => block.ops))
        {
            if (op.GetStorageAccess() is { Kind: Op.VariableAccessKind.Write } access)
                storedVariables.Add(access.Variable);
        }
    }

    // A promoted local's live-in version is still represented by the original Local Variable. If
    // that value remains in the IR, assigning its physical slot to a later SSA value could clobber
    // it before a logical reload. Reserve such slots; otherwise one derived value may reclaim them.
    private void ReserveLiveOriginalStorage()
    {
        HashSet<Variable> preferredStorage =
        [
            .. optimizer.variables.Select(variable => variable.preferredStorage)
                .OfType<Variable>(),
        ];
        if (preferredStorage.Count == 0)
            return;

        IEnumerable<Variable> referencedVariables = optimizer.basicBlocks
            .SelectMany(block => block.entryStackVariables.Concat(
                block.ops.SelectMany(op => op.inputs.Concat(op.outputs))));
        foreach (Variable variable in referencedVariables)
        {
            if (preferredStorage.Contains(variable))
                occupiedPreferredStorage.Add(variable);
        }
    }

    // Into-SSA can wire a producer directly to an operation in a dominated block. Unless that value
    // is one of the target's real entry-stack slots, regular CFG edges do not carry it. Spill once
    // at its defining operation so every body use in another block can reload it.
    private void FindCrossBlockUses()
    {
        Dictionary<Variable, BasicBlock> definitions = [];
        foreach (BasicBlock block in optimizer.basicBlocks)
        {
            foreach (Variable output in block.ops.SelectMany(op =>
                         op.outputs.Take(op.stackOutputCount)).Distinct())
            {
                if (definitions.ContainsKey(output))
                    continue;
                definitions.Add(output, block);
            }
        }

        foreach (BasicBlock block in optimizer.basicBlocks)
        {
            foreach (Op op in block.ops)
            {
                foreach (Variable input in op.inputs.Distinct())
                {
                    if (CanReload(input) || block.entryStackVariables.Contains(input))
                        continue;

                    if (!definitions.TryGetValue(input, out BasicBlock? definition))
                        throw new InvalidOperationException($"Use of {input} has no available definition");
                    if (definition != block)
                        crossBlockSpills.Add(input);
                }
            }
        }
    }

    // Validate the complete input before mutating any block. In particular, edge assignments are
    // canonical only in SSA form and must already have been eliminated before regular lowering.
    private Dictionary<BasicBlock, List<Variable>> CheckPreconditions()
    {
        if (optimizer.Form != Optimizer.IrForm.Variables)
            throw new InvalidOperationException($"Cannot convert {optimizer.Form} form to stack");

        ControlFlowEdge? assignedEdge = optimizer.basicBlocks.SelectMany(block => block.outgoingEdges)
            .FirstOrDefault(edge => edge.assignments.Count != 0);
        if (assignedEdge != null)
        {
            throw new InvalidOperationException(
                $"Cannot lower variables to stack while edge {assignedEdge.Source.ID} => " +
                $"{assignedEdge.Target.ID} still has SSA assignments");
        }

        Dictionary<BasicBlock, List<Variable>> exitStacks = [];
        foreach (var block in optimizer.basicBlocks)
        {
            foreach (var op in block.ops)
            {
                if (op.stackInputCount < 0 || op.stackInputCount > op.inputs.Count)
                    throw new InvalidOperationException(
                        $"Invalid variable input count on {op.Opcode} in {block.ID}");
                if (op.stackOutputCount < 0 || op.stackOutputCount > op.outputs.Count)
                    throw new InvalidOperationException(
                        $"Invalid variable output count on {op.Opcode} in {block.ID}");
            }

            List<Variable> exitStack = block.outgoingEdges.Count == 0
                ? []
                : [.. block.outgoingEdges[0].Target.entryStackVariables];
            foreach (ControlFlowEdge edge in block.outgoingEdges.Skip(1))
            {
                if (!exitStack.SequenceEqual(edge.Target.entryStackVariables))
                {
                    throw new InvalidOperationException(
                        $"Successors of {block.ID} require different exit stacks");
                }
            }

            exitStacks.Add(block, exitStack);
        }

        return exitStacks;
    }

    // Postcondition: block.ops is an executable stack-form sequence that realizes every
    // canonical operand use and leaves the successor entry stack on the CIL stack.
    private void ConvertBlock(BasicBlock block, List<Variable> exitStack)
    {
        List<Variable> stack = [.. block.entryStackVariables];
        Dictionary<Variable, int> remainingUses = CountRemainingUses(block, exitStack);
        List<Op> operations = [];

        for (int operationIndex = 0; operationIndex < block.ops.Count; operationIndex++)
        {
            Op op = block.ops[operationIndex];
            List<Variable> inputs = [.. op.inputs.Take(op.stackInputCount)];
            foreach (Variable input in op.inputs)
                RemoveUse(remainingUses, input);

            if (TryConvertLogicalLoad(op, stack, remainingUses, operations, block))
                continue;

            // No operation appended after a non-fallthrough terminator executes on every outgoing
            // path. Arrange both its surviving exit stack and consumed operands before it.
            bool finalControlTransfer = operationIndex == block.ops.Count - 1 &&
                                        (op.CanBranch || !op.CanFallThrough);
            List<Variable> required = finalControlTransfer
                ? [.. exitStack, .. inputs]
                : inputs;
            IEnumerable<Variable> produced = finalControlTransfer
                ? exitStack.Concat(op.outputs.Take(op.stackOutputCount))
                : op.outputs.Take(op.stackOutputCount);

            ArrangeStack(
                stack,
                required,
                remainingUses,
                operations,
                block,
                produced,
                finalControlTransfer);

            stack.RemoveRange(stack.Count - inputs.Count, inputs.Count);
            operations.Add(ConvertOperation(op));
            stack.AddRange(op.outputs.Take(op.stackOutputCount));

            foreach (Variable output in op.outputs.Take(op.stackOutputCount).Distinct())
            {
                if (!crossBlockSpills.Contains(output))
                    continue;
                if (stack.Count == 0 || stack[^1] != output)
                {
                    throw new NotSupportedException(
                        $"Cannot spill non-top multi-output value {output} at its definition");
                }

                // Store a duplicate so the operation's natural evaluation-stack result is
                // unchanged when this block still consumes it. If only another block uses the
                // value, consume the existing stack copy: retaining it merely forces later
                // operands to be spilled while the block restores its required exit stack.
                if (remainingUses.ContainsKey(output))
                    operations.Add(Ops.Dup);
                else
                    stack.RemoveAt(stack.Count - 1);
                operations.Add(StoreVariable(output));
            }
            if (op.ClearsStack)
                stack.Clear();
        }

        foreach (var variable in exitStack)
            RemoveUse(remainingUses, variable);
        ArrangeStack(stack, exitStack, remainingUses, operations, block, exact: true);

        if (remainingUses.Count != 0)
            throw new InvalidOperationException($"Unaccounted variable uses remain in {block.ID}");

        block.ops.Clear();
        block.ops.AddRange(operations);
    }

    // These counts are scheduling obligations: the final available copy of a value cannot be
    // consumed or popped while any operation or exit slot still needs it.
    private static Dictionary<Variable, int> CountRemainingUses(
        BasicBlock block,
        IEnumerable<Variable> exitStack)
    {
        Dictionary<Variable, int> uses = [];
        foreach (var variable in block.ops.SelectMany(op => op.inputs)
                     .Concat(exitStack))
            uses[variable] = uses.GetValueSafe(variable) + 1;
        return uses;
    }

    // A Variables-form rewrite may make ldloc/ldarg name a pure logical temporary. There is no
    // physical storage to load in that case. Schedule the source value onto the stack and reinterpret
    // the value already there as the load's output; ArrangeStack performs any required dup/spill/load.
    private bool TryConvertLogicalLoad(
        Op op,
        List<Variable> stack,
        IReadOnlyDictionary<Variable, int> remainingUses,
        List<Op> operations,
        BasicBlock block)
    {
        if (op.GetStorageAccess() is not { Kind: Op.VariableAccessKind.Read } access ||
            HasStorage(access.Variable))
        {
            return false;
        }

        if (op.stackInputCount != 0 || op.stackOutputCount != 1)
            throw new InvalidOperationException("A logical storage load has an unexpected stack shape");

        Variable output = op.outputs[0];
        ArrangeStack(
            stack,
            [access.Variable],
            remainingUses,
            operations,
            block,
            [output]);
        stack.RemoveAt(stack.Count - 1);
        stack.Add(output);
        return true;
    }

    private static void RemoveUse(Dictionary<Variable, int> uses, Variable variable)
    {
        int count = uses[variable] - 1;
        if (count == 0)
            uses.Remove(variable);
        else
            uses[variable] = count;
    }

    // Postcondition: required is on top of the modeled stack (or is the entire stack when
    // exact), and operations contains the spills, pops, and reloads needed to make it so.
    private void ArrangeStack(
        List<Variable> stack,
        IReadOnlyList<Variable> required,
        IReadOnlyDictionary<Variable, int> futureUses,
        List<Op> operations,
        BasicBlock block,
        IEnumerable<Variable>? producedVariables = null,
        bool exact = false)
    {
        int prefixCount = stack.Count - required.Count;
        bool inputsAlreadyOnTop = exact
            ? stack.SequenceEqual(required)
            : prefixCount >= 0 && stack.Skip(prefixCount).SequenceEqual(required);

        if (inputsAlreadyOnTop && CanConsumeWithoutLosingValues(
                stack, prefixCount, required, producedVariables ?? [], futureUses))
            return;

        if (!exact)
        {
            // Existing values below an operation's inputs may remain on the evaluation stack. Find
            // the longest prefix of the required inputs already at the top, then append a suffix
            // which can be reloaded. This covers both [saved] + [argument] and [managed pointer] +
            // [argument] without spilling the values which are already in their final positions.
            int maximumMatched = Math.Min(stack.Count, required.Count);
            for (int matched = maximumMatched; matched >= 0; matched--)
            {
                IEnumerable<Variable> stackSuffix = stack.Skip(stack.Count - matched);
                if (!stackSuffix.SequenceEqual(required.Take(matched)))
                    continue;

                IReadOnlyList<Variable> appended = [.. required.Skip(matched)];
                if (appended.Any(variable => !CanReload(variable)))
                    continue;

                List<Variable> extendedStack = [.. stack, .. appended];
                int extendedPrefixCount = stack.Count - matched;
                if (extendedPrefixCount > 0 &&
                    !futureUses.ContainsKey(stack[extendedPrefixCount - 1]))
                {
                    // A dead value at the top of the preserved prefix can be discarded now. Keeping
                    // it only postpones cleanup and may obstruct the next operation's stack layout.
                    continue;
                }
                if (!CanConsumeWithoutLosingValues(
                        extendedStack,
                        extendedPrefixCount,
                        required,
                        producedVariables ?? [],
                        futureUses))
                {
                    continue;
                }

                foreach (Variable variable in appended)
                {
                    operations.Add(LoadVariable(variable, block));
                    stack.Add(variable);
                }
                return;
            }
        }

        // If there is one input which is already on top of the stack, but will be needed later, just 'dup' it.
        if (inputsAlreadyOnTop && required.Count == 1)
        {
            stack.Add(stack[^1]);
            operations.Add(Ops.Dup);
            return;
        }

        HashSet<Variable> needed = [.. required, .. futureUses.Keys];
        int keepCount = exact ? 0 : Math.Max(prefixCount, 0);
        foreach (var variable in required.Distinct())
        {
            if (CanReload(variable))
                continue;

            int availableIndex = stack.LastIndexOf(variable);
            if (availableIndex >= 0 && availableIndex < keepCount)
                keepCount = availableIndex;
        }

        try
        {
            EmptyStack(stack, keepCount, needed, operations);
        }
        catch (InvalidOperationException exception)
        {
            throw new InvalidOperationException(
                $"Cannot arrange stack [{string.Join(", ", stack)}] as [{string.Join(", ", required)}] in {block.ID}",
                exception);
        }
        foreach (var variable in required)
        {
            operations.Add(LoadVariable(variable, block));
            stack.Add(variable);
        }
    }

    // A ready-made stack layout is usable only if consuming it leaves some representation of
    // every value that still has future uses, either in storage or on the stack.
    private bool CanConsumeWithoutLosingValues(
        IReadOnlyList<Variable> stack,
        int prefixCount,
        IEnumerable<Variable> consumedVariables,
        IEnumerable<Variable> producedVariables,
        IReadOnlyDictionary<Variable, int> futureUses)
    {
        var produced = producedVariables.ToList();
        foreach (var variable in consumedVariables.Distinct())
        {
            futureUses.TryGetValue(variable, out int useCount);
            if (CanReload(variable))
                continue;

            int remainingStackCopies = stack.Take(prefixCount).Count(candidate => candidate == variable) +
                                       produced.Count(candidate => candidate == variable);
            // One surviving copy is sufficient even for several later uses: a later dup may
            // create those copies, and preserving the existing stack schedule should not spill.
            if (useCount > 0 && remainingStackCopies == 0)
                return false;
        }

        return true;
    }

    // Discard the selected suffix while materializing any value whose last stack copy is still
    // needed. The stack model is updated in lockstep with the emitted operations.
    private void EmptyStack(
        List<Variable> stack,
        int keepCount,
        ISet<Variable> needed,
        List<Op> operations)
    {
        for (int index = stack.Count - 1; index >= keepCount; index--)
        {
            Variable variable = stack[index];
            if (needed.Contains(variable) && !CanReload(variable))
                operations.Add(StoreVariable(variable));
            else
                operations.Add(Ops.Pop);
        }

        stack.RemoveRange(keepCount, stack.Count - keepCount);
    }

    // Storage accesses must follow their canonical variable operand, which an optimization may
    // have changed independently of the original instruction's numeric operand.
    private Op ConvertOperation(Op op)
    {
        Op.StorageAccess? access = op.GetStorageAccess();
        Op operation;
        if (access is not { } variableAccess)
        {
            operation = new(op.Opcode, op.Operand, op.Prefixes);
        }
        else if (IsOriginalStorage(variableAccess.VariableKind, variableAccess.Variable, op.Index))
        {
            operation = new(op.Opcode, op.Operand, op.Prefixes);
        }
        else
        {
            Storage storage = GetStorage(variableAccess.Variable);
            operation = variableAccess.Kind switch
            {
                Op.VariableAccessKind.Read => LoadStorage(storage, op.Prefixes),
                Op.VariableAccessKind.Write => StoreStorage(storage, op.Prefixes),
                Op.VariableAccessKind.Address => LoadStorageAddress(storage, op.Prefixes),
                _ => throw new ArgumentOutOfRangeException(),
            };
        }

        return operation;
    }

    private static bool IsOriginalStorage(VariableKind encodedKind, Variable variable, int originalIndex) =>
        variable.index == originalIndex && variable.kind == encodedKind;

    private bool HasStorage(Variable variable) =>
        variable.kind is VariableKind.Argument or VariableKind.Local ||
        storedVariables.Contains(variable) ||
        crossBlockSpills.Contains(variable) ||
        spillStorage.ContainsKey(variable);

    private bool CanReload(Variable variable) =>
        variable.type == typeof(TypeLattice.NullType) || HasStorage(variable);

    // Original arguments and locals retain their storage identity. Logical values share one
    // lazily declared spill local across all blocks that preserve or reload that value.
    private Storage GetStorage(Variable variable)
    {
        switch (variable.kind)
        {
            case VariableKind.Argument: return new(VariableKind.Argument, variable.index, null);
            case VariableKind.Local: return new(VariableKind.Local, variable.index, variable.localBuilder);
        }

        if (spillStorage.TryGetValue(variable, out Storage storage))
            return storage;

        if (variable.preferredStorage is { kind: VariableKind.Local } preferred &&
            !occupiedPreferredStorage.Contains(preferred))
        {
            storage = new(VariableKind.Local, preferred.index, preferred.localBuilder);
            occupiedPreferredStorage.Add(preferred);
            spillStorage.Add(variable, storage);
            return storage;
        }

        if (variable.type == null || TypeLattice.IsSpecialType(variable.type))
            throw new InvalidOperationException($"Cannot spill {variable}: its exact CIL type is unknown");
        // TODO: Extend the conservative original-slot reuse above with liveness-based coalescing so
        //       noninterfering SSA versions can share it, then reuse other dead locals as well.
        LocalBuilder local = optimizer.generator.DeclareLocal(variable.type);
        storage = new(VariableKind.Local, local.LocalIndex, local);
        spillStorage.Add(variable, storage);
        return storage;
    }

    private Op LoadVariable(Variable variable, BasicBlock block)
    {
        // The transient null type has no corresponding local signature. Since every definition
        // of a NullType variable is the same value, rematerialization is both exact and cheaper.
        if (variable.type == typeof(TypeLattice.NullType))
            return new Op(OpCodes.Ldnull);
        if (!HasStorage(variable))
            throw new InvalidOperationException($"{variable} is not available when rebuilding the stack in {block.ID}");
        return LoadStorage(GetStorage(variable), []);
    }

    private Op StoreVariable(Variable variable) => StoreStorage(GetStorage(variable), []);

    private static Op LoadStorage(Storage storage, IReadOnlyList<Op> prefixes) => storage.Kind switch
    {
        VariableKind.Argument => new(OpCodes.Ldarg, storage.Index, prefixes),
        VariableKind.Local => new(OpCodes.Ldloc, storage.Operand, prefixes),
        _ => throw new ArgumentOutOfRangeException(),
    };

    private static Op StoreStorage(Storage storage, IReadOnlyList<Op> prefixes) => storage.Kind switch
    {
        VariableKind.Argument => new(OpCodes.Starg, storage.Index, prefixes),
        VariableKind.Local => new(OpCodes.Stloc, storage.Operand, prefixes),
        _ => throw new ArgumentOutOfRangeException(),
    };

    private static Op LoadStorageAddress(Storage storage, IReadOnlyList<Op> prefixes) => storage.Kind switch
    {
        VariableKind.Argument => new(OpCodes.Ldarga, storage.Index, prefixes),
        VariableKind.Local => new(OpCodes.Ldloca, storage.Operand, prefixes),
        _ => throw new ArgumentOutOfRangeException(),
    };

    private readonly record struct Storage(VariableKind Kind, int Index, LocalBuilder? LocalBuilder)
    {
        public object Operand => (object?)LocalBuilder ?? Index;
    }
}
