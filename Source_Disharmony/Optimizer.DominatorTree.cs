namespace Disharmony;

internal partial class Optimizer
{
    /// <summary>
    ///     Immutable block-dominance information for one version of the optimizer's CFG. The
    ///     computation uses an artificial root because exception handlers and filters have
    ///     implicit entries which are not represented by normal control-flow edges. A pass which
    ///     mutates the CFG must discard any captured instance; clearing the optimizer's cache
    ///     cannot revoke references already held by a worker.
    /// </summary>
    internal sealed class DominatorTree
    {
        private readonly record struct TreeInterval(int Start, int End);

        private sealed class Node
        {
            public BasicBlock? immediateDominator;
            public BasicBlock[] children = [];

            // A node's dominator-tree descendants occupy [interval.Start, interval.End). This is the
            // canonical representation used by Dominates; full dominator sets are not retained.
            public TreeInterval interval;
        }

        private readonly Dictionary<BasicBlock, Node> nodes;

        /// <summary>
        ///     Roots of the exposed dominator forest. This includes every CFG entry and may also
        ///     include a join which has no common real dominator across independent entries.
        /// </summary>
        public IReadOnlyList<BasicBlock> Roots { get; }

        private DominatorTree(Dictionary<BasicBlock, Node> nodes, BasicBlock[] roots)
        {
            this.nodes = nodes;
            Roots = roots;
        }

        /// <summary>Returns whether <paramref name="dominator"/> dominates <paramref name="block"/>.</summary>
        public bool Dominates(BasicBlock dominator, BasicBlock block)
        {
            Node dominatorNode = GetNode(dominator);
            Node blockNode = GetNode(block);
            return dominatorNode.interval.Start <= blockNode.interval.Start &&
                   blockNode.interval.Start < dominatorNode.interval.End;
        }

        /// <summary>
        ///     Returns the block's immediate dominator, or null when its immediate dominator is
        ///     the artificial root.
        /// </summary>
        public BasicBlock? GetImmediateDominator(BasicBlock block) => GetNode(block).immediateDominator;

        /// <summary>Returns the block's children in the dominator tree.</summary>
        public IReadOnlyList<BasicBlock> GetChildren(BasicBlock block) => GetNode(block).children;

        private Node GetNode(BasicBlock block)
        {
            if (!nodes.TryGetValue(block, out Node? node))
                throw new ArgumentException("Basic block is not part of this dominator tree", nameof(block));
            return node;
        }

