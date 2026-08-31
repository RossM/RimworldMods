using Disharmony.Optimizer;

namespace Disharmony;

internal static class ExceptionFixup
{
    // IntPtr is compatible with most things, so use it if we don't know a type.
    private static readonly Type fallbackType = typeof(IntPtr);

    /// <summary>
    ///     Fix up a method which may try to keep stack variables live across an exception block
    ///     by saving and restoring the stack before and after the block.
    /// </summary>
    /// <remarks>
    ///     This currently can't handle exception blocks with an explicit 'leave' instruction,
    ///     so it works for infix patches but can fail for inlining.
    /// </remarks>
    /// <param name="method"></param>
    /// <param name="instructions"></param>
    /// <param name="generator"></param>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    internal static void Fix(MethodBase method, ref List<CodeInstruction> instructions, ILGenerator generator)
    {
        Dictionary<Label, Type[]> branchStacks = [];
        List<LocalTrackerBuilder[]> exceptionStacks = [];
        List<Type> stack = [];
        List<CodeInstruction> output = [];
        Type returnType = method switch
        {
            MethodInfo methodInfo => methodInfo.ReturnType,
            ConstructorInfo => typeof(void),
            _ => throw new ArgumentOutOfRangeException(nameof(method), method, null)
        };

        ParameterInfo[] parameters = method.GetParameters();
        IList<LocalVariableInfo> locals = method.GetMethodBody()?.LocalVariables ?? [];

        bool newBlock = true;

        foreach (var instruction in instructions)
        {
            if (newBlock)
            {
                stack.Clear();
                foreach (var label in instruction.labels)
                {
                    if (!branchStacks.TryGetValue(label, out var branchStack))
                        continue;

                    stack.AddRange(branchStack);
                    break;
                }
            }

            foreach (var _ in instruction.blocks.Where(b => b.blockType is ExceptionBlockType.BeginExceptionBlock))
            {
                if (stack.Count == 0)
                    exceptionStacks.Add([]);
                else
                {
                    LocalTrackerBuilder[] savedStack = [.. stack.Select(t => new LocalTrackerBuilder(generator.DeclareLocal(t)))];
                    for (int i = savedStack.Length - 1; i >= 0; --i)
                        output.Add(savedStack[i].Store());
                    exceptionStacks.Add(savedStack);
                    stack.Clear();
                }
            }

            int popCount = instruction.PopCount(returnType);
            int pushCount = instruction.PushCount();

            while (stack.Count < popCount)
            {
                stack.Insert(0, fallbackType);
            }

            var poppedTypes = stack.GetRange(stack.Count - popCount, popCount);
            stack.RemoveRange(stack.Count - popCount, popCount);

            switch (pushCount)
            {
                case 0: break;
                case 1:
                {
                    var data = OpCodeData.Get(instruction.opcode);
                    Type[] inputTypes;
                    if ((data.flags & OpCodeFlags.Argument) != 0)
                    {
                        int argumentIndex = instruction.ArgumentIndex();
                        Type parameterType;
                        if (method.HasThis && argumentIndex == 0)
                            parameterType = method.DeclaringType.CallableType;
                        else if (method.HasThis)
                            parameterType = parameters[argumentIndex - 1].ParameterType;
                        else
                            parameterType = parameters[argumentIndex].ParameterType;
                        inputTypes = [parameterType, .. poppedTypes];
                    }
                    else if ((data.flags & OpCodeFlags.Local) != 0)
                    {
                        int localIndex = instruction.LocalIndex();
                        Type localType;
                        if (localIndex < locals.Count)
                            localType = locals[localIndex].LocalType;
                        else if (instruction.operand is LocalBuilder builder)
                            localType = builder.LocalType;
                        else
                            localType = fallbackType;
                        inputTypes = [localType, .. poppedTypes];
                    }
                    else
                        inputTypes = [.. poppedTypes];

                    Type pushType = OpcodeUtilities.GetOutputType(instruction.opcode, instruction.operand, inputTypes);

                    if (pushType.IsByRef)
                        pushType = typeof(IntPtr);
                    else if (!pushType.IsValueType)
                        pushType = typeof(object);

                    stack.Add(pushType);
                    break;
                }
                case 2:
                {
                    stack.Add(poppedTypes[0]);
                    stack.Add(poppedTypes[0]);
                    break;
                }
            }

            switch (instruction.operand)
            {
                case Label label: branchStacks[label] = [.. stack]; break;
                case Label[] labels:
                {
                    foreach (var label in labels)
                        branchStacks[label] = [.. stack];
                    break;
                }
            }

            output.Add(instruction);

            foreach (var _ in instruction.blocks.Where(b => b.blockType is ExceptionBlockType.EndExceptionBlock))
            {
                LocalTrackerBuilder[] savedStack = exceptionStacks[^1];
                exceptionStacks.RemoveAt(exceptionStacks.Count - 1);

                foreach (LocalTrackerBuilder local in savedStack)
                {
                    output.Add(local.Load());
                    stack.Add(local.Type);
                }
            }

            newBlock = instruction.opcode.FlowControl is FlowControl.Branch or FlowControl.Throw or FlowControl.Return;
        }

        instructions = output;
    }
}
