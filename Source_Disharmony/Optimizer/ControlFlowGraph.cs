using System.Diagnostics;

namespace Disharmony.Optimizer;

internal class ControlFlowGraph
{
    /// <summary>
    ///     Returns all <see cref="BasicBlock" />s in the control flow graph.
    /// </summary>
    public IEnumerable<BasicBlock> BasicBlocks => basicBlocks.Values;

    /// <summary>
    ///     Returns all <see cref="Edge" />s in the control flow graph.
    /// </summary>
    public IEnumerable<Edge> Edges => edges.Values;

    /// <summary>
    ///     Returns all <see cref="ExceptionGroup" />s in the control flow graph.
    /// </summary>
    public IEnumerable<ExceptionGroup> ExceptionGroups => exceptionGroups;

    private readonly HashSet<ExceptionGroup> exceptionGroups = [];
    private readonly Dictionary<ExceptionRegion, ExceptionGroup> exceptionGroupsByRegion = [];
    private readonly Dictionary<ExceptionRegion, ExceptionRegion?> nextRegion = [];
    private readonly Dictionary<BlockLabel, BasicBlock> basicBlocks = [];
    private readonly Dictionary<(BlockLabel Source, BlockLabel Destination), Edge> edges = [];
    private readonly Dictionary<BlockLabel, HashSet<Edge>> edgesFrom = [];
    private readonly Dictionary<BlockLabel, HashSet<Edge>> edgesTo = [];

    /// <summary>
    ///     Returns the root region of the control flow graph.
    /// </summary>
    public RootRegion RootRegion { get; } = new(new BlockLabel());

    /// <summary>
    ///     Returns all incoming <see cref="Edge" />s for a <see cref="BasicBlock" />.
    /// </summary>
    /// <param name="block">The <see cref="BasicBlock" /> whose incoming <see cref="Edge" />s to return.</param>
    /// <returns>The <see cref="Edge" />s whose destination is <paramref name="block" />.</returns>
    /// <exception cref="KeyNotFoundException"><paramref name="block" /> is not present in the graph.</exception>
    public IEnumerable<Edge> IncomingEdges(BasicBlock block) => edgesTo[block.Label];

    /// <summary>
    ///     Returns all incoming <see cref="Edge" />s for the <see cref="BasicBlock" /> with the given
    ///     <see cref="BlockLabel" />.
    /// </summary>
    /// <param name="label">The label of the <see cref="BasicBlock" /> whose incoming <see cref="Edge" />s to return.</param>
    /// <returns>The <see cref="Edge" />s whose destination is <paramref name="label" />.</returns>
    /// <exception cref="KeyNotFoundException"><paramref name="label" /> is not present in the graph.</exception>
    public IEnumerable<Edge> IncomingEdges(BlockLabel label) => edgesTo[label];

    /// <summary>
    ///     Returns all outgoing <see cref="Edge" />s for a <see cref="BasicBlock" />.
    /// </summary>
    /// <param name="block">The <see cref="BasicBlock" /> whose outgoing <see cref="Edge" />s to return.</param>
    /// <returns>The <see cref="Edge" />s whose source is <paramref name="block" />.</returns>
    /// <exception cref="KeyNotFoundException"><paramref name="block" /> is not present in the graph.</exception>
    public IEnumerable<Edge> OutgoingEdges(BasicBlock block) => edgesFrom[block.Label];

    /// <summary>
    ///     Returns all outgoing <see cref="Edge" />s for the <see cref="BasicBlock" /> with the given
    ///     <see cref="BlockLabel" />.
    /// </summary>
    /// <param name="label">The label of the <see cref="BasicBlock" /> whose outgoing <see cref="Edge" />s to return.</param>
    /// <returns>The <see cref="Edge" />s whose source is <paramref name="label" />.</returns>
    /// <exception cref="KeyNotFoundException"><paramref name="label" /> is not present in the graph.</exception>
    public IEnumerable<Edge> OutgoingEdges(BlockLabel label) => edgesFrom[label];

    /// <summary>
    ///     Gets all <see cref="BasicBlock" />s with edges to <paramref name="block" />.
    /// </summary>
    /// <param name="block">The <see cref="BasicBlock" /> whose predecessors to return.</param>
    /// <returns>The <see cref="BasicBlock" />s with <see cref="Edge" />s to <paramref name="block" />.</returns>
    /// <exception cref="KeyNotFoundException"><paramref name="block" /> is not present in the graph.</exception>
    public IEnumerable<BasicBlock> Predecessors(BasicBlock block) => edgesTo[block.Label].Select(edge => basicBlocks[edge.Source]);

    /// <summary>
    ///     Gets the <see cref="BlockLabel" />s of all <see cref="BasicBlock" />s with edges to the block with
    ///     <see cref="BlockLabel" /> <paramref name="label" />.
    /// </summary>
    /// <param name="label">The label of the block whose predecessor labels to return.</param>
    /// <returns>
    ///     The labels of the <see cref="BasicBlock" />s with <see cref="Edge" />s to the block identified by
    ///     <paramref name="label" />.
    /// </returns>
    /// <exception cref="KeyNotFoundException"><paramref name="label" /> is not present in the graph.</exception>
    public IEnumerable<BlockLabel> Predecessors(BlockLabel label) => edgesTo[label].Select(edge => edge.Source);

    /// <summary>
    ///     Gets all <see cref="BasicBlock" />s with edges from <paramref name="block" />.
    /// </summary>
    /// <param name="block">The <see cref="BasicBlock" /> whose successors to return.</param>
    /// <returns>The <see cref="BasicBlock" />s with <see cref="Edge" />s from <paramref name="block" />.</returns>
    /// <exception cref="KeyNotFoundException"><paramref name="block" /> is not present in the graph.</exception>
    public IEnumerable<BasicBlock> Successors(BasicBlock block) => edgesFrom[block.Label].Select(edge => basicBlocks[edge.Destination]);

    /// <summary>
    ///     Gets the <see cref="BlockLabel" />s of all <see cref="BasicBlock" />s with edges from the block with
    ///     <see cref="BlockLabel" /> <paramref name="label" />.
    /// </summary>
    /// <param name="label">The label of the block whose successor labels to return.</param>
    /// <returns>
    ///     The labels of the <see cref="BasicBlock" />s with <see cref="Edge" />s from the block identified by
    ///     <paramref name="label" />.
    /// </returns>
    /// <exception cref="KeyNotFoundException"><paramref name="label" /> is not present in the graph.</exception>
    public IEnumerable<BlockLabel> Successors(BlockLabel label) => edgesFrom[label].Select(edge => edge.Destination);

    /// <summary>
    ///     Gets the edge from the block with the given source <see cref="BlockLabel" /> to the block with the given
    ///     destination <see cref="BlockLabel" />.
    /// </summary>
    /// <param name="source">The source block label.</param>
    /// <param name="destination">The destination block label.</param>
    /// <returns>The <see cref="Edge" /> from <paramref name="source" /> to <paramref name="destination" />.</returns>
    /// <exception cref="KeyNotFoundException">No edge exists between the specified blocks.</exception>
    public Edge GetEdge(BlockLabel source, BlockLabel destination) => edges[(source, destination)];

    /// <summary>
    ///     Gets the <see cref="Edge" /> from the given source <see cref="BasicBlock" /> to the given destination
    ///     <see cref="BasicBlock" />.
    /// </summary>
    /// <param name="source">The source <see cref="BasicBlock" />.</param>
    /// <param name="destination">The destination <see cref="BasicBlock" />.</param>
    /// <returns>The <see cref="Edge" /> from <paramref name="source" /> to <paramref name="destination" />.</returns>
    /// <exception cref="KeyNotFoundException">No edge exists between the specified blocks.</exception>
    public Edge GetEdge(BasicBlock source, BasicBlock destination) => edges[(source.Label, destination.Label)];

    /// <summary>
    ///     Gets the edge from the block with the given source <see cref="BlockLabel" /> to the block with the given
    ///     destination <see cref="BlockLabel" />,
    ///     or <see langword="null" /> if no edge exists.
    /// </summary>
    /// <param name="source">The source block label.</param>
    /// <param name="destination">The destination block label.</param>
    /// <returns>The <see cref="Edge" /> between the specified blocks, or <see langword="null" /> if no such edge exists.</returns>
    public Edge? GetEdgeOrNull(BlockLabel source, BlockLabel destination)
    {
        edges.TryGetValue((source, destination), out Edge? result);
        return result;
    }

    /// <summary>
    ///     Gets the <see cref="Edge" /> from the given source <see cref="BasicBlock" /> to the given destination
    ///     <see cref="BasicBlock" />, or <see langword="null" /> if no edge
    ///     exists.
    /// </summary>
    /// <param name="source">The source <see cref="BasicBlock" />.</param>
    /// <param name="destination">The destination <see cref="BasicBlock" />.</param>
    /// <returns>The <see cref="Edge" /> between the specified blocks, or <see langword="null" /> if no such edge exists.</returns>
    public Edge? GetEdgeOrNull(BasicBlock source, BasicBlock destination) => GetEdgeOrNull(source.Label, destination.Label);

    /// <summary>
    ///     Gets the <see cref="BasicBlock" /> with the given <see cref="BlockLabel" />.
    /// </summary>
    /// <param name="label">The label of the block to return.</param>
    /// <returns>The <see cref="BasicBlock" /> identified by <paramref name="label" />.</returns>
    /// <exception cref="KeyNotFoundException">No block with the specified label exists.</exception>
    public BasicBlock GetBlock(BlockLabel label) => basicBlocks[label];

    /// <summary>
    ///     Gets the <see cref="ExceptionGroup" /> containing the given <see cref="ExceptionRegion" />.
    /// </summary>
    /// <param name="region">The exception region whose group to return.</param>
    /// <returns>The <see cref="ExceptionGroup" /> containing <paramref name="region" />.</returns>
    /// <exception cref="KeyNotFoundException"><paramref name="region" /> does not belong to a group in the graph.</exception>
    public ExceptionGroup GetExceptionGroup(ExceptionRegion region) => exceptionGroupsByRegion[region];

    /// <summary>
    ///     Gets the next <see cref="ExceptionRegion" /> in order in the <see cref="ExceptionGroup" /> containing the given
    ///     <see cref="ExceptionRegion" />, or <see langword="null" /> if it is the last region in the group.
    /// </summary>
    /// <param name="region">The exception region whose successor to return.</param>
    /// <returns>The next region in the group, or <see langword="null" /> if <paramref name="region" /> is the last region.</returns>
    /// <exception cref="KeyNotFoundException"><paramref name="region" /> does not belong to a group in the graph.</exception>
    public ExceptionRegion? GetNextRegion(ExceptionRegion region) => nextRegion[region];

    /// <summary>
    ///     Adds a <see cref="BasicBlock" /> to the control flow graph.
    /// </summary>
    /// <remarks>
    ///     It is the caller's responsibility to add the <see cref="Edge" />s for the new block.
    /// </remarks>
    /// <param name="block">The <see cref="BasicBlock" /> to add.</param>
    /// <exception cref="ArgumentException">A block with the same <see cref="BasicBlock.Label" /> already exists.</exception>
    public void AddBlock(BasicBlock block)
    {
        basicBlocks.Add(block.Label, block);

        if (!edgesFrom.ContainsKey(block.Label))
            edgesFrom[block.Label] = [];
        if (!edgesTo.ContainsKey(block.Label))
            edgesTo[block.Label] = [];
    }

    /// <summary>
    ///     Replaces an existing <see cref="BasicBlock" /> with a new one with the same <see cref="BlockLabel" />.
    /// </summary>
    /// <remarks>
    ///     It is the caller's responsibility to update the <see cref="Edge" />s for the replaced block if necessary.
    /// </remarks>
    /// <param name="block">The replacement <see cref="BasicBlock" />.</param>
    /// <exception cref="KeyNotFoundException">No block with the same <see cref="BasicBlock.Label" /> exists.</exception>
    public void ReplaceBlock(BasicBlock block)
    {
        RemoveBlock(block.Label);
        AddBlock(block);
    }

    /// <summary>
    ///     Removes a <see cref="BasicBlock" /> from the control flow graph.
    /// </summary>
    /// <remarks>
    ///     It is the caller's responsibility to remove the <see cref="Edge" />s for the removed block.
    /// </remarks>
    /// <param name="block">The <see cref="BasicBlock" /> to remove.</param>
    /// <exception cref="KeyNotFoundException">No block with the same <see cref="BasicBlock.Label" /> exists.</exception>
    /// <exception cref="InvalidOperationException">
    ///     The graph contains a different block with the same
    ///     <see cref="BasicBlock.Label" />.
    /// </exception>
    public void RemoveBlock(BasicBlock block)
    {
        if (basicBlocks[block.Label] != block)
            throw new InvalidOperationException();
        basicBlocks.Remove(block.Label);
    }

    /// <summary>
    ///     Removes the <see cref="BasicBlock" /> with the given <see cref="BlockLabel" /> from the control flow graph.
    /// </summary>
    /// <remarks>
    ///     It is the caller's responsibility to remove the <see cref="Edge" />s for the removed block.
    /// </remarks>
    /// <param name="label">The label of the block to remove.</param>
    public void RemoveBlock(BlockLabel label) => basicBlocks.Remove(label);

    /// <summary>
    ///     Adds an <see cref="Edge" /> to the control flow graph.
    /// </summary>
    /// <param name="edge">The <see cref="Edge" /> to add.</param>
    /// <exception cref="InvalidOperationException">An edge with the same source and destination already exists.</exception>
    public void AddEdge(Edge edge)
    {
        if (edges.ContainsKey((edge.Source, edge.Destination)))
            throw new InvalidOperationException();

        edges[(edge.Source, edge.Destination)] = edge;
        edgesFrom[edge.Source].Add(edge);
        edgesTo[edge.Destination].Add(edge);
    }

    /// <summary>
    ///     Replaces an existing <see cref="Edge" /> with a new one with the same source and destination labels.
    /// </summary>
    /// <param name="edge">The replacement <see cref="Edge" />.</param>
    /// <exception cref="KeyNotFoundException">No edge with the same source and destination exists.</exception>
    public void ReplaceEdge(Edge edge)
    {
        RemoveEdge(edges[(edge.Source, edge.Destination)]);
        AddEdge(edge);
    }

    /// <summary>
    ///     Removes an <see cref="Edge" /> from the control flow graph.
    /// </summary>
    /// <param name="edge">The <see cref="Edge" /> to remove.</param>
    /// <exception cref="KeyNotFoundException">No edge with the same source and destination exists.</exception>
    /// <exception cref="InvalidOperationException">The graph contains a different edge with the same source and destination.</exception>
    public void RemoveEdge(Edge edge)
    {
        if (edges[(edge.Source, edge.Destination)] != edge)
            throw new InvalidOperationException();

        edges.Remove((edge.Source, edge.Destination));
        edgesFrom[edge.Source].Remove(edge);
        edgesTo[edge.Destination].Remove(edge);
    }

    /// <summary>
    ///     Removes the <see cref="Edge" /> with the given source and destination labels.
    /// </summary>
    /// <param name="source">The source block label.</param>
    /// <param name="destination">The destination block label.</param>
    /// <exception cref="KeyNotFoundException">No edge exists between the specified blocks.</exception>
    public void RemoveEdge(BlockLabel source, BlockLabel destination) => RemoveEdge(GetEdge(source, destination));

    /// <summary>
    ///     Adds an <see cref="ExceptionGroup" /> to the control flow graph.
    /// </summary>
    /// <param name="group">The <see cref="ExceptionGroup" /> to add.</param>
    /// <exception cref="InvalidOperationException"><paramref name="group" /> is already present in the graph.</exception>
    public void AddExceptionGroup(ExceptionGroup group)
    {
        if (!exceptionGroups.Add(group))
            throw new InvalidOperationException();

        ExceptionRegion[] regions = [group.ProtectedRegion, .. group.HandlerRegions];
        for (int i = 0; i < regions.Length; i++)
        {
            var region = regions[i];
            var next = i + 1 < regions.Length ? regions[i + 1] : null;
            exceptionGroupsByRegion[region] = group;
            nextRegion[region] = next;
        }
    }

    /// <summary>
    ///     Validates the current state.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    ///     The graph is missing an edge or contains an edge that is not referenced by
    ///     its source block.
    /// </exception>
    [Conditional("DEBUG")]
    public void Validate()
    {
        foreach (var block in BasicBlocks)
        foreach (var successor in block.Branch.Labels)
        {
            if (!edges.ContainsKey((block.Label, successor)))
                throw new InvalidOperationException("Edge not found");
        }

        foreach (var edge in Edges)
        {
            if (!basicBlocks[edge.Source].Branch.Labels.Contains(edge.Destination))
                throw new InvalidOperationException("Edge not referenced");
        }
    }
}

/// <summary>
///     Represents a region, which can be the root region or an exception region.
/// </summary>
/// <remarks>
///     Every <see cref="BasicBlock" /> belongs to a <see cref="Region" />, and all regions except the
///     <see cref="ControlFlowGraph.RootRegion" /> are contained in a parent region.
/// </remarks>
/// <param name="EntryLabel">The <see cref="BlockLabel" /> of the region's entry <see cref="BasicBlock" />.</param>
internal abstract record Region(BlockLabel EntryLabel);

/// <summary>
///     The root region which all <see cref="BasicBlock" />s ultimately belong to.
/// </summary>
/// <remarks>
///     The <paramref name="EntryLabel" /> of the root region is the entry <see cref="BasicBlock" /> for the method.
/// </remarks>
/// <param name="EntryLabel">The <see cref="BlockLabel" /> of the method's entry <see cref="BasicBlock" />.</param>
internal sealed record RootRegion(BlockLabel EntryLabel) : Region(EntryLabel);

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
internal sealed record ProtectedRegion(BlockLabel EntryLabel, Region Parent) : ExceptionRegion(EntryLabel, Parent);

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
}

// Note that there is no FilterRegion, because Harmony's filter handling is broken.

/// <summary>
///     Represents a <see langword="finally"/> handler region.
/// </summary>
/// <remarks>
///     It is invalid for any <see cref="BasicBlock" /> in a <see cref="FinallyRegion" /> to have incoming
///     <see cref="Edge" />s from outside that region. Control flow can only exit a <see langword="finally"/> region using the
///     <see cref="OpCodes.Endfinally" /> instruction.
/// </remarks>
/// <param name="EntryLabel">The <see cref="BlockLabel" /> of the <see langword="finally"/> region's entry <see cref="BasicBlock" />.</param>
/// <param name="Parent">The <see cref="Region" /> that contains this region.</param>
internal sealed record FinallyRegion(BlockLabel EntryLabel, Region Parent) : HandlerRegion(EntryLabel, Parent);

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
internal sealed record FaultRegion(BlockLabel EntryLabel, Region Parent) : HandlerRegion(EntryLabel, Parent);

/// <summary>
///     Represents a group of exception regions, consisting of a <see cref="Disharmony.Optimizer.ProtectedRegion" /> and one or more
///     <see cref="HandlerRegion" />s.
/// </summary>
/// <remarks>
///     The CLI enforces a number of rules around exception regions and control flow. Control flow can only enter
///     a <see cref="Disharmony.Optimizer.ProtectedRegion" /> through its <see cref="Region.EntryLabel" />. Control flow can only
///     enter a <see cref="HandlerRegion" /> through the action of the exception system; there can't be any
///     explicit <see cref="Edge" />s into a handler region. Control flow can only leave a <see cref="HandlerRegion" />
///     or <see cref="CatchRegion" /> through a <see cref="Leave" />, and control flow can only leave a
///     <see cref="FinallyRegion" /> or <see cref="FaultRegion" /> through a <see cref="Return" /> with a
///     <see cref="Return.IL" /> of <see cref="OpCodes.Endfinally" />. However, a <see cref="Leave"/> <i>can</i> transfer control from
///     a <see cref="CatchRegion"/> to any <see cref="BasicBlock"/> in the associated <see cref="ProtectedRegion"/>.
/// </remarks>
/// <param name="ProtectedRegion">The protected region.</param>
/// <param name="HandlerRegions">The handlers associated with <paramref name="ProtectedRegion" />.</param>
internal sealed record ExceptionGroup(ProtectedRegion ProtectedRegion, IReadOnlyList<HandlerRegion> HandlerRegions);

