namespace Disharmony.Optimizer.Passes;

internal class StackToVariableConversion(Optimizer optimizer) : Pass
{
    /// <summary>
    ///     Non-canonical transient state owned only by one ConvertStackToVariables invocation.
    ///     Inputs are ordered from the deepest popped value to the top of the evaluation stack.
    /// </summary>
    internal sealed class StackTransition
    {
        public readonly List<Type> inputTypes = [];
        public readonly List<StackOutput> outputs = [];
        public readonly List<VariableAccess> variableAccesses = [];
        public bool clearsStack;
    }

    /// <summary>
    ///     Transient StackToVariableConverter result. InputIndex identifies an output which aliases
    ///     a popped input, as with both outputs of dup; a negative index denotes a new value.
    /// </summary>
    /// <param name="Type"></param>
    /// <param name="InputIndex"></param>
    internal readonly record struct StackOutput(Type Type, int InputIndex = -1);

    /// <summary>
    ///     Transient StackToVariableConverter result recorded by symbolic execution so variable
    ///     materialization does not reinterpret the opcode or its original storage operand.
    /// </summary>
    /// <param name="Kind"></param>
    /// <param name="VariableKind"></param>
    /// <param name="Index"></param>
    internal readonly record struct VariableAccess(Op.VariableAccessKind Kind, VariableKind VariableKind, int Index);

    private readonly Dictionary<RegionNode, List<Type>> entryLocals = optimizer.regions.Cast<RegionNode>()
        .Concat(optimizer.basicBlocks).ToDictionary(block => block, _ => new List<Type>());

    private readonly Dictionary<RegionNode, List<Type>> entryStacks = optimizer.regions.Cast<RegionNode>()
        .Concat(optimizer.basicBlocks).ToDictionary(block => block, _ => new List<Type>());

    private readonly Dictionary<BasicBlock, List<Type>> exitStacks = [];
    private readonly Dictionary<BasicBlock, List<Variable>> exitStackVariables = [];
    private readonly Dictionary<Op, StackTransition> transitions = [];
    private readonly UniqueQueue<RegionNode> worklist = [];
    private readonly HashSet<RegionNode> initializedEntries = [];

