namespace Disharmony;

internal class Optimizer(MethodBase method, List<CodeInstruction> inputInstructions, ILGenerator generator, bool debug)
{
    private record Op(OpCode opcode, object? operand = null)
    {
        public CodeInstruction ToCodeInstruction() => new(opcode, operand);

        public bool IsLeave => opcode == OpCodes.Leave_S || opcode == OpCodes.Leave;
        public bool IsUnconditionalBranch => opcode == OpCodes.Br_S || opcode == OpCodes.Br;
        public bool CanBranch => opcode.FlowControl is not (FlowControl.Next or FlowControl.Call or FlowControl.Meta);
        public bool CanFallThrough => opcode.FlowControl is FlowControl.Next or FlowControl.Call or FlowControl.Meta or FlowControl.Cond_Branch;
    }

    private static class Ops
    {
        public static readonly Op Nop = new(OpCodes.Nop);
        public static readonly Op Ret = new(OpCodes.Ret);
        public static readonly Op Pop = new(OpCodes.Pop);
    }

    private class BasicBlockBase
    {
        public int id = 0;
        public readonly List<Label> labels = [];
        public readonly List<BasicBlock> successors = [];
        public readonly List<BasicBlock> predecessors = [];
        public ExceptionRegion? parent;
        public string ID => $"#{id}";
    }

    private class ExceptionRegion : BasicBlockBase
    {
        public ExceptionBlock? harmonyBlock;
        public ExceptionRegion? next;
        public BasicBlock? entry;
        public int depth;

        public static ExceptionRegion? SharedParent(ExceptionRegion? first, ExceptionRegion? second)
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

    private class BasicBlock : BasicBlockBase
    {
        public bool EntryPoint => parent!.entry == this;
        public readonly List<Op> ops = [];
        public BasicBlock? fallthroughBlock;
    }

    public readonly InstructionList output = [];
    private readonly List<BasicBlock> basicBlocks = [];
    private readonly ExceptionRegion exceptionRoot = new();
    private readonly Dictionary<Label, BasicBlock> labelToBlock = new();
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

        FileLog.LogBuffered($"### Optimizer {phase}: {method.FullDescription()}");

        for (var index = 0; index < basicBlocks.Count; index++)
        {
            BasicBlock? block = basicBlocks[index];
            FileLog.LogBuffered("#####################################");
            FileLog.LogBuffered($"# Basic block:  {block.ID,-19} #");
            FileLog.LogBuffered($"# Predecessors: {string.Join(", ", block.predecessors.Select(b => b.ID)),-19} #");
            FileLog.LogBuffered($"# Successors:   {string.Join(", ", block.successors.Select(b => b.ID)),-19} #");
            if (block.EntryPoint)
                FileLog.LogBuffered(
                    $"# Entry Point:  {block.parent!.harmonyBlock?.blockType.ToString() ?? "Function",-19} #");
            FileLog.LogBuffered("#####################################");

            foreach (var label in block.labels)
                FileLog.LogIL(codePos, label);

            foreach (var harmonyBlock in ExceptionBlockBegins(index))
                FileLog.LogILBlockBegin(codePos, harmonyBlock);

            foreach (var op in block.ops)
                LogInstruction(op.ToCodeInstruction(), ref codePos);
            if (block.ops.Count == 0)
                LogInstruction(Ops.Nop.ToCodeInstruction(), ref codePos);

            if (block.fallthroughBlock != null)
                FileLog.LogBuffered($"IL_{codePos:X4}: // fallthrough => {block.fallthroughBlock.ID}");

            foreach (var harmonyBlock in ExceptionBlockEnds(index))
                FileLog.LogILBlockEnd(codePos, harmonyBlock);
        }

        FileLog.LogBuffered("");
        FileLog.FlushBuffer();
    }

    private IEnumerable<ExceptionBlock> ExceptionBlockBegins(int index)
    {
        ExceptionRegion blockExceptionRegion = basicBlocks[index].parent!;
        ExceptionRegion prevBlockExceptionRegion = index >= 1 ? basicBlocks[index - 1].parent! : exceptionRoot;
        if (blockExceptionRegion == prevBlockExceptionRegion)
            return [];

        var parent = ExceptionRegion.SharedParent(prevBlockExceptionRegion, blockExceptionRegion);
        List<ExceptionBlock> blocks = [];
        for (var region = blockExceptionRegion; region != parent && region != null; region = region.parent)
            if (region.harmonyBlock != null)
                blocks.Add(region.harmonyBlock);
        blocks.Reverse();
        return blocks;
    }

