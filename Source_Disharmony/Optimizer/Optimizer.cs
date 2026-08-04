using Disharmony.Optimizer.Passes;

namespace Disharmony.Optimizer;

/// <summary>
///     Owns one shared IR whose canonical interpretation changes over the pipeline. The normal state
///     sequence is: no IR; unordered Stack form after MakeBasicBlocks; regular Variables form after
///     ConvertStackToVariables; SSA Variables form between ConstructSsa and DestructSsa; regular
///     Variables form again after DestructSsa; Stack form after ConvertVariablesToStack; emission-ordered
///     Stack form after AggressiveDeadCodeEliminationAndReorder and InsertBranches; then canonical
///     output after Emit. Every form uses the same blocks and operations; <see cref="IrForm" />
///     selects which fields are canonical at each pass boundary.
///     These are pass-boundary invariants: conversion workers temporarily build the destination
///     representation before changing Form, but no other pass may observe that mixed state.
/// </summary>
internal class Optimizer
{
    /// <summary>
    ///     Which interpretation of the shared block and operation data is canonical. The same
    ///     structures are used in every form. An SSA function with no joins can have no edge
    ///     assignments, so assignment-list contents are not a sufficient form discriminator.
    /// </summary>
    internal enum IrForm
    {
        /// <summary>
        ///     Operations are executable CIL stack operations. Variable operands, counts, block
        ///     entry stacks, the variable registry, and edge assignments are empty/non-canonical.
        /// </summary>
        Stack,

        /// <summary>
        ///     Regular, non-SSA Variables form. Operation operands are canonical explicit variables,
        ///     stack operand counts retain the original CIL stack arity needed to lower back to Stack
        ///     form, and every CFG edge assignment list is empty.
        /// </summary>
        Variables,

        /// <summary>
        ///     SSA Variables form. Reads and writes of promoted storage have been removed and their
        ///     producers are wired directly to consumers; incoming-edge assignments encode phi
        ///     operands in parallel. Unpromoted storage and imprecisely typed stack slots remain
        ///     mutable and may still have several definitions.
        /// </summary>
        Ssa,
    }

    // Factories rather than shared instances: passes freely attach variable operands and prefixes
    // to returned Ops, so every use must receive a fresh object.

    // Read-only collection views for passes and tests. Their elements remain mutable optimizer IR;
    // callers must respect the canonical-state and pass-precondition comments on the backing fields.
    internal IReadOnlyList<BasicBlock> BasicBlocks => basicBlocks;
    internal IReadOnlyList<Region> Regions => regions;
    internal IReadOnlyList<ExceptionEntryGroup> ExceptionEntryGroups => exceptionEntryGroups;
    internal IReadOnlyList<Variable> Variables => variables;
    internal IReadOnlyDictionary<int, Variable> ArgumentVariables => argumentVariables;
    internal IReadOnlyDictionary<int, Variable> LocalVariables => localVariables;

    // Canonical output only after Emit completes. Emit appends to this collection and therefore
    // requires it to be empty on entry; earlier passes must use the block/operation IR instead.
    public readonly InstructionList outputInstructions = [];

    // Canonical lexical hierarchy nodes after MakeBasicBlocks, including root exactly once. This
    // list records membership, not nesting or layout; parent/entry record nesting and the aggressive
    // reorder postcondition gives basicBlocks its eventual emission layout.
    internal readonly List<Region> regions = [];

    // Canonical exception-group membership and handler/filter order after MakeBasicBlocks. The
    // normal CFG intentionally contains no implicit exceptional edges represented by these groups.
    private readonly List<ExceptionEntryGroup> exceptionEntryGroups = [];

    // Canonical normal-CFG node set after MakeBasicBlocks. Membership does not imply reachability:
    // CFG rewrites such as JumpThreading and MergeBlocks deliberately leave dead blocks for a later
    // removal pass. List order is initially input order and is non-canonical for analysis; only
    // AggressiveDeadCodeEliminationAndReorder establishes the final canonical emission order.
    internal List<BasicBlock> basicBlocks = [];

    // Null means block dominance has not been computed or has been invalidated. A non-null tree is
    // canonical for the current block set, edge endpoints, and implicit exception-entry roots;
    // operation, IR-form, and block-order changes do not invalidate it. Edge mutations must use the
    // helpers below; block-set or implicit-root mutations must explicitly clear this cache.
    // Computation is explicit at pass entry, never hidden in a property.
    private DominatorTree? dominatorTree;

    // Canonical in regular and SSA Variables forms and empty in Stack form. variables owns every
    // current Variable. The two dictionaries are consistent subsets: each maps a physical slot
    // index to the one Argument/Local Variable for that slot, and every mapped value occurs in
    // variables. Logical values occur only in variables. nextVariableId is the next identity in
    // this interval.
    internal readonly List<Variable> variables = [];
    internal readonly Dictionary<int, Variable> argumentVariables = [];
    internal readonly Dictionary<int, Variable> localVariables = [];
    internal int nextVariableId;

    // Stable synthetic hierarchy root. It becomes canonical when MakeBasicBlocks adds it to regions
    // and sets its entry; it has no parent, Harmony marker, or exception-entry group.
    private readonly Region root = new();

    // Shared ID allocator for BasicBlock and non-root Region nodes. IDs are stable and never reused.
    private int nextBlockId = 1;

    // Guard checked by Optimize. The current constructor sets it after deriving signature state, so
    // it is true for every successfully constructed instance; the false value is presently only a
    // reserved/legacy state rather than an observable optimizer phase.
    private readonly bool valid = false;

    // Immutable input/context. inputInstructions is authoritative only until MakeBasicBlocks builds
    // the IR. parameterTypes includes the instance receiver at index zero when method.HasThis.
    private readonly MethodBase method;
    private readonly List<CodeInstruction> inputInstructions;
    internal readonly ILGenerator generator;
    private readonly bool debug;
    internal readonly List<Type> parameterTypes;
    internal readonly Type returnType;

    public Optimizer(MethodBase method, List<CodeInstruction> inputInstructions, ILGenerator generator, bool debug)
    {
        this.method = method;
        this.inputInstructions = inputInstructions;
        this.generator = generator;
        this.debug = debug;

        if (method.HasThis)
            parameterTypes = [method.DeclaringType.CallableType, .. method.GetParameters().Types()];
        else
            parameterTypes = [.. method.GetParameters().Types()];

        returnType = method is MethodInfo methodInfo ? methodInfo.ReturnType : typeof(void);

        valid = true;
    }

    // Meaningful once MakeBasicBlocks has created the IR. Defaults to Stack; each conversion pass
    // changes it only after completing the destination representation. It is the authoritative
    // regular-versus-SSA discriminator even when SSA happens to contain no phi assignments.
    internal IrForm Form { get; set; }

    private static bool IsBlockStart(ExceptionBlock b) => b.blockType != ExceptionBlockType.EndExceptionBlock;
    private static bool IsBlockEnd(ExceptionBlock b) => b.blockType == ExceptionBlockType.EndExceptionBlock;

    private void LogInstructions(string phase, IEnumerable<CodeInstruction> instructions)
    {
        if (!debug)
            return;

        int codePos = 0;

        FileLog.LogBuffered($"### Optimizer {phase}: {method.FullDescription()}");

        foreach (var codeInstruction in instructions)
            LogInstruction(codeInstruction, ref codePos);

        FileLog.LogBuffered("");
        FileLog.FlushBuffer();
    }

