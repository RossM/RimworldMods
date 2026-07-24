namespace Disharmony;

internal class Optimizer(MethodBase method, List<CodeInstruction> inputInstructions, ILGenerator generator)
{
    private class ExceptionRegion
    {
        public ExceptionBlock? harmonyBlock;
        public ExceptionRegion? parent;
        public ExceptionRegion? next;
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
        public required ExceptionRegion exceptionRegion;
        public int startingInstructionIndex = 0;
        public readonly List<Label> labels = [];
        public readonly List<CodeInstruction> instructions = [];
        public readonly List<BasicBlock> successors = [];
        public readonly List<BasicBlock> predecessors = [];
        public BasicBlock? fallthroughBlock;

        public string ID => $"#{startingInstructionIndex}";
    }

    static bool IsBlockStart(ExceptionBlock b) => b.blockType != ExceptionBlockType.EndExceptionBlock;
    static bool IsBlockEnd(ExceptionBlock b) => b.blockType == ExceptionBlockType.EndExceptionBlock;

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
            FileLog.LogBuffered("#####################################");

            foreach (var label in block.labels)
                FileLog.LogIL(codePos, label);

            foreach (var harmonyBlock in ExceptionBlockBegins(index))
                FileLog.LogILBlockBegin(codePos, harmonyBlock);

            foreach (var codeInstruction in block.instructions)
                LogInstruction(codeInstruction, ref codePos);

            if (block.fallthroughBlock != null)
                FileLog.LogBuffered($"// fallthrough => {block.fallthroughBlock.ID}");

            foreach (var harmonyBlock in ExceptionBlockEnds(index))
                FileLog.LogILBlockEnd(codePos, harmonyBlock);
        }

        FileLog.LogBuffered("");
        FileLog.FlushBuffer();
    }

    private IEnumerable<ExceptionBlock> ExceptionBlockBegins(int index)
    {
        ExceptionRegion? blockExceptionRegion = basicBlocks[index].exceptionRegion;
        ExceptionRegion? prevBlockExceptionRegion = index >= 1 ? basicBlocks[index - 1].exceptionRegion : null;
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
        ExceptionRegion? blockExceptionRegion = basicBlocks[index].exceptionRegion;
        ExceptionRegion? nextBlockExceptionRegion = index < basicBlocks.Count - 1 ? basicBlocks[index + 1].exceptionRegion : null;
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
        LogBlocks("MakeBasicBlocks");

        BranchElimination();
        LogBlocks("BranchElimination");

        InsertBranches();
        LogBlocks("InsertBranches");

        Emit();
        LogInstructions("Output", output.Instructions);
    }

    private void Emit()
    {
        for (var index = 0; index < basicBlocks.Count; index++)
        {
            BasicBlock? block = basicBlocks[index];
            if (block.instructions.Count == 0)
                block.instructions.Add(new(OpCodes.Nop));
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

        int instructionIndex = 0;
        ExceptionRegion exceptionRegion = exceptionRoot;

        foreach (var inst in inputInstructions)
        {
            if (inst.labels.Count > 0 || inst.blocks.Any(IsBlockStart))
            {
                foreach (var harmonyBlock in inst.blocks.Where(IsBlockStart))
                    EnterExceptionRegion(harmonyBlock);

                NewBasicBlock();
                curBlock.labels.AddRange(inst.labels);
                if (curBlock.labels.Count == 0)
                    curBlock.labels.Add(generator.DefineLabel());
            }

            curBlock.instructions.Add(inst);
            instructionIndex++;

            if (inst.CanBranch || inst.blocks.Any(IsBlockEnd))
            {
                foreach (var _ in inst.blocks.Where(IsBlockEnd))
                    exceptionRegion = exceptionRegion.parent!;

                NewBasicBlock();
            }

            inst.labels.Clear();
            inst.blocks.Clear();
        }

        if (curBlock.instructions.Count == 0)
            basicBlocks.Remove(curBlock);

        for (int i = 0; i < basicBlocks.Count - 1; i++)
        {
            if (basicBlocks[i].instructions[^1].CanFallThrough)
                basicBlocks[i].fallthroughBlock = basicBlocks[i + 1];
        }
        // Add a ret to the last basic block if one is missing (perhaps because of a poorly behaved transpiler)
        if (basicBlocks[^1].instructions[^1].CanFallThrough)
            basicBlocks[^1].instructions.Add(new(OpCodes.Ret));

        foreach (var block in basicBlocks)
        {
            if (block.fallthroughBlock is not null)
                block.successors.Add(block.fallthroughBlock);

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

        foreach (var block in basicBlocks)
        foreach (var label in block.labels)
            labelToBlock[label] = block;

        UpdatePredecessors();

        return;

        void NewBasicBlock()
        {
            if (curBlock.instructions.Count == 0)
            {
                curBlock.exceptionRegion = exceptionRegion;
                return;
            }

            BasicBlock newBlock = new() { startingInstructionIndex = instructionIndex, exceptionRegion = exceptionRegion };
            basicBlocks.Add(newBlock);
            curBlock = newBlock;
        }

        void EnterExceptionRegion(ExceptionBlock harmonyBlock)
        {
            if (harmonyBlock.blockType == ExceptionBlockType.BeginExceptionBlock)
            {
                var newRegion = new ExceptionRegion()
                {
                    depth = exceptionRegion.depth + 1,
                    harmonyBlock = harmonyBlock,
                    parent = exceptionRegion,
                };
                exceptionRegion = newRegion;
            }
            else
            {
                var newRegion = new ExceptionRegion()
                {
                    depth = exceptionRegion.depth,
                    harmonyBlock = harmonyBlock,
                    parent = exceptionRegion.parent,
                };
                exceptionRegion.next = newRegion;
                exceptionRegion = newRegion;
            }
        }
    }

    private void BranchElimination()
    {
        bool changed = false;

        foreach (var block in basicBlocks)
        {
            if (block.instructions.Count == 0)
                continue;

            if (block.instructions[^1].IsUnconditionalBranch)
            {
                block.fallthroughBlock = labelToBlock[(Label)block.instructions[^1].operand];
                block.instructions.RemoveAt(block.instructions.Count - 1);
                changed = true;
            }
            else if (block.instructions[^1].opcode is
                     {
                         FlowControl: FlowControl.Cond_Branch, 
                         StackBehaviourPop: StackBehaviour.Pop1,
                         StackBehaviourPush: StackBehaviour.Push0,
                     })
            {
                var targetBlock = labelToBlock[(Label)block.instructions[^1].operand];
                if (targetBlock == block.fallthroughBlock)
                {
                    block.instructions[^1] = new(OpCodes.Pop);
                    block.successors.Remove(targetBlock);
                    changed = true;
                }
            }
        }

        if (changed)
            UpdatePredecessors();
    }

    private void InsertBranches()
    {
        for (int i = 0; i < basicBlocks.Count; i++)
        {
            BasicBlock? fallthroughBlock = basicBlocks[i].fallthroughBlock;
            if (fallthroughBlock == null || (i < basicBlocks.Count - 1 && fallthroughBlock == basicBlocks[i + 1]))
                continue;
            basicBlocks[i].instructions.Add(new(OpCodes.Br, fallthroughBlock.labels[0]));
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

    public readonly InstructionList output = [];
    private readonly List<BasicBlock> basicBlocks = [];
    private readonly ExceptionRegion exceptionRoot = new();
    private readonly Dictionary<Label, BasicBlock> labelToBlock = new();
}
