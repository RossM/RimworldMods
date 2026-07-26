namespace Disharmony;

internal class Optimizer(MethodBase method, List<CodeInstruction> inputInstructions, ILGenerator generator, bool debug)
{
    private record Op(OpCode opcode, object? operand = null)
    {
        public bool IsLeave => opcode == OpCodes.Leave_S || opcode == OpCodes.Leave;
        public bool IsUnconditionalBranch => opcode == OpCodes.Br_S || opcode == OpCodes.Br;
        public bool CanBranch => opcode.FlowControl is not (FlowControl.Next or FlowControl.Call or FlowControl.Meta);

        public bool CanFallThrough =>
            opcode.FlowControl is FlowControl.Next or FlowControl.Call or FlowControl.Meta or FlowControl.Cond_Branch;

        public CodeInstruction ToCodeInstruction() => new(opcode, operand);
    }

    private static class Ops
    {
        public static readonly Op Nop = new(OpCodes.Nop);
        public static readonly Op Ret = new(OpCodes.Ret);
        public static readonly Op Pop = new(OpCodes.Pop);
    }

    private class Block
    {
        public bool EntryPoint => parent == null || parent.entry == this;
        public virtual string ID => $"#{id}";
        public int id = 0;
        public Label? label;
        public readonly List<Block> successors = [];
        public readonly List<Block> predecessors = [];
        public Region? parent;

        /// <summary>
        ///     For BasicBlocks, has the next block in the flow of control. For Regions, has the next
        ///     exception region in the chain.
        /// </summary>
        public Block? next;
    }

    private class Region : Block
    {
        public override string ID => parent == null ? "Root" : $"{harmonyBlock!.blockType} #{id}";
        public ExceptionBlock? harmonyBlock;
        public Block? entry;
        public int depth;

        public static Region? SharedParent(Region? first, Region? second)
        {
            if (first is null || second is null)
                return null;

            while (first.parent != null && first.depth > second.depth)
                first = first.parent;
            while (second.parent != null && second.depth > first.depth)
                second = second.parent;

            while (second != null && first != null)
            {
                if (first == second)
                    return first;
                first = first.parent;
                second = second.parent;
            }

            return null;
        }
    }

    private class BasicBlock : Block
    {
        public readonly List<Op> ops = [];
    }

    public readonly InstructionList output = [];
    private readonly List<Block> allBlocks = [];
    private List<BasicBlock> basicBlocks = [];
    private readonly Region root = new();
    private int nextBlockId = 1;

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

    private void LogBlocks(string phase)
    {
        if (!debug)
            return;

        int codePos = 0;
        Stack<Region> regionStack = new();

        FileLog.LogBuffered($"### Optimizer {phase}: {method.FullDescription()}");

        foreach (var block in allBlocks)
        {
            while (regionStack.Count >= 1 && block.parent != regionStack.Peek())
            {
                if (regionStack.Peek().harmonyBlock != null && regionStack.Peek().next == null)
                    FileLog.LogILBlockEnd(codePos, new ExceptionBlock(ExceptionBlockType.EndExceptionBlock));
                regionStack.Pop();
            }

            FileLog.LogBuffered("#########################################");
            FileLog.LogBuffered($"# Block:        {block.ID,-23} #");
            FileLog.LogBuffered($"# Predecessors: {string.Join(", ", block.predecessors.Select(b => b.ID)),-23} #");
            FileLog.LogBuffered($"# Successors:   {string.Join(", ", block.successors.Select(b => b.ID)),-23} #");
            if (block is { EntryPoint: true, parent: not null })
                FileLog.LogBuffered(
                    $"# Entry Point:  {block.parent!.ID,-23} #");
            FileLog.LogBuffered("#########################################");

            if (block.label is Label label)
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
                        LogInstruction(ConvertToCodeInstruction(op), ref codePos);
                    if (bb.ops.Count == 0)
                        LogInstruction(Ops.Nop.ToCodeInstruction(), ref codePos);
                    break;
                }
            }