    /// <summary>
    ///     Logs whichever IR interpretation <see cref="Form" /> makes canonical. Before final
    ///     reordering, blocks are shown in their current non-canonical list order with region paths.
    ///     With <paramref name="structuredLayout" />, derived region boundaries are also shown, so
    ///     that mode requires the aggressive-reorder postconditions. Logging never mutates the IR;
    ///     a displayed nop for an empty block is only a placeholder.
    /// </summary>
    private void LogBlocks(string phase, bool structuredLayout = false)
    {
        if (!debug)
            return;

        int codePos = 0;
        Stack<Region> regionStack = new();

        FileLog.LogBuffered($"### Optimizer {phase}: {method.FullDescription()}");

        IEnumerable<RegionNode> nodes = structuredLayout ? GetStructuredLayout() : basicBlocks;
        foreach (var block in nodes)
        {
            while (structuredLayout && regionStack.Count >= 1 && block.parent != regionStack.Peek())
            {
                Region exitedRegion = regionStack.Peek();
                if (exitedRegion.harmonyBlock != null && exitedRegion.Next == null)
                    FileLog.LogILBlockEnd(codePos, new ExceptionBlock(ExceptionBlockType.EndExceptionBlock));
                regionStack.Pop();
            }

            FileLog.LogBuffered($"## Block:        {block.ID}");
            if (block is BasicBlock basicBlock)
            {
                if (!structuredLayout)
                {
                    FileLog.LogBuffered($"## Region Path:  {string.Join(" > ", GetRegionPath(basicBlock.parent).Select(r => r.ID))}");
                }

                FileLog.LogBuffered($"## Predecessors: {string.Join(", ", basicBlock.Predecessors.Select(b => b.ID))}");
                FileLog.LogBuffered($"## Successors:   {string.Join(", ", basicBlock.Successors.Select(b => b.ID))}");
                if (Form != IrForm.Stack)
                {
                    FileLog.LogBuffered($"## Entry Stack:  {string.Join(", ", basicBlock.entryStackVariables)}");
                    foreach (var edge in basicBlock.incomingEdges)
                    {
                        string assignments = string.Join(", ", edge.assignments.Select(assignment =>
                            $"{assignment.Destination} = {assignment.Source}"));
                        FileLog.LogBuffered($"## Assignments {edge.Source.ID} => {edge.Target.ID}: {assignments}");
                    }
                }
            }

            if (block is { EntryPoint: true, parent: not null })
                FileLog.LogBuffered($"## Entry Point:  {block.parent.ID}");

            if (block is BasicBlock { label: Label label })
                FileLog.LogIL(codePos, label);

            switch (block)
            {
                case Region region:
                {
                    regionStack.Push(region);
                    if (region.harmonyBlock != null)
                        FileLog.LogILBlockBegin(codePos, region.harmonyBlock);
                    break;
                }
                case BasicBlock bb:
                {
                    foreach (var op in bb.ops)
                    {
                        foreach (var prefix in op.Prefixes)
                            LogInstruction(new(prefix), ref codePos);
                        if (Form != IrForm.Stack)
                            LogVariableInstruction(op, ref codePos);
                        else
                            LogInstruction(ConvertToCodeInstruction(op), ref codePos);
                    }

                    if (bb.ops.Count == 0)
                        LogInstruction(Ops.Nop.ToCodeInstruction(), ref codePos);

                    break;
                }
            }

            if (block is BasicBlock { Next: not null } bb2)
                FileLog.LogBuffered($"IL_{codePos:X4}: // fallthrough => {bb2.Next.ID}");
        }

        while (structuredLayout && regionStack.Count > 0)
        {
            if (regionStack.Peek().harmonyBlock != null)
                FileLog.LogILBlockEnd(codePos, new ExceptionBlock(ExceptionBlockType.EndExceptionBlock));
            regionStack.Pop();
        }

        FileLog.LogBuffered("");
        FileLog.FlushBuffer();
    }

    /// <summary>
    ///     Derives a structured emission layout without storing a second canonical layout list.
    ///     Preconditions: aggressive reorder has removed dead regions and ordered basicBlocks so
    ///     every Region begins with its recursive entry block and is contiguous, and each associated
    ///     filter/handler immediately follows the preceding Region in its exception-entry group.
    ///     The returned list interleaves canonical Region nodes with the existing BasicBlocks; it
    ///     does not change either hierarchy or block order.
    /// </summary>
    private List<RegionNode> GetStructuredLayout()
    {
        Dictionary<RegionNode, Region> regionByEntry = [];
        foreach (var region in regions.Where(region => region != root))
        {
            if (region.entry == null)
                throw new InvalidOperationException($"Region {region.ID} has no entry node");
            if (regionByEntry.ContainsKey(region.entry))
                throw new InvalidOperationException($"More than one region has entry node {region.entry.ID}");
            regionByEntry.Add(region.entry, region);
        }

        HashSet<Region> emittedRegions = [root];
        List<RegionNode> layout = [root];

        foreach (var block in basicBlocks)
        {
            AddRegionsBefore(block);
            layout.Add(block);
        }

        Region? missingRegion = regions.FirstOrDefault(region => !emittedRegions.Contains(region));
        if (missingRegion != null)
            throw new InvalidOperationException($"Region {missingRegion.ID} contains no retained basic block");

        return layout;

        void AddRegionsBefore(RegionNode entry)
        {
            if (!regionByEntry.TryGetValue(entry, out var region))
                return;
            if (!emittedRegions.Add(region))
                throw new InvalidOperationException($"Region {region.ID} has a cyclic entry chain");
            AddRegionsBefore(region);
            layout.Add(region);
        }
    }

    private static List<Region> GetRegionPath(Region? region)
    {
        List<Region> path = [];
        for (; region != null; region = region.parent)
            path.Add(region);
        path.Reverse();
        return path;
    }

    private static void LogVariableInstruction(Op op, ref int codePos)
    {
        string opcode = op.Opcode.ToString();
        if (op.Opcode.FlowControl is FlowControl.Branch or FlowControl.Cond_Branch)
            opcode += " =>";
        opcode = opcode.PadRight(10);

        string inputs = string.Join(", ", op.inputs);
        string outputs = string.Join(", ", op.outputs);
        string variables = (inputs.Length, outputs.Length) switch
        {
            (0, 0) => "",
            (0, _) => $"=> {outputs}",
            (_, 0) => inputs,
            _ => $"{inputs} => {outputs}",
        };
        string separator = variables.Length > 0 ? " " : "";
        FileLog.LogBuffered($"IL_{codePos:X4}: {opcode}{separator}{variables}");
        codePos += ReflectionTools.ILSize(op.Opcode);
    }

    private static void LogInstruction(CodeInstruction codeInstruction, ref int codePos)
    {
        foreach (var label in codeInstruction.labels)
            FileLog.LogIL(codePos, label);
        foreach (var block2 in codeInstruction.blocks)
            FileLog.LogILBlockBegin(codePos, block2);

        var code = codeInstruction.opcode;
        var operand = codeInstruction.operand;

        var realCode = true;
        switch (code.OperandType)
        {
            case OperandType.InlineNone:
                if (code == OpCodes.Nop && operand is string s)
                {
                    FileLog.LogILComment(codePos, s);
                    realCode = false;
                }
                else
                    FileLog.LogIL(codePos, code);

                break;

            default: FileLog.LogIL(codePos, code, operand); break;
        }

        foreach (var block2 in codeInstruction.blocks)
            FileLog.LogILBlockEnd(codePos, block2);
        if (realCode)
            codePos += ReflectionTools.ILSize(codeInstruction.opcode);
    }

    /// <summary>
    ///     Runs the complete pipeline exactly once on a newly constructed optimizer. It builds Stack
    ///     form, converts through regular and SSA Variables forms for variable-aware optimization,
    ///     lowers back to Stack form, restores dead-code and CIL layout invariants, and emits the
    ///     canonical output instruction list.
    /// </summary>
    public List<CodeInstruction> Optimize()
    {
        if (!valid)
            return inputInstructions;

        LogInstructions("Input", inputInstructions);

        // Convert from raw instructions to basic blocks
        MakeBasicBlocks();
        LogBlocks(nameof(MakeBasicBlocks));

        // Remove nop instructions
        NopElimination();
        LogBlocks(nameof(NopElimination));

        // Eliminate trivially dead blocks
        SimpleDeadCodeElimination();
        LogBlocks(nameof(SimpleDeadCodeElimination));

        // Convert from stack-based operations to explicit input and output variables
        ConvertStackToVariables();
        LogBlocks(nameof(ConvertStackToVariables));

        // Replace edges leading to empty blocks with direct edges to their successors
        JumpThreading();
        LogBlocks(nameof(JumpThreading));

        // Eliminate dead blocks produced by jump threading
        SimpleDeadCodeElimination();
        LogBlocks(nameof(SimpleDeadCodeElimination));

        // Remove conditional branches with the same target as the fallthrough block
        BranchElimination();
        LogBlocks(nameof(BranchElimination));

        // Merge blocks that are each other's only predecessor/successor
        MergeBlocks();
        LogBlocks(nameof(MergeBlocks));

        // Eliminate dead blocks produced by block merging
        SimpleDeadCodeElimination();
        LogBlocks(nameof(SimpleDeadCodeElimination));

        // Do simple constant propagation to expose further optimization opportunities
        ConservativeConstantPropagation();
        LogBlocks(nameof(ConservativeConstantPropagation));

        // Rename promotable storage and cross-block stack slots into SSA values.
        ConstructSsa();
        LogBlocks(nameof(ConstructSsa));

        // Give edge-specific stack phi transfers dedicated paths before materializing them.
        SplitSsaEdges();
        LogBlocks(nameof(SplitSsaEdges));

        // Materialize phi transfers and return to regular Variables form for stack lowering.
        DestructSsa();
        LogBlocks(nameof(DestructSsa));

        // Convert from variable based operations back to stack based operations
        ConvertVariablesToStack();
        LogBlocks(nameof(ConvertVariablesToStack));

        // Reverse the sense of conditional branches to increase reordering opportunities
        BranchInversion();
        LogBlocks(nameof(BranchInversion));

        // Remove all dead blocks and regions, and put blocks in a valid order for IL emission
        AggressiveDeadCodeEliminationAndReorder();
        LogBlocks(nameof(AggressiveDeadCodeEliminationAndReorder), true);

        // Insert unconditional branches after blocks whose fallthrough is not the next in order
        InsertBranches();
        LogBlocks(nameof(InsertBranches), true);

        // Emit IL
        Emit();
        LogInstructions("Output", outputInstructions.instructions);

        return outputInstructions.instructions;
    }

