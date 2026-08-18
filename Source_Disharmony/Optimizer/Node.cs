namespace Disharmony.Optimizer;

internal abstract record Node
{
    public abstract T Accept<T>(IVisitor<T> visitor);

    public virtual void DebugPrint() => FileLog.LogBuffered(ToString());
}


/// <summary>
///     Represents a region, which can be the root region or an exception region.
/// </summary>
/// <remarks>
///     Every <see cref="BasicBlock" /> belongs to a <see cref="Region" />, and all regions except the
///     <see cref="ControlFlowGraph.RootRegion" /> are contained in a parent region.
/// </remarks>
/// <param name="EntryLabel">The <see cref="BlockLabel" /> of the region's entry <see cref="BasicBlock" />.</param>
internal abstract record Region(BlockLabel EntryLabel) : Node;

/// <summary>
///     The root region which all <see cref="BasicBlock" />s ultimately belong to.
/// </summary>
/// <remarks>
///     The <paramref name="EntryLabel" /> of the root region is the entry <see cref="BasicBlock" /> for the method.
/// </remarks>
/// <param name="EntryLabel">The <see cref="BlockLabel" /> of the method's entry <see cref="BasicBlock" />.</param>
internal sealed record RootRegion(BlockLabel EntryLabel) : Region(EntryLabel)
{
    public override T Accept<T>(IVisitor<T> visitor) => visitor.Visit(this);

    public override string ToString() => "root";
}

/// <summary>
///     Base class for exception regions.
/// </summary>
/// <param name="EntryLabel">The <see cref="BlockLabel" /> of the region's entry <see cref="BasicBlock" />.</param>
/// <param name="Parent">The <see cref="Region" /> that contains this region.</param>
internal abstract record ExceptionRegion(BlockLabel EntryLabel, Region Parent) : Region(EntryLabel);

/// <summary>
///     Represents a protected region of a try block.
/// </summary>
/// <remarks>
///     It is valid for the entry <see cref="BasicBlock" /> of a <see cref="ProtectedRegion" /> to have incoming
///     <see cref="Edge" />s from outside that region, but all other <see cref="BasicBlock" />s in the region must only
///     have incoming <see cref="Edge" />s from within the region.
/// </remarks>
/// <param name="EntryLabel">The <see cref="BlockLabel" /> of the try region's entry <see cref="BasicBlock" />.</param>
/// <param name="Parent">The <see cref="Region" /> that contains this region.</param>
internal sealed record ProtectedRegion(BlockLabel EntryLabel, Region Parent, ExceptionGroup Group) : ExceptionRegion(EntryLabel, Parent)
{
    public override T Accept<T>(IVisitor<T> visitor) => visitor.Visit(this);

    public override string ToString() => "try";
}

internal abstract record HandlerRegion(BlockLabel EntryLabel, Region Parent) : ExceptionRegion(EntryLabel, Parent);

/// <summary>
///     Represents a catch handler region.
/// </summary>
/// <remarks>
///     It is invalid for any <see cref="BasicBlock" /> in a <see cref="CatchRegion" /> to have incoming
///     <see cref="Edge" />s from outside that region.
///     On entry to the catch region, <paramref name="IncomingException" /> is the exception object that was thrown.
/// </remarks>
/// <param name="EntryLabel">The <see cref="BlockLabel" /> of the catch region's entry <see cref="BasicBlock" />.</param>
/// <param name="Parent">The <see cref="Region" /> that contains this region.</param>
/// <param name="IncomingException">The <see cref="StackSlot" /> containing the exception on entry to the handler.</param>
internal sealed record CatchRegion(BlockLabel EntryLabel, Region Parent, StackSlot IncomingException) : HandlerRegion(EntryLabel, Parent)
{
    public Type ExceptionType => IncomingException.Type;
    public override T Accept<T>(IVisitor<T> visitor) => visitor.Visit(this);

    public override string ToString() => $"catch ({IncomingException})";
}

// Note that there is no FilterRegion, because Harmony's filter handling is broken.

/// <summary>
///     Represents a <see langword="finally" /> handler region.
/// </summary>
/// <remarks>
///     It is invalid for any <see cref="BasicBlock" /> in a <see cref="FinallyRegion" /> to have incoming
///     <see cref="Edge" />s from outside that region. Control flow can only exit a <see langword="finally" /> region using
///     the
///     <see cref="OpCodes.Endfinally" /> instruction.
/// </remarks>
/// <param name="EntryLabel">
///     The <see cref="BlockLabel" /> of the <see langword="finally" /> region's entry
///     <see cref="BasicBlock" />.
/// </param>
/// <param name="Parent">The <see cref="Region" /> that contains this region.</param>
internal sealed record FinallyRegion(BlockLabel EntryLabel, Region Parent) : HandlerRegion(EntryLabel, Parent)
{
    public override T Accept<T>(IVisitor<T> visitor) => visitor.Visit(this);

