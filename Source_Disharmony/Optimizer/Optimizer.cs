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

        // TODO: Region handling

        Dictionary<BlockLabel, int> incomingStackSize = [];
        foreach (var (label, instructions) in instructionBlocks)
        {
            incomingStackSize.TryGetValue(label, out int blockStartStackSize);
            BasicBlock block = ConvertBasicBlock(label, instructions, blockStartStackSize, out int blockEndStackSize);
            cfg.AddBlock(block);
            foreach (var successor in block.Branch.Labels)
                incomingStackSize[successor] = blockEndStackSize;
        }

        // TODO: Create edges
    }

    private static bool EndsBasicBlock(CodeInstruction instruction) => instruction.opcode.FlowControl is
        FlowControl.Branch or FlowControl.Cond_Branch or FlowControl.Return or FlowControl.Throw;

    private BasicBlock ConvertBasicBlock(
        BlockLabel label,
        IReadOnlyList<CodeInstruction> instructions,
        int incomingStackSize,
        out int blockEndStackSize)
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