    /// <summary>
    ///     Preconditions: Stack form with a complete CFG, empty edge assignments, and no block
    ///     unreachable from every lexical Region entry. Postconditions: regular Variables form;
    ///     operation operands/counts, entryStackVariables, the variable registry, and addressTaken
    ///     are canonical, every edge assignment list is empty, and CFG, regions, and block order are
    ///     unchanged. Any cached dominator tree remains valid. The normal pipeline's first
    ///     SimpleDeadCodeElimination establishes the reachability precondition.
    /// </summary>
    private void ConvertStackToVariables()
    {
        new StackToVariableConversion(this).Run();
    }

    /// <summary>
    ///     Preconditions: regular Variables form with empty edge assignments, valid operation stack
    ///     counts, and one common required entry stack across all successors of each block.
    ///     Operations need not retain the original natural stack schedule: SSA may have removed
    ///     storage copies. Postconditions: executable Stack form; variable operands, entry stacks,
    ///     registries, and counts are cleared/non-canonical. CFG, regions, block order, and cached
    ///     dominance are unchanged.
    /// </summary>
    private void ConvertVariablesToStack()
    {
        new VariableToStackConversion(this).Run();
    }

    /// <summary>
    ///     Requires regular Variables form, empty edge assignments, unique ownership of every Op,
    ///     and every retained block to be reachable from GetDominatorRoots. In the normal pipeline,
    ///     SimpleDeadCodeElimination immediately after MergeBlocks removes ordinary stranded blocks
    ///     and restores unique Op ownership; dominator computation enforces its narrower root-
    ///     reachability requirement. This pass computes and caches dominators if unavailable. It
    ///     changes only operation and variable data: CFG, regions, block order, and dominance remain
    ///     valid, edge assignments remain empty, and addressTaken is canonical on return.
    /// </summary>
    internal void ConservativeConstantPropagation()
    {
        new ConservativeConstantPropagation(this).Run();
    }

    /// <summary>
    ///     Promotes every currently eligible mutable storage variable and every precisely typed
    ///     cross-block stack slot not promoted by an earlier invocation. Requires regular or SSA
    ///     Variables form, a complete CFG, empty edge assignments in regular form, and dead blocks
    ///     removed. The pass computes dominance explicitly if necessary. It may be rerun after an
    ///     optimization makes additional storage eligible; existing SSA families and phi
    ///     assignments are preserved.
    /// </summary>
    internal void ConstructSsa()
    {
        new SsaConstruction(this).Run();
    }

    /// <summary>
    ///     Requires SplitSsaEdges' guarantee that edges carrying evaluation-stack assignments have
    ///     a unique-successor source. Eliminates all SSA edge assignments and returns to regular
    ///     Variables form. SSA variables remain ordinary logical Variables; versions derived from
    ///     a local retain that physical slot as a conservative spill hint.
    /// </summary>
    internal void DestructSsa()
    {
        new SsaDestruction(this).Run();
    }

    /// <summary>
    ///     Requires SSA Variables form. Splits every stack-assignment edge whose source has another
    ///     successor, so rebuilding a predecessor-specific evaluation stack occurs only on the
    ///     selected edge. Storage assignments may remain on multi-successor sources because their
    ///     nonescaping destinations can be written speculatively. The pass preserves SSA form but
    ///     mutates the CFG and therefore invalidates dominance.
    /// </summary>
    internal void SplitSsaEdges()
    {
        new SsaEdgeSplitting(this).Run();
    }

    /// <summary>
    ///     The authoritative eligibility predicate for mutable argument/local storage. Unknown
    ///     metadata cannot prove that a store/load boundary is lossless. Pinned storage, address-
    ///     taken storage, and storage whose declared representation narrows its stack value remain
    ///     physical. Until exceptional local dataflow is represented in the CFG, all storage in a
    ///     method with exception entries is conservatively considered exception-exposed.
    /// </summary>
    internal bool IsEligibleForSsaPromotion(Variable variable)
    {
        if (variable.kind is not (VariableKind.Argument or VariableKind.Local))
            return false;
        if (variable.addressTaken || variable.pinned || exceptionEntryGroups.Count != 0)
            return false;
        if (variable.type == null || TypeLattice.IsSpecialType(variable.type))
            return false;
        return !TypeLattice.StorageNarrowsStackValue(variable.type);
    }

    /// <summary>
    ///     Explicitly returns the cached dominance result or computes it if absent. Requires a
    ///     complete CFG in either IR form and every retained block to be reachable from at least one
    ///     root returned by <see cref="GetDominatorRoots" />. Block order, operation ownership, and
    ///     SSA edge assignments do not affect block dominance. The result remains valid until a CFG
    ///     or implicit-entry mutation calls <see cref="InvalidateControlFlowAnalyses" />.
    /// </summary>
    internal DominatorTree ComputeDominatorTreeIfNeeded()
    {
        return dominatorTree ??= DominatorTree.Compute(basicBlocks, GetDominatorRoots());
    }

    /// <summary>
    ///     Returns the explicit entries used for normal-CFG dominance: the recursive method entry
    ///     plus every filter and handler entry whose exceptional predecessor is absent from the
    ///     edge graph. A protected-region entry is reached normally and is not an extra root. This
    ///     root set is intentionally narrower than the lexical-entry root set used by
    ///     SimpleDeadCodeElimination.
    /// </summary>
    private List<BasicBlock> GetDominatorRoots()
    {
        List<BasicBlock> roots = [GetRecursiveEntryBlock(root)];
        foreach (var entryGroup in exceptionEntryGroups)
        {
            foreach (var associatedRegion in entryGroup.associatedRegions)
                roots.Add(GetRecursiveEntryBlock(associatedRegion));
        }

        return roots;
    }

    // Requires a complete, acyclic Region entry chain. This is hierarchy data and does not inspect
    // basicBlocks order; aggressive reorder is not required.
    private static BasicBlock GetRecursiveEntryBlock(Region region)
    {
        HashSet<Region> visited = [];
        RegionNode? entry = region;
        while (entry is Region entryRegion)
        {
            if (!visited.Add(entryRegion))
                throw new InvalidOperationException($"Region {entryRegion.ID} has a cyclic entry chain");
            entry = entryRegion.entry;
        }

        return entry as BasicBlock ??
               throw new InvalidOperationException($"Region {region.ID} has no recursive entry block");
    }

    /// <summary>
    ///     Invalidates all cached facts derived from CFG topology or implicit entry roots. This
    ///     cannot revoke a DominatorTree reference already held by a worker; a pass must not mutate
    ///     the CFG and then continue using such a captured analysis.
    /// </summary>
    private void InvalidateControlFlowAnalyses()
    {
        dominatorTree = null;
    }

