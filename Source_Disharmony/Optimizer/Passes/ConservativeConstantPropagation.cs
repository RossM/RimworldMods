namespace Disharmony.Optimizer.Passes;

/// <summary>
///     Propagates values held by single-assignment locals before SSA construction. This pass
///     exists primarily for the temporary managed-reference locals introduced while inlining
///     patches: replacing those locals early exposes their target arguments/locals to later
///     promotion and keeps the eventual SSA graph smaller.
/// </summary>
internal sealed class ConservativeConstantPropagation(Optimizer optimizer) : Pass
{
    private readonly record struct OperationUse(Op Operation, int InputIndex);

    private readonly record struct RematerializableValue(Op Producer, ConstantValue Constant);

    /// <summary>
    ///     The result of recognizing an indirect access through a known managed reference.
    ///     Instances have passed all checks required by <see cref="MakeDirectStorageAccess"/>.
    /// </summary>
    private readonly record struct DirectAccess(Op Indirect, Variable Storage, Op.VariableAccessKind Kind);

    private sealed class Candidate(
        Op definition,
        RematerializableValue value,
        IReadOnlyList<Op> reads)
    {
        public Op Definition { get; } = definition;
        public RematerializableValue Value { get; } = value;
        public IReadOnlyList<Op> Reads { get; } = reads;
    }

    private readonly Dictionary<Op, BasicBlock> blockByOperation = [];
    private readonly MultiDictionary<Variable, Op> definitions = new();
    private readonly MultiDictionary<Variable, OperationUse> uses = new();
    private readonly MultiDictionary<Variable, (Op Operation, Op.StorageAccess Access)> storageAccesses = new();

    /// <summary>
    ///     Preconditions: the optimizer is in regular Variables form and all CFG edges are
    ///     empty, and dead-code removal has run since the last CFG mutation so every block and
    ///     operation is reachable and uniquely owned. Postconditions: every propagated local
    ///     access has been replaced by an equivalent rematerialized constant or direct storage
    ///     access; the form and CFG are unchanged, and <see cref="Variable.addressTaken"/>
    ///     describes the rewritten IR.
    /// </summary>
    public override void Run()
    {
        CheckPreconditions();
        DominatorTree dominators = optimizer.ComputeDominatorTreeIfNeeded();

        // A propagated reference can expose another singleton reference local. Rebuilding the
        // deliberately small conservative index is simpler and safer than incrementally
        // maintaining it while operations are removed and replaced.
        while (PropagateOnce(dominators)) { }

        RecomputeAddressTaken();
    }

    private void CheckPreconditions()
    {
        if (optimizer.Form != Optimizer.IrForm.Variables)
            throw new InvalidOperationException("Conservative constant propagation requires regular variable form");

        foreach (var block in optimizer.basicBlocks)
        {
            foreach (var edge in block.outgoingEdges)
            {
                if (edge.assignments.Count != 0)
                {
                    throw new InvalidOperationException(
                        $"Conservative constant propagation requires empty edge {edge.Source.ID} => {edge.Target.ID}");
                }
            }

            foreach (var op in block.ops)
            {
                if (op.stackInputCount < 0 || op.stackInputCount > op.inputs.Count)
                    throw new InvalidOperationException(
                        $"Invalid variable input count on {op.Opcode} in {block.ID}");
                if (op.stackOutputCount < 0 || op.stackOutputCount > op.outputs.Count)
                    throw new InvalidOperationException(
                        $"Invalid variable output count on {op.Opcode} in {block.ID}");
            }
        }
    }

    private bool PropagateOnce(DominatorTree dominators)
    {
        BuildConservativeIndex();
        List<Candidate> candidates = FindCandidates(dominators);
        if (candidates.Count == 0)
            return false;

        Dictionary<Op, Op> replacements = [];
        HashSet<Op> removals = [];
        foreach (var candidate in candidates)
        {
            foreach (var read in candidate.Reads)
            {
                if (TryGetDirectAccess(candidate, read, out DirectAccess directAccess))
                {
                    Op replacement = MakeDirectStorageAccess(directAccess);
                    replacements.Add(directAccess.Indirect, replacement);
                    removals.Add(read);
                    continue;
                }

                replacements.Add(read, Rematerialize(candidate.Value.Producer, read));
            }

            removals.Add(candidate.Definition);
            removals.Add(candidate.Value.Producer);
        }

        foreach (var block in optimizer.basicBlocks)
        {
            for (int index = block.ops.Count - 1; index >= 0; index--)
            {
                Op operation = block.ops[index];
                if (removals.Contains(operation))
                    block.ops.RemoveAt(index);
                else if (replacements.TryGetValue(operation, out Op? replacement))
                    block.ops[index] = replacement;
            }
        }

        return true;
    }

