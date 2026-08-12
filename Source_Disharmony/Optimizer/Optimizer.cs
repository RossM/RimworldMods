namespace Disharmony.Optimizer;

internal class Optimizer
{
    private readonly MethodBase method;
    private readonly List<CodeInstruction> inputInstructions;
    internal readonly ILGenerator generator;
    private readonly bool debug;
    internal readonly List<Type> parameterTypes;
    internal readonly Type returnType;

    internal readonly ControlFlowGraph cfg = new();
    internal readonly Dictionary<int, Argument> arguments = [];
    internal readonly Dictionary<int, Local> locals = [];

    private static readonly bool forceDebug;
    private static readonly string forceDebugForMethod;

    private readonly RootRegion rootRegion = new(new BlockLabel());

    static Optimizer()
    {
        forceDebug = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("DISHARMONY_DEBUG"));
        forceDebugForMethod = Environment.GetEnvironmentVariable("DISHARMONY_DEBUG_METHOD");
    }

    public Optimizer(MethodBase method, List<CodeInstruction> inputInstructions, ILGenerator generator, bool debug)
    {
        this.method = method;
        this.inputInstructions = inputInstructions;
        this.generator = generator;
        this.debug = debug || forceDebug || !string.IsNullOrEmpty(forceDebugForMethod) && method.Name == forceDebugForMethod;

        if (method.HasThis)
            parameterTypes = [method.DeclaringType.CallableType, .. method.GetParameters().Types()];
        else
            parameterTypes = [.. method.GetParameters().Types()];

        returnType = method is MethodInfo methodInfo ? methodInfo.ReturnType : typeof(void);
    }

    private void CreateControlFlowGraph()
    {
        Dictionary<Label, BlockLabel> blockLabels = [];

        for (int index = 0; index < parameterTypes.Count; index++)
            arguments.Add(index, new Argument(index, parameterTypes[index]));

        MethodBody? methodBody = GetMethodBodyOrNull();
        if (methodBody != null)
        {
            foreach (var local in methodBody.LocalVariables)
                locals.Add(local.LocalIndex, new Local(local.LocalIndex, local.LocalType, null));
        }

        foreach (var instruction in inputInstructions)
        {
            if (instruction.operand is not LocalBuilder localBuilder)
                continue;

            if (locals.TryGetValue(localBuilder.LocalIndex, out var local))
            {
                if (local.Type != localBuilder.LocalType)
                    throw new InvalidOperationException($"Conflicting types for local #{localBuilder.LocalIndex}");

                locals[localBuilder.LocalIndex] = local with { LocalBuilder = localBuilder };
            }
            else
            {
                locals.Add(localBuilder.LocalIndex,
                    new Local(localBuilder.LocalIndex, localBuilder.LocalType, localBuilder));
            }
        }

        foreach (var instruction in inputInstructions)
        {
            if (instruction.labels.Count == 0)
                continue;

            var blockLabel = new BlockLabel(instruction.labels[0]);
            foreach (var label in instruction.labels)
                blockLabels.Add(label, blockLabel);
        }

        List<(BlockLabel Label, List<CodeInstruction> Instructions)> instructionBlocks = [];

        bool newBlock = true;
        for (int index = 0; index < inputInstructions.Count; index++)
        {
            var instruction = inputInstructions[index];

            if (instruction.labels.Count > 0)
                newBlock = true;
            if (instruction.blocks.Any(b => b.blockType != ExceptionBlockType.EndExceptionBlock))
                newBlock = true;

            if (newBlock)
            {
                var label = instruction.labels.Count > 0 ? blockLabels[instruction.labels[0]] : new BlockLabel();
                instructionBlocks.Add(new(label, []));
                newBlock = false;
            }

            instructionBlocks[^1].Instructions.Add(instruction);

            if (EndsBasicBlock(instruction))
                newBlock = true;
            if (instruction.blocks.Any(b => b.blockType == ExceptionBlockType.EndExceptionBlock))
                newBlock = true;
        }

        Stack<Region> regionStack = [];
        Stack<(ProtectedRegion ProtectedRegion, List<HandlerRegion> HandlerRegions)> exceptionGroupStack = [];
        regionStack.Push(rootRegion);

        Dictionary<BlockLabel, int> incomingStackSize = [];
        Dictionary<BlockLabel, (List<StackSlot> IncomingStack, List<StackSlot> OutgoingStack)> stacks = [];
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
            BasicBlock block = ConvertBasicBlock(label, instructions, blockStartStackSize, out var stack);
            cfg.AddBlock(block);
            stacks[label] = stack;
            foreach (var successor in block.Branch.Labels)
                incomingStackSize[successor] = stack.OutgoingStack.Count;

            foreach (var exceptionBlock in instructions[^1].blocks)
            {
                if (exceptionBlock.blockType == ExceptionBlockType.EndExceptionBlock)
                {
                    regionStack.Pop();
                    var (protectedRegion, handlerRegions) = exceptionGroupStack.Pop();
                    cfg.AddExceptionGroup(new ExceptionGroup(protectedRegion, handlerRegions));
                }
            }
        }

        foreach (var block in cfg.BasicBlocks)
        {
            var label = block.Label;
            foreach (var successor in block.Branch.Labels)
            {
                if (cfg.GetEdgeOrNull(label, successor) != null)
                    continue;

                var edgeAssignments = stacks[successor].IncomingStack.Zip(stacks[label].OutgoingStack,
                    (incoming, outgoing) => new AssignmentOp(incoming, outgoing)).ToList();
                var edge = new Edge(label, successor, edgeAssignments);
                cfg.AddEdge(edge);
            }
        }
    }

    private static bool EndsBasicBlock(CodeInstruction instruction) => instruction.opcode.FlowControl is
        FlowControl.Branch or FlowControl.Cond_Branch or FlowControl.Return or FlowControl.Throw;

    private BasicBlock ConvertBasicBlock(
        BlockLabel label,
        IReadOnlyList<CodeInstruction> instructions,
        int incomingStackSize,
        out (List<StackSlot> IncomingStack, List<StackSlot> OutgoingStack) stack)
    {
        throw new NotImplementedException();
    }

    private MethodBody? GetMethodBodyOrNull()
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

    public List<CodeInstruction> Optimize()
    {
        return inputInstructions;
    }
}