    /// <summary>
    ///     Emits the canonical output from Stack form without changing CFG or operation state.
    ///     Preconditions: aggressive reorder's structured-region ordering, InsertBranches' guarantee
    ///     that every remaining fallthrough targets the next physical block, bundled prefixes, empty
    ///     edge assignments, and an empty outputInstructions list. Empty blocks receive an emitted
    ///     nop because labels and exception markers require a physical CIL instruction. On return,
    ///     outputInstructions is canonical; BasicBlock.label may also have been assigned lazily.
    /// </summary>
    internal void Emit()
    {
        if (Form != IrForm.Stack)
            throw new InvalidOperationException($"Cannot emit {Form} form; convert it to stack form first");

        // Derive and materialize before mutating outputInstructions, so entry-chain errors cannot
        // leave behind a partially emitted method.
        List<RegionNode> emissionLayout = GetStructuredLayout();
        Stack<Region> regionStack = new();
        List<ExceptionBlock> harmonyBlocks = [];
        List<Label> labels = [];

        foreach (var block in emissionLayout)
        {
            while (regionStack.Count >= 1 && block.parent != regionStack.Peek())
            {
                Region exitedRegion = regionStack.Peek();
                if (exitedRegion.harmonyBlock != null && exitedRegion.Next == null)
                    outputInstructions.instructions[^1].blocks.Add(new ExceptionBlock(ExceptionBlockType.EndExceptionBlock));
                regionStack.Pop();
            }

            if (block is BasicBlock { label: Label label })
                labels.Add(label);

            switch (block)
            {
                case Region region:
                {
                    regionStack.Push(region);
                    if (region.harmonyBlock != null)
                        harmonyBlocks.Add(region.harmonyBlock);
                    break;
                }
                case BasicBlock bb:
                {
                    List<CodeInstruction> instructions = [.. bb.ops.SelectMany(GetCodeInstructions)];
                    if (instructions.Count == 0)
                        instructions.Add(Ops.Nop.ToCodeInstruction());
                    instructions[0].labels.AddRange(labels);
                    labels.Clear();
                    instructions[0].blocks.AddRange(harmonyBlocks);
                    harmonyBlocks.Clear();
                    outputInstructions.instructions.AddRange(instructions);
                    break;
                }
            }
        }

        while (regionStack.Count > 0)
        {
            if (regionStack.Peek().harmonyBlock != null)
                outputInstructions.instructions[^1].blocks.Add(new ExceptionBlock(ExceptionBlockType.EndExceptionBlock));
            regionStack.Pop();
        }
    }

    private IEnumerable<CodeInstruction> GetCodeInstructions(Op op)
    {
        foreach (var prefix in op.Prefixes)
            yield return new(prefix);
        yield return ConvertToCodeInstruction(op);
    }

    // Stack-form/emission conversion. It is also safe for prefix Ops during Variables-form logging,
    // because prefixes have no canonical variable operands. Branch operands remain CFG edges in the
    // IR and are replaced with lazily allocated labels only in the returned CodeInstruction.
    private CodeInstruction ConvertToCodeInstruction(Op i)
    {
        var codeInstruction = i.ToCodeInstruction();
        codeInstruction.operand = codeInstruction.operand switch
        {
            ControlFlowEdge edge => GetLabel(edge),
            ControlFlowEdge[] edges => edges.Select(GetLabel).ToArray(),
            _ => codeInstruction.operand,
        };

        return codeInstruction;

        Label GetLabel(ControlFlowEdge edge) => edge.Target.label ??= generator.DefineLabel();
    }

    /// <summary>
    ///     Converts inputInstructions into the initial Stack-form IR. Preconditions: a freshly
    ///     constructed optimizer with empty block, region, variable, edge-assignment, and output
    ///     state. Postconditions: canonical normal CFG and lexical-region hierarchy; labels in
    ///     branch operands are replaced by edges, unconditional branches are represented as default
    ///     continuations, prefixes are bundled with their operations, and variable-form fields are
    ///     empty/non-canonical. Blocks remain in input order, may include dead code, and are not yet
    ///     guaranteed to satisfy final CIL emission order. Dominance is unavailable.
    /// </summary>
    internal void MakeBasicBlocks()
    {
        InvalidateControlFlowAnalyses();
        Dictionary<Label, BasicBlock> labelToBlock = [];

        Region currentRegion = root;
        regions.Add(root);

        BasicBlock curBlock = new() { id = nextBlockId++, parent = currentRegion };
        basicBlocks.Add(curBlock);
        currentRegion.entry ??= curBlock;
        List<OpCode> prefixes = [];

        foreach (var inst in inputInstructions)
        {
            if (inst.labels.Count > 0 || inst.blocks.Any(IsBlockStart))
            {
                foreach (var harmonyBlock in inst.blocks.Where(IsBlockStart))
                    EnterRegion(harmonyBlock);

                NewBasicBlock();
                foreach (var label in inst.labels)
                    labelToBlock[label] = curBlock;
                if (inst.labels.Count >= 1)
                    curBlock.label = inst.labels[0];
            }

            if (inst.opcode.OpCodeType == OpCodeType.Prefix)
                prefixes.Add(inst.opcode);
            else
            {
                curBlock.ops.Add(new(inst.opcode, inst.operand, prefixes));
                prefixes = [];
            }

            if (inst.CanBranch || inst.blocks.Any(IsBlockEnd))
            {
                foreach (var _ in inst.blocks.Where(IsBlockEnd))
                    currentRegion = currentRegion.parent!;

                NewBasicBlock();
            }
        }

        if (curBlock.ops.Count == 0)
            basicBlocks.Remove(curBlock);

        Dictionary<BasicBlock, BasicBlock> fallthroughTargets = [];
        for (int i = 0; i < basicBlocks.Count - 1; i++)
        {
            if (CanFallThrough(basicBlocks[i]))
                fallthroughTargets.Add(basicBlocks[i], basicBlocks[i + 1]);
        }

        // Add a ret to the last basic block if one is missing (perhaps because of a poorly behaved transpiler)
        if (CanFallThrough(basicBlocks[^1]))
            basicBlocks[^1].ops.Add(Ops.Ret);

        // Convert branch instructions to point directly at the basic block
        foreach (var block in basicBlocks)
        {
            for (var index = 0; index < block.ops.Count; index++)
            {
                Op? op = block.ops[index];
                block.ops[index] = op.Operand switch
                {
                    Label label => new(op.Opcode, GetTarget(label), op.Prefixes),
                    Label[] labels => new(op.Opcode, labels.Select(GetTarget).ToArray(), op.Prefixes),
                    _ => block.ops[index],
                };
            }

            BasicBlock GetTarget(Label label) => labelToBlock[label];
        }

        // Convert block-final unconditional branches to fallthrough
        foreach (var block in basicBlocks)
        {
            if (block.ops.Count == 0)
                continue;

            if (block.ops[^1].IsUnconditionalBranch)
            {
                fallthroughTargets[block] = (BasicBlock)block.ops[^1].Operand!;
                block.ops.RemoveAt(block.ops.Count - 1);
            }
        }

        foreach (var block in basicBlocks)
        {
            if (fallthroughTargets.TryGetValue(block, out var fallthroughTarget))
                block.fallthroughEdge = AddControlFlowEdge(block, fallthroughTarget);

            if (block.ops.Count == 0)
                continue;

            Op finalOperation = block.ops[^1];
            block.ops[^1] = finalOperation.Operand switch
            {
                BasicBlock target => new(finalOperation.Opcode, AddControlFlowEdge(block, target), finalOperation.Prefixes),
                BasicBlock[] targets => new(finalOperation.Opcode,
                    targets.Select(target => AddControlFlowEdge(block, target)).ToArray(), finalOperation.Prefixes),
                _ => finalOperation,
            };
        }

        return;

        void EnterRegion(ExceptionBlock harmonyBlock)
        {
            if (harmonyBlock.blockType == ExceptionBlockType.BeginExceptionBlock)
            {
                var newRegion = new Region
                {
                    id = nextBlockId++,
                    harmonyBlock = harmonyBlock,
                    parent = currentRegion,
                };
                var newEntryGroup = new ExceptionEntryGroup(newRegion);
                newRegion.exceptionEntryGroup = newEntryGroup;
                exceptionEntryGroups.Add(newEntryGroup);
                regions.Add(newRegion);
                currentRegion.entry ??= newRegion;
                currentRegion = newRegion;
            }
            else
            {
                ExceptionEntryGroup entryGroup = currentRegion.exceptionEntryGroup ??
                                                 throw new InvalidOperationException(
                                                     "Handler marker does not follow a protected or handler region");
                var newRegion = new Region
                {
                    id = nextBlockId++,
                    harmonyBlock = harmonyBlock,
                    parent = currentRegion.parent,
                    exceptionEntryGroup = entryGroup,
                };
                entryGroup.associatedRegions.Add(newRegion);
                regions.Add(newRegion);
                currentRegion = newRegion;
            }
        }

        void NewBasicBlock()
        {
            if (curBlock.ops.Count == 0)
            {
                curBlock.parent = currentRegion;
            }
            else
            {
                BasicBlock newBlock = new() { id = nextBlockId++, parent = currentRegion };
                basicBlocks.Add(newBlock);
                curBlock = newBlock;
            }

            currentRegion.entry ??= curBlock;
        }

        static bool CanFallThrough(BasicBlock basicBlock) =>
            basicBlock.ops.Count == 0 || basicBlock.ops[^1].CanFallThrough;
    }

