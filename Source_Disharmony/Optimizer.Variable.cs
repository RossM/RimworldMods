namespace Disharmony;

internal partial class Optimizer
{
    /// <summary>Identifies the storage or logical value represented by a variable.</summary>
    internal enum VariableKind
    {
        /// <summary>A mutable CIL argument slot, including <c>this</c> at index zero.</summary>
        Argument,

        /// <summary>A mutable CIL local. Its declared type may be unavailable.</summary>
        Local,

        /// <summary>A basic-block entry stack position, analogous to a block parameter.</summary>
        EntryStackSlot,

        /// <summary>A value produced by an operation within a basic block.</summary>
        Temporary,
    }

    /// <summary>Describes whether an edge is represented by layout or an explicit branch.</summary>
    internal enum ControlFlowEdgeKind
    {
        FallThrough,
        Branch,
    }

    internal sealed class Variable
    {
        // Stable optimizer identity; unlike index, this is unique across all variable kinds.
        public required int id;
        public required VariableKind kind;

        // For a Local this is set only from authoritative local metadata or a LocalBuilder, never
        // inferred from stores. EntryStackSlot and Temporary types come from symbolic stack analysis.
        public Type? type;

        // Argument/local index, or stack position for an EntryStackSlot. Temporaries leave this at -1.
        public int index = -1;
        // Only EntryStackSlots have an owning block.
        public BasicBlock? block;
        // Preserves both the identity and authoritative type of transpiler-created locals when known.
        public LocalBuilder? localBuilder;
        public bool pinned;
        // Address-taken arguments and locals cannot be promoted as ordinary SSA values.
        public bool addressTaken;

        public string Name => kind switch
        {
            VariableKind.Argument => $"a{index}",
            VariableKind.Local => $"l{index}",
            VariableKind.EntryStackSlot => $"s{block!.id}_{index}",
            VariableKind.Temporary => $"v{id}",
            _ => throw new ArgumentOutOfRangeException(),
        };

        public override string ToString() => Name;
    }

    internal sealed class VariableAssignment(Variable source, Variable destination)
    {
        // This is a logical value transfer on a CFG edge, not an instruction to emit.
        public Variable Source { get; } = source;
        public Variable Destination { get; } = destination;
    }

    internal sealed class ControlFlowEdge(BasicBlock source, BasicBlock target, ControlFlowEdgeKind kind)
    {
        // Source and Target mirror the canonical Block CFG. They must not be independently
        // redirected until ControlFlowEdge becomes the canonical CFG representation.
        public BasicBlock Source { get; } = source;
        public BasicBlock Target { get; } = target;
        public ControlFlowEdgeKind Kind { get; } = kind;

        // Parallel assignments from the source exit stack to the target's block parameters.
        public readonly List<VariableAssignment> assignments = [];
    }
}