        /// <summary>
        ///     Computes dominance for blocks reachable from <paramref name="entryBlocks"/>. Every
        ///     block must be reachable from at least one entry; callers normally establish this by
        ///     running dead-code removal before requesting the analysis.
        /// </summary>
        internal static DominatorTree Compute(
            IReadOnlyList<BasicBlock> blocks,
            IReadOnlyList<BasicBlock> entryBlocks)
        {
            int blockCount = blocks.Count;
            int artificialRoot = blockCount;
            Dictionary<BasicBlock, int> indexByBlock = new(blockCount);
            for (int index = 0; index < blockCount; index++)
            {
                if (indexByBlock.ContainsKey(blocks[index]))
                    throw new InvalidOperationException("The CFG contains the same basic block more than once");
                indexByBlock.Add(blocks[index], index);
            }

            List<int>[] successors = MakeAdjacencyLists(blockCount + 1);
            List<int>[] predecessors = MakeAdjacencyLists(blockCount + 1);

            for (int sourceIndex = 0; sourceIndex < blockCount; sourceIndex++)
            {
                BasicBlock source = blocks[sourceIndex];
                foreach (var edge in source.outgoingEdges)
                {
                    if (edge.Source != source)
                        throw new InvalidOperationException("A CFG edge is attached to the wrong source block");
                    if (!indexByBlock.TryGetValue(edge.Target, out int targetIndex))
                        throw new InvalidOperationException("A CFG edge targets a block outside the optimizer");

                    successors[sourceIndex].Add(targetIndex);
                    predecessors[targetIndex].Add(sourceIndex);
                }
            }

            HashSet<BasicBlock> distinctEntries = [];
            foreach (var entry in entryBlocks)
            {
                if (!distinctEntries.Add(entry))
                    continue;
                if (!indexByBlock.TryGetValue(entry, out int entryIndex))
                    throw new InvalidOperationException("A dominator entry is not part of the CFG");

                successors[artificialRoot].Add(entryIndex);
                predecessors[entryIndex].Add(artificialRoot);
            }

            if (blockCount > 0 && successors[artificialRoot].Count == 0)
                throw new InvalidOperationException("A nonempty CFG has no dominator entry blocks");

            int[] reversePostorder = ComputeReversePostorder(successors, artificialRoot);
            if (reversePostorder.Length != blockCount + 1)
                throw new InvalidOperationException("The CFG contains a block unreachable from every dominator entry");

            int[] reversePostorderIndex = new int[blockCount + 1];
            for (int index = 0; index < reversePostorder.Length; index++)
                reversePostorderIndex[reversePostorder[index]] = index;

            int[] immediateDominators = [.. Enumerable.Repeat(-1, blockCount + 1)];
            immediateDominators[artificialRoot] = artificialRoot;

            bool changed;
            do
            {
                changed = false;
                for (int reversePostorderPosition = 1;
                     reversePostorderPosition < reversePostorder.Length;
                     reversePostorderPosition++)
                {
                    int block = reversePostorder[reversePostorderPosition];
                    int newImmediateDominator = -1;
                    foreach (int predecessor in predecessors[block])
                    {
                        if (immediateDominators[predecessor] < 0)
                            continue;

                        newImmediateDominator = newImmediateDominator < 0
                            ? predecessor
                            : Intersect(predecessor, newImmediateDominator);
                    }

                    if (newImmediateDominator < 0)
                        throw new InvalidOperationException("No processed predecessor reaches a CFG block");
                    if (immediateDominators[block] == newImmediateDominator)
                        continue;

                    immediateDominators[block] = newImmediateDominator;
                    changed = true;
                }
            } while (changed);

            List<int>[] children = MakeAdjacencyLists(blockCount + 1);
            for (int position = 1; position < reversePostorder.Length; position++)
            {
                int block = reversePostorder[position];
                children[immediateDominators[block]].Add(block);
            }

            TreeInterval[] intervals = NumberTree(children, artificialRoot);
            Dictionary<BasicBlock, Node> nodes = new(blockCount);
            for (int block = 0; block < blockCount; block++)
            {
                int immediateDominator = immediateDominators[block];
                nodes.Add(blocks[block], new Node
                {
                    immediateDominator = immediateDominator == artificialRoot
                        ? null
                        : blocks[immediateDominator],
                    children = [.. children[block].Select(index => blocks[index])],
                    interval = intervals[block],
                });
            }

            BasicBlock[] roots = [.. children[artificialRoot].Select(index => blocks[index])];
            return new(nodes, roots);

            int Intersect(int first, int second)
            {
                while (first != second)
                {
                    while (reversePostorderIndex[first] > reversePostorderIndex[second])
                        first = immediateDominators[first];
                    while (reversePostorderIndex[second] > reversePostorderIndex[first])
                        second = immediateDominators[second];
                }

                return first;
            }
        }

        private static List<int>[] MakeAdjacencyLists(int count)
        {
            List<int>[] result = new List<int>[count];
            for (int index = 0; index < count; index++)
                result[index] = [];
            return result;
        }

        private static int[] ComputeReversePostorder(IReadOnlyList<List<int>> successors, int root)
        {
            bool[] visited = new bool[successors.Count];
            List<int> postorder = new(successors.Count);
            Stack<(int Node, int NextSuccessor)> stack = [];
            visited[root] = true;
            stack.Push((root, 0));

            // An explicit traversal stack avoids consuming the CLR call stack on large inlined
            // methods. A frame is revisited once for each successor and once to emit the node.
            while (stack.Count > 0)
            {
                (int node, int nextSuccessor) = stack.Pop();
                if (nextSuccessor == successors[node].Count)
                {
                    postorder.Add(node);
                    continue;
                }

                stack.Push((node, nextSuccessor + 1));
                int successor = successors[node][nextSuccessor];
                if (visited[successor])
                    continue;

                visited[successor] = true;
                stack.Push((successor, 0));
            }

            postorder.Reverse();
            return [.. postorder];
        }

        private static TreeInterval[] NumberTree(
            IReadOnlyList<List<int>> children,
            int root)
        {
            TreeInterval[] intervals = new TreeInterval[children.Count];
            int nextNumber = 0;
            Stack<(int Node, bool Leaving)> stack = [];
            stack.Push((root, false));

            while (stack.Count > 0)
            {
                (int node, bool leaving) = stack.Pop();
                if (leaving)
                {
                    intervals[node] = new(intervals[node].Start, nextNumber);
                    continue;
                }

                intervals[node] = new(nextNumber++, 0);
                stack.Push((node, true));
                for (int child = children[node].Count - 1; child >= 0; child--)
                    stack.Push((children[node][child], false));
            }

            return intervals;
        }
    }
}