    public override string ToString() => "finally";
}

/// <summary>
///     Represents a fault handler region.
/// </summary>
/// <remarks>
///     It is invalid for any <see cref="BasicBlock" /> in a <see cref="FinallyRegion" /> to have incoming
///     <see cref="Edge" />s from outside that region. Control flow can only exit a fault region using the
///     <see cref="OpCodes.Endfinally" /> instruction.
/// </remarks>
/// <param name="EntryLabel">The <see cref="BlockLabel" /> of the fault region's entry <see cref="BasicBlock" />.</param>
/// <param name="Parent">The <see cref="Region" /> that contains this region.</param>
internal sealed record FaultRegion(BlockLabel EntryLabel, Region Parent) : HandlerRegion(EntryLabel, Parent)
{
    public override T Accept<T>(IVisitor<T> visitor) => visitor.Visit(this);

    public override string ToString() => "fault";
}

/// <summary>
///     Represents a group of exception regions, consisting of a <see cref="Disharmony.Optimizer.ProtectedRegion" /> and
///     one or more
///     <see cref="HandlerRegion" />s.
/// </summary>
/// <remarks>
///     The CLI enforces a number of rules around exception regions and control flow. Control flow can only enter
///     a <see cref="Disharmony.Optimizer.ProtectedRegion" /> through its <see cref="Region.EntryLabel" />. Control flow
///     can only
///     enter a <see cref="HandlerRegion" /> through the action of the exception system; there can't be any
///     explicit <see cref="Edge" />s into a handler region. Control flow can only leave a <see cref="HandlerRegion" />
///     or <see cref="CatchRegion" /> through a <see cref="Leave" />, and control flow can only leave a
///     <see cref="FinallyRegion" /> or <see cref="FaultRegion" /> through a <see cref="Return" /> with a
///     <see cref="Return.IL" /> of <see cref="OpCodes.Endfinally" />. However, a <see cref="Leave" /> <i>can</i> transfer
///     control from
///     a <see cref="CatchRegion" /> to any <see cref="BasicBlock" /> in the associated <see cref="ProtectedRegion" />.
/// </remarks>
/// <param name="HandlerRegions"></param>
internal sealed record ExceptionGroup(IReadOnlyList<HandlerRegion> HandlerRegions) : Node
{
    public override T Accept<T>(IVisitor<T> visitor) => visitor.Visit(this);
}

/// <summary>
///     Provides a name for a <see cref="BasicBlock" />.
/// </summary>
/// <remarks>
///     In order to allow most data elements to be immutable, references to a <see cref="BasicBlock" /> are stored as the
///     block's label rather than as a direct reference to the block. This allows a <see cref="BasicBlock" /> to be
///     replaced with a new block with the same label without updating other data structures.
/// </remarks>
/// <param name="label">The original IL label, or <see langword="null" /> if the block did not have an IL label.</param>
internal sealed class BlockLabel(Label? label = null, int id = -1)
{
    public Label? Label { get; } = label;
    public int Id { get; } = id;

    public override string ToString() => $"Block{Id}";
}

/// <summary>
///     Represents the transfer of control to the next <see cref="BasicBlock" /> at the end of a <see cref="BasicBlock" />.
/// </summary>
/// <remarks>
///     Branch-type <see cref="ILOp" />s must be represented as a <see cref="Branch" />. Exceptions are not represented in
///     the <see cref="BasicBlock" /> structure, except for unconditional throws.
/// </remarks>
/// <param name="Labels">The <see cref="BlockLabel" />s of the possible successor <see cref="BasicBlock" />s.</param>
internal abstract record Branch(IReadOnlyList<BlockLabel> Labels) : Node;

/// <summary>
///     Represents unconditional transfer of control.
/// </summary>
internal record UnconditionalBranch : Branch
{
    /// <summary>Gets the <see cref="BlockLabel" /> of the branch target.</summary>
    public BlockLabel Label => Labels[0];

    /// <summary>Initializes a branch to the specified target.</summary>
    /// <param name="label">The <see cref="BlockLabel" /> of the branch target.</param>
    public UnconditionalBranch(BlockLabel label) : base([label]) { }

    public override T Accept<T>(IVisitor<T> visitor) => visitor.Visit(this);

    public override string ToString() => $"br {Labels[0]}";
}

/// <summary>
///     Represents transfer of control by the <see cref="OpCodes.Leave" /> instruction.
/// </summary>
/// <remarks>
///     Unlike regular transfer of control, <see cref="OpCodes.Leave" /> is permitted to exit an exception handler region.
///     No <see cref="StackSlot" />s can be live when a leave is taken.
/// </remarks>
/// <param name="Label">The <see cref="BlockLabel" /> of the branch target.</param>
internal record Leave(BlockLabel Label) : UnconditionalBranch(Label)
{
    public override T Accept<T>(IVisitor<T> visitor) => visitor.Visit(this);

