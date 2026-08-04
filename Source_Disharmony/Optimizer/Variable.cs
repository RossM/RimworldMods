namespace Disharmony.Optimizer;

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

    /// <summary>
    ///     A storage-free CIL constant. It has no defining operation in SSA form and is
    ///     rematerialized directly at every stack use.
    /// </summary>
    Constant,
}

internal abstract class Variable
{

    private string BaseName => kind switch
    {
        VariableKind.Argument => $"A{index}",
        VariableKind.Local => $"L{index}",
        VariableKind.StackSlot => $"S{id}",
        VariableKind.Temporary => $"V{id}",
        VariableKind.Constant => constantValue == null ? $"C{id}" : $"C{id}({constantValue})",
        _ => throw new ArgumentOutOfRangeException(),
    };

    public string Name => ssaOrigin switch
    {
        null => BaseName,
        { } origin when origin == this => $"{BaseName}.{ssaVersion}",
        { } origin => $"{origin.BaseName}.{ssaVersion}",
    } + (type is Type t ? $"[{t}]" : "");

    /// <summary>
    ///     The authoritative decision whether this value may participate in SSA substitution and
    ///     copy elimination. Mutable arguments/locals are promotable only when their storage
    ///     boundary is a lossless copy and cannot be observed indirectly or by an exception
    ///     handler. Cross-block stack slots require a precise type because phi destruction may
    ///     spill them. Temporaries and constants are already single-definition SSA values and are
    ///     therefore promotable without further renaming. Whether a mutable family has already
    ///     been renamed is recorded separately by <see cref="ssaOrigin" />.
    /// </summary>
    public bool IsPromotable
    {
        get
        {
            return kind switch
            {
                VariableKind.Argument or VariableKind.Local =>
                    type != null && !TypeLattice.IsSpecialType(type) &&
                    !addressTaken && !pinned && !exceptionExposed &&
                    !TypeLattice.StorageNarrowsStackValue(type),
                // TODO: ConditionalStructCopy currently produces an imprecisely typed join slot.
                // Preserve its concrete stack type so it can become promotable too.
                VariableKind.StackSlot => type != null && !TypeLattice.IsSpecialType(type),
                VariableKind.Temporary or VariableKind.Constant => true,
                _ => throw new ArgumentOutOfRangeException(),
            };
        }
    }

    // Stable identity within one Variables-form interval. Unlike index, this is unique across
    // all variable kinds. IDs and the variable registry are reset when Variables form is built
    // or discarded; Variable objects are not canonical in Stack form.
    public int id;
    public abstract VariableKind kind { get; }

    // Canonical Variables-form type information. Argument types come from the method signature.
    // A Local type is set only from MethodBody metadata or a LocalBuilder, never inferred from
    // stores, and may therefore be null. StackSlot and Temporary types come from symbolic stack
    // analysis and may contain the special lattice-marker types below. A Constant's type is the
    // CIL stack type implied by constantValue.
    public Type? type;

    // Canonical and non-null exactly when kind is Constant. This describes the complete,
    // side-effect-free instruction sequence used to materialize the value; constants never have
    // physical storage and must never be spilled.
    public ConstantValue? constantValue;

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

    // Canonical in both Variables forms for Argument and Local; false for other kinds. Until
    // exceptional storage dataflow is represented explicitly, every argument/local in a method
    // with a filter or handler is conservatively exception-exposed. This is immutable throughout a
    // Variables-form interval and is one of the facts consumed by IsPromotable.
    public bool exceptionExposed;

    // Canonical only in SSA Variables form. A promoted mutable variable points to itself with
    // version zero, letting incremental construction recognize it without a second registry. Phi
    // destinations and promoted-storage assignments generated for that name point to the original
    // with a positive version. Unrelated operation results have no origin.
    public Variable? ssaOrigin;
    public int ssaVersion = -1;

    // Canonical from SSA construction through Variables-to-Stack lowering. Null means no preference.
    // When a logical value derived from promoted argument/local storage requires a spill, lowering
    // may reuse that physical slot if it has not already granted it to an incompatible value.
    public Variable? preferredStorage;

    public override string ToString() => Name;
}

internal class LocalVariable : Variable
{
    public override VariableKind kind => VariableKind.Local;
}

internal class ConstantVariable : Variable
{
    public override VariableKind kind => VariableKind.Constant;

}

internal class TemporaryVariable : Variable
{
    public override VariableKind kind => VariableKind.Temporary;

}

internal class ArgumentVariable : Variable
{
    public override VariableKind kind => VariableKind.Argument;

}

internal class StackSlotVariable : Variable
{
    public override VariableKind kind => VariableKind.StackSlot;

}