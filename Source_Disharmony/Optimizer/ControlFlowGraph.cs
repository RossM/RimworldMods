using System.Diagnostics;

namespace Disharmony.Optimizer;

internal class ControlFlowGraph
{
    /// <summary>
    ///     Returns all basic blocks in the control flow graph.
    /// </summary>
    public IEnumerable<BasicBlock> BasicBlocks => basicBlocks.Values;

    /// <summary>
    ///     Returns all edges in the control flow graph.
    /// </summary>
    public IEnumerable<Edge> Edges => edges.Values;

    /// <summary>
    ///     Returns all exception groups in the control flow graph.
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
    public RootRegion RootRegion { get; } = new RootRegion(new BlockLabel());

    /// <summary>
    ///     Returns all incoming edges for a basic block.
    /// </summary>
    /// <param name="block">The block whose incoming edges to return.</param>
    /// <returns>The edges whose destination is <paramref name="block" />.</returns>
    /// <exception cref="KeyNotFoundException"><paramref name="block" /> is not present in the graph.</exception>
    public IEnumerable<Edge> IncomingEdges(BasicBlock block) => edgesTo[block.Label];

    /// <summary>
    ///     Returns all incoming edges for the block with the given <see cref="BlockLabel" />.
    /// </summary>
    /// <param name="label">The label of the block whose incoming edges to return.</param>
    /// <returns>The edges whose destination is <paramref name="label" />.</returns>
    /// <exception cref="KeyNotFoundException"><paramref name="label" /> is not present in the graph.</exception>
    public IEnumerable<Edge> IncomingEdges(BlockLabel label) => edgesTo[label];

    /// <summary>
    ///     Returns all outgoing edges for a basic block.
    /// </summary>
    /// <param name="block">The block whose outgoing edges to return.</param>
    /// <returns>The edges whose source is <paramref name="block" />.</returns>
    /// <exception cref="KeyNotFoundException"><paramref name="block" /> is not present in the graph.</exception>
    public IEnumerable<Edge> OutgoingEdges(BasicBlock block) => edgesFrom[block.Label];

    /// <summary>
    ///     Returns all outgoing edges for the block with the given <see cref="BlockLabel" />.
    /// </summary>
    /// <param name="label">The label of the block whose outgoing edges to return.</param>
    /// <returns>The edges whose source is <paramref name="label" />.</returns>
    /// <exception cref="KeyNotFoundException"><paramref name="label" /> is not present in the graph.</exception>
    public IEnumerable<Edge> OutgoingEdges(BlockLabel label) => edgesFrom[label];

    /// <summary>
    ///     Gets all <see cref="BasicBlock" />s with edges to <paramref name="block" />.
    /// </summary>
    /// <param name="block">The block whose predecessors to return.</param>
    /// <returns>The blocks with edges to <paramref name="block" />.</returns>
    /// <exception cref="KeyNotFoundException"><paramref name="block" /> is not present in the graph.</exception>
    public IEnumerable<BasicBlock> Predecessors(BasicBlock block) => edgesTo[block.Label].Select(edge => basicBlocks[edge.Source]);

    /// <summary>
    ///     Gets the <see cref="BlockLabel" />s of all <see cref="BasicBlock" />s with edges to the block with
    ///     <see cref="BlockLabel" /> <paramref name="label" />.
    /// </summary>
    /// <param name="label">The label of the block whose predecessor labels to return.</param>
    /// <returns>The labels of the blocks with edges to the block identified by <paramref name="label" />.</returns>
    /// <exception cref="KeyNotFoundException"><paramref name="label" /> is not present in the graph.</exception>
    public IEnumerable<BlockLabel> Predecessors(BlockLabel label) => edgesTo[label].Select(edge => edge.Source);

    /// <summary>
    ///     Gets all <see cref="BasicBlock" />s with edges from <paramref name="block" />.
    /// </summary>
    /// <param name="block">The block whose successors to return.</param>
    /// <returns>The blocks with edges from <paramref name="block" />.</returns>
    /// <exception cref="KeyNotFoundException"><paramref name="block" /> is not present in the graph.</exception>
    public IEnumerable<BasicBlock> Successors(BasicBlock block) => edgesFrom[block.Label].Select(edge => basicBlocks[edge.Destination]);

    /// <summary>
    ///     Gets the <see cref="BlockLabel" />s of all <see cref="BasicBlock" />s with edges from the block with
    ///     <see cref="BlockLabel" /> <paramref name="label" />.
    /// </summary>
    /// <param name="label">The label of the block whose successor labels to return.</param>
    /// <returns>The labels of the blocks with edges from the block identified by <paramref name="label" />.</returns>
    /// <exception cref="KeyNotFoundException"><paramref name="label" /> is not present in the graph.</exception>
    public IEnumerable<BlockLabel> Successors(BlockLabel label) => edgesFrom[label].Select(edge => edge.Destination);