    /// <summary>
    ///     Builds intentionally conservative def-use information: every syntactic definition
    ///     is considered capable of reaching every use. This loses opportunities in exchange
    ///     for avoiding a full reaching-definitions analysis before SSA; candidates are
    ///     accepted only when that conservative view finds one definition and that definition
    ///     dominates every read.
    /// </summary>
    private void BuildConservativeIndex()
    {
        blockByOperation.Clear();
        definitions.Clear();
        uses.Clear();
        storageAccesses.Clear();

        foreach (var block in optimizer.basicBlocks)
        {
            foreach (var operation in block.ops)
            {
                blockByOperation.Add(operation, block);
                for (int index = 0; index < operation.inputs.Count; index++)
                    uses.Add(operation.inputs[index], new(operation, index));

                // dup outputs alias its input rather than defining a value. Treating them as
                // definitions would only lose opportunities, but recording the correct fact is
                // important for future consumers of this indexing pattern.
                if (operation.Opcode != OpCodes.Dup)
                {
                    foreach (var output in operation.outputs)
                        definitions.Add(output, operation);
                }

                if (operation.GetStorageAccess() is { } access)
                    storageAccesses.Add(access.Variable, (operation, access));
            }
        }
    }

    private List<Candidate> FindCandidates(DominatorTree dominators)
    {
        List<Candidate> candidates = [];
        foreach (var storage in optimizer.localVariables.Values)
        {
            if (!storageAccesses.TryGetValues(storage, out var accesses))
                continue;

            Op[] writes =
            [
                .. accesses.Where(item => item.Access.Kind == Op.VariableAccessKind.Write)
                    .Select(item => item.Operation),
            ];
            Op[] reads =
            [
                .. accesses.Where(item => item.Access.Kind == Op.VariableAccessKind.Read)
                    .Select(item => item.Operation),
            ];
            if (writes is not [var definition])
                continue;
            if (reads.Length == 0)
                continue;

            // Taking the temporary's own address would make its storage identity observable.
            if (accesses.Any(item => item.Access.Kind == Op.VariableAccessKind.Address))
                continue;

            // A prefix belongs to the stloc and cannot be transferred to rematerialized values.
            if (definition.Prefixes.Count != 0)
                continue;

            // Storage writes recognized by this pass consume exactly one stack value.
            if (definition.stackInputCount != 1)
                continue;

            if (!TryGetRematerializableValue(storage, definition, out RematerializableValue value))
                continue;

            // One syntactic assignment does not imply that it reaches every read: a normal
            // entry path or an exception handler may observe the local's entry value instead.
            if (!DefinitionDominatesEveryRead(definition, reads, dominators))
                continue;

            // A prefix belongs to the ldloc being replaced, not to the value substituted for it.
            if (reads.Any(read => read.Prefixes.Count != 0))
                continue;

            candidates.Add(new(definition, value, reads));
        }

        return candidates;
    }

    /// <summary>
    ///     Checks whether <paramref name="definition"/> stores the result of a producer which
    ///     has no other uses. The producer must be a literal or address that this pass can
    ///     recreate at each load of <paramref name="storage"/>. The producer must appear earlier
    ///     in the same block, but need not be adjacent: patch setup commonly pushes several
    ///     arguments before storing them in reverse order. The caller must separately check that
    ///     the store executes before every load.
    /// </summary>
    private bool TryGetRematerializableValue(
        Variable storage,
        Op definition,
        out RematerializableValue value)
    {
        value = default;
        Variable source = definition.inputs[0];

        if (!definitions.TryGetValues(source, out var sourceDefinitions))
            return false;
        if (sourceDefinitions is not [var producer])
            return false;

        // Removing the producer is valid only if the stloc is its sole consumer.
        if (!uses.TryGetValues(source, out var sourceUses))
            return false;
        if (sourceUses is not [var soleUse])
            return false;
        if (soleUse.Operation != definition)
            return false;

        // Producer prefixes cannot be copied onto every rematerialized use in general.
        if (producer.Prefixes.Count != 0)
            return false;

        BasicBlock block = blockByOperation[definition];
        if (blockByOperation[producer] != block)
            return false;
        if (block.ops.IndexOf(producer) >= block.ops.IndexOf(definition))
            return false;

        // Literals and address loads have the simple zero-input, one-output stack shape.
        if (producer.stackInputCount != 0 || producer.stackOutputCount != 1)
            return false;

        if (producer.GetStorageAccess() is
            { Kind: Op.VariableAccessKind.Address, Variable: var referencedStorage })
        {
            if (producer.outputs[0].type != storage.type)
                return false;
            value = new(producer, ConstantValue.ReferenceTo(referencedStorage));
            return true;
        }

        if (!producer.TryGetLiteral(out ConstantValue? constant))
            return false;
        if (!CanRoundTripLiteralThroughStorage(constant, storage.type))
            return false;

        value = new(producer, constant);
        return true;
    }

