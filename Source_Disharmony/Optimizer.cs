namespace Disharmony;

internal class Optimizer(MethodBase method, List<CodeInstruction> inputInstructions, ILGenerator generator)
{
    private class ExceptionRegion
    {
        public ExceptionBlock? harmonyBlock;
        public ExceptionRegion? parent;
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

    private class BasicBlock
    {
        public string ID => $"#{id}";

        public bool EntryPoint => exceptionRegion.entry == this;
        public required ExceptionRegion exceptionRegion;
        public int id = 0;
        public readonly List<Label> labels = [];
        public readonly List<CodeInstruction> instructions = [];
        public readonly List<BasicBlock> successors = [];
        public readonly List<BasicBlock> predecessors = [];
        public BasicBlock? fallthroughBlock;
    }

    private static readonly CodeInstruction Nop = new(OpCodes.Nop);

    public readonly InstructionList output = [];
    private readonly List<BasicBlock> basicBlocks = [];
    private readonly ExceptionRegion exceptionRoot = new();
    private readonly Dictionary<Label, BasicBlock> labelToBlock = new();

    private static bool IsBlockStart(ExceptionBlock b) => b.blockType != ExceptionBlockType.EndExceptionBlock;
    private static bool IsBlockEnd(ExceptionBlock b) => b.blockType == ExceptionBlockType.EndExceptionBlock;

    private void LogInstructions(string phase, IEnumerable<CodeInstruction> instructions)
    {
        int codePos = 0;

        FileLog.LogBuffered($"### Optimizer {phase}: {method.FullDescription()}");

        foreach (var codeInstruction in instructions)
            LogInstruction(codeInstruction, ref codePos);

        FileLog.LogBuffered("");
        FileLog.FlushBuffer();
    }

    private void LogBlocks(string phase)
    {
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
                    $"# Entry Point:  {block.exceptionRegion.harmonyBlock?.blockType.ToString() ?? "Function entry",-19} #");
            FileLog.LogBuffered("#####################################");

            foreach (var label in block.labels)
                FileLog.LogIL(codePos, label);

            foreach (var harmonyBlock in ExceptionBlockBegins(index))
                FileLog.LogILBlockBegin(codePos, harmonyBlock);

            foreach (var codeInstruction in block.instructions)
                LogInstruction(codeInstruction, ref codePos);
            if (block.instructions.Count == 0)
                LogInstruction(Nop, ref codePos);

            if (block.fallthroughBlock != null)
                FileLog.LogBuffered($"IL_{codePos:X4}: // fallthrough => {block.fallthroughBlock.ID}");

            foreach (var harmonyBlock in ExceptionBlockEnds(index))
                FileLog.LogILBlockEnd(codePos, harmonyBlock);
        }

        //while (FileLog.indentLevel > 0)
        //    FileLog.LogILBlockEnd(codePos, new ExceptionBlock(ExceptionBlockType.EndExceptionBlock));