    /// <summary>
    ///     Gets the edge from the block with the given source <see cref="BlockLabel" /> to the block with the given
    ///     destination <see cref="BlockLabel" />.
    /// </summary>
    /// <param name="source">The source block label.</param>
    /// <param name="destination">The destination block label.</param>
    /// <returns>The edge from <paramref name="source" /> to <paramref name="destination" />.</returns>
    /// <exception cref="KeyNotFoundException">No edge exists between the specified blocks.</exception>
    public Edge GetEdge(BlockLabel source, BlockLabel destination) => edges[(source, destination)];

    /// <summary>
    ///     Gets the edge from the given source block to the given destination block.
    /// </summary>
    /// <param name="source">The source block.</param>
    /// <param name="destination">The destination block.</param>
    /// <returns>The edge from <paramref name="source" /> to <paramref name="destination" />.</returns>
    /// <exception cref="KeyNotFoundException">No edge exists between the specified blocks.</exception>
    public Edge GetEdge(BasicBlock source, BasicBlock destination) => edges[(source.Label, destination.Label)];

    /// <summary>
    ///     Gets the edge from the block with the given source <see cref="BlockLabel" /> to the block with the given
    ///     destination <see cref="BlockLabel" />,
    ///     or <see langword="null" /> if no edge exists.
    /// </summary>
    /// <param name="source">The source block label.</param>
    /// <param name="destination">The destination block label.</param>
    /// <returns>The edge between the specified blocks, or <see langword="null" /> if no such edge exists.</returns>
    public Edge? GetEdgeOrNull(BlockLabel source, BlockLabel destination)
    {
        edges.TryGetValue((source, destination), out Edge? result);
        return result;
    }

    /// <summary>
    ///     Gets the edge from the given source block to the given destination block, or <see langword="null" /> if no edge
    ///     exists.
    /// </summary>
    /// <param name="source">The source block.</param>
    /// <param name="destination">The destination block.</param>
    /// <returns>The edge between the specified blocks, or <see langword="null" /> if no such edge exists.</returns>
    public Edge? GetEdgeOrNull(BasicBlock source, BasicBlock destination) => GetEdgeOrNull(source.Label, destination.Label);

    /// <summary>
    ///     Gets the block with the given <see cref="BlockLabel" />.
    /// </summary>
    /// <param name="label">The label of the block to return.</param>
    /// <returns>The block identified by <paramref name="label" />.</returns>
    /// <exception cref="KeyNotFoundException">No block with the specified label exists.</exception>
    public BasicBlock GetBlock(BlockLabel label) => basicBlocks[label];

    /// <summary>
    ///     Gets the exception group containing the given region.
    /// </summary>
    /// <param name="region">The exception region whose group to return.</param>
    /// <returns>The exception group containing <paramref name="region" />.</returns>
    /// <exception cref="KeyNotFoundException"><paramref name="region" /> does not belong to a group in the graph.</exception>
    public ExceptionGroup GetExceptionGroup(ExceptionRegion region) => exceptionGroupsByRegion[region];

    /// <summary>
    ///     Gets the next region in order in the exception group containing the given region, or <see langword="null" /> if it
    ///     is the last region
    ///     in the group.
    /// </summary>
    /// <param name="region">The exception region whose successor to return.</param>
    /// <returns>The next region in the group, or <see langword="null" /> if <paramref name="region" /> is the last region.</returns>
    /// <exception cref="KeyNotFoundException"><paramref name="region" /> does not belong to a group in the graph.</exception>
    public ExceptionRegion? GetNextRegion(ExceptionRegion region) => nextRegion[region];

    /// <summary>
    ///     Adds a basic block to the control flow graph.
    /// </summary>
    /// <remarks>
    ///     It is the caller's responsibility to add the <see cref="Edge" />s for the new block.
    /// </remarks>
    /// <param name="block">The block to add.</param>
    /// <exception cref="ArgumentException">A block with the same <see cref="BasicBlock.Label" /> already exists.</exception>
    public void AddBlock(BasicBlock block)
    {
        basicBlocks.Add(block.Label, block);
    }

    /// <summary>
    ///     Replaces an existing block with a new block with the same <see cref="BlockLabel" />.
    /// </summary>
    /// <remarks>
    ///     It is the caller's responsibility to update the <see cref="Edge" />s for the replaced block if necessary.
    /// </remarks>
    /// <param name="block">The replacement block.</param>
    /// <exception cref="KeyNotFoundException">No block with the same <see cref="BasicBlock.Label" /> exists.</exception>
    public void ReplaceBlock(BasicBlock block)
    {
        RemoveBlock(block.Label);
        AddBlock(block);
    }

