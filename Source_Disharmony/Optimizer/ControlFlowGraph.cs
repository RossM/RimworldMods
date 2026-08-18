using System.Diagnostics;

namespace Disharmony.Optimizer;

internal record ControlFlowGraph : Node
{
    public IEnumerable<ExceptionGroup> ExceptionGroups => exceptionGroups;
    public RootRegion RootRegion { get; }
    public IReadOnlyList<BasicBlock> BasicBlocks { get; }
    public IReadOnlyList<Edge> Edges { get; }
    public IReadOnlyList<Argument> Arguments { get; }
    public IReadOnlyList<Local> Locals { get; }

    private readonly HashSet<ExceptionGroup> exceptionGroups = [];
    private readonly Dictionary<ExceptionRegion, ExceptionGroup> exceptionGroupsByRegion = [];
    private readonly Dictionary<ExceptionRegion, ExceptionRegion?> nextRegion = [];
    private readonly Dictionary<(BlockLabel Source, BlockLabel Destination), Edge> edges = [];
    private readonly Dictionary<BlockLabel, HashSet<Edge>> edgesFrom = [];
    private readonly Dictionary<BlockLabel, HashSet<Edge>> edgesTo = [];
    private readonly Dictionary<BlockLabel, BasicBlock> basicBlocks = [];

    public ControlFlowGraph(RootRegion RootRegion, IReadOnlyList<BasicBlock> BasicBlocks, IReadOnlyList<Edge> Edges, IReadOnlyList<Argument> Arguments, IReadOnlyList<Local> Locals, bool validate = true)
    {
        this.RootRegion = RootRegion;
        this.BasicBlocks = BasicBlocks;
        this.Edges = Edges;
        this.Arguments = Arguments;
        this.Locals = Locals;

        foreach (var block in BasicBlocks)
            AddBlock(block);
        foreach (var edge in Edges)
            AddEdge(edge);

        if (validate)
            Validate();
    }

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

    private void AddBlock(BasicBlock block)
    {
        basicBlocks.Add(block.Label, block);

        if (!edgesFrom.ContainsKey(block.Label))
            edgesFrom[block.Label] = [];
        if (!edgesTo.ContainsKey(block.Label))
            edgesTo[block.Label] = [];

        for (ExceptionRegion? region = block.Region as ExceptionRegion; region != null; region = region.Parent as ExceptionRegion)
        {
            if (region is ProtectedRegion protectedRegion && !exceptionGroupsByRegion.ContainsKey(protectedRegion))
                AddProtectedRegion(protectedRegion);
        }
    }

    private void AddEdge(Edge edge)
    {
        if (edges.ContainsKey((edge.Source, edge.Destination)))
            throw new InvalidOperationException();

        edges[(edge.Source, edge.Destination)] = edge;
        edgesFrom[edge.Source].Add(edge);
        edgesTo[edge.Destination].Add(edge);
    }

    private void AddProtectedRegion(ProtectedRegion protectedRegion)
    {
        var group = protectedRegion.Group;
        exceptionGroups.Add(group);
        
        ExceptionRegion[] regions = [protectedRegion, .. group.HandlerRegions];
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
    private void Validate()
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

    public virtual bool Equals(ControlFlowGraph? other)
    {
        if (other is null)
            return false;
        if (ReferenceEquals(this, other))
            return true;
        return RootRegion.Equals(other.RootRegion) && BasicBlocks.Equals(other.BasicBlocks) && Edges.Equals(other.Edges);
    }

    public override T Accept<T>(IVisitor<T> visitor) => visitor.Visit(this);

    public override int GetHashCode()
    {
        unchecked
        {
            int hashCode = RootRegion.GetHashCode();
            hashCode = (hashCode * 397) ^ BasicBlocks.GetHashCode();
            hashCode = (hashCode * 397) ^ Edges.GetHashCode();
            return hashCode;
        }
    }
}
