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
    /// <param name="block"></param>
    /// <returns></returns>
    public IEnumerable<Edge> IncomingEdges(BasicBlock block) => edgesTo[block.Label];

    /// <summary>
    ///     Returns all incoming edges for the block with the given label.
    /// </summary>
    /// <param name="label"></param>
    /// <returns></returns>
    public IEnumerable<Edge> IncomingEdges(BlockLabel label) => edgesTo[label];

    /// <summary>
    ///     Returns all outgoing edges for a basic block.
    /// </summary>
    /// <param name="block"></param>
    /// <returns></returns>
    public IEnumerable<Edge> OutgoingEdges(BasicBlock block) => edgesFrom[block.Label];

    /// <summary>
    ///     Returns all outgoing edges for the block with the given label.
    /// </summary>
    /// <param name="label"></param>
    /// <returns></returns>
    public IEnumerable<Edge> OutgoingEdges(BlockLabel label) => edgesFrom[label];

    /// <summary>
    ///     Gets the edge from the block with the given source label to the block with the given destination label.
    /// </summary>
    /// <param name="source"></param>
    /// <param name="destination"></param>
    /// <returns></returns>
    public Edge GetEdge(BlockLabel source, BlockLabel destination) => edges[(source, destination)];

    /// <summary>
    ///     Gets the block with the givene label.
    /// </summary>
    /// <param name="label"></param>
    /// <returns></returns>
    public BasicBlock GetBlock(BlockLabel label) => basicBlocks[label];

    /// <summary>
    ///     Gets the exception group containing the given region.
    /// </summary>
    /// <param name="region"></param>
    /// <returns></returns>
    public ExceptionGroup GetExceptionGroup(ExceptionRegion region) => exceptionGroupsByRegion[region];

    /// <summary>
    ///     Gets the next region in order in the exception group containing the given region, or null if it is the last region
    ///     in the group.
    /// </summary>
    /// <param name="region"></param>
    /// <returns></returns>
    public ExceptionRegion? GetNextRegion(ExceptionRegion region) => nextRegion[region];

    /// <summary>
    ///     Adds a basic block to the control flow graph.
    /// </summary>
    /// <remarks>
    ///     It is the caller's responsibility to add the <see cref="Edge"/>s for the new block.
    /// </remarks>
    /// <param name="block"></param>
    public void AddBlock(BasicBlock block)
    {
        basicBlocks.Add(block.Label, block);
    }

    /// <summary>
    ///     Replaces an existing block with a new block with the same label.
    /// </summary>
    /// <remarks>
    ///     It is the caller's responsibility to update the <see cref="Edge"/>s for the replaced block if necessary.
    /// </remarks>
    /// <param name="block"></param>
    /// <exception cref="KeyNotFoundException"></exception>
    public void ReplaceBlock(BasicBlock block)
    {
        RemoveBlock(block.Label);
        AddBlock(block);
    }

    /// <summary>
    ///     Removes a block from the control flow graph.
    /// </summary>
    /// <remarks>
    ///     It is the caller's responsibility to remove the <see cref="Edge"/>s for the removed block.
    /// </remarks>
    /// <param name="block"></param>
    /// <exception cref="InvalidOperationException"></exception>
    public void RemoveBlock(BasicBlock block)
    {
        if (basicBlocks[block.Label] != block)
            throw new InvalidOperationException();
        basicBlocks.Remove(block.Label);
    }

    /// <summary>
    ///     Removes the block with the given label from the control flow graph
    /// </summary>
    /// <remarks>
    ///     It is the caller's responsibility to remove the <see cref="Edge"/>s for the removed block.
    /// </remarks>
    /// <param name="label"></param>
    public void RemoveBlock(BlockLabel label) => basicBlocks.Remove(label);

    /// <summary>
    ///     Adds an edge to the control flow graph
    /// </summary>
    /// <param name="edge"></param>
    /// <exception cref="InvalidOperationException"></exception>
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
    ///     Replaces an existing edge with a new edge with the same source and destination labels
    /// </summary>
    /// <param name="edge"></param>
    public void ReplaceEdge(Edge edge)
    {
        RemoveEdge(edges[(edge.Source, edge.Destination)]);
        AddEdge(edge);
    }

    /// <summary>
    ///     Removes an edge from the control flow graph
    /// </summary>
    /// <param name="edge"></param>
    /// <exception cref="InvalidOperationException"></exception>
    public void RemoveEdge(Edge edge)
    {
        if (edges[(edge.Source, edge.Destination)] != edge)
            throw new InvalidOperationException();

        edges.Remove((edge.Source, edge.Destination));
        edgesFrom[edge.Source].Remove(edge);
        edgesTo[edge.Destination].Remove(edge);
    }

    /// <summary>
    ///     Removes the edge with the given source and destination labels
    /// </summary>
    /// <param name="source"></param>
    /// <param name="destination"></param>
    public void RemoveEdge(BlockLabel source, BlockLabel destination) => RemoveEdge(GetEdge(source, destination));

    /// <summary>
    ///     Adds an exception group to the control flow graph
    /// </summary>
    /// <param name="group"></param>
    /// <exception cref="InvalidOperationException"></exception>
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
}