    /// <summary>
    ///     Removes a block from the control flow graph.
    /// </summary>
    /// <remarks>
    ///     It is the caller's responsibility to remove the <see cref="Edge" />s for the removed block.
    /// </remarks>
    /// <param name="block">The block to remove.</param>
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
    ///     Removes the block with the given <see cref="BlockLabel" /> from the control flow graph.
    /// </summary>
    /// <remarks>
    ///     It is the caller's responsibility to remove the <see cref="Edge" />s for the removed block.
    /// </remarks>
    /// <param name="label">The label of the block to remove.</param>
    public void RemoveBlock(BlockLabel label) => basicBlocks.Remove(label);

    /// <summary>
    ///     Adds an edge to the control flow graph.
    /// </summary>
    /// <param name="edge">The edge to add.</param>
    /// <exception cref="InvalidOperationException">An edge with the same source and destination already exists.</exception>
    public void AddEdge(Edge edge)
    {
        if (edges.ContainsKey((edge.Source, edge.Destination)))
            throw new InvalidOperationException();

        if (!edgesFrom.ContainsKey(edge.Source))
            edgesFrom[edge.Source] = [];
        if (!edgesTo.ContainsKey(edge.Destination))
            edgesTo[edge.Destination] = [];

        edges[(edge.Source, edge.Destination)] = edge;
        edgesFrom[edge.Source].Add(edge);
        edgesTo[edge.Destination].Add(edge);
    }

    /// <summary>
    ///     Replaces an existing edge with a new edge with the same source and destination labels.
    /// </summary>
    /// <param name="edge">The replacement edge.</param>
    /// <exception cref="KeyNotFoundException">No edge with the same source and destination exists.</exception>
    public void ReplaceEdge(Edge edge)
    {
        RemoveEdge(edges[(edge.Source, edge.Destination)]);
        AddEdge(edge);
    }

    /// <summary>
    ///     Removes an edge from the control flow graph.
    /// </summary>
    /// <param name="edge">The edge to remove.</param>
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
    ///     Removes the edge with the given source and destination labels.
    /// </summary>
    /// <param name="source">The source block label.</param>
    /// <param name="destination">The destination block label.</param>
    /// <exception cref="KeyNotFoundException">No edge exists between the specified blocks.</exception>
    public void RemoveEdge(BlockLabel source, BlockLabel destination) => RemoveEdge(GetEdge(source, destination));

