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

    public void RemoveExceptionGroup(ExceptionGroup group)
    {
        if (!exceptionGroups.Remove(group))
            throw new InvalidOperationException();

        ExceptionRegion[] regions = [group.ProtectedRegion, .. group.HandlerRegions];
        foreach (ExceptionRegion region in regions)
        {
            exceptionGroupsByRegion.Remove(region);
            nextRegion.Remove(region);
        }
    }

    public void ReplaceExceptionGroup(ExceptionGroup group)
    {
        RemoveExceptionGroup(GetExceptionGroup(group.ProtectedRegion));
        AddExceptionGroup(group);
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

        foreach (var block in BasicBlocks)
        {
            var region = block.Region;
            while (region is ExceptionRegion exceptionRegion)
            {
                if (!exceptionGroupsByRegion.ContainsKey(exceptionRegion))
                    throw new InvalidOperationException("Region not in group");
                region = exceptionRegion.Parent;
            }
        }
    }
}
