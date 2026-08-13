namespace Disharmony.Optimizer;

internal class ControlFlowGraphGenerator
{
    public ControlFlowGraphGenerator(MethodBase method, List<CodeInstruction> codeInstructions)
    {
        Method = method;
        CodeInstructions = codeInstructions;

        if (method.HasThis)
            ParameterTypes = [method.DeclaringType.CallableType, .. method.GetParameters().Types()];
        else
            ParameterTypes = [.. method.GetParameters().Types()];

        MethodBody = GetMethodBodyOrNull(method);
        ReturnType = method is MethodInfo methodInfo ? methodInfo.ReturnType : typeof(void);
    }

    public MethodBase Method { get; }
    public Dictionary<Label, BlockLabel> BlockLabels { get; } = [];
    public Dictionary<BlockLabel, (List<StackSlot> IncomingStack, List<StackSlot> OutgoingStack)> BlockStacks { get; } = [];

    public ControlFlowGraph ControlFlowGraph { get; } = new();
    public RootRegion RootRegion { get; } = new(new BlockLabel());
    public List<CodeInstruction> CodeInstructions { get; }
    public Dictionary<int, Local> Locals { get; } = [];
    public MethodBody? MethodBody { get; }
    public Dictionary<int, Argument> Arguments { get; } = [];
    public List<Type> ParameterTypes { get; }

    public Type ReturnType { get; }

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
        for (var index = 0; index < instructionBlocks.Count; index++)
        {
            var (label, instructions) = instructionBlocks[index];
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
                    case ExceptionBlockType.BeginExceptFilterBlock: throw new NotSupportedException();
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

            var fallthroughLabel = index + 1 < instructionBlocks.Count ? instructionBlocks[index + 1].Label : null;
            incomingStackSize.TryGetValue(label, out int blockStartStackSize);
            BasicBlock block = ConvertBasicBlock(label, instructions, fallthroughLabel, regionStack.Peek(), blockStartStackSize,
                out var stacks);
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
        BlockLabel blockLabel,
        IReadOnlyList<CodeInstruction> instructions,
        BlockLabel? fallthroughLabel,
        Region region,
        int incomingStackSize,
        out (List<StackSlot> IncomingStack, List<StackSlot> OutgoingStack) stacks)
    {
        List<StackSlot> incomingStack = [];
        for (int i = 0; i < incomingStackSize; i++)
            incomingStack.Add(new StackSlot(i, TypeLattice.Unknown));
        List<StackSlot> curStack = [.. incomingStack];

        List<Op> ops = [];
        List<Prefix> prefixes = [];
        Branch? branch = null;
        foreach (var instruction in instructions)
        {
            if (instruction.opcode.OpCodeType == OpCodeType.Prefix)
            {
                prefixes.Add(new(instruction.opcode, instruction.operand));
                continue;
            }

            int popCount = PopCount(instruction);
            List<StackSlot> popped = curStack.GetRange(curStack.Count - popCount, popCount);
            curStack.RemoveRange(curStack.Count - popCount, popCount);

            // Dup is the only instruction that pushes multiple values, handle it specially
            if (instruction.opcode == OpCodes.Dup)
            {
                curStack.Add(popped[0]);
                curStack.Add(popped[0]);
                continue;
            }

            ILInstruction il = new ILInstruction(instruction.opcode, instruction.operand, prefixes);
            prefixes = [];

            if (EndsBasicBlock(instruction))
            {
                branch = ConvertBranch(instruction, il, popped, fallthroughLabel);
                break;
            }

            switch (instruction.opcode.StackBehaviourPush)
            {
                case StackBehaviour.Push0:
                case StackBehaviour.Varpush when instruction.operand is MethodInfo method && method.ReturnType == typeof(void):
                {
                    ops.Add(new ILOp(il, popped, typeof(void))); 
                    break;
                }
                default:
                {
                    StackSlot result = new StackSlot(curStack.Count, TypeLattice.Unknown);
                    ops.Add(new AssignmentOp(result, new ILOp(il, popped, TypeLattice.Unknown)));
                    curStack.Add(result);
                    break;
                }
            }
        }

        branch ??= new UnconditionalBranch(fallthroughLabel ?? throw new InvalidOperationException());

        stacks = (incomingStack, curStack);
        return new BasicBlock(blockLabel, ops, region, branch);
    }

    private Branch? ConvertBranch(CodeInstruction instruction, ILInstruction il, List<StackSlot> popped, BlockLabel? fallthroughLabel)
    {
        Branch? branch = OpCodeData.GetCanonicalOpcode(instruction) switch
        {
            OpCodeValues.Br when instruction.operand is Label label => new UnconditionalBranch(BlockLabels[label]),
            OpCodeValues.Leave when instruction.operand is Label label => new Leave(BlockLabels[label]),
            OpCodeValues.Throw => new Throw(popped[0]),
            OpCodeValues.Rethrow => new Rethrow(),
            _ => instruction.opcode.FlowControl switch
            {
                FlowControl.Cond_Branch when instruction.operand is Label label =>
                    new ConditionalBranch(instruction.opcode, popped,
                        [fallthroughLabel ?? throw new InvalidOperationException(), BlockLabels[label]]),
                FlowControl.Cond_Branch when instruction.operand is Label[] labels =>
                    new ConditionalBranch(instruction.opcode, popped,
                        [fallthroughLabel ?? throw new InvalidOperationException(), .. labels.Select(label => BlockLabels[label])]),
                FlowControl.Return when popped.Count == 0 => new Return(il, new VoidOp()),
                FlowControl.Return => new Return(il, popped[0]),
                FlowControl.Throw => new Throw(popped[0]),
                _ => throw new ArgumentOutOfRangeException(),
            },
        };
        return branch;
    }

    private int PopCount(CodeInstruction instruction)
    {
        return instruction.opcode.StackBehaviourPop switch
        {
            StackBehaviour.Pop0 => 0,
            StackBehaviour.Pop1 => 1,
            StackBehaviour.Pop1_pop1 => 2,
            StackBehaviour.Popi => 1,
            StackBehaviour.Popi_pop1 => 2,
            StackBehaviour.Popi_popi => 2,
            StackBehaviour.Popi_popi8 => 2,
            StackBehaviour.Popi_popi_popi => 3,
            StackBehaviour.Popi_popr4 => 2,
            StackBehaviour.Popi_popr8 => 2,
            StackBehaviour.Popref => 1,
            StackBehaviour.Popref_pop1 => 2,
            StackBehaviour.Popref_popi => 2,
            StackBehaviour.Popref_popi_popi => 3,
            StackBehaviour.Popref_popi_popi8 => 3,
            StackBehaviour.Popref_popi_popr4 => 3,
            StackBehaviour.Popref_popi_popr8 => 3,
            StackBehaviour.Popref_popi_popref => 3,
            StackBehaviour.Popref_popi_pop1 => 3,
            StackBehaviour.Varpop when instruction.operand is MethodInfo methodInfo => methodInfo.GetParameters().Length +
                                                                                       (methodInfo.HasThis ? 1 : 0),
            StackBehaviour.Varpop when instruction.operand is ConstructorInfo constructorInfo => constructorInfo.GetParameters().Length,
            StackBehaviour.Varpop => OpCodeData.GetCanonicalOpcode(instruction) switch
            {
                OpCodeValues.Ret => ReturnType == typeof(void) ? 0 : 1,
                _ => throw new ArgumentOutOfRangeException(),
            },
            _ => throw new ArgumentOutOfRangeException(),
        };
    }
}