    // CFG mutation primitives. They preserve the bidirectional endpoint-list invariant and keep
    // cached analysis state coherent by invalidating it. MoveControlFlowEdgeSource additionally
    // transfers fallthrough classification; redirecting a target does not change whether the edge
    // is its source's default continuation.
    internal ControlFlowEdge AddControlFlowEdge(BasicBlock source, BasicBlock target)
    {
        var edge = new ControlFlowEdge(source, target);
        source.outgoingEdges.Add(edge);
        target.incomingEdges.Add(edge);
        InvalidateControlFlowAnalyses();
        return edge;
    }

    private void RemoveControlFlowEdge(ControlFlowEdge edge)
    {
        if (!edge.Source.outgoingEdges.Remove(edge) || !edge.Target.incomingEdges.Remove(edge))
            throw new InvalidOperationException("Control-flow edge is not attached to both endpoint blocks");
        if (edge.Source.fallthroughEdge == edge)
            edge.Source.fallthroughEdge = null;
        InvalidateControlFlowAnalyses();
    }

    internal void RedirectControlFlowEdge(ControlFlowEdge edge, BasicBlock target)
    {
        if (!edge.Target.incomingEdges.Remove(edge))
            throw new InvalidOperationException("Control-flow edge is not attached to its target block");
        edge.Target = target;
        target.incomingEdges.Add(edge);
        InvalidateControlFlowAnalyses();
    }

    private void MoveControlFlowEdgeSource(ControlFlowEdge edge, BasicBlock source)
    {
        BasicBlock oldSource = edge.Source;
        if (oldSource == source)
        {
            if (!source.outgoingEdges.Contains(edge))
                throw new InvalidOperationException("Control-flow edge is not attached to its source block");
            return;
        }

        bool isFallthrough = oldSource.fallthroughEdge == edge;
        if (isFallthrough && source.fallthroughEdge != null)
            throw new InvalidOperationException($"Basic block {source.ID} already has a fallthrough edge");
        if (!oldSource.outgoingEdges.Remove(edge))
            throw new InvalidOperationException("Control-flow edge is not attached to its source block");
        if (isFallthrough)
            oldSource.fallthroughEdge = null;
        edge.Source = source;
        source.outgoingEdges.Add(edge);
        if (isFallthrough)
            source.fallthroughEdge = edge;
        InvalidateControlFlowAnalyses();
    }

    /// <summary>
    ///     Inserts an empty fallthrough block on an existing edge and moves the edge's parallel
    ///     assignments to the new second edge. The new block belongs to the source's lexical Region;
    ///     callers must ensure that this is legal for the edge (in particular, a <c>leave</c> edge
    ///     never requires splitting because it is its source's sole successor).
    /// </summary>
    internal ControlFlowEdge SplitControlFlowEdge(ControlFlowEdge edge)
    {
        BasicBlock source = edge.Source;
        BasicBlock target = edge.Target;
        BasicBlock split = new() { id = nextBlockId++, parent = source.parent };
        int sourceIndex = basicBlocks.IndexOf(source);
        if (sourceIndex < 0)
            throw new InvalidOperationException("Control-flow edge source is not part of the optimizer");
        basicBlocks.Insert(sourceIndex + 1, split);

        List<VariableAssignment> assignments = [.. edge.assignments];
        edge.assignments.Clear();
        RedirectControlFlowEdge(edge, split);
        ControlFlowEdge second = AddControlFlowEdge(split, target);
        second.assignments.AddRange(assignments);
        split.fallthroughEdge = second;
        return second;
    }

    /// <summary>
    ///     May run after MakeBasicBlocks in Stack, regular Variables, or SSA Variables form. It
    ///     removes only zero-effect nop operations, so variable/edge invariants, CFG topology,
    ///     dominance, regions, and block order remain unchanged. Empty blocks may remain.
    /// </summary>
    internal void NopElimination()
    {
        foreach (var block in basicBlocks)
            block.ops.RemoveAll(i => i.Opcode == OpCodes.Nop);
    }

    /// <summary>
    ///     Requires the complete CFG produced by MakeBasicBlocks in Stack form or regular Variables
    ///     form; every edge assignment list must be empty. It removes a conditional branch when all
    ///     of the block's outgoing edges target its default continuation, replacing the condition
    ///     consumption with equivalent pops. In Variables form those pops retain canonical variable
    ///     operands and stack counts. Removed edges invalidate dominance; region membership and
    ///     block order are unchanged.
    /// </summary>
    internal void BranchElimination()
    {
        foreach (var block in basicBlocks)
        {
            if (block.ops.Count == 0)
                continue;
            if (block.fallthroughEdge is not { } fallthroughEdge)
                continue;
            if (block.outgoingEdges.Any(edge => edge.Target != fallthroughEdge.Target))
                continue;

            switch (block.ops[^1].Opcode)
            {
                // Brtrue, Brfalse
                case
                {
                    FlowControl: FlowControl.Cond_Branch, StackBehaviourPop: StackBehaviour.Popi, StackBehaviourPush: StackBehaviour.Push0,
                }:
                {
                    ReplaceBranchWithPops(block.ops[^1], 1);
                    RemoveBranchEdges();
                    break;
                }
                // Beq, Bge, Bgt, Ble, Blt, Bne_Un, Bge_Un, Bgt_Un, Ble_Un, Blt_Un
                case
                {
                    FlowControl: FlowControl.Cond_Branch, StackBehaviourPop: StackBehaviour.Pop1_pop1,
                    StackBehaviourPush: StackBehaviour.Push0,
                }:
                {
                    ReplaceBranchWithPops(block.ops[^1], 2);
                    RemoveBranchEdges();
                    break;
                }
            }

            void ReplaceBranchWithPops(Op branch, int popCount)
            {
                block.ops.RemoveAt(block.ops.Count - 1);
                if (Form == IrForm.Stack)
                {
                    for (int index = 0; index < popCount; index++)
                        block.ops.Add(Ops.Pop);
                    return;
                }

                if (branch.stackInputCount != popCount || branch.inputs.Count < popCount)
                    throw new InvalidOperationException($"Invalid variable operands on {branch.Opcode} in {block.ID}");

                // A branch records popped values from deepest to topmost; separate pop operations
                // must consume them in the reverse order in which they occur on the CIL stack.
                for (int index = popCount - 1; index >= 0; index--)
                {
                    Op pop = Ops.Pop;
                    pop.inputs.Add(branch.inputs[index]);
                    pop.stackInputCount = 1;
                    block.ops.Add(pop);
                }
            }

            void RemoveBranchEdges()
            {
                foreach (var edge in block.outgoingEdges.Where(edge => edge != fallthroughEdge).ToArray())
                    RemoveControlFlowEdge(edge);
            }
        }
    }

