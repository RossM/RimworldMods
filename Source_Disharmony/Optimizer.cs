namespace Disharmony;

internal class Optimizer(MethodBase method, List<CodeInstruction> inputInstructions, ILGenerator generator)
{
    private class BasicBlock
    {
        public int startingInstructionIndex = 0;
        public readonly List<Label> labels = [];
        public readonly List<CodeInstruction> instructions = [];
        public readonly List<BasicBlock> successors = [];
        public readonly List<BasicBlock> predecessors = [];
        public BasicBlock? fallthroughBlock;

        public string ID => $"#{startingInstructionIndex}";
    }

    public static List<CodeInstruction> Transpiler(
        MethodBase method,
        IEnumerable<CodeInstruction> instructions,
        ILGenerator generator)
    {
        var optimizer = new Optimizer(method, [.. instructions], generator);
        optimizer.Optimize();
        return optimizer.output.Instructions;
    }

    private void LogInstructions(string phase)
    {
        int codePos = 0;

        FileLog.LogBuffered($"### Optimizer {phase}: {method.FullDescription()}");

        foreach (var block in basicBlocks)
        {
            FileLog.LogBuffered("#####################################");
            FileLog.LogBuffered($"# Basic block:  {block.ID,-19} #");
            FileLog.LogBuffered($"# Predecessors: {string.Join(", ", block.predecessors.Select(b => b.ID)),-19} #");
            FileLog.LogBuffered($"# Successors:   {string.Join(", ", block.successors.Select(b => b.ID)),-19} #");
            FileLog.LogBuffered("#####################################");
            foreach (var codeInstruction in block.instructions)
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
        }

        FileLog.LogBuffered("");
        FileLog.FlushBuffer();
    }

    private void Optimize()
    {
        MakeBasicBlocks();
        LogInstructions("MakeBasicBlocks");

        FileLog.LogBuffered($"### Final instructions: {method.FullDescription()}");
        foreach (var block in basicBlocks)
        foreach (var inst in block.instructions)
            output.Add(inst);
    }

    private void MakeBasicBlocks()
    {
        BasicBlock curBlock = new();
        basicBlocks.Add(curBlock);

        int instructionIndex = 0;

        List<CodeInstruction> annotations = [];

        foreach (var inst in inputInstructions)
        {
            if (inst.labels.Count == 0 && inst.opcode == OpCodes.Nop && inst.operand is string)
            {
                annotations.Add(inst);
                continue;
            }

            if (inst.labels.Count > 0)
            {
                NewBasicBlock();
                curBlock.labels.AddRange(inst.labels);
            }

            curBlock.instructions.AddRange(annotations);
            annotations.Clear();
            
            curBlock.instructions.Add(inst);
            instructionIndex++;

            if (inst.opcode.FlowControl is not (FlowControl.Next or FlowControl.Call or FlowControl.Meta))
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

        foreach (var block in basicBlocks)
        foreach (var successor in block.successors)
            successor.predecessors.Add(block);

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

    private readonly ILGenerator generator = generator;
    private readonly InstructionList output = [];
    private readonly List<BasicBlock> basicBlocks = [];
}