    public override void Run()
    {
        if (optimizer.Form != Optimizer.IrForm.Stack)
            throw new InvalidOperationException($"Cannot convert {optimizer.Form} form to variables");

        InitializeVariables();
        // Region nodes supply entry state, including implicit handler stacks. Seeding them
        // before any basic block makes that state independent of storage order.
        foreach (var region in optimizer.regions)
        {
            initializedEntries.Add(region);
            worklist.Enqueue(region);
        }

        while (worklist.Count > 0)
        {
            RegionNode block = worklist.Dequeue();
            switch (block)
            {
                case Region region:
                {
                    List<Type> stack = region.harmonyBlock?.blockType switch
                    {
                        ExceptionBlockType.BeginCatchBlock => [region.harmonyBlock.catchType],
                        ExceptionBlockType.BeginExceptFilterBlock => [typeof(object)],
                        _ => entryStacks[region],
                    };
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

        BasicBlock? uninitializedBlock = optimizer.basicBlocks
            .FirstOrDefault(block => !initializedEntries.Contains(block));
        if (uninitializedBlock != null)
        {
            throw new InvalidOperationException(
                $"Cannot convert unreachable basic block {uninitializedBlock.ID} to variable form");
        }

        foreach (var block in optimizer.basicBlocks)
        {
            block.entryStackVariables.Clear();
            for (int index = 0; index < entryStacks[block].Count; index++)
                block.entryStackVariables.Add(optimizer.NewVariable(VariableKind.StackSlot, entryStacks[block][index]));
        }

        foreach (var block in optimizer.basicBlocks)
            exitStackVariables[block] = MaterializeBlockVariables(block, exitStacks[block].Count);

        MergeCrossBlockStackSlots();
        optimizer.Form = Optimizer.IrForm.Variables;

        return;

        void UpdateEntry(RegionNode successor, List<Type> outgoingLocals, List<Type> outgoingStack)
        {
            if (initializedEntries.Add(successor))
            {
                entryLocals[successor] = [.. outgoingLocals];
                entryStacks[successor] = [.. outgoingStack];
                worklist.Enqueue(successor);
                return;
            }

            List<Type> locals = Optimizer.CombineTypeLists(entryLocals[successor], outgoingLocals, true);
            List<Type> stack = Optimizer.CombineTypeLists(entryStacks[successor], outgoingStack);

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
            var transition = new StackTransition();
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
                    transition.variableAccesses.Add(new(Op.VariableAccessKind.Read, VariableKind.Local, index));
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
                        locals.Add(typeof(Optimizer.UnknownType));
                    transition.variableAccesses.Add(new(Op.VariableAccessKind.Write, VariableKind.Local, index));
                    locals[index] = stack[^1];
                    stack.RemoveAt(stack.Count - 1);
                    break;
                }
                case OpCodeValues.Ldloca:
                case OpCodeValues.Ldloca_S:
                {
                    int index = op.Index;
                    while (locals.Count < index + 1)
                        locals.Add(typeof(Optimizer.UnknownType));
                    transition.variableAccesses.Add(new(Op.VariableAccessKind.Address, VariableKind.Local, index));
                    Type declaredType = optimizer.localVariables.TryGetValue(index, out Variable? local)
                        ? local.type ?? typeof(Optimizer.AnyType)
                        : typeof(Optimizer.AnyType);
                    stack.Add(Optimizer.ToRef(declaredType));
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
                    transition.variableAccesses.Add(new(Op.VariableAccessKind.Read, VariableKind.Argument, index));
                    stack.Add(((IReadOnlyList<Type>)optimizer.parameterTypes)[index]);
                    break;
                }
                case OpCodeValues.Ldarga:
                case OpCodeValues.Ldarga_S:
                {
                    int index = op.Index;
                    transition.variableAccesses.Add(new(Op.VariableAccessKind.Address, VariableKind.Argument, index));
                    stack.Add(Optimizer.ToRef(((IReadOnlyList<Type>)optimizer.parameterTypes)[index]));
                    break;
                }
                case OpCodeValues.Starg:
                case OpCodeValues.Starg_S:
                {
                    int index = op.Index;
                    transition.variableAccesses.Add(new(Op.VariableAccessKind.Write, VariableKind.Argument, index));
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
                    stack[^1] = Optimizer.FromRef(stack[^1]);
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
                    stack[^1] = Optimizer.ToRef(field.FieldType);
                    break;
                }
                case OpCodeValues.Ldsfld when op.Operand is FieldInfo field:
                {
                    stack.Add(field.FieldType);
                    break;
                }
                case OpCodeValues.Ldsflda when op.Operand is FieldInfo field:
                {
                    stack.Add(Optimizer.ToRef(field.FieldType));
                    break;
                }
                case OpCodeValues.Newobj when op.Operand is ConstructorInfo constructor:
                {
                    var count = constructor.GetParameters().Length;
                    for (int i = 0; i < count; i++)
                        stack.RemoveAt(stack.Count - 1);
                    stack.Add(constructor.DeclaringType);
                    break;
                }
                case OpCodeValues.Ldc_I4:
                case OpCodeValues.Ldc_I4_S:
                case OpCodeValues.Ldc_I4_M1:
                case OpCodeValues.Ldc_I4_0:
                case OpCodeValues.Ldc_I4_1:
                case OpCodeValues.Ldc_I4_2:
                case OpCodeValues.Ldc_I4_3:
                case OpCodeValues.Ldc_I4_4:
                case OpCodeValues.Ldc_I4_5:
                case OpCodeValues.Ldc_I4_6:
                case OpCodeValues.Ldc_I4_7:
                case OpCodeValues.Ldc_I4_8:
                case OpCodeValues.Ceq:
                case OpCodeValues.Cgt:
                case OpCodeValues.Cgt_Un:
                case OpCodeValues.Clt:
                case OpCodeValues.Clt_Un:
                case OpCodeValues.Sizeof:
                case OpCodeValues.Conv_I1:
                case OpCodeValues.Conv_I2:
                case OpCodeValues.Conv_I4:
                case OpCodeValues.Conv_U1:
                case OpCodeValues.Conv_U2:
                case OpCodeValues.Conv_U4:
                case OpCodeValues.Conv_Ovf_I1:
                case OpCodeValues.Conv_Ovf_I1_Un:
                case OpCodeValues.Conv_Ovf_I2:
                case OpCodeValues.Conv_Ovf_I2_Un:
                case OpCodeValues.Conv_Ovf_I4:
                case OpCodeValues.Conv_Ovf_I4_Un:
                case OpCodeValues.Conv_Ovf_U1:
                case OpCodeValues.Conv_Ovf_U1_Un:
                case OpCodeValues.Conv_Ovf_U2:
                case OpCodeValues.Conv_Ovf_U2_Un:
                case OpCodeValues.Conv_Ovf_U4:
                case OpCodeValues.Conv_Ovf_U4_Un:
                case OpCodeValues.Ldind_I1:
                case OpCodeValues.Ldind_I2:
                case OpCodeValues.Ldind_I4:
                case OpCodeValues.Ldind_U1:
                case OpCodeValues.Ldind_U2:
                case OpCodeValues.Ldind_U4:
                case OpCodeValues.Ldelem_I1:
                case OpCodeValues.Ldelem_I2:
                case OpCodeValues.Ldelem_I4:
                case OpCodeValues.Ldelem_U1:
                case OpCodeValues.Ldelem_U2:
                case OpCodeValues.Ldelem_U4:
                {
                    PopInputsAndPush(typeof(int), popCount);
                    break;
                }
                case OpCodeValues.Conv_I:
                case OpCodeValues.Conv_Ovf_I:
                case OpCodeValues.Conv_Ovf_I_Un:
                case OpCodeValues.Ldind_I:
                case OpCodeValues.Ldelem_I:
                case OpCodeValues.Ldftn:
                case OpCodeValues.Ldvirtftn:
                case OpCodeValues.Localloc:
                {
                    PopInputsAndPush(typeof(IntPtr), popCount);
                    break;
                }
                case OpCodeValues.Conv_U:
                case OpCodeValues.Conv_Ovf_U:
                case OpCodeValues.Conv_Ovf_U_Un:
                case OpCodeValues.Ldlen:
                {
                    PopInputsAndPush(typeof(UIntPtr), popCount);
                    break;
                }
                case OpCodeValues.Isinst:
                case OpCodeValues.Castclass:
                {
                    Type type = op.Operand is Type { IsValueType: false } testedType
                        ? testedType
                        : typeof(object);
                    PopInputsAndPush(type, popCount);
                    break;
                }
                case OpCodeValues.Unbox_Any:
                case OpCodeValues.Ldelem:
                {
                    PopInputsAndPush(op.Operand as Type ?? typeof(Optimizer.AnyType), popCount);
                    break;
                }
                case OpCodeValues.Unbox:
                case OpCodeValues.Ldelema:
                case OpCodeValues.Refanyval:
                {
                    PopInputsAndPush(op.Operand is Type type ? Optimizer.ToRef(type) : typeof(Optimizer.AnyType), popCount);
                    break;
                }
                case OpCodeValues.Ldnull:
                {
                    PopInputsAndPush(typeof(Optimizer.NullType), popCount);
                    break;
                }
                case OpCodeValues.Box:
                {
                    PopInputsAndPush(typeof(object), popCount);
                    break;
                }
                case OpCodeValues.Newarr:
                {
                    PopInputsAndPush(op.Operand is Type elementType ? elementType.MakeArrayType() : typeof(Optimizer.AnyType), popCount);
                    break;
                }
                case OpCodeValues.Arglist:
                {
                    PopInputsAndPush(typeof(RuntimeArgumentHandle), popCount);
                    break;
                }
                case OpCodeValues.Mkrefany:
                {
                    PopInputsAndPush(typeof(TypedReference), popCount);
                    break;
                }
                case OpCodeValues.Refanytype:
                {
                    PopInputsAndPush(typeof(IntPtr), popCount);
                    break;
                }
                case OpCodeValues.Ldtoken:
                {
                    Type type = op.Operand switch
                    {
                        Type => typeof(RuntimeTypeHandle),
                        MethodBase => typeof(RuntimeMethodHandle),
                        FieldInfo => typeof(RuntimeFieldHandle),
                        _ => typeof(Optimizer.AnyType),
                    };
                    PopInputsAndPush(type, popCount);
                    break;
                }
                case OpCodeValues.Add:
                case OpCodeValues.Add_Ovf_Un:
                {
                    var left = transition.inputTypes[0];
                    var right = transition.inputTypes[1];
                    if (left == typeof(Optimizer.UnknownType) || right == typeof(Optimizer.UnknownType))
                        PopInputsAndPush(typeof(Optimizer.UnknownType), popCount);
                    else if (left.IsPointerLike && right.IsPointerCompatibleNumeric)
                        PopInputsAndPush(left, popCount);
                    else if (right.IsPointerLike && left.IsPointerCompatibleNumeric)
                        PopInputsAndPush(right, popCount);
                    else
                        PopInputsAndPush(transition.inputTypes[0], popCount);
                    break;
                }
                case OpCodeValues.Sub:
                case OpCodeValues.Sub_Ovf_Un:
                {
                    var left = transition.inputTypes[0];
                    var right = transition.inputTypes[1];
                    if (left == typeof(Optimizer.UnknownType) || right == typeof(Optimizer.UnknownType))
                        PopInputsAndPush(typeof(Optimizer.UnknownType), popCount);
                    else if (left.IsPointerLike && right.IsPointerLike)
                        PopInputsAndPush(typeof(IntPtr), popCount);
                    else if (right.IsPointerCompatibleNumeric)
                        PopInputsAndPush(left, popCount);
                    else
                        PopInputsAndPush(transition.inputTypes[0], popCount);
                    break;
                }
                case OpCodeValues.Add_Ovf:
                case OpCodeValues.Sub_Ovf:
                case OpCodeValues.Mul:
                case OpCodeValues.Mul_Ovf:
                case OpCodeValues.Mul_Ovf_Un:
                case OpCodeValues.Div:
                case OpCodeValues.Div_Un:
                case OpCodeValues.Rem:
                case OpCodeValues.Rem_Un:
                case OpCodeValues.And:
                case OpCodeValues.Or:
                case OpCodeValues.Xor:
                case OpCodeValues.Shl:
                case OpCodeValues.Shr:
                case OpCodeValues.Shr_Un:
                {
                    var left = transition.inputTypes[0];
                    var right = transition.inputTypes[1];
                    if (left == typeof(Optimizer.UnknownType) || right == typeof(Optimizer.UnknownType))
                        PopInputsAndPush(typeof(Optimizer.UnknownType), popCount);
                    else
                        PopInputsAndPush(transition.inputTypes[0], popCount);
                    break;
                }
                case OpCodeValues.Neg:
                case OpCodeValues.Not:
                {
                    PopInputsAndPush(transition.inputTypes[0], popCount);
                    break;
                }
                default:
                {
                    for (int i = 0; i < popCount; i++)
                        stack.RemoveAt(stack.Count - 1);

                    switch (op.Opcode.StackBehaviourPush)
                    {
                        case StackBehaviour.Push0: break;
                        case StackBehaviour.Push1: stack.Add(typeof(Optimizer.AnyType)); break;
                        case StackBehaviour.Push1_push1:
                            stack.Add(typeof(Optimizer.AnyType));
                            stack.Add(typeof(Optimizer.AnyType));
                            break;
                        case StackBehaviour.Pushi: stack.Add(typeof(Optimizer.AnyType)); break;
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
                    .Select(type => new StackOutput(ClrTypeToStackType(type), inputIndex)));
            }
        }

        return;

        void ExpandLocals(int index)
        {
            while (locals.Count < index + 1)
                locals.Add(typeof(Optimizer.UnknownType));
        }

        void PopInputsAndPush(Type type, int inputCount)
        {
            for (int i = 0; i < inputCount; i++)
                stack.RemoveAt(stack.Count - 1);
            stack.Add(type);
        }
    }

    private Type ClrTypeToStackType(Type type)
    {
        if (type == typeof(sbyte) || type == typeof(byte) || type == typeof(bool) ||
            type == typeof(short) || type == typeof(ushort) || type == typeof(char) ||
            type == typeof(int) || type == typeof(uint))
        {
            return typeof(int);
        }

        if (type == typeof(long) || type == typeof(ulong))
            return typeof(long);

        if (type == typeof(float) || type == typeof(double))
            return typeof(double);

        if (type == typeof(IntPtr) || type == typeof(UIntPtr))
            return typeof(IntPtr);

        return type;
    }

    public void InitializeVariables()
    {
        optimizer.variables.Clear();
        optimizer.argumentVariables.Clear();
        optimizer.localVariables.Clear();
        optimizer.nextVariableId = 0;

        for (int index = 0; index < optimizer.parameterTypes.Count; index++)
            optimizer.argumentVariables.Add(index,
                optimizer.NewVariable(VariableKind.Argument, optimizer.parameterTypes[index], index));

        MethodBody? methodBody = optimizer.GetMethodBodyOrNull();
        if (methodBody != null)
        {
            foreach (var local in methodBody.LocalVariables)
            {
                optimizer.localVariables.Add(local.LocalIndex,
                    optimizer.NewVariable(VariableKind.Local, local.LocalType, local.LocalIndex, pinned: local.IsPinned));
            }
        }

        foreach (var op in optimizer.basicBlocks.SelectMany(block => block.ops))
        {
            if (!Optimizer.ReferencesLocal(op) || op.Operand is not LocalBuilder localBuilder)
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
                optimizer.localVariables.Add(localBuilder.LocalIndex, optimizer.NewVariable(VariableKind.Local, localBuilder.LocalType,
                    localBuilder.LocalIndex, localBuilder: localBuilder, pinned: localBuilder.IsPinned));
            }
        }
    }

    public List<Variable> MaterializeBlockVariables(
        BasicBlock block,
        int expectedExitStackSize)
    {
        List<Variable> stack = [.. block.entryStackVariables];

        foreach (var op in block.ops)
        {
            op.inputs.Clear();
            op.outputs.Clear();
            StackTransition transition = transitions[op];

            int inputCount = transition.inputTypes.Count;
            op.stackInputCount = inputCount;
            op.stackOutputCount = transition.outputs.Count;
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

        if (stack.Count != expectedExitStackSize)
            throw new InvalidOperationException($"Variable stack disagrees with type stack at the exit of {block.ID}");
        return stack;
    }

    // Postcondition: each source exit stack is identical to every successor entry stack, using
    // shared mutable StackSlot variables. This is deliberately not SSA: a join's slot may have
    // definitions in several predecessors, and no value transfer is attached to a CFG edge.
    private void MergeCrossBlockStackSlots()
    {
        Dictionary<Variable, HashSet<Variable>> connections = [];

        foreach (var source in optimizer.basicBlocks)
        {
            foreach (var edge in source.outgoingEdges)
            {
                BasicBlock target = edge.Target;
                if (edge.assignments.Count != 0)
                    throw new InvalidOperationException(
                        $"Edge {source.ID} => {target.ID} already has assignments before SSA construction");

                List<Variable> sourceStack = exitStackVariables[source];
                if (sourceStack.Count != target.entryStackVariables.Count)
                {
                    throw new InvalidOperationException(
                        $"Stack depth mismatch on edge {source.ID} => {target.ID}: " +
                        $"{sourceStack.Count} != {target.entryStackVariables.Count}");
                }

                for (int index = 0; index < sourceStack.Count; index++)
                {
                    Variable sourceVariable = sourceStack[index];
                    Variable targetVariable = target.entryStackVariables[index];
                    GetConnections(sourceVariable).Add(targetVariable);
                    GetConnections(targetVariable).Add(sourceVariable);
                }
            }
        }

        Dictionary<Variable, Variable> replacements = [];
        HashSet<Variable> visited = [];
        foreach (var initial in connections.Keys)
        {
            if (!visited.Add(initial))
                continue;

            List<Variable> component = [];
            Stack<Variable> pending = new();
            pending.Push(initial);
            while (pending.Count > 0)
            {
                Variable variable = pending.Pop();
                component.Add(variable);
                foreach (var connected in connections[variable])
                {
                    if (visited.Add(connected))
                        pending.Push(connected);
                }
            }

            Variable representative = component[0];
            foreach (var variable in component.Skip(1))
            {
                if (variable.id < representative.id)
                    representative = variable;
            }

            Type type = component.Select(variable => variable.type ??
                                                     throw new InvalidOperationException(
                                                         $"Cross-block stack variable {variable} has no type"))
                .Aggregate(Optimizer.CombineTypes);
            if (type == typeof(void))
                throw new InvalidOperationException("Incompatible types in a cross-block stack slot");

            representative.kind = VariableKind.StackSlot;
            representative.type = type;
            foreach (var variable in component)
                replacements[variable] = representative;
        }

        foreach (var block in optimizer.basicBlocks)
        {
            ReplaceVariables(block.entryStackVariables);
            foreach (var op in block.ops)
            {
                ReplaceVariables(op.inputs);
                ReplaceVariables(op.outputs);
            }
        }

        optimizer.variables.RemoveAll(variable =>
            replacements.TryGetValue(variable, out Variable? replacement) && replacement != variable);

        return;

        HashSet<Variable> GetConnections(Variable variable)
        {
            if (!connections.TryGetValue(variable, out HashSet<Variable>? values))
            {
                values = [];
                connections.Add(variable, values);
            }

            return values;
        }

        void ReplaceVariables(List<Variable> values)
        {
            for (int index = 0; index < values.Count; index++)
            {
                if (replacements.TryGetValue(values[index], out Variable? replacement))
                    values[index] = replacement;
            }
        }
    }
}