    private IEnumerable<ExceptionBlock> ExceptionBlockEnds(int index)
    {
        ExceptionRegion blockExceptionRegion = basicBlocks[index].parent!;
        ExceptionRegion nextBlockExceptionRegion = index < basicBlocks.Count - 1 ? basicBlocks[index + 1].parent! : exceptionRoot;
        if (blockExceptionRegion == nextBlockExceptionRegion)
            yield break;

        var parent = ExceptionRegion.SharedParent(blockExceptionRegion, nextBlockExceptionRegion);
        for (var region = blockExceptionRegion; region != null && region != parent && region.next == null; region = region.parent)
            if (region.harmonyBlock != null)
                yield return new ExceptionBlock(ExceptionBlockType.EndExceptionBlock);
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
        for (var index = 0; index < basicBlocks.Count; index++)
        {
            BasicBlock? block = basicBlocks[index];
            List<CodeInstruction> instructions = [.. block.ops.Select(i => i.ToCodeInstruction())];
            if (instructions.Count == 0)
                instructions.Add(Ops.Nop.ToCodeInstruction());
            instructions[0].labels.AddRange(block.labels);
            instructions[0].blocks.AddRange(ExceptionBlockBegins(index));
            instructions[^1].blocks.AddRange(ExceptionBlockEnds(index));

            foreach (var inst in instructions)
                output.Add(inst);
        }
    }