    /// <summary>
    ///     Returns whether the unique definition executes before every read. Block dominance
    ///     handles reads in other blocks; operation order supplies the stronger instruction-
    ///     level fact required for reads in the definition's own block.
    /// </summary>
    private bool DefinitionDominatesEveryRead(
        Op definition,
        IReadOnlyList<Op> reads,
        DominatorTree dominators)
    {
        BasicBlock definitionBlock = blockByOperation[definition];
        int definitionIndex = definitionBlock.ops.IndexOf(definition);
        foreach (var read in reads)
        {
            BasicBlock readBlock = blockByOperation[read];
            if (readBlock != definitionBlock)
            {
                if (!dominators.Dominates(definitionBlock, readBlock))
                    return false;
                continue;
            }

            if (definitionIndex >= readBlock.ops.IndexOf(read))
                return false;
        }

        return true;
    }

    // CIL stores can truncate or otherwise normalize values according to the declared local
    // type. Limit this early pass to cases where removing the store/load round trip plainly
    // preserves the value; SSA-era folding can support richer conversions later.
    private static bool CanRoundTripLiteralThroughStorage(ConstantValue constant, Type? storageType) =>
        constant.Kind switch
        {
            ConstantValueKind.Null => storageType is { IsValueType: false },
            ConstantValueKind.Int32 => storageType == typeof(int),
            ConstantValueKind.Int64 => storageType == typeof(long),
            ConstantValueKind.Float32 => storageType == typeof(float),
            ConstantValueKind.Float64 => storageType == typeof(double),
            ConstantValueKind.String => storageType == typeof(string),
            _ => false,
        };

    /// <summary>
    ///     Recognizes a load followed by one indirect access through a known reference. This
    ///     method only inspects the IR; construction and registration of the replacement happen
    ///     after it succeeds.
    /// </summary>
    private bool TryGetDirectAccess(Candidate candidate, Op read, out DirectAccess directAccess)
    {
        directAccess = default;
        if (candidate.Value.Constant.Kind != ConstantValueKind.ManagedReference)
            return false;
        if (read.stackOutputCount != 1)
            return false;

        Variable address = read.outputs[0];
        if (!uses.TryGetValues(address, out var addressUses))
            return false;
        if (addressUses is not [var (operation, inputIndex)])
            return false;

        // The sole consumer must use this value as its address, which is the first stack input
        // of every ldobj/stobj and ldind/stind operation.
        if (inputIndex != 0)
            return false;

        // Require straight-line execution from the load to its consumer rather than reasoning
        // about an address crossing the CFG or flowing around a loop.
        BasicBlock block = blockByOperation[read];
        if (blockByOperation[operation] != block)
            return false;
        if (block.ops.IndexOf(read) >= block.ops.IndexOf(operation))
            return false;

        Variable storage = candidate.Value.Constant.GetReferencedVariable();
        if (!TryClassifyDirectAccess(operation, address, storage, out Op.VariableAccessKind kind))
            return false;

        directAccess = new(operation, storage, kind);
        return true;
    }

    /// <summary>
    ///     Checks whether an indirect operation has exactly the opcode, type, operands, and
    ///     stack shape needed to replace it with a direct access to <paramref name="storage"/>.
    /// </summary>
    private static bool TryClassifyDirectAccess(
        Op indirect,
        Variable address,
        Variable storage,
        out Op.VariableAccessKind kind)
    {
        kind = default;

        // Indirect-access prefixes such as volatile. and unaligned. do not apply to locals or
        // arguments and cannot be transferred to the replacement.
        if (indirect.Prefixes.Count != 0)
            return false;
        if (storage.type is not Type storageType)
            return false;

        if (indirect.GetIndirectAccessKind() is not { } accessKind)
            return false;

        // Direct local/argument access is equivalent only when the indirect opcode preserves
        // the storage type rather than extending, truncating, or reinterpreting it.
        if (!IndirectTypeMatchesStorage(indirect, storageType))
            return false;
        if (indirect.inputs.Count == 0)
            return false;
        if (indirect.inputs[0] != address)
            return false;

        if (accessKind == Op.VariableAccessKind.Read)
        {
            if (indirect.stackInputCount != 1 || indirect.stackOutputCount != 1)
                return false;
        }
        else if (indirect.stackInputCount != 2 || indirect.stackOutputCount != 0)
        {
            return false;
        }

        kind = accessKind;
        return true;
    }