        FileLog.LogBuffered("");
        FileLog.FlushBuffer();
    }

    private IEnumerable<ExceptionBlock> ExceptionBlockBegins(int index)
    {
        ExceptionRegion blockExceptionRegion = basicBlocks[index].exceptionRegion;
        ExceptionRegion prevBlockExceptionRegion = index >= 1 ? basicBlocks[index - 1].exceptionRegion : exceptionRoot;
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
        ExceptionRegion blockExceptionRegion = basicBlocks[index].exceptionRegion;
        ExceptionRegion nextBlockExceptionRegion = index < basicBlocks.Count - 1 ? basicBlocks[index + 1].exceptionRegion : exceptionRoot;
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

            //case OperandType.InlineSig:
            //    FileLog.LogIL(codePos, code, (ICallSiteGenerator)operand);
            //    break;

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
            if (block.instructions.Count == 0)
                block.instructions.Add(Nop);
            block.instructions[0].labels.AddRange(block.labels);
            block.instructions[0].blocks.AddRange(ExceptionBlockBegins(index));
            block.instructions[^1].blocks.AddRange(ExceptionBlockEnds(index));

            foreach (var inst in block.instructions)
                output.Add(inst);
        }
    }

    /// <summary>
    ///     Generate basic blocks.
    /// </summary>
    private void MakeBasicBlocks()
    {
        BasicBlock curBlock = new() { exceptionRegion = exceptionRoot };
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

            curBlock.instructions.Add(inst);

            if (inst.CanBranch || inst.blocks.Any(IsBlockEnd))
            {
                foreach (var _ in inst.blocks.Where(IsBlockEnd))
                    exceptionRegion = exceptionRegion.parent!;

                NewBasicBlock([]);
            }

            inst.labels.Clear();
            inst.blocks.Clear();
        }

        if (curBlock.instructions.Count == 0)
            basicBlocks.Remove(curBlock);

        for (int i = 0; i < basicBlocks.Count - 1; i++)
        {
            if (CanFallThrough(basicBlocks[i]))
                basicBlocks[i].fallthroughBlock = basicBlocks[i + 1];
        }

        // Add a ret to the last basic block if one is missing (perhaps because of a poorly behaved transpiler)
        if (CanFallThrough(basicBlocks[^1]))
            basicBlocks[^1].instructions.Add(new(OpCodes.Ret));

        foreach (var block in basicBlocks)
        foreach (var label in block.labels)
            labelToBlock[label] = block;

        // Convert block-final unconditional branches to fallthrough
        foreach (var block in basicBlocks)
        {
            if (block.instructions.Count == 0)
                continue;

            if (block.instructions[^1].IsUnconditionalBranch)
            {
                block.fallthroughBlock = labelToBlock[(Label)block.instructions[^1].operand];
                block.instructions.RemoveAt(block.instructions.Count - 1);
            }
        }

        foreach (var block in basicBlocks)
        {
            if (block.fallthroughBlock is not null)
                block.successors.Add(block.fallthroughBlock);

            if (block.instructions.Count == 0)
                continue;

            switch (block.instructions[^1].operand)
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
            if (curBlock.instructions.Count == 0)
            {
                curBlock.exceptionRegion = exceptionRegion;
                for (var region = exceptionRegion; region != null; region = region.parent)
                    region.entry ??= curBlock;
            }
            else
            {
                BasicBlock newBlock = new() { id = basicBlocks.Count, exceptionRegion = exceptionRegion };
                basicBlocks.Add(newBlock);
                curBlock = newBlock;

                for (var region = exceptionRegion; region != null; region = region.parent)
                    region.entry ??= curBlock;
            }

            curBlock.labels.AddRange(labels);
            if (curBlock.labels.Count == 0)
                curBlock.labels.Add(generator.DefineLabel());
        }

        static bool CanFallThrough(BasicBlock basicBlock) =>
            basicBlock.instructions.Count == 0 || basicBlock.instructions[^1].CanFallThrough;
    }

    private void NopElimination()
    {
        foreach (var block in basicBlocks)
            block.instructions.RemoveAll(i => i.opcode == OpCodes.Nop);
    }

    private void BranchElimination()
    {
        bool changed = false;

        foreach (var block in basicBlocks)
        {
            if (block.instructions.Count == 0)
                continue;
            if (block.fallthroughBlock is null)
                continue;
            if (block.successors.Any(s => s != block.fallthroughBlock))
                continue;

            switch (block.instructions[^1].opcode)
            {
                // Brtrue, Brfalse
                case
                {
                    FlowControl: FlowControl.Cond_Branch, StackBehaviourPop: StackBehaviour.Popi, StackBehaviourPush: StackBehaviour.Push0,
                }:
                {
                    block.instructions[^1] = new(OpCodes.Pop);
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
                    block.instructions[^1] = new(OpCodes.Pop);
                    block.instructions.Add(new(OpCodes.Pop));
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
            while (fallthroughBlock.instructions.Count == 0 &&
                   fallthroughBlock.fallthroughBlock!.exceptionRegion == fallthroughBlock.exceptionRegion &&
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
            if (block.instructions.Count > 0 && block.instructions[^1].CanBranch)
                continue;
            if (successor.predecessors.Count != 1 || successor.exceptionRegion != block.exceptionRegion || successor.EntryPoint)
                continue;
            block.instructions.AddRange(successor.instructions);
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

        FileLog.Log($"{"".PadLeft(region.depth * 2)}- Visiting {region.harmonyBlock?.blockType.ToString() ?? "Root"}");

        while (queue.Count > 0)
        {
            var block = queue.First.Value;
            queue.RemoveFirst();
            if (!emitted.Add(block))
                continue;

            FileLog.Log($"{"".PadLeft(region.depth * 2 + 1)}- Processing {block.ID}");

            List<BasicBlock> successors;
            if (block.exceptionRegion == region)
            {
                outputBlocks.Add(block);
                successors = block.successors;
                if (block.fallthroughBlock != null && block.fallthroughBlock.exceptionRegion == region)
                    queue.AddFirst(block.fallthroughBlock);
            }
            else
            {
                var immediateChild = block.exceptionRegion;
                while (immediateChild.parent != region)
                    immediateChild = immediateChild.parent!;

                successors = AggressiveDeadCodeEliminationAndReorder(immediateChild, outputBlocks);
            }

            foreach (var successor in successors)
            {
                if (successor.exceptionRegion == region || ExceptionRegion.SharedParent(region, successor.exceptionRegion) == region)
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
            if (block.instructions.Count == 0)
                continue;
            if (block.fallthroughBlock is null)
                continue;
            if (block.fallthroughBlock.predecessors.Count == 1)
                continue;

            var finalInstruction = block.instructions[^1];
            if (finalInstruction.opcode.FlowControl is FlowControl.Cond_Branch &&
                finalInstruction.operand is Label label &&
                labelToBlock[label].predecessors.Count == 1)
            {
                if (finalInstruction.opcode == OpCodes.Brfalse || finalInstruction.opcode == OpCodes.Brfalse_S)
                {
                    block.instructions[^1] = new(OpCodes.Brtrue_S, block.fallthroughBlock.labels[0]);
                    block.fallthroughBlock = labelToBlock[label];
                }

                if (finalInstruction.opcode == OpCodes.Brtrue || finalInstruction.opcode == OpCodes.Brtrue_S)
                {
                    block.instructions[^1] = new(OpCodes.Brfalse_S, block.fallthroughBlock.labels[0]);
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
            basicBlocks[i].instructions.Add(new(OpCodes.Br_S, fallthroughBlock.labels[0]));
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