    /// <summary>
    ///     Generate basic blocks.
    /// </summary>
    private void MakeBasicBlocks()
    {
        BasicBlock curBlock = new() { id = nextBlockId++, parent = exceptionRoot };
        basicBlocks.Add(curBlock);
        exceptionRoot.entry = curBlock;

        ExceptionRegion exceptionRegion = exceptionRoot;

        foreach (var inst in inputInstructions)
        {
            if (inst.labels.Count > 0 || inst.blocks.Any(IsBlockStart))
            {
                foreach (var harmonyBlock in inst.blocks.Where(IsBlockStart))
                    EnterExceptionRegion(harmonyBlock);

                NewBasicBlock(inst.labels);
            }

            curBlock.ops.Add(new(inst.opcode, inst.operand));

            if (inst.CanBranch || inst.blocks.Any(IsBlockEnd))
            {
                foreach (var _ in inst.blocks.Where(IsBlockEnd))
                    exceptionRegion = exceptionRegion.parent!;

                NewBasicBlock([]);
            }
        }

        if (curBlock.ops.Count == 0)
            basicBlocks.Remove(curBlock);

        for (int i = 0; i < basicBlocks.Count - 1; i++)
        {
            if (CanFallThrough(basicBlocks[i]))
                basicBlocks[i].fallthroughBlock = basicBlocks[i + 1];
        }

        // Add a ret to the last basic block if one is missing (perhaps because of a poorly behaved transpiler)
        if (CanFallThrough(basicBlocks[^1]))
            basicBlocks[^1].ops.Add(Ops.Ret);

        foreach (var block in basicBlocks)
        {
            if (block.labels.Count == 0)
                block.labels.Add(generator.DefineLabel());

            foreach (var label in block.labels)
                labelToBlock[label] = block;
        }

        // Convert block-final unconditional branches to fallthrough
        foreach (var block in basicBlocks)
        {
            if (block.ops.Count == 0)
                continue;

            if (block.ops[^1].IsUnconditionalBranch)
            {
                block.fallthroughBlock = labelToBlock[(Label)block.ops[^1].operand!];
                block.ops.RemoveAt(block.ops.Count - 1);
            }
        }

        foreach (var block in basicBlocks)
        {
            if (block.fallthroughBlock is not null)
                block.successors.Add(block.fallthroughBlock);

            if (block.ops.Count == 0)
                continue;

            switch (block.ops[^1].operand)
            {
                case Label label:
                {
                    block.successors.Add(basicBlocks.Single(b => b.labels.Contains(label)));
                    break;
                }
                case Label[] labels:
                {
                    foreach (var label in labels)
                        block.successors.Add(basicBlocks.Single(b => b.labels.Contains(label)));
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
                var newRegion = new ExceptionRegion
                {
                    id = nextBlockId++,
                    depth = exceptionRegion.depth + 1,
                    harmonyBlock = harmonyBlock,
                    parent = exceptionRegion,
                };
                exceptionRegion = newRegion;
            }
            else
            {
                var newRegion = new ExceptionRegion
                {
                    id = nextBlockId++,
                    depth = exceptionRegion.depth,
                    harmonyBlock = harmonyBlock,
                    parent = exceptionRegion.parent,
                };
                exceptionRegion.next = newRegion;
                exceptionRegion = newRegion;
            }
        }

        void NewBasicBlock(List<Label> labels)
        {
            if (curBlock.ops.Count == 0)
            {
                curBlock.parent = exceptionRegion;
                for (var region = exceptionRegion; region != null; region = region.parent)
                    region.entry ??= curBlock;
            }
            else
            {
                BasicBlock newBlock = new() { id = nextBlockId++, parent = exceptionRegion };
                basicBlocks.Add(newBlock);
                curBlock = newBlock;

                for (var region = exceptionRegion; region != null; region = region.parent)
                    region.entry ??= curBlock;
            }

            curBlock.labels.AddRange(labels);
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
            if (block.fallthroughBlock is null)
                continue;
            if (block.successors.Any(s => s != block.fallthroughBlock))
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
                    block.successors.Add(block.fallthroughBlock);
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
                    block.successors.Add(block.fallthroughBlock);
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
            BasicBlock? fallthroughBlock = block.fallthroughBlock;
            if (fallthroughBlock == null)
                continue;
            int iterations = 0;
            while (fallthroughBlock.ops.Count == 0 &&
                   fallthroughBlock.fallthroughBlock!.parent == fallthroughBlock.parent &&
                   !fallthroughBlock.EntryPoint &&
                   iterations++ < 20)
                fallthroughBlock = fallthroughBlock.fallthroughBlock;

            if (block.fallthroughBlock != fallthroughBlock)
            {
                block.successors.Remove(block.fallthroughBlock!);
                block.successors.Add(fallthroughBlock);
                block.fallthroughBlock = fallthroughBlock;
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
            if (successor.predecessors.Count != 1 || successor.parent != block.parent || successor.EntryPoint)
                continue;
            block.ops.AddRange(successor.ops);
            block.fallthroughBlock = successor.fallthroughBlock;
            block.successors.Clear();
            block.successors.AddRange(successor.successors);
        }

        UpdatePredecessors();
    }

    private void SimpleDeadCodeElimination()
    {
        Queue<BasicBlock> queue = new();
        HashSet<BasicBlock> liveBlocks = new();

        foreach (var block in basicBlocks)
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

        basicBlocks.RemoveAll(b => !liveBlocks.Contains(b));

        UpdatePredecessors();
    }

    private void AggressiveDeadCodeEliminationAndReorder()
    {
        List<BasicBlock> outputBlocks = [];
        AggressiveDeadCodeEliminationAndReorder(exceptionRoot, outputBlocks);
        basicBlocks.Clear();
        basicBlocks.AddRange(outputBlocks);
    }

    private List<BasicBlock> AggressiveDeadCodeEliminationAndReorder(ExceptionRegion region, List<BasicBlock> outputBlocks)
    {
        LinkedList<BasicBlock> queue = new();
        HashSet<BasicBlock> emitted = new();
        List<BasicBlock> leaveTargets = [];

        queue.AddFirst(region.entry!);

        if (debug)
            FileLog.Log($"{"".PadLeft(region.depth * 4)}- Visiting {region.harmonyBlock?.blockType.ToString() ?? "Root"}");

        while (queue.Count > 0)
        {
            var block = queue.First.Value;
            queue.RemoveFirst();
            if (!emitted.Add(block))
                continue;

            if (debug)
                FileLog.Log($"{"".PadLeft(region.depth * 4 + 2)}- Processing {block.ID}");

            List<BasicBlock> successors;
            if (block.parent == region)
            {
                outputBlocks.Add(block);
                successors = block.successors;
                if (block.fallthroughBlock != null && block.fallthroughBlock.parent == region)
                    queue.AddFirst(block.fallthroughBlock);
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
                if (successor.parent == region || ExceptionRegion.SharedParent(region, successor.parent) == region)
                    queue.AddLast(successor);
                else
                    leaveTargets.Add(successor);
            }
        }

        if (region.next != null)
            leaveTargets.AddRange(AggressiveDeadCodeEliminationAndReorder(region.next, outputBlocks));

        return leaveTargets;
    }

    private void BranchInversion()
    {
        foreach (var block in basicBlocks)
        {
            if (block.ops.Count == 0)
                continue;
            if (block.fallthroughBlock is null)
                continue;
            if (block.fallthroughBlock.predecessors.Count == 1)
                continue;

            var finalInstruction = block.ops[^1];
            if (finalInstruction.opcode.FlowControl is FlowControl.Cond_Branch &&
                finalInstruction.operand is Label label &&
                labelToBlock[label].predecessors.Count == 1)
            {
                if (finalInstruction.opcode == OpCodes.Brfalse || finalInstruction.opcode == OpCodes.Brfalse_S)
                {
                    block.ops[^1] = new(OpCodes.Brtrue_S, block.fallthroughBlock.labels[0]);
                    block.fallthroughBlock = labelToBlock[label];
                }

                if (finalInstruction.opcode == OpCodes.Brtrue || finalInstruction.opcode == OpCodes.Brtrue_S)
                {
                    block.ops[^1] = new(OpCodes.Brfalse_S, block.fallthroughBlock.labels[0]);
                    block.fallthroughBlock = labelToBlock[label];
                }
            }
        }
    }

    private void InsertBranches()
    {
        for (int i = 0; i < basicBlocks.Count; i++)
        {
            BasicBlock? fallthroughBlock = basicBlocks[i].fallthroughBlock;
            if (fallthroughBlock == null || i < basicBlocks.Count - 1 && fallthroughBlock == basicBlocks[i + 1])
                continue;
            basicBlocks[i].ops.Add(new(OpCodes.Br_S, fallthroughBlock.labels[0]));
            basicBlocks[i].fallthroughBlock = null;
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