    /// <summary>
    ///     Builds the direct operation described by a successfully classified
    ///     <see cref="DirectAccess"/>. This method performs no eligibility checks.
    /// </summary>
    private static Op MakeDirectStorageAccess(DirectAccess access)
    {
        Op indirect = access.Indirect;
        Variable storage = access.Storage;
        object operand = storage switch
        {
            ArgumentVariable argumentVariable => argumentVariable.index,
            LocalVariable localVariable => (object?)localVariable.localBuilder ?? localVariable.index,
            _ => throw new InvalidOperationException($"Known reference targets non-storage variable {storage}"),
        };

        Op direct;
        switch (access.Kind)
        {
            case Op.VariableAccessKind.Read:
                direct = new(storage.Kind == VariableKind.Argument ? OpCodes.Ldarg : OpCodes.Ldloc, operand, [])
                {
                    stackInputCount = 0,
                    stackOutputCount = 1,
                };
                direct.inputs.Add(storage);
                direct.outputs.AddRange(indirect.outputs);
                break;

            case Op.VariableAccessKind.Write:
                direct = new(storage.Kind == VariableKind.Argument ? OpCodes.Starg : OpCodes.Stloc, operand, [])
                {
                    stackInputCount = 1,
                    stackOutputCount = 0,
                };
                direct.inputs.Add(indirect.inputs[1]);
                direct.outputs.Add(storage);
                break;

            default:
                throw new ArgumentOutOfRangeException();
        }

        return direct;
    }

    // Signedness matters for 1- and 2-byte loads: ldloc extends according to the declared
    // storage type, while ldind selects sign- or zero-extension through its opcode. Stores only
    // truncate, and 4-byte-or-larger loads have no signedness-dependent extension.
    private static bool IndirectTypeMatchesStorage(Op operation, Type storageType) =>
        operation.OpcodeValue switch
        {
            OpCodeValues.Ldobj or OpCodeValues.Stobj => operation.Operand is Type type && type == storageType,
            OpCodeValues.Ldind_I1 => storageType == typeof(sbyte),
            OpCodeValues.Ldind_U1 => storageType == typeof(byte) || storageType == typeof(bool),
            OpCodeValues.Stind_I1 =>
                storageType == typeof(sbyte) || storageType == typeof(byte) || storageType == typeof(bool),
            OpCodeValues.Ldind_I2 => storageType == typeof(short),
            OpCodeValues.Ldind_U2 => storageType == typeof(ushort) || storageType == typeof(char),
            OpCodeValues.Stind_I2 =>
                storageType == typeof(short) || storageType == typeof(ushort) || storageType == typeof(char),
            OpCodeValues.Ldind_I4 or OpCodeValues.Ldind_U4 or OpCodeValues.Stind_I4 =>
                storageType == typeof(int) || storageType == typeof(uint),
            OpCodeValues.Ldind_I8 or OpCodeValues.Stind_I8 =>
                storageType == typeof(long) || storageType == typeof(ulong),
            OpCodeValues.Ldind_I or OpCodeValues.Stind_I =>
                storageType == typeof(IntPtr) || storageType == typeof(UIntPtr),
            OpCodeValues.Ldind_R4 or OpCodeValues.Stind_R4 => storageType == typeof(float),
            OpCodeValues.Ldind_R8 or OpCodeValues.Stind_R8 => storageType == typeof(double),
            OpCodeValues.Ldind_Ref or OpCodeValues.Stind_Ref => storageType is { IsValueType: false, IsByRef: false },
            _ => false,
        };

    private static Op Rematerialize(Op producer, Op read)
    {
        var replacement = new Op(producer.Opcode, producer.Operand, [])
        {
            stackInputCount = producer.stackInputCount,
            stackOutputCount = read.stackOutputCount,
        };
        replacement.inputs.AddRange(producer.inputs);
        replacement.outputs.AddRange(read.outputs);
        return replacement;
    }

    private void RecomputeAddressTaken()
    {
        foreach (var variable in optimizer.argumentVariables.Values.Concat<Variable>(optimizer.localVariables.Values))
            variable.addressTaken = false;
        foreach (var operation in optimizer.Ops)
        {
            if (operation.GetStorageAccess() is { Kind: Op.VariableAccessKind.Address, Variable: var variable })
                variable.addressTaken = true;
        }
    }
}