/// <summary>
///     Provides a name for a <see cref="BasicBlock" />.
/// </summary>
/// <remarks>
///     In order to allow most data elements to be immutable, references to a <see cref="BasicBlock" /> are stored as the
///     block's label rather than as a direct reference to the block. This allows a <see cref="BasicBlock" /> to be
///     replaced with a new block with the same label without updating other data structures.
/// </remarks>
/// <param name="label">The original IL label, or <see langword="null" /> if the block did not have an IL label.</param>
internal sealed class BlockLabel(Label? label = null)
{
    public Label? Label { get; } = label;
}

/// <summary>
///     Represents the transfer of control to the next <see cref="BasicBlock" /> at the end of a <see cref="BasicBlock" />.
/// </summary>
/// <remarks>
///     Branch-type <see cref="ILOp" />s must be represented as a <see cref="Branch" />. Exceptions are not represented in
///     the <see cref="BasicBlock" /> structure, except for unconditional throws.
/// </remarks>
/// <param name="Labels">The <see cref="BlockLabel" />s of the possible successor <see cref="BasicBlock" />s.</param>
internal abstract record Branch(IReadOnlyList<BlockLabel> Labels);

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
}

/// <summary>
///     Represents transfer of control by the <see cref="OpCodes.Leave" /> instruction.
/// </summary>
/// <remarks>
///     Unlike regular transfer of control, <see cref="OpCodes.Leave" /> is permitted to exit an exception handler region.
///     No <see cref="StackSlot" />s can be live when a leave is taken.
/// </remarks>
/// <param name="Label">The <see cref="BlockLabel" /> of the branch target.</param>
internal record Leave(BlockLabel Label) : UnconditionalBranch(Label);

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
internal sealed record ConditionalBranch(OpCode OpCode, IReadOnlyList<Op> Inputs, IReadOnlyList<BlockLabel> Labels) : Branch(Labels);

/// <summary>
///     Represents throwing an exception.
/// </summary>
/// <remarks>
///     Exceptional control transfer is not represented as <see cref="Edge" />s, so a <see cref="BasicBlock" /> ending in a
///     <see cref="Throw" /> has no outgoing edges.
/// </remarks>
/// <param name="Exception">The <see cref="Op" /> that produces the exception to throw.</param>
internal sealed record Throw(Op Exception) : Branch([]);

internal sealed record Rethrow() : Branch([]);

/// <summary>
///     Represents returning from a method.
/// </summary>
/// <param name="IL">
///     The instruction that generated the return, which can be <see cref="OpCodes.Ret" /> or
///     <see cref="OpCodes.Endfinally" />.
/// </param>
/// <param name="Value">The <see cref="Op" /> that produces the return value, or a <see cref="VoidOp" /> for a void return.</param>
internal sealed record Return(ILInstruction IL, Op Value) : Branch([]);

/// <summary>
///     Represents <see cref="OpCodes.Jmp" />.
/// </summary>
/// <param name="Value"></param>
internal sealed record Jump(Op Value) : Branch([]);

/// <summary>
///     Represents a basic block.
/// </summary>
/// <param name="Label">The block's <see cref="BlockLabel" />.</param>
/// <param name="Ops">The <see cref="Op" />s executed by the block.</param>
/// <param name="Region">The <see cref="Region" /> containing the block.</param>
/// <param name="Branch">The transfer of control at the end of the block.</param>
internal sealed record BasicBlock(BlockLabel Label, IReadOnlyList<Op> Ops, Region Region, Branch Branch);

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
internal sealed record Edge(BlockLabel Source, BlockLabel Destination, IReadOnlyList<AssignmentOp> EdgeAssignments);
