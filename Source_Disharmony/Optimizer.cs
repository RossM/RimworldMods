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

        MergeBasicBlocks();
        RewriteLabels();
        LogInstructions("MergeBasicBlocks");

        foreach (var block in basicBlocks)
        foreach (var inst in block.instructions)
            output.Add(inst);
    }

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

            if (inst.opcode.FlowControl is not (FlowControl.Next or FlowControl.Call or FlowControl.Meta) || inst.blocks.Any(IsBlockEnd))
                NewBasicBlock();
        }

        if (curBlock.instructions.Count == 0)
            basicBlocks.Remove(curBlock);

        foreach (var block in basicBlocks)
        {
            var finalInstruction = block.instructions[^1];
            if (finalInstruction.opcode.FlowControl is FlowControl.Next or FlowControl.Call or FlowControl.Meta or FlowControl.Cond_Branch && block.fallthroughBlock is not null)
                block.successors.Add(block.fallthroughBlock);
            if (finalInstruction.operand is Label label)
                block.successors.Add(basicBlocks.Single(b => b.labels.Contains(label)));
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

    private void RewriteLabels()
    {
        foreach (var block in basicBlocks)
        {
            foreach (var inst in block.instructions)
                inst.labels.Clear();
            block.instructions[0].labels.AddRange(block.labels);
        }
    }

    private void MergeBasicBlocks()
    {
        for (int i = 0; i < basicBlocks.Count; i++)
        {
            var block = basicBlocks[i];

            while (true)
            {
                if (block.successors.Count != 1 || block.instructions[^1].blocks.Any(IsBlockEnd))
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

    private readonly ILGenerator generator = generator;
    public readonly InstructionList output = [];
    private readonly List<BasicBlock> basicBlocks = [];
}