    /// <summary>
    ///     Requires a complete CFG in Stack form or regular Variables form after empty blocks have
    ///     acquired compatible entry/exit stack state; SSA edge assignments must be absent. It
    ///     redirects default edges through short chains of empty, non-entry blocks within one
    ///     Region. Redirected edges invalidate dominance and may leave skipped blocks unreachable, so
    ///     SimpleDeadCodeElimination must run afterward before dominator computation or any pass
    ///     requiring every listed block to be live. Block order and region data are unchanged.
    /// </summary>
    internal void JumpThreading()
    {
        foreach (var block in basicBlocks)
        {
            ControlFlowEdge? fallthroughEdge = block.fallthroughEdge;
            if (fallthroughEdge == null)
                continue;
            BasicBlock fallthroughBlock = fallthroughEdge.Target;
            int iterations = 0;
            while (fallthroughBlock is { ops.Count: 0, EntryPoint: false, fallthroughEdge: not null } bb &&
                   bb.fallthroughEdge.Target.parent == bb.parent &&
                   iterations++ < 20)
                fallthroughBlock = bb.fallthroughEdge.Target;

            if (fallthroughEdge.Target != fallthroughBlock)
                RedirectControlFlowEdge(fallthroughEdge, fallthroughBlock);
        }
    }

    /// <summary>
    ///     Requires a complete CFG in Stack form or regular Variables form with empty edge
    ///     assignments. It merges a same-Region, non-entry successor having one incoming edge into
    ///     its predecessor. CFG edge changes invalidate dominance. The absorbed successor remains
    ///     in basicBlocks as an unreachable node and temporarily shares its Op instances with the
    ///     merged predecessor.
    ///     SimpleDeadCodeElimination must run before dominator computation or any pass which indexes
    ///     operations by identity; the normal pipeline runs it immediately next. Region data and
    ///     block order are otherwise unchanged.
    /// </summary>
    internal void MergeBlocks()
    {
        for (int i = basicBlocks.Count - 1; i >= 0; i--)
        {
            var block = basicBlocks[i];
            if (block.outgoingEdges is not [var successorEdge])
                continue;
            if (block.ops.Count > 0 && block.ops[^1].CanBranch)
                continue;
            BasicBlock successor = successorEdge.Target;
            if (successor.incomingEdges.Count != 1 || successor.parent != block.parent || successor is not { EntryPoint: false })
                continue;

            block.ops.AddRange(successor.ops);
            successor.ops.Clear();
            RemoveControlFlowEdge(successorEdge);

            foreach (var edge in successor.outgoingEdges.ToArray())
                MoveControlFlowEdgeSource(edge, block);
        }
    }

    /// <summary>
    ///     May run in Stack, regular Variables, or SSA Variables form after MakeBasicBlocks. It
    ///     removes blocks unreachable through normal edges from every lexical Region entry, because
    ///     handler/filter predecessors are absent from the CFG. Protected-region entries are also
    ///     roots here even though they are ordinary-flow entries for dominance. It removes attached
    ///     edges and their assignments, invalidating dominance when the graph changes. Relative block
    ///     order, operation/variable form, and Region metadata are unchanged; unreachable Regions
    ///     and exception groups are retained until AggressiveDeadCodeEliminationAndReorder.
    ///     Postcondition: every retained block is reachable from some lexical entry, but not
    ///     necessarily from the narrower root set used by dominator computation.
    /// </summary>
    internal void SimpleDeadCodeElimination()
    {
        Queue<BasicBlock> queue = new();
        HashSet<BasicBlock> liveBlocks = [];

        // Every lexical region entry is an analysis root: handlers have implicit exceptional
        // predecessors which are intentionally absent from the normal-control-flow CFG.
        foreach (var block in basicBlocks)
        {
            if (block.EntryPoint)
                queue.Enqueue(block);
        }

        while (queue.Count > 0)
        {
            var block = queue.Dequeue();
            if (!liveBlocks.Add(block))
                continue;
            foreach (var edge in block.outgoingEdges)
                queue.Enqueue(edge.Target);
        }

        foreach (var deadBlock in basicBlocks.Where(block => !liveBlocks.Contains(block)).ToArray())
        {
            foreach (var edge in deadBlock.incomingEdges.Concat(deadBlock.outgoingEdges).Distinct().ToArray())
                RemoveControlFlowEdge(edge);
        }

        if (basicBlocks.RemoveAll(block => !liveBlocks.Contains(block)) > 0)
            InvalidateControlFlowAnalyses();
    }

    /// <summary>
    ///     May run in either IR form, but the normal pipeline runs it in Stack form after
    ///     BranchInversion and before InsertBranches. Preconditions: a complete, bidirectionally
    ///     consistent CFG and a valid Region/exception-group hierarchy; no prior block ordering is
    ///     required. It removes unreachable blocks, Regions, and exception groups, invalidating
    ///     dominance if topology or implicit roots change, and establishes canonical emission order.
    ///     On return every Region begins with its recursive entry block and is contiguous, each
    ///     associated filter/handler immediately follows the preceding Region in its group, and a
    ///     stack-carrying backward edge has the forward predecessor required by CIL. Form-specific
    ///     operation and variable data, edge assignments, and CFG semantics are preserved.
    /// </summary>
    internal void AggressiveDeadCodeEliminationAndReorder()
    {
        ValidateAggressiveReorderPreconditions();

        // CIL permits a backward edge carrying evaluation-stack values only when the target also
        // has a forward incoming edge. Earlier optimizer passes may temporarily violate that
        // restriction; ordering blocks by first control-flow visit restores it before emission.
        List<BasicBlock> outputBlocks = [];
        HashSet<RegionNode> retainedNodes = [root];
        Stack<(Region region, LinkedList<RegionNode> queue)> stack = [];
        List<RegionNode> leavingBlocks = [];

        stack.Push((root, []));
        stack.Peek().queue.AddLast(root.entry!);

        while (stack.Count >= 1)
        {
            var (region, queue) = stack.Peek();

            if (queue.Count == 0)
            {
                stack.Pop();
                if (stack.Count > 0)
                {
                    (region, queue) = stack.Peek();
                    foreach (var leavingBlock in leavingBlocks.Where(b => b.parent == region))
                        queue.AddLast(leavingBlock);
                    leavingBlocks.RemoveAll(b => b.parent == region);
                }

                continue;
            }

            var block = queue.First.Value;
            queue.RemoveFirst();
            if (!block.HasAncestor(region))
                throw new InvalidOperationException();
            while (block.parent != region)
                block = block.parent!;

            if (!retainedNodes.Add(block))
                continue;
            if (block is BasicBlock retainedBlock)
                outputBlocks.Add(retainedBlock);

            if (debug)
                FileLog.LogBuffered($"{"".PadLeft(stack.Count * 2)}- {block.ID}");

            switch (block)
            {
                case Region chainedRegion:
                {
                    if (chainedRegion.Next != null)
                        queue.AddFirst(chainedRegion.Next);
                    break;
                }
                case BasicBlock { fallthroughEdge: not null } basicBlock: queue.AddFirst(basicBlock.fallthroughEdge.Target); break;
            }

            switch (block)
            {
                case Region nestedRegion:
                {
                    (region, queue) = (nestedRegion, []);
                    stack.Push((region, queue));
                    queue.AddLast(nestedRegion.entry!);
                    break;
                }
                case BasicBlock bb:
                {
                    foreach (var edge in bb.outgoingEdges)
                    {
                        BasicBlock successor = edge.Target;
                        if (!successor.HasAncestor(region))
                            leavingBlocks.Add(successor);
                        else if (edge != bb.fallthroughEdge)
                            queue.AddLast(successor);
                    }

                    break;
                }
                default: throw new InvalidOperationException();
            }
        }

        foreach (var deadBlock in basicBlocks.Where(block => !retainedNodes.Contains(block)).ToArray())
        {
            foreach (var edge in deadBlock.incomingEdges.Concat(deadBlock.outgoingEdges).Distinct().ToArray())
                RemoveControlFlowEdge(edge);
        }

        if (basicBlocks.Count != outputBlocks.Count)
            InvalidateControlFlowAnalyses();
        basicBlocks = outputBlocks;
        regions.RemoveAll(region => !retainedNodes.Contains(region));
        if (exceptionEntryGroups.RemoveAll(group => !retainedNodes.Contains(group.ProtectedRegion)) > 0)
            InvalidateControlFlowAnalyses();
    }

