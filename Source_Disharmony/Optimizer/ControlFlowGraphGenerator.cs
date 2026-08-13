namespace Disharmony.Optimizer;

internal class ControlFlowGraphGenerator
{
    public Dictionary<Label, BlockLabel> BlockLabels { get; } = [];
    public Dictionary<BlockLabel, (List<StackSlot> IncomingStack, List<StackSlot> OutgoingStack)> BlockStacks { get; } = [];

    public ControlFlowGraphGenerator(MethodBase method, List<CodeInstruction> codeInstructions)
    {
        CodeInstructions = codeInstructions;

        if (method.HasThis)
            ParameterTypes = [method.DeclaringType.CallableType, .. method.GetParameters().Types()];
        else
            ParameterTypes = [.. method.GetParameters().Types()];

        MethodBody = GetMethodBodyOrNull(method);
        ReturnType = method is MethodInfo methodInfo ? methodInfo.ReturnType : typeof(void);
    }

    private MethodBody? GetMethodBodyOrNull(MethodBase method)
    {
        try
        {
            return method.GetMethodBody();
        }
        catch (InvalidOperationException)
        {
            return null;
        }
        catch (NotSupportedException)
        {
            return null;
        }
    }

    public ControlFlowGraph ControlFlowGraph { get; } = new();
    public RootRegion RootRegion { get; } = new(new BlockLabel());
    public List<CodeInstruction> CodeInstructions { get; }
    public Dictionary<int, Local> Locals { get; } = [];
    public MethodBody? MethodBody { get; }
    public Dictionary<int, Argument> Arguments { get; } = [];
    public List<Type> ParameterTypes { get; }

    public Type ReturnType { get; }

    public void CreateControlFlowGraph()
    {
        CreateArguments();

        CreateLocals();

        CreateBlockLabels();

        List<(BlockLabel Label, List<CodeInstruction> Instructions)> instructionBlocks = FindBasicBlocks();

        CreateBasicBlocks(instructionBlocks);

        CreateEdges();
    }

    private void CreateArguments()
    {
        // Get arguments from MethodInfo
        for (int index = 0; index < ParameterTypes.Count; index++)
            Arguments.Add(index, new Argument(index, ParameterTypes[index]));
    }

    private void CreateLocals()
    {
        // Get locals from MethodInfo
        if (MethodBody != null)
        {
            foreach (var local in MethodBody.LocalVariables)
                Locals.Add(local.LocalIndex, new Local(local.LocalIndex, local.LocalType, null));
        }

        // Get locals from LocalBuilders
        foreach (var instruction in CodeInstructions)
        {
            if (instruction.operand is not LocalBuilder localBuilder)
                continue;

            if (Locals.TryGetValue(localBuilder.LocalIndex, out var local))
            {
                if (local.Type != localBuilder.LocalType)
                    throw new InvalidOperationException($"Conflicting types for local #{localBuilder.LocalIndex}");

                Locals[localBuilder.LocalIndex] = local with { LocalBuilder = localBuilder };
            }
            else
            {
                Locals.Add(localBuilder.LocalIndex,
                    new Local(localBuilder.LocalIndex, localBuilder.LocalType, localBuilder));
            }
        }
    }

    private void CreateBlockLabels()
    {
        // Generate BlockLabels
        foreach (var instruction in CodeInstructions)
        {
            if (instruction.labels.Count == 0)
                continue;

            var blockLabel = new BlockLabel(instruction.labels[0]);
            foreach (var label in instruction.labels)
                BlockLabels.Add(label, blockLabel);
        }
    }

    private List<(BlockLabel Label, List<CodeInstruction> Instructions)> FindBasicBlocks()
    {
        // Find basic blocks
        List<(BlockLabel Label, List<CodeInstruction> Instructions)> instructionBlocks = [];
        bool newBlock = true;
        foreach (CodeInstruction instruction in CodeInstructions)
        {
            if (instruction.labels.Count > 0)
                newBlock = true;
            if (instruction.blocks.Any(b => b.blockType != ExceptionBlockType.EndExceptionBlock))
                newBlock = true;

            if (newBlock)
            {
                var label = instruction.labels.Count > 0 ? BlockLabels[instruction.labels[0]] : new BlockLabel();
                instructionBlocks.Add(new(label, []));
                newBlock = false;
            }

            instructionBlocks[^1].Instructions.Add(instruction);

            if (EndsBasicBlock(instruction))
                newBlock = true;
            if (instruction.blocks.Any(b => b.blockType == ExceptionBlockType.EndExceptionBlock))
                newBlock = true;
        }

        return instructionBlocks;
    }

