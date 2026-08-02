namespace Disharmony;

internal partial class Optimizer
{
    internal class StackToVariableConverter(Optimizer optimizer)
    {
        private readonly Dictionary<Block, List<Type>> entryLocals = optimizer.allBlocks.ToDictionary(block => block, _ => new List<Type>());
        private readonly Dictionary<Block, List<Type>> entryStacks = optimizer.allBlocks.ToDictionary(block => block, _ => new List<Type>());
        private readonly Dictionary<BasicBlock, List<Type>> exitStacks = [];
        private readonly Dictionary<Op, Op.StackTransition> transitions = [];
        private readonly UniqueQueue<Block> worklist = [];

        public void ConvertStackToVariables()
        {
            if (optimizer.Form != IrForm.Stack)
                throw new InvalidOperationException($"Cannot convert {optimizer.Form} form to variables");

            foreach (var block in optimizer.allBlocks)
                worklist.Enqueue(block);

            while (worklist.Count > 0)
            {
                Block block = worklist.Dequeue();
                switch (block)
                {
                    case Region region:
                    {
                        List<Type> stack = region.harmonyBlock is { blockType: ExceptionBlockType.BeginCatchBlock }
                            ? [region.harmonyBlock.catchType]
                            : entryStacks[region];
                        if (region.entry != null)
                            UpdateEntry(region.entry, entryLocals[region], stack);
                        break;
                    }
                    case BasicBlock basicBlock:
                    {
                        List<Type> locals = [.. entryLocals[basicBlock]];
                        List<Type> stack = [.. entryStacks[basicBlock]];

                        SymbolicExecute(basicBlock, locals, stack);

                        exitStacks[basicBlock] = stack;
                        foreach (var edge in basicBlock.outgoingEdges)
                            UpdateEntry(edge.Target, locals, stack);
                        break;
                    }
                    default: throw new ArgumentOutOfRangeException();
                }
            }

            InitializeVariables();

            foreach (var block in optimizer.basicBlocks)
            {
                block.entryStackVariables.Clear();
                for (int index = 0; index < entryStacks[block].Count; index++)
                    block.entryStackVariables.Add(optimizer.NewVariable(VariableKind.EntryStackSlot, entryStacks[block][index], index, block));
            }

            foreach (var block in optimizer.basicBlocks)
                MaterializeBlockVariables(block, exitStacks[block].Count, transitions);

            PopulateEdgeAssignments();
            optimizer.Form = IrForm.Variables;

            return;

            void UpdateEntry(Block successor, List<Type> outgoingLocals, List<Type> outgoingStack)
            {
                List<Type> locals = CombineTypeLists(entryLocals[successor], outgoingLocals, true);
                List<Type> stack = entryStacks[successor].Count == 0 && outgoingStack.Count > 0
                    ? [.. outgoingStack]
                    : CombineTypeLists(entryStacks[successor], outgoingStack);

                if (locals.SequenceEqual(entryLocals[successor]) && stack.SequenceEqual(entryStacks[successor]))
                    return;

                entryLocals[successor] = locals;
                entryStacks[successor] = stack;
                worklist.Enqueue(successor);
            }
        }