/// <summary>
///     Represents a region, which can be the root region or an exception region.
/// </summary>
/// <remarks>
///     Every basic block belongs to a region, and all regions except the root region are contained in a parent region.
/// </remarks>
/// <param name="EntryLabel"></param>
internal abstract record Region(BlockLabel EntryLabel);

/// <summary>
///     The root region which all blocks ultimately belong to.
/// </summary>
/// <remarks>
///     The <paramref name="EntryLabel" /> of the root region is the entry block for the method.
/// </remarks>
/// <param name="EntryLabel"></param>
internal sealed record RootRegion(BlockLabel EntryLabel) : Region(EntryLabel);

/// <summary>
///     Base class for exception regions.
/// </summary>
/// <param name="EntryLabel"></param>
/// <param name="Parent"></param>
internal abstract record ExceptionRegion(BlockLabel EntryLabel, Region Parent) : Region(EntryLabel);

/// <summary>
///     Represents a try block.
/// </summary>
/// <remarks>
///     It is valid for the entry block of a try region to have incoming edges from outside the try region,
///     but all other blocks in the try region must only have incoming edges from within the try region.
/// </remarks>
/// <param name="EntryLabel"></param>
/// <param name="Parent"></param>
internal sealed record TryRegion(BlockLabel EntryLabel, Region Parent) : ExceptionRegion(EntryLabel, Parent);

/// <summary>
///     Represents a catch block.
/// </summary>
/// <remarks>
///     It is invalid for any block in a catch region to have incoming edges from outside the catch region.
///     On entry to the catch region, <paramref name="IncomingException" /> is the exception object that was thrown.
/// </remarks>
/// <param name="EntryLabel"></param>
/// <param name="Parent"></param>
/// <param name="ExceptionType"></param>
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
/// <param name="EntryLabel"></param>
/// <param name="Parent"></param>
internal sealed record FinallyRegion(BlockLabel EntryLabel, Region Parent) : ExceptionRegion(EntryLabel, Parent);

/// <summary>
///     Represents a group of exception regions, consisting of a try region and one or more handler regions.
/// </summary>
/// <param name="TryRegion"></param>
/// <param name="HandlerRegions"></param>
internal sealed record ExceptionGroup(TryRegion TryRegion, IReadOnlyList<ExceptionRegion> HandlerRegions);

/// <summary>
///     Provides a name for a block.
/// </summary>
/// <remarks>
///     In order to allow most data elements to be immutable, references to a block are stored as the block's label
///     rather than as a direct reference to a block. This allows a block to be replaced with a new block with the same
///     label without updating other data structures.
/// </remarks>
/// <param name="Label"></param>
internal sealed record BlockLabel(Label? Label = null);

/// <summary>
///     Represents the transfer of control to the next block at the end of a block.
/// </summary>
/// <remarks>
///     Branch-type IL ops must be represented as a branch. Exceptions are not represented in the basic block structure,
///     except for unconditional throw.
/// </remarks>
/// <param name="Labels"></param>
internal abstract record Branch(IReadOnlyList<BlockLabel> Labels);

/// <summary>
///     Represents unconditional transfer of control.
/// </summary>
internal record UnconditionalBranch : Branch
{
    public BlockLabel Label => Labels[0];
    public UnconditionalBranch(BlockLabel label) : base([label]) { }
}

/// <summary>
///     Represents transfer of control by the <see cref="OpCodes.Leave" /> instruction.
/// </summary>
/// <remarks>
///     Unlike regular transfer of control, <see cref="OpCodes.Leave" /> is permitted to exit an exception handler region.
///     No stack slots can be live when a leave is taken.
/// </remarks>
/// <param name="Label"></param>
internal record Leave(BlockLabel Label) : UnconditionalBranch(Label);

/// <summary>
///     Represents a conditional transfer of control.
/// </summary>
/// <remarks>
///     <paramref name="Labels[0]" /> represents the fallthrough block. For an ordinary conditional branch,
///     <paramref name="Labels[1]" /> represents the taken branch target, while for a switch, the remaining
///     elements of <paramref name="Labels" /> represent switch targets.
/// </remarks>
/// <param name="Labels"></param>
/// <param name="OpCode"></param>
internal sealed record ConditionalBranch(IReadOnlyList<BlockLabel> Labels, OpCode OpCode) : Branch(Labels);

/// <summary>
///     Represents throwing an exception.
/// </summary>
/// <remarks>
///     Exceptional control transfer is not represented as edges, so a block ending in a throw has no outgoing edges.
/// </remarks>
/// <param name="Exception"></param>
internal sealed record Throw(Op Exception) : Branch([]);

/// <summary>
///     Represents returning from a method.
/// </summary>
/// <param name="Value"></param>
internal sealed record Return(Op Value) : Branch([]);

/// <summary>
///     Represents a basic block.
/// </summary>
/// <param name="Label"></param>
/// <param name="Ops"></param>
/// <param name="Region"></param>
/// <param name="Branch"></param>
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
/// <param name="Source"></param>
/// <param name="Destination"></param>
/// <param name="EdgeAssignments"></param>
internal sealed record Edge(BlockLabel Source, BlockLabel Destination, IReadOnlyList<AssignmentOp> EdgeAssignments);