    private void CreateBasicBlocks(List<(BlockLabel Label, List<CodeInstruction> Instructions)> instructionBlocks)
    {
        // Exception region data
        Stack<Region> regionStack = [];
        Stack<(ProtectedRegion ProtectedRegion, List<HandlerRegion> HandlerRegions)> exceptionGroupStack = [];
        regionStack.Push(RootRegion);

        // Translate basic blocks
        Dictionary<BlockLabel, int> incomingStackSize = [];
        foreach (var (label, instructions) in instructionBlocks)
        {
            foreach (var exceptionBlock in instructions[0].blocks)
            {
                switch (exceptionBlock.blockType)
                {
                    case ExceptionBlockType.BeginExceptionBlock:
                    {
                        var protectedRegion = new ProtectedRegion(label, regionStack.Peek());
                        regionStack.Push(protectedRegion);
                        exceptionGroupStack.Push((protectedRegion, []));
                        break;
                    }
                    case ExceptionBlockType.BeginCatchBlock:
                    {
                        regionStack.Pop();
                        var catchRegion = new CatchRegion(label, regionStack.Peek(), new StackSlot(0, exceptionBlock.catchType));
                        regionStack.Push(catchRegion);
                        exceptionGroupStack.Peek().HandlerRegions.Add(catchRegion);
                        break;
                    }
                    case ExceptionBlockType.BeginExceptFilterBlock:
                        throw new NotSupportedException();
                    case ExceptionBlockType.BeginFaultBlock:
                    {
                        regionStack.Pop();
                        var faultRegion = new FaultRegion(label, regionStack.Peek());
                        regionStack.Push(faultRegion);
                        exceptionGroupStack.Peek().HandlerRegions.Add(faultRegion);
                        break;
                    }
                    case ExceptionBlockType.BeginFinallyBlock:
                    {
                        regionStack.Pop();
                        var finallyRegion = new FinallyRegion(label, regionStack.Peek());
                        regionStack.Push(finallyRegion);
                        exceptionGroupStack.Peek().HandlerRegions.Add(finallyRegion);
                        break;
                    }
                    case ExceptionBlockType.EndExceptionBlock:
                        // Handled later
                        break;
                    default: throw new ArgumentOutOfRangeException();
                }
            }

            incomingStackSize.TryGetValue(label, out int blockStartStackSize);
            BasicBlock block = ConvertBasicBlock(label, instructions, blockStartStackSize, out var stacks);
            ControlFlowGraph.AddBlock(block);
            BlockStacks[label] = stacks;
            foreach (var successor in block.Branch.Labels)
                incomingStackSize[successor] = stacks.OutgoingStack.Count;

            foreach (var exceptionBlock in instructions[^1].blocks)
            {
                if (exceptionBlock.blockType == ExceptionBlockType.EndExceptionBlock)
                {
                    regionStack.Pop();
                    var (protectedRegion, handlerRegions) = exceptionGroupStack.Pop();
                    ControlFlowGraph.AddExceptionGroup(new ExceptionGroup(protectedRegion, handlerRegions));
                }
            }
        }
    }

    private void CreateEdges()
    {
        foreach (var block in ControlFlowGraph.BasicBlocks)
        {
            var label = block.Label;
            foreach (var successor in block.Branch.Labels)
            {
                if (ControlFlowGraph.GetEdgeOrNull(label, successor) != null)
                    continue;

                var edgeAssignments = BlockStacks[successor].IncomingStack.Zip(BlockStacks[label].OutgoingStack,
                    (incoming, outgoing) => new AssignmentOp(incoming, outgoing)).ToList();
                var edge = new Edge(label, successor, edgeAssignments);
                ControlFlowGraph.AddEdge(edge);
            }
        }
    }

    private static bool EndsBasicBlock(CodeInstruction instruction) => instruction.opcode.FlowControl is
        FlowControl.Branch or FlowControl.Cond_Branch or FlowControl.Return or FlowControl.Throw;

    private BasicBlock ConvertBasicBlock(
        BlockLabel label,
        IReadOnlyList<CodeInstruction> instructions,
        int incomingStackSize,
        out (List<StackSlot> IncomingStack, List<StackSlot> OutgoingStack) stacks)
    {
        List<StackSlot> incomingStack = [];
        for (int i = 0; i < incomingStackSize; i++)
            incomingStack.Add(new StackSlot(i, typeof(TypeLattice.UnknownType)));
        List<StackSlot> curStack = [.. incomingStack];

        throw new NotImplementedException();
    }
}