        public void SymbolicExecute(BasicBlock basicBlock, List<Type> locals, List<Type> stack)
        {
            foreach (var op in basicBlock.ops)
            {
                var transition = new Op.StackTransition();
                transitions[op] = transition;
                List<Type> inputStack = [.. stack];
                int popCount = op.GetStackPops(optimizer.returnType);
                if (popCount > inputStack.Count)
                    throw new InvalidOperationException($"{op.Opcode} pops {popCount} values from a stack of {inputStack.Count}");

                transition.inputTypes.AddRange(inputStack.Skip(inputStack.Count - popCount));

                switch (unchecked((ushort)op.Opcode.Value))
                {
                    case OpCodeValues.Ldloc_0:
                    case OpCodeValues.Ldloc_1:
                    case OpCodeValues.Ldloc_2:
                    case OpCodeValues.Ldloc_3:
                    case OpCodeValues.Ldloc:
                    case OpCodeValues.Ldloc_S:
                    {
                        int index = op.Index;
                        ExpandLocals(index);
                        transition.variableAccesses.Add(new(VariableKind.Local, index, Op.VariableAccessKind.Read));
                        stack.Add(locals[index]);
                        break;
                    }
                    case OpCodeValues.Stloc_0:
                    case OpCodeValues.Stloc_1:
                    case OpCodeValues.Stloc_2:
                    case OpCodeValues.Stloc_3:
                    case OpCodeValues.Stloc:
                    case OpCodeValues.Stloc_S:
                    {
                        int index = op.Index;
                        while (locals.Count < index + 1)
                            locals.Add(typeof(UnknownType));
                        transition.variableAccesses.Add(new(VariableKind.Local, index, Op.VariableAccessKind.Write));
                        locals[index] = stack[^1];
                        stack.RemoveAt(stack.Count - 1);
                        break;
                    }
                    case OpCodeValues.Ldloca:
                    case OpCodeValues.Ldloca_S:
                    {
                        int index = op.Index;
                        while (locals.Count < index + 1)
                            locals.Add(typeof(UnknownType));
                        transition.variableAccesses.Add(new(VariableKind.Local, index, Op.VariableAccessKind.Address));
                        stack.Add(ToRef(locals[index]));
                        // Can't be bothered to do fancy analysis here
                        if (!locals[index].IsValueType)
                            locals[index] = typeof(object);
                        break;
                    }
                    case OpCodeValues.Ldarg_0:
                    case OpCodeValues.Ldarg_1:
                    case OpCodeValues.Ldarg_2:
                    case OpCodeValues.Ldarg_3:
                    case OpCodeValues.Ldarg:
                    case OpCodeValues.Ldarg_S:
                    {
                        int index = op.Index;
                        transition.variableAccesses.Add(new(VariableKind.Argument, index, Op.VariableAccessKind.Read));
                        stack.Add(((IReadOnlyList<Type>)optimizer.parameterTypes)[index]);
                        break;
                    }
                    case OpCodeValues.Ldarga:
                    case OpCodeValues.Ldarga_S:
                    {
                        int index = op.Index;
                        transition.variableAccesses.Add(new(VariableKind.Argument, index, Op.VariableAccessKind.Address));
                        stack.Add(ToRef(((IReadOnlyList<Type>)optimizer.parameterTypes)[index]));
                        break;
                    }
                    case OpCodeValues.Starg:
                    case OpCodeValues.Starg_S:
                    {
                        int index = op.Index;
                        transition.variableAccesses.Add(new(VariableKind.Argument, index, Op.VariableAccessKind.Write));
                        stack.RemoveAt(stack.Count - 1);
                        break;
                    }
                    case OpCodeValues.Dup:
                    {
                        stack.Add(stack[^1]);
                        break;
                    }
                    case OpCodeValues.Ldobj:
                    {
                        stack[^1] = FromRef(stack[^1]);
                        break;
                    }
                    case OpCodeValues.Ldstr:
                    {
                        stack.Add(typeof(string));
                        break;
                    }
                    case OpCodeValues.Ldfld when op.Operand is FieldInfo field:
                    {
                        stack[^1] = field.FieldType;
                        break;
                    }
                    case OpCodeValues.Ldflda when op.Operand is FieldInfo field:
                    {
                        stack[^1] = ToRef(field.FieldType);
                        break;
                    }
                    case OpCodeValues.Ldsfld when op.Operand is FieldInfo field:
                    {
                        stack.Add(field.FieldType);
                        break;
                    }
                    case OpCodeValues.Ldsflda when op.Operand is FieldInfo field:
                    {
                        stack.Add(ToRef(field.FieldType));
                        break;
                    }
                    case OpCodeValues.NewObj when op.Operand is ConstructorInfo constructor:
                    {
                        var count = constructor.GetParameters().Length;
                        for (int i = 0; i < count; i++)
                            stack.RemoveAt(stack.Count - 1);
                        stack.Add(constructor.DeclaringType);
                        break;
                    }
                    default:
                    {
                        for (int i = 0; i < popCount; i++)
                            stack.RemoveAt(stack.Count - 1);

                        switch (op.Opcode.StackBehaviourPush)
                        {
                            case StackBehaviour.Push0: break;
                            case StackBehaviour.Push1: stack.Add(typeof(AnyType)); break;
                            case StackBehaviour.Push1_push1:
                                stack.Add(typeof(AnyType));
                                stack.Add(typeof(AnyType));
                                break;
                            case StackBehaviour.Pushi: stack.Add(typeof(AnyType)); break;
                            case StackBehaviour.Pushi8: stack.Add(typeof(long)); break;
                            case StackBehaviour.Pushr4: stack.Add(typeof(float)); break;
                            case StackBehaviour.Pushr8: stack.Add(typeof(double)); break;
                            case StackBehaviour.Pushref: stack.Add(typeof(object)); break;
                            case StackBehaviour.Varpush when op.Operand is MethodInfo methodInfo:
                            {
                                if (methodInfo.ReturnType != typeof(void))
                                    stack.Add(methodInfo.ReturnType);
                                break;
                            }
                            default: throw new ArgumentException();
                        }

                        break;
                    }
                }

                transition.clearsStack = op.ClearsStack;
                if (transition.clearsStack)
                {
                    stack.Clear();
                }
                else
                {
                    int pushCount = stack.Count - (inputStack.Count - popCount);
                    if (pushCount < 0)
                        throw new InvalidOperationException($"Invalid stack effect for {op.Opcode}");

                    int inputIndex = op.Opcode == OpCodes.Dup ? 0 : -1;
                    transition.outputs.AddRange(stack
                        .Skip(stack.Count - pushCount)
                        .Select(type => new Op.StackOutput(type, inputIndex)));
                }
            }

            return;

            void ExpandLocals(int index)
            {
                while (locals.Count < index + 1)
                    locals.Add(typeof(UnknownType));
            }
        }