    public override string ToString() => $"leave {Labels[0]}";
}

/// <summary>
///     Represents a conditional transfer of control.
/// </summary>
/// <remarks>
///     <c>Labels[0]</c> represents the fallthrough block. For an ordinary conditional branch,
///     <c>Labels[1]</c> represents the taken branch target, while for a switch, the remaining
///     elements of <paramref name="Labels" /> represent switch targets.
/// </remarks>
/// <param name="Labels">The fallthrough and branch-target <see cref="BlockLabel" />s.</param>
/// <param name="OpCode">The conditional branch or switch opcode.</param>
internal sealed record ConditionalBranch(OpCode OpCode, IReadOnlyList<Op> Inputs, IReadOnlyList<BlockLabel> Labels) : Branch(Labels)
{
    public override T Accept<T>(IVisitor<T> visitor) => visitor.Visit(this);

    public override string ToString() => $"{OpCode} ({string.Join(", ", Inputs)}) {{ {string.Join(", ", Labels)} }}";
}

/// <summary>
///     Represents throwing an exception.
/// </summary>
/// <remarks>
///     Exceptional control transfer is not represented as <see cref="Edge" />s, so a <see cref="BasicBlock" /> ending in a
///     <see cref="Throw" /> has no outgoing edges.
/// </remarks>
/// <param name="Exception">The <see cref="Op" /> that produces the exception to throw.</param>
internal sealed record Throw(Op Exception) : Branch([])
{
    public override T Accept<T>(IVisitor<T> visitor) => visitor.Visit(this);

    public override string ToString() => $"throw {Exception}";
}

internal sealed record Rethrow() : Branch([])
{
    public override T Accept<T>(IVisitor<T> visitor) => visitor.Visit(this);

    public override string ToString() => "rethrow";
}

/// <summary>
///     Represents returning from a method.
/// </summary>
/// <param name="IL">
///     The instruction that generated the return, which can be <see cref="OpCodes.Ret" /> or
///     <see cref="OpCodes.Endfinally" />.
/// </param>
/// <param name="Value">The <see cref="Op" /> that produces the return value, or a <see cref="VoidOp" /> for a void return.</param>
internal sealed record Return(ILInstruction IL, Op Value) : Branch([])
{
    public override T Accept<T>(IVisitor<T> visitor) => visitor.Visit(this);

    public override string ToString() => $"{IL.OpCode} {Value}";
}

/// <summary>
///     Represents <see cref="OpCodes.Jmp" />.
/// </summary>
/// <param name="Value"></param>
internal sealed record Jump(Op Value) : Branch([])
{
    public override T Accept<T>(IVisitor<T> visitor) => visitor.Visit(this);

    public override string ToString() => $"jmp {Value}";
}

/// <summary>
///     Represents a basic block.
/// </summary>
/// <param name="Label">The block's <see cref="BlockLabel" />.</param>
/// <param name="Ops">The <see cref="Op" />s executed by the block.</param>
/// <param name="Region">The <see cref="Region" /> containing the block.</param>
/// <param name="Branch">The transfer of control at the end of the block.</param>
internal sealed record BasicBlock(BlockLabel Label, IReadOnlyList<Op> Ops, Region Region, Branch Branch) : Node
{
    public override T Accept<T>(IVisitor<T> visitor) => visitor.Visit(this);

    public override void DebugPrint()
    {
        FileLog.ChangeIndent(-1);
        FileLog.LogBuffered($"{Label}:");
        FileLog.ChangeIndent(1);
        foreach (var op in Ops)
            op.DebugPrint();
        Branch.DebugPrint();
    }
}

/// <summary>
///     Represents an edge between <see cref="BasicBlock" />s.
/// </summary>
/// <remarks>
///     <para>
///         All <paramref name="EdgeAssignments" /> are done in parallel. Non-SSA form of the control graph will
///         have no <paramref name="EdgeAssignments" />.
///     </para>
///     <para>
///         There must be one edge for each distinct branch target of a <see cref="BasicBlock" />. Edges are not created
///         or removed automatically; it is the responsibility of code that adds, removes, or updates a
///         <see cref="BasicBlock" /> to also update the edges.
///     </para>
/// </remarks>
/// <param name="Source">The <see cref="BlockLabel" /> of the source block.</param>
/// <param name="Destination">The <see cref="BlockLabel" /> of the destination block.</param>
/// <param name="EdgeAssignments">The <see cref="AssignmentOp" />s performed while control transfers across the edge.</param>
internal sealed record Edge(BlockLabel Source, BlockLabel Destination, IReadOnlyList<AssignmentOp> EdgeAssignments) : Node
{
    public override T Accept<T>(IVisitor<T> visitor) => visitor.Visit(this);
}