    /// <summary>
    ///     Adds an exception group to the control flow graph.
    /// </summary>
    /// <param name="group">The exception group to add.</param>
    /// <exception cref="InvalidOperationException"><paramref name="group" /> is already present in the graph.</exception>
    public void AddExceptionGroup(ExceptionGroup group)
    {
        if (!exceptionGroups.Add(group))
            throw new InvalidOperationException();

        ExceptionRegion[] regions = [group.TryRegion, .. group.HandlerRegions];
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
///     Every basic block belongs to a region, and all regions except the root region are contained in a parent region.
/// </remarks>
/// <param name="EntryLabel">The label of the region's entry block.</param>
internal abstract record Region(BlockLabel EntryLabel);

/// <summary>
///     The root region which all blocks ultimately belong to.
/// </summary>
/// <remarks>
///     The <paramref name="EntryLabel" /> of the root region is the entry block for the method.
/// </remarks>
/// <param name="EntryLabel">The label of the method's entry block.</param>
internal sealed record RootRegion(BlockLabel EntryLabel) : Region(EntryLabel);

/// <summary>
///     Base class for exception regions.
/// </summary>
/// <param name="EntryLabel">The label of the region's entry block.</param>
/// <param name="Parent">The region that contains this region.</param>
internal abstract record ExceptionRegion(BlockLabel EntryLabel, Region Parent) : Region(EntryLabel);

/// <summary>
///     Represents a try block.
/// </summary>
/// <remarks>
///     It is valid for the entry block of a try region to have incoming edges from outside the try region,
///     but all other blocks in the try region must only have incoming edges from within the try region.
/// </remarks>
/// <param name="EntryLabel">The label of the try region's entry block.</param>
/// <param name="Parent">The region that contains this region.</param>
internal sealed record TryRegion(BlockLabel EntryLabel, Region Parent) : ExceptionRegion(EntryLabel, Parent);

/// <summary>
///     Represents a catch block.
/// </summary>
/// <remarks>
///     It is invalid for any block in a catch region to have incoming edges from outside the catch region.
///     On entry to the catch region, <paramref name="IncomingException" /> is the exception object that was thrown.
/// </remarks>
/// <param name="EntryLabel">The label of the catch region's entry block.</param>
/// <param name="Parent">The region that contains this region.</param>
/// <param name="IncomingException">The stack slot containing the exception on entry to the handler.</param>
internal sealed record CatchRegion(BlockLabel EntryLabel, Region Parent, StackSlot IncomingException) : ExceptionRegion(EntryLabel, Parent)
{
    public Type ExceptionType => IncomingException.Type;
}

/// <summary>
///     Represents a finally block.
/// </summary>
/// <remarks>
///     It is invalid for any block in a finally region to have incoming edges from outside the finally region.
/// </remarks>
/// <param name="EntryLabel">The label of the finally region's entry block.</param>
/// <param name="Parent">The region that contains this region.</param>
internal sealed record FinallyRegion(BlockLabel EntryLabel, Region Parent) : ExceptionRegion(EntryLabel, Parent);

/// <summary>
///     Represents a group of exception regions, consisting of a try region and one or more handler regions.
/// </summary>
/// <param name="TryRegion">The protected try region.</param>
/// <param name="HandlerRegions">The handlers associated with <paramref name="TryRegion" />.</param>
internal sealed record ExceptionGroup(TryRegion TryRegion, IReadOnlyList<ExceptionRegion> HandlerRegions);

/// <summary>
///     Provides a name for a block.
/// </summary>
/// <remarks>
///     In order to allow most data elements to be immutable, references to a block are stored as the block's label
///     rather than as a direct reference to a block. This allows a block to be replaced with a new block with the same
///     label without updating other data structures.
/// </remarks>
/// <param name="Label">The original IL label, or <see langword="null" /> if the block did not have an IL label.</param>
internal sealed record BlockLabel(Label? Label = null);

/// <summary>
///     Represents the transfer of control to the next block at the end of a block.
/// </summary>
/// <remarks>
///     Branch-type IL ops must be represented as a branch. Exceptions are not represented in the basic block structure,
///     except for unconditional throw.
/// </remarks>
/// <param name="Labels">The labels of the possible successor blocks.</param>
internal abstract record Branch(IReadOnlyList<BlockLabel> Labels);

/// <summary>
///     Represents unconditional transfer of control.
/// </summary>
internal record UnconditionalBranch : Branch
{
    /// <summary>Gets the label of the branch target.</summary>
    public BlockLabel Label => Labels[0];

    /// <summary>Initializes a branch to the specified target.</summary>
    /// <param name="label">The label of the branch target.</param>
    public UnconditionalBranch(BlockLabel label) : base([label]) { }
}

/// <summary>
///     Represents transfer of control by the <see cref="OpCodes.Leave" /> instruction.
/// </summary>
/// <remarks>
///     Unlike regular transfer of control, <see cref="OpCodes.Leave" /> is permitted to exit an exception handler region.
///     No stack slots can be live when a leave is taken.
/// </remarks>
/// <param name="Label">The label of the branch target.</param>
internal record Leave(BlockLabel Label) : UnconditionalBranch(Label);

/// <summary>
///     Represents a conditional transfer of control.
/// </summary>
/// <remarks>
///     <c>Labels[0]</c> represents the fallthrough block. For an ordinary conditional branch,
///     <c>Labels[1]</c> represents the taken branch target, while for a switch, the remaining
///     elements of <paramref name="Labels" /> represent switch targets.
/// </remarks>
/// <param name="Labels">The fallthrough and branch-target labels.</param>
/// <param name="OpCode">The conditional branch or switch opcode.</param>
internal sealed record ConditionalBranch(IReadOnlyList<BlockLabel> Labels, OpCode OpCode) : Branch(Labels);

/// <summary>
///     Represents throwing an exception.
/// </summary>
/// <remarks>
///     Exceptional control transfer is not represented as edges, so a block ending in a throw has no outgoing edges.
/// </remarks>
/// <param name="Exception">The operation that produces the exception to throw.</param>
internal sealed record Throw(Op Exception) : Branch([]);

/// <summary>
///     Represents returning from a method.
/// </summary>
/// <param name="Value">The operation that produces the return value, or a <see cref="VoidOp" /> for a void return.</param>
internal sealed record Return(Op Value) : Branch([]);

/// <summary>
///     Represents a basic block.
/// </summary>
/// <param name="Label">The block's label.</param>
/// <param name="Ops">The operations executed by the block.</param>
/// <param name="Region">The region containing the block.</param>
/// <param name="Branch">The transfer of control at the end of the block.</param>
internal sealed record BasicBlock(BlockLabel Label, IReadOnlyList<Op> Ops, Region Region, Branch Branch);

/// <summary>
///     Represents an edge between basic blocks.
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
/// <param name="Source">The label of the source block.</param>
/// <param name="Destination">The label of the destination block.</param>
/// <param name="EdgeAssignments">The assignments performed while control transfers across the edge.</param>
internal sealed record Edge(BlockLabel Source, BlockLabel Destination, IReadOnlyList<AssignmentOp> EdgeAssignments);