            if (block.next != null)
                FileLog.LogBuffered($"IL_{codePos:X4}: // fallthrough => {block.next.ID}");
        }

        while (regionStack.Count > 0)
        {
            if (regionStack.Peek().harmonyBlock != null)
                FileLog.LogILBlockEnd(codePos, new ExceptionBlock(ExceptionBlockType.EndExceptionBlock));
            regionStack.Pop();
        }

        FileLog.LogBuffered("");
        FileLog.FlushBuffer();
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

    public void Optimize()
    {
        LogInstructions("Input", inputInstructions);

        MakeBasicBlocks();
        LogBlocks(nameof(MakeBasicBlocks));

        NopElimination();
        LogBlocks(nameof(NopElimination));

        JumpThreading();
        LogBlocks(nameof(JumpThreading));

        SimpleDeadCodeElimination();
        LogBlocks(nameof(SimpleDeadCodeElimination));

        BranchElimination();
        LogBlocks(nameof(BranchElimination));

        MergeBlocks();
        LogBlocks(nameof(MergeBlocks));

        BranchInversion();
        LogBlocks(nameof(BranchInversion));

        AggressiveDeadCodeEliminationAndReorder();
        LogBlocks(nameof(AggressiveDeadCodeEliminationAndReorder));

        InsertBranches();
        LogBlocks(nameof(InsertBranches));

        Emit();
        LogInstructions("Output", output.Instructions);
    }

    private void Emit()
    {
        Stack<Region> regionStack = new();
        List<ExceptionBlock> harmonyBlocks = new();
        List<Label> labels = new();

        foreach (var block in allBlocks)
        {
            while (regionStack.Count >= 1 && block.parent != regionStack.Peek())
            {
                if (regionStack.Peek().harmonyBlock != null && regionStack.Peek().next == null)
                    output.Instructions[^1].blocks.Add(new ExceptionBlock(ExceptionBlockType.EndExceptionBlock));
                regionStack.Pop();
            }

            if (block.label is Label label)
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
                    List<CodeInstruction> instructions = [.. bb.ops.Select(ConvertToCodeInstruction)];
                    if (instructions.Count == 0)
                        instructions.Add(Ops.Nop.ToCodeInstruction());
                    instructions[0].labels.AddRange(labels);
                    labels.Clear();
                    instructions[0].blocks.AddRange(harmonyBlocks);
                    harmonyBlocks.Clear();
                    output.Instructions.AddRange(instructions);
                    break;
                }
            }
        }

        while (regionStack.Count > 0)
        {
            if (regionStack.Peek().harmonyBlock != null)
                output.Instructions[^1].blocks.Add(new ExceptionBlock(ExceptionBlockType.EndExceptionBlock));
            regionStack.Pop();
        }
    }

    private CodeInstruction ConvertToCodeInstruction(Op i)
    {
        var codeInstruction = i.ToCodeInstruction();
        switch (codeInstruction.operand)
        {
            case Block blockTarget: codeInstruction.operand = GetLabel(blockTarget); break;
            case Block[] blocksTarget: codeInstruction.operand = blocksTarget.Select(GetLabel).ToArray(); break;
        }

        return codeInstruction;

        Label GetLabel(Block block) => block.label ??= generator.DefineLabel();
    }

    /// <summary>
    ///     Generate basic blocks.
    /// </summary>
    private void MakeBasicBlocks()
    {
        Dictionary<Label, BasicBlock> labelToBlock = new();

        Region exceptionRegion = root;
        allBlocks.Add(root);

        BasicBlock curBlock = new() { id = nextBlockId++, parent = exceptionRegion };
        allBlocks.Add(curBlock);
        exceptionRegion.entry ??= curBlock;

        foreach (var inst in inputInstructions)
        {
            if (inst.labels.Count > 0 || inst.blocks.Any(IsBlockStart))
            {
                foreach (var harmonyBlock in inst.blocks.Where(IsBlockStart))
                    EnterExceptionRegion(harmonyBlock);

                NewBasicBlock();
                foreach (var label in inst.labels)
                    labelToBlock[label] = curBlock;
                if (inst.labels.Count >= 1)
                    curBlock.label = inst.labels[0];
            }

            curBlock.ops.Add(new(inst.opcode, inst.operand));

            if (inst.CanBranch || inst.blocks.Any(IsBlockEnd))
            {
                foreach (var _ in inst.blocks.Where(IsBlockEnd))
                    exceptionRegion = exceptionRegion.parent!;

                NewBasicBlock();
            }
        }

        if (curBlock.ops.Count == 0)
            allBlocks.Remove(curBlock);

        basicBlocks = [.. allBlocks.OfType<BasicBlock>()];

        for (int i = 0; i < basicBlocks.Count - 1; i++)
        {
            if (CanFallThrough(basicBlocks[i]))
                basicBlocks[i].next = basicBlocks[i + 1];
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
                switch (op.operand)
                {
                    case Label label: block.ops[index] = new(op.opcode, labelToBlock[label]); break;
                    case Label[] labels: block.ops[index] = new(op.opcode, labels.Select(l => labelToBlock[l]).ToArray<Block>()); break;
                }
            }
        }

        // Convert block-final unconditional branches to fallthrough
        foreach (var block in basicBlocks)
        {
            if (block.ops.Count == 0)
                continue;

            if (block.ops[^1].IsUnconditionalBranch)
            {
                block.next = (BasicBlock?)block.ops[^1].operand;
                block.ops.RemoveAt(block.ops.Count - 1);
            }
        }

        foreach (var block in basicBlocks)
        {
            if (block.next is not null)
                block.successors.Add(block.next);

            if (block.ops.Count == 0)
                continue;

            switch (block.ops[^1].operand)
            {
                case Block label:
                {
                    block.successors.Add(label);
                    break;
                }
                case Block[] labels:
                {
                    block.successors.AddRange(labels);
                    break;
                }
            }
        }

        UpdatePredecessors();

        return;

        void EnterExceptionRegion(ExceptionBlock harmonyBlock)
        {
            if (harmonyBlock.blockType == ExceptionBlockType.BeginExceptionBlock)
            {
                var newRegion = new Region
                {
                    id = nextBlockId++,
                    depth = exceptionRegion.depth + 1,
                    harmonyBlock = harmonyBlock,
                    parent = exceptionRegion,
                };
                allBlocks.Add(newRegion);
                exceptionRegion.entry ??= newRegion;
                exceptionRegion = newRegion;
            }
            else
            {
                var newRegion = new Region
                {
                    id = nextBlockId++,
                    depth = exceptionRegion.depth,
                    harmonyBlock = harmonyBlock,
                    parent = exceptionRegion.parent,
                };
                allBlocks.Add(newRegion);
                exceptionRegion.next = newRegion;
                exceptionRegion = newRegion;
            }
        }

        void NewBasicBlock()
        {
            if (curBlock.ops.Count == 0)
            {
                curBlock.parent = exceptionRegion;
                exceptionRegion.entry ??= curBlock;
            }
            else
            {
                BasicBlock newBlock = new() { id = nextBlockId++, parent = exceptionRegion };
                allBlocks.Add(newBlock);
                curBlock = newBlock;
                exceptionRegion.entry ??= curBlock;
            }
        }

        static bool CanFallThrough(BasicBlock basicBlock) =>
            basicBlock.ops.Count == 0 || basicBlock.ops[^1].CanFallThrough;
    }

    private void NopElimination()
    {
        foreach (var block in basicBlocks)
            block.ops.RemoveAll(i => i.opcode == OpCodes.Nop);
    }

    private void BranchElimination()
    {
        bool changed = false;

        foreach (var block in basicBlocks)
        {
            if (block.ops.Count == 0)
                continue;
            if (block.next is null)
                continue;
            if (block.successors.Any(s => s != block.next))
                continue;

            switch (block.ops[^1].opcode)
            {
                // Brtrue, Brfalse
                case
                {
                    FlowControl: FlowControl.Cond_Branch, StackBehaviourPop: StackBehaviour.Popi, StackBehaviourPush: StackBehaviour.Push0,
                }:
                {
                    block.ops[^1] = Ops.Pop;
                    block.successors.Clear();
                    block.successors.Add(block.next);
                    changed = true;
                    break;
                }
                // Beq, Bge, Bgt, Ble, Blt, Bne_Un, Bge_Un, Bgt_Un, Ble_Un, Blt_Un
                case
                {
                    FlowControl: FlowControl.Cond_Branch, StackBehaviourPop: StackBehaviour.Pop1_pop1,
                    StackBehaviourPush: StackBehaviour.Push0,
                }:
                {
                    block.ops[^1] = Ops.Pop;
                    block.ops.Add(Ops.Pop);
                    block.successors.Clear();
                    block.successors.Add(block.next);
                    changed = true;
                    break;
                }
            }
        }

        if (changed)
            UpdatePredecessors();
    }

    private void JumpThreading()
    {
        foreach (var block in basicBlocks)
        {
            Block? fallthroughBlock = block.next;
            if (fallthroughBlock == null)
                continue;
            int iterations = 0;
            while (fallthroughBlock is BasicBlock { ops.Count: 0, EntryPoint: false } bb &&
                   bb.next!.parent == bb.parent &&
                   iterations++ < 20)
                fallthroughBlock = bb.next;

            if (block.next != fallthroughBlock)
            {
                block.successors.Remove(block.next!);
                block.successors.Add(fallthroughBlock);
                block.next = fallthroughBlock;
            }
        }

        UpdatePredecessors();
    }

    private void MergeBlocks()
    {
        for (int i = basicBlocks.Count - 1; i >= 0; i--)
        {
            var block = basicBlocks[i];
            if (block.successors is not [var successor])
                continue;
            if (block.ops.Count > 0 && block.ops[^1].CanBranch)
                continue;
            if (successor.predecessors.Count != 1 || successor.parent != block.parent ||
                successor is not BasicBlock { EntryPoint: false } bb)
                continue;
            block.ops.AddRange(bb.ops);
            block.next = bb.next;
            block.successors.Clear();
            block.successors.AddRange(bb.successors);
        }

        UpdatePredecessors();
    }

    private void SimpleDeadCodeElimination()
    {
        Queue<Block> queue = new();
        HashSet<Block> liveBlocks = new();

        foreach (var block in allBlocks)
        {
            if (block.EntryPoint)
                queue.Enqueue(block);
        }

        while (queue.Count > 0)
        {
            var block = queue.Dequeue();
            if (!liveBlocks.Add(block))
                continue;
            foreach (var successor in block.successors)
                queue.Enqueue(successor);
        }

        allBlocks.RemoveAll(b => b is BasicBlock && !liveBlocks.Contains(b));
        basicBlocks = [.. allBlocks.OfType<BasicBlock>()];

        UpdatePredecessors();
    }

    private void AggressiveDeadCodeEliminationAndReorder()
    {
        List<Block> outputBlocks = [];
        AggressiveDeadCodeEliminationAndReorder(root, outputBlocks);
        allBlocks.Clear();
        allBlocks.AddRange(outputBlocks);
        basicBlocks = [.. allBlocks.OfType<BasicBlock>()];
    }

    private List<Block> AggressiveDeadCodeEliminationAndReorder(Region region, List<Block> outputBlocks)
    {
        LinkedList<Block> queue = new();
        HashSet<Block> emitted = new();
        List<Block> leaveTargets = [];

        outputBlocks.Add(region);

        Block entry = region;
        while (entry is Region r)
            entry = r.entry!;
        queue.AddFirst(entry);

        if (debug)
            FileLog.Log($"{"".PadLeft(region.depth * 4)}- Visiting {region.ID}");

        while (queue.Count > 0)
        {
            var block = queue.First.Value;
            queue.RemoveFirst();
            if (!emitted.Add(block))
                continue;

            if (debug)
                FileLog.Log($"{"".PadLeft(region.depth * 4 + 2)}- Processing {block.ID}");

            List<Block> successors;
            if (block.parent == region && block is BasicBlock bb)
            {
                outputBlocks.Add(bb);
                successors = block.successors;
                if (bb.next != null && bb.next.parent == region)
                    queue.AddFirst(bb.next);
            }
            else
            {
                var immediateChild = block.parent!;
                while (immediateChild.parent != region)
                    immediateChild = immediateChild.parent!;

                successors = AggressiveDeadCodeEliminationAndReorder(immediateChild, outputBlocks);
            }

            foreach (var successor in successors)
            {
                if (successor.parent == region || Region.SharedParent(region, successor.parent) == region)
                    queue.AddLast(successor);
                else
                    leaveTargets.Add(successor);
            }
        }

        if (region.next != null)
            leaveTargets.AddRange(AggressiveDeadCodeEliminationAndReorder((Region)region.next, outputBlocks));

        return leaveTargets;
    }

    private void BranchInversion()
    {
        foreach (var block in basicBlocks)
        {
            if (block.ops.Count == 0)
                continue;
            if (block.next is null)
                continue;
            if (block.next.predecessors.Count == 1)
                continue;

            var finalInstruction = block.ops[^1];
            if (finalInstruction.opcode.FlowControl is FlowControl.Cond_Branch &&
                finalInstruction.operand is Block label &&
                label.predecessors.Count == 1)
            {
                if (finalInstruction.opcode == OpCodes.Brfalse || finalInstruction.opcode == OpCodes.Brfalse_S)
                {
                    block.ops[^1] = new(OpCodes.Brtrue_S, block.next);
                    block.next = label;
                }

                if (finalInstruction.opcode == OpCodes.Brtrue || finalInstruction.opcode == OpCodes.Brtrue_S)
                {
                    block.ops[^1] = new(OpCodes.Brfalse_S, block.next);
                    block.next = label;
                }
            }
        }
    }

    private void InsertBranches()
    {
        for (int i = 0; i < basicBlocks.Count; i++)
        {
            Block? fallthroughBlock = basicBlocks[i].next;
            if (fallthroughBlock == null || i < basicBlocks.Count - 1 && fallthroughBlock == basicBlocks[i + 1])
                continue;
            basicBlocks[i].ops.Add(new(OpCodes.Br_S, fallthroughBlock));
            basicBlocks[i].next = null;
        }
    }

    /// <summary>
    ///     Update predecessor lists. Should be called whenever control flow changes.
    /// </summary>
    private void UpdatePredecessors()
    {
        foreach (var block in basicBlocks)
            block.predecessors.Clear();

        foreach (var block in basicBlocks)
        foreach (var successor in block.successors)
            successor.predecessors.Add(block);
    }
}
