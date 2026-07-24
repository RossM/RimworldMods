using System.Security.Policy;

namespace Disharmony;

internal class Optimizer(MethodBase method, List<CodeInstruction> inputInstructions, ILGenerator generator)
{
    private class BasicBlock
    {
        public int startingInstructionIndex = 0;
        public List<Label> labels = [];
        public readonly List<CodeInstruction> instructions = [];
        public List<BasicBlock> successors = [];
        public List<BasicBlock> predecessors = [];
        public BasicBlock? fallthroughBlock;

        public string ID => $"#{startingInstructionIndex}";

        public void Append(CodeInstruction inst)
        {
            inst.blocks.AddRange(instructions[^1].blocks.Where(IsBlockEnd));
            instructions[^1].blocks.RemoveAll(IsBlockEnd);
            instructions.Add(inst);
        }
    }

    static bool IsBlockStart(ExceptionBlock b) => b.blockType != ExceptionBlockType.EndExceptionBlock;
    static bool IsBlockEnd(ExceptionBlock b) => b.blockType == ExceptionBlockType.EndExceptionBlock;

    private void LogInstructions(string phase)
    {
        int codePos = 0;

        FileLog.LogBuffered($"### Optimizer {phase}: {method.FullDescription()}");

        if (basicBlocks.Count > 0)
        {
            foreach (var block in basicBlocks)
            {
                FileLog.LogBuffered("#####################################");
                FileLog.LogBuffered($"# Basic block:  {block.ID,-19} #");
                FileLog.LogBuffered($"# Predecessors: {string.Join(", ", block.predecessors.Select(b => b.ID)),-19} #");
                FileLog.LogBuffered($"# Successors:   {string.Join(", ", block.successors.Select(b => b.ID)),-19} #");
                FileLog.LogBuffered("#####################################");
                foreach (var codeInstruction in block.instructions)
                    LogInstruction(codeInstruction, ref codePos);
            }
        }
        else
        {
            foreach (var codeInstruction in inputInstructions)
                LogInstruction(codeInstruction, ref codePos);
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
        LogInstructions("Input");

        MakeBasicBlocks();
        RemoveNops();
        RewriteLabels();
        LogInstructions("MakeBasicBlocks");

        RemoveFallthroughs();
        RewriteLabels();
        LogInstructions("RemoveFallthroughs");

        JumpThreading();
        LogInstructions("JumpThreading");

        MergeBasicBlocks();
        RewriteLabels();
        LogInstructions("MergeBasicBlocks");

        DeadCodeElimination();
        LogInstructions("DeadCodeElimination");

        foreach (var block in basicBlocks)
        foreach (var inst in block.instructions)
            output.Add(inst);
    }

    /// <summary>
    ///     Generate basic blocks and remove some nops (those with no labels or blocks).
    /// </summary>
    private void MakeBasicBlocks()
    {
        BasicBlock curBlock = new();
        basicBlocks.Add(curBlock);

        int instructionIndex = 0;

        foreach (var inst in inputInstructions)
        {
            if (inst.labels.Count == 0 && inst.blocks.Count == 0 && inst.opcode == OpCodes.Nop)
                continue;

            if (inst.labels.Count > 0 || inst.blocks.Any(IsBlockStart))
            {
                NewBasicBlock();
                curBlock.labels.AddRange(inst.labels);
            }

            curBlock.instructions.Add(inst);
            instructionIndex++;

            if (inst.CanBranch || inst.blocks.Any(IsBlockEnd))
                NewBasicBlock();
        }

        if (curBlock.instructions.Count == 0)
            basicBlocks.Remove(curBlock);

        foreach (var block in basicBlocks)
        {
            var finalInstruction = block.instructions[^1];
            
            if (finalInstruction.CanFallThrough && block.fallthroughBlock is not null)
                block.successors.Add(block.fallthroughBlock);

            switch (finalInstruction.operand)
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

        void NewBasicBlock()
        {
            if (curBlock.instructions.Count == 0)
                return;

            BasicBlock newBlock = new() { startingInstructionIndex = instructionIndex };
            basicBlocks.Add(newBlock);
            curBlock.fallthroughBlock = newBlock;
            curBlock = newBlock;
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

    private void RemoveNops()
    {
        foreach (var block in basicBlocks)
            block.instructions.RemoveAll(i => i.opcode == OpCodes.Nop && i.blocks.Count == 0);
    }

    /// <summary>
    ///     Rewrite instruction labels to match the basic block's.
    /// </summary>
    private void RewriteLabels()
    {
        foreach (var block in basicBlocks)
        {
            foreach (var inst in block.instructions)
                inst.labels.Clear();
            block.instructions[0].labels.AddRange(block.labels);
        }
    }

    /// <summary>
    ///     Ensure that all basic blocks end with a control transfer instruction. After this has
    ///     run, there may be a conditional control transfer instruction that is not the last
    ///     instruction in the block.
    /// </summary>
    private void RemoveFallthroughs()
    {
        foreach (var block in basicBlocks)
        {
            if (!block.instructions[^1].CanFallThrough)
                continue;
            if (block.fallthroughBlock == null)
                block.Append(new(OpCodes.Ret));
            else
            {
                if (block.fallthroughBlock.labels.Count == 0)
                    block.fallthroughBlock.labels.Add(generator.DefineLabel());
                block.Append(new(OpCodes.Br, block.fallthroughBlock.labels[0]));
            }
        }
    }

    private void JumpThreading()
    {
        foreach (var block in basicBlocks)
        foreach (var inst in block.instructions)
        {
            if (inst.operand is Label label)
            {
                var targetBlock = basicBlocks.Single(b => b.labels.Contains(label));
                CodeInstruction firstInstruction = targetBlock.instructions[0];
                if (firstInstruction.IsUnconditionalBranch && !firstInstruction.blocks.Any(IsBlockStart))
                {
                    var newTargetBlock = basicBlocks.Single(b => b.labels.Contains((Label)firstInstruction.operand));
                    block.successors.Remove(targetBlock);
                    block.successors.Add(newTargetBlock);
                    inst.operand = firstInstruction.operand;
                }
            }
        }

        UpdatePredecessors();
    }

    /// <summary>
    ///     Merge basic blocks that are each other's only successor and predecessor.
    ///     Must not be run before RemoveFallthroughs.
    /// </summary>
    private void MergeBasicBlocks()
    {
        for (int i = 0; i < basicBlocks.Count; i++)
        {
            var block = basicBlocks[i];

            while (true)
            {
                if (block.successors.Count != 1 || block.instructions[^1].blocks.Any(IsBlockEnd) || block.instructions[^1].IsLeave)
                    break;
                var successor = block.successors[0];

                if (successor.predecessors.Count != 1 || successor.instructions[0].blocks.Any(IsBlockStart))
                    break;

                if (block.instructions[^1].opcode.FlowControl == FlowControl.Branch)
                    block.instructions.RemoveAt(block.instructions.Count - 1);

                block.instructions.AddRange(successor.instructions);
                block.successors = successor.successors;
                block.fallthroughBlock = successor.fallthroughBlock;
                basicBlocks.Remove(successor);
                UpdatePredecessors();
            }
        }
    }

    private void DeadCodeElimination()
    {
        Queue<BasicBlock> worklist = new();
        HashSet<BasicBlock> liveBlocks = new();

        worklist.Enqueue(basicBlocks[0]);

        // Badly need better structured exception handling
        foreach (var block in basicBlocks)
        {
            if (block.instructions[0].blocks.Any(IsBlockStart))
                worklist.Enqueue(block);
        }

        while (worklist.Count > 0)
        {
            var block = worklist.Dequeue();
            if (liveBlocks.Add(block))
                foreach (var successor in block.successors)
                    worklist.Enqueue(successor);
        }

        basicBlocks.RemoveAll(b => !liveBlocks.Contains(b));
    }

    public readonly InstructionList output = [];
    private readonly List<BasicBlock> basicBlocks = [];
}