    // Validates hierarchy membership and exception-group consistency before any reordering or
    // pruning occurs. It intentionally imposes no preexisting basicBlocks layout requirement.
    private void ValidateAggressiveReorderPreconditions()
    {
        if (root.entry == null)
            throw new InvalidOperationException("The root region has no entry node");

        foreach (var region in regions)
        {
            if (region != root && region.parent == null)
                throw new InvalidOperationException($"Region {region.ID} is not attached to the region hierarchy");
            if (region.entry == null)
                throw new InvalidOperationException($"Region {region.ID} has no entry node");
            if (!region.entry.HasAncestor(region))
                throw new InvalidOperationException($"Entry node {region.entry.ID} is outside region {region.ID}");
        }

        foreach (var block in basicBlocks)
        {
            if (block.parent == null || !regions.Contains(block.parent))
                throw new InvalidOperationException($"Basic block {block.ID} has no retained parent region");
        }

        foreach (var group in exceptionEntryGroups)
        {
            if (!regions.Contains(group.ProtectedRegion) ||
                group.ProtectedRegion.exceptionEntryGroup != group ||
                group.associatedRegions.Any(region => !regions.Contains(region) || region.exceptionEntryGroup != group) ||
                group.associatedRegions.Distinct().Count() != group.associatedRegions.Count)
            {
                throw new InvalidOperationException("Exception-entry group membership is inconsistent");
            }
        }
    }

    internal MethodBody? GetMethodBodyOrNull()
    {
        try
        {
            return method.GetMethodBody();
        }
        catch (InvalidOperationException)
        {
            return null;
        }
        catch (NotSupportedException)
        {
            return null;
        }
    }

    // Variables-form registry helpers used while StackToVariableConverter materializes canonical
    // operands. ArgumentVariables must already be initialized from parameterTypes. A previously
    // unseen local is created with unknown declared type; later stores never refine that metadata.
    internal Variable GetArgumentVariable(int index) => argumentVariables.TryGetValue(index, out var variable)
        ? variable
        : throw new InvalidOperationException($"Unknown argument #{index}");

    internal Variable GetLocalVariable(int index)
    {
        if (localVariables.TryGetValue(index, out var variable))
            return variable;

        variable = NewVariable(VariableKind.Local, null, index);
        localVariables.Add(index, variable);
        return variable;
    }

    // Adds one canonical Variables-form object to the owning registry. Callers adding an Argument
    // or Local must also add the same object to the corresponding index dictionary.
    internal Variable NewVariable(
        VariableKind kind,
        Type? type,
        int index = -1,
        LocalBuilder? localBuilder = null,
        bool pinned = false)
    {
        var variable = new Variable
        {
            id = nextVariableId++,
            kind = kind,
            type = type,
            index = index,
            localBuilder = localBuilder,
            pinned = pinned,
        };
        variables.Add(variable);
        return variable;
    }

    internal static bool ReferencesLocal(Op op) => unchecked((ushort)op.Opcode.Value) is
        OpCodeValues.Ldloc_0 or OpCodeValues.Ldloc_1 or OpCodeValues.Ldloc_2 or OpCodeValues.Ldloc_3 or
        OpCodeValues.Ldloc or OpCodeValues.Ldloc_S or OpCodeValues.Ldloca or OpCodeValues.Ldloca_S or
        OpCodeValues.Stloc_0 or OpCodeValues.Stloc_1 or OpCodeValues.Stloc_2 or OpCodeValues.Stloc_3 or
        OpCodeValues.Stloc or OpCodeValues.Stloc_S;

    /// <summary>
    ///     Requires Stack form with absent variable operands and empty edge assignments. The normal
    ///     pipeline runs it immediately after Variables-to-Stack lowering and before aggressive
    ///     reorder. It inverts brtrue or brfalse when doing so makes a single-predecessor target the
    ///     semantic fallthrough, helping the subsequent reorder choose a useful layout. Edge objects
    ///     and endpoints are unchanged, so CFG topology, dominance, Regions, and current block order
    ///     remain valid; only the explicit/default classification of two outgoing edges changes.
    /// </summary>
    internal void BranchInversion()
    {
        foreach (var block in basicBlocks)
        {
            if (block.ops.Count == 0)
                continue;
            if (block.fallthroughEdge is not { } fallthroughEdge)
                continue;
            if (fallthroughEdge.Target.incomingEdges.Count == 1)
                continue;

            var finalInstruction = block.ops[^1];
            if (finalInstruction.Opcode.FlowControl is FlowControl.Cond_Branch &&
                finalInstruction.Operand is ControlFlowEdge { Target.incomingEdges.Count: 1 } branchEdge)
            {
                if (finalInstruction.Opcode == OpCodes.Brfalse || finalInstruction.Opcode == OpCodes.Brfalse_S)
                {
                    block.ops[^1] = new(OpCodes.Brtrue_S, fallthroughEdge, block.ops[^1].Prefixes);
                    block.fallthroughEdge = branchEdge;
                }

                if (finalInstruction.Opcode == OpCodes.Brtrue || finalInstruction.Opcode == OpCodes.Brtrue_S)
                {
                    block.ops[^1] = new(OpCodes.Brfalse_S, fallthroughEdge, block.ops[^1].Prefixes);
                    block.fallthroughEdge = branchEdge;
                }
            }
        }
    }

    /// <summary>
    ///     Requires Stack form, empty edge assignments, and the canonical emission order established
    ///     by AggressiveDeadCodeEliminationAndReorder; it runs immediately before Emit. It converts
    ///     every default continuation which does not target the next physical block into an explicit
    ///     branch. On return each remaining fallthroughEdge targets the following block, so Emit may
    ///     rely on physical fallthrough. CFG endpoints, dominance, Regions, and block order are
    ///     unchanged, but fallthroughEdge is cleared for each materialized branch.
    /// </summary>
    internal void InsertBranches()
    {
        if (Form != IrForm.Stack)
            throw new InvalidOperationException($"Cannot insert emitting branches in {Form} form");

        for (int i = 0; i < basicBlocks.Count; i++)
        {
            ControlFlowEdge? fallthroughEdge = basicBlocks[i].fallthroughEdge;
            if (fallthroughEdge == null || i < basicBlocks.Count - 1 && fallthroughEdge.Target == basicBlocks[i + 1])
                continue;
            basicBlocks[i].ops.Add(new(OpCodes.Br_S, fallthroughEdge, []));
            basicBlocks[i].fallthroughEdge = null;
        }
    }
}

/// <summary>
///     Exception entries which share a protected region, with their filter and handler regions
///     in CIL layout order.
/// </summary>
internal sealed class ExceptionEntryGroup(Region protectedRegion)
{
    // Canonical CIL layout order of the filters/handlers associated with ProtectedRegion. A
    // filtered entry contributes both its filter Region and its handler Region. This order is
    // independent of basicBlocks order until aggressive reorder reestablishes emission layout.
    public readonly List<Region> associatedRegions = [];

    // Canonical protected body for this group. It is not repeated in associatedRegions.
    public Region ProtectedRegion { get; } = protectedRegion;

    // Returns the next Region in the group's required CIL layout chain. The argument must be
    // ProtectedRegion or a current associatedRegions member; null means the exception group ends.
    public Region? NextRegion(Region region)
    {
        if (region == ProtectedRegion)
            return associatedRegions.FirstOrDefault();

        int index = associatedRegions.IndexOf(region);
        if (index < 0)
            throw new ArgumentException("Region is not a member of this exception-entry group", nameof(region));
        return index + 1 < associatedRegions.Count ? associatedRegions[index + 1] : null;
    }

    // Exceptional local dataflow does not follow the normal block CFG. A handler may observe
    // a local written at any potentially throwing instruction in ProtectedRegion, and a store
    // whose value computation throws leaves the old local value visible. Consequently, future
    // SSA construction must either model instruction-level exceptional predecessors (and
    // their unsplittable transfers), or leave exception-exposed arguments and locals in
    // physical storage. Catch/filter entry stack values are supplied by the runtime and are a
    // separate concern from those locals.
}

/// <summary>
///     A synthetic root, protected region, filter region, or handler region. Regions form the
///     canonical lexical containment hierarchy after MakeBasicBlocks. They do not form CFG
///     nodes. Their eventual emission positions are derived from entries and basic-block order,
///     not stored as a second independently mutable layout.
/// </summary>
internal class Region : RegionNode
{
    public override string ID => parent == null ? "Root" : $"{harmonyBlock!.blockType} #{id}";

    // Canonical region kind and catch type after MakeBasicBlocks; null only for the synthetic
    // root. The marker's eventual output position is derived rather than stored here.
    public ExceptionBlock? harmonyBlock;