        public void InitializeVariables()
        {
            optimizer.variables.Clear();
            optimizer.argumentVariables.Clear();
            optimizer.localVariables.Clear();
            optimizer.nextVariableId = 0;

            for (int index = 0; index < optimizer.parameterTypes.Count; index++)
                optimizer.argumentVariables.Add(index, optimizer.NewVariable(VariableKind.Argument, optimizer.parameterTypes[index], index));

            MethodBody? methodBody = optimizer.GetMethodBodyOrNull();
            if (methodBody != null)
            {
                foreach (var local in methodBody.LocalVariables)
                {
                    optimizer.localVariables.Add(local.LocalIndex, optimizer.NewVariable(VariableKind.Local, local.LocalType, local.LocalIndex, pinned: local.IsPinned));
                }
            }

            foreach (var op in optimizer.basicBlocks.SelectMany(block => block.ops))
            {
                if (!ReferencesLocal(op) || op.Operand is not LocalBuilder localBuilder)
                    continue;

                if (optimizer.localVariables.TryGetValue(localBuilder.LocalIndex, out var local))
                {
                    if (local.type != localBuilder.LocalType)
                        throw new InvalidOperationException($"Conflicting types for local #{localBuilder.LocalIndex}");
                    local.localBuilder ??= localBuilder;
                    local.pinned |= localBuilder.IsPinned;
                }
                else
                {
                    optimizer.localVariables.Add(localBuilder.LocalIndex, optimizer.NewVariable(VariableKind.Local, localBuilder.LocalType, localBuilder.LocalIndex,
                        localBuilder: localBuilder, pinned: localBuilder.IsPinned));
                }
            }
        }

        public void MaterializeBlockVariables(
            BasicBlock block,
            int expectedExitStackSize,
            IReadOnlyDictionary<Op, Op.StackTransition> transitions)
        {
            List<Variable> stack = [.. block.entryStackVariables];

            foreach (var op in block.ops)
            {
                op.inputs.Clear();
                op.outputs.Clear();
                Op.StackTransition transition = transitions[op];

                int inputCount = transition.inputTypes.Count;
                if (inputCount > stack.Count)
                    throw new InvalidOperationException($"{op.Opcode} reads {inputCount} values from a stack of {stack.Count}");

                int firstInput = stack.Count - inputCount;
                op.inputs.AddRange(stack.Skip(firstInput));
                stack.RemoveRange(firstInput, inputCount);

                foreach (var output in transition.outputs)
                {
                    Variable variable = output.InputIndex >= 0
                        ? op.inputs[output.InputIndex]
                        : optimizer.NewVariable(VariableKind.Temporary, output.Type);
                    op.outputs.Add(variable);
                    stack.Add(variable);
                }

                if (transition.clearsStack)
                    stack.Clear();

                foreach (var access in transition.variableAccesses)
                {
                    Variable variable = access.VariableKind switch
                    {
                        VariableKind.Argument => optimizer.GetArgumentVariable(access.Index),
                        VariableKind.Local => optimizer.GetLocalVariable(access.Index),
                        _ => throw new InvalidOperationException($"Invalid explicit variable kind {access.VariableKind}"),
                    };

                    switch (access.Kind)
                    {
                        case Op.VariableAccessKind.Read: op.inputs.Add(variable); break;
                        case Op.VariableAccessKind.Write: op.outputs.Add(variable); break;
                        case Op.VariableAccessKind.Address:
                            variable.addressTaken = true;
                            op.inputs.Add(variable);
                            break;
                        default: throw new ArgumentOutOfRangeException();
                    }
                }
            }

            block.exitStackVariables.Clear();
            block.exitStackVariables.AddRange(stack);
            if (block.exitStackVariables.Count != expectedExitStackSize)
                throw new InvalidOperationException($"Variable stack disagrees with type stack at the exit of {block.ID}");
        }

        public void PopulateEdgeAssignments()
        {
            foreach (var source in optimizer.basicBlocks)
            {
                foreach (var edge in source.outgoingEdges)
                {
                    BasicBlock target = edge.Target;
                    edge.assignments.Clear();

                    if (source.exitStackVariables.Count != target.entryStackVariables.Count)
                    {
                        throw new InvalidOperationException(
                            $"Stack depth mismatch on edge {source.ID} => {target.ID}: " +
                            $"{source.exitStackVariables.Count} != {target.entryStackVariables.Count}");
                    }

                    for (int index = 0; index < source.exitStackVariables.Count; index++)
                    {
                        edge.assignments.Add(new VariableAssignment(
                            source.exitStackVariables[index], target.entryStackVariables[index]));
                    }
                }
            }
        }
    }
}