    // Canonical first lexical child after MakeBasicBlocks. It may be a nested Region, so callers
    // needing a block follow the recursive entry chain. Before aggressive reorder this child
    // need not be the earliest member of the Region in basicBlocks; afterward it is.
    public RegionNode? entry;

    // Canonical exception-group membership after MakeBasicBlocks. Null only for the synthetic
    // root; protected, filter, and handler Regions all point to their shared group.
    public ExceptionEntryGroup? exceptionEntryGroup;

    public Region? Next => field ??= exceptionEntryGroup?.NextRegion(this);
}

internal sealed class VariableAssignment(Variable source, Variable destination)
{
    // Valid only as an element of ControlFlowEdge.assignments in SSA Variables form. Source and
    // Destination participate in one parallel logical transfer; this is never emitted directly.
    public Variable Source { get; } = source;
    public Variable Destination { get; } = destination;
}

/// <summary>Identifies the storage or logical value represented by a variable.</summary>
internal enum VariableKind
{
    /// <summary>A mutable CIL argument slot, including <c>this</c> at index zero.</summary>
    Argument,

    /// <summary>A mutable CIL local. Its declared type may be unavailable.</summary>
    Local,

    /// <summary>
    ///     A logical evaluation-stack slot crossing a basic-block boundary. In regular
    ///     Variables form the same mutable slot may be defined by several predecessors; SSA
    ///     construction replaces that interpretation with single-definition values.
    /// </summary>
    StackSlot,

    /// <summary>A value produced by an operation within a basic block.</summary>
    Temporary,
}

internal sealed class Variable
{
    private string BaseName => kind switch
    {
        VariableKind.Argument => $"a{index}",
        VariableKind.Local => $"l{index}",
        VariableKind.StackSlot => $"s{id}",
        VariableKind.Temporary => $"v{id}",
        _ => throw new ArgumentOutOfRangeException(),
    };

    public string Name => ssaOrigin switch
    {
        null => BaseName,
        { } origin when origin == this => $"{BaseName}.{ssaVersion}",
        { } origin => $"{origin.BaseName}.{ssaVersion}",
    };

    // Stable identity within one Variables-form interval. Unlike index, this is unique across
    // all variable kinds. IDs and the variable registry are reset when Variables form is built
    // or discarded; Variable objects are not canonical in Stack form.
    public required int id;
    public required VariableKind kind;

    // Canonical Variables-form type information. Argument types come from the method signature.
    // A Local type is set only from MethodBody metadata or a LocalBuilder, never inferred from
    // stores, and may therefore be null. StackSlot and Temporary types come from symbolic stack
    // analysis and may contain the special lattice-marker types below.
    public Type? type;

    // Canonical physical slot index for Argument and Local; -1 for logical StackSlot and
    // Temporary values. Distinct argument/local variables never represent the same slot.
    public int index = -1;

    // Optional canonical metadata for a Local created by a transpiler. When present, its index
    // and type agree with this Variable; pinned combines all authoritative metadata seen for the
    // slot. Null means only that no LocalBuilder was supplied, since the original MethodBody may
    // still provide authoritative type metadata.
    public LocalBuilder? localBuilder;

    // Canonical Variables-form pinned flag for Local; false for other variable kinds. It is
    // populated only from authoritative local metadata or a LocalBuilder.
    public bool pinned;

    // Canonical in regular and SSA Variables forms. True exactly when a remaining operation takes this
    // argument/local's address. Rewriting address operations can change the value, so such a
    // pass must recompute it; this is a current-IR summary, not historical escape information.
    public bool addressTaken;

    // Canonical only in SSA Variables form. A promoted mutable variable points to itself with
    // version zero, letting incremental construction recognize it without a second registry. Phi
    // destinations generated for that name point to the original with a positive version. Ordinary
    // operation results need no origin: after copy removal they can directly be the name's value.
    public Variable? ssaOrigin;
    public int ssaVersion = -1;

    // Canonical from SSA construction through Variables-to-Stack lowering. Null means no preference.
    // When a logical value assigned to a promoted local requires a spill, lowering may reuse this
    // physical local if it has not already granted the slot to an incompatible value.
    public Variable? preferredStorage;

    public override string ToString() => Name;
}

internal sealed class ControlFlowEdge(BasicBlock source, BasicBlock target)
{
    // Canonical only in SSA Variables form and during conversion into or out of it. Stack form
    // and regular Variables form require every edge's list to be empty. Assignments on one edge
    // execute in parallel and are logical value transfers, not emitted CIL instructions.
    public readonly List<VariableAssignment> assignments = [];

    // Canonical endpoints after MakeBasicBlocks. Mutated only by the optimizer's edge helpers,
    // which also update endpoint collections, preserve fallthroughEdge, and invalidate cached
    // control-flow analyses.
    public BasicBlock Source { get; internal set; } = source;
    public BasicBlock Target { get; internal set; } = target;
}

/// <summary>
///     A node in the normal CFG containing operations which execute consecutively unless an
///     operation throws. Normal branches, returns, explicit throws, and leaves occur only as
///     the final operation. Exceptional transfers are represented by the region hierarchy,
///     not by <see cref="incomingEdges" /> or <see cref="outgoingEdges" />.
/// </summary>
internal class BasicBlock : RegionNode
{
    // Non-canonical read-only projections of the edge fields below. They may contain the same
    // block more than once when distinct CFG edges share an endpoint.
    public BasicBlock? Next => fallthroughEdge?.Target;
    public IEnumerable<BasicBlock> Predecessors => incomingEdges.Select(edge => edge.Source);
    public IEnumerable<BasicBlock> Successors => outgoingEdges.Select(edge => edge.Target);

    // Canonical operation sequence in every IR form. In Stack form the CIL evaluation stack is
    // implicit; in both Variables forms each operation's inputs/outputs are canonical.
    public readonly List<Op> ops = [];

    // Non-canonical emission metadata. This preserves one input label when available and is
    // otherwise assigned lazily if Emit needs a label. CFG edges, never labels, are canonical.
    public Label? label;

    // The canonical normal CFG after MakeBasicBlocks. Every edge occurs exactly once in its
    // source's outgoingEdges and target's incomingEdges, and its endpoints agree with those
    // collections. Every edge referenced by a branch Op is also in that Op's block's
    // outgoingEdges. fallthroughEdge is either null or one member of outgoingEdges identifying
    // the default continuation not encoded by the final operation. InsertBranches may turn a
    // non-adjacent default continuation into an explicit branch and clear fallthroughEdge.
    public readonly List<ControlFlowEdge> incomingEdges = [];
    public readonly List<ControlFlowEdge> outgoingEdges = [];
    public ControlFlowEdge? fallthroughEdge;

    // Canonical in both Variables forms and deliberately empty in Stack form. In regular
    // Variables form these are shared mutable logical stack slots: every predecessor's natural
    // exit stack is identical by object identity to its target's entryStackVariables, and edge
    // assignments are empty. In SSA Variables form precisely typed slots are block-entry SSA names
    // whose predecessor-specific values are supplied by parallel incoming-edge assignments;
    // imprecisely typed slots temporarily retain the regular shared-slot interpretation.
    public readonly List<Variable> entryStackVariables = [];
}

/// <summary>
///     A node in the canonical lexical region-containment hierarchy built by MakeBasicBlocks.
///     This hierarchy is independent of normal CFG reachability and basic-block list order.
/// </summary>
internal class RegionNode
{
    // Lexical-entry predicate only: true when this is the first child recorded by its parent.
    // It does not imply normal CFG reachability and is not the dominator-root predicate.
    public bool EntryPoint => parent == null || parent.entry == this;

    // Stable identity shared by Regions and BasicBlocks. IDs are unique after MakeBasicBlocks;
    // the synthetic root alone retains zero.
    public virtual string ID => $"#{id}";
    public int id = 0;

    // Canonical lexical parent after MakeBasicBlocks. A BasicBlock's parent is its immediate
    // containing Region; a non-root Region's parent is the surrounding Region.
    public Region? parent;

    public override string ToString() => ID;

    public bool HasAncestor(Region region)
    {
        for (RegionNode? b = this; b != null; b = b.parent)
        {
            if (b == region)
                return true;
        }

        return false;
    }
}

internal abstract class Pass
{
    public abstract void Run();
}
