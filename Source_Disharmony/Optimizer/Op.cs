using System.Diagnostics.CodeAnalysis;
using Disharmony.Optimizer.Passes;

namespace Disharmony.Optimizer;

internal static class Ops
{
    public static Op Nop => new(OpCodes.Nop);
    public static Op Ret => new(OpCodes.Ret);
    public static Op Pop => new(OpCodes.Pop);
    public static Op Dup => new(OpCodes.Dup);
}

internal class Op(OpCode opcode, object? operand, IReadOnlyList<Op> prefixes)
{
    /// <summary>How an instruction accesses storage outside the evaluation stack.</summary>
    internal enum VariableAccessKind
    {
        /// <summary>Loads the current value of an argument or local.</summary>
        Read,

        /// <summary>Replaces the current value of an argument or local.</summary>
        Write,

        /// <summary>Takes the storage location's address, preventing ordinary SSA promotion.</summary>
        Address,
    }

    /// <summary>
    ///     Describes the canonical variable operand of an argument/local access after stack
    ///     conversion. <see cref="Disharmony.Optimizer.VariableKind" /> records what the original opcode
    ///     names; an optimization may independently replace <see cref="Disharmony.Optimizer.Variable" />.
    /// </summary>
    internal readonly record struct StorageAccess(
        VariableAccessKind Kind,
        VariableKind VariableKind,
        Variable Variable);

    public bool IsLeave => Opcode == OpCodes.Leave_S || Opcode == OpCodes.Leave;
    public bool ClearsStack => Opcode == OpCodes.Leave_S || Opcode == OpCodes.Leave;
    public bool IsUnconditionalBranch => Opcode == OpCodes.Br_S || Opcode == OpCodes.Br;
    public bool CanBranch => Opcode.FlowControl is FlowControl.Branch or FlowControl.Cond_Branch;

    public OperationEffects Effects => effectsCached ??= OperationEffectClassifier.Classify(this);
    public bool CanDiscardIfUnused => (Effects & OperationEffectClassifier.PreventsDiscard) == 0;

    public bool CanFallThrough =>
        Opcode.FlowControl is FlowControl.Next or FlowControl.Call or FlowControl.Meta or FlowControl.Cond_Branch or FlowControl.Break;

    public int StackPops =>
        Opcode.StackBehaviourPop switch
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
            StackBehaviour.Varpop => Operand switch
            {
                MethodBase method => method.GetParameters().Length + (method is MethodInfo { IsStatic: false } ? 1 : 0),
                _ => 0,
            },
            StackBehaviour.Popref_popi_pop1 => 3,
            _ => throw new ArgumentOutOfRangeException(),
        };

    public int Index => OpcodeValue switch
    {
        OpCodeValues.Ldarg_0 => 0,
        OpCodeValues.Ldarg_1 => 1,
        OpCodeValues.Ldarg_2 => 2,
        OpCodeValues.Ldarg_3 => 3,
        OpCodeValues.Ldarg or OpCodeValues.Ldarg_S => ToLocalIndex(Operand),
        OpCodeValues.Ldarga or OpCodeValues.Ldarga_S => ToLocalIndex(Operand),
        OpCodeValues.Starg or OpCodeValues.Starg_S => ToLocalIndex(Operand),
        OpCodeValues.Ldloc_0 => 0,
        OpCodeValues.Ldloc_1 => 1,
        OpCodeValues.Ldloc_2 => 2,
        OpCodeValues.Ldloc_3 => 3,
        OpCodeValues.Ldloc or OpCodeValues.Ldloc_S => ToLocalIndex(Operand),
        OpCodeValues.Ldloca or OpCodeValues.Ldloca_S => ToLocalIndex(Operand),
        OpCodeValues.Stloc_0 => 0,
        OpCodeValues.Stloc_1 => 1,
        OpCodeValues.Stloc_2 => 2,
        OpCodeValues.Stloc_3 => 3,
        OpCodeValues.Stloc or OpCodeValues.Stloc_S => ToLocalIndex(Operand),
        _ => throw new ArgumentOutOfRangeException(),
    };

    // Canonical in both forms after MakeBasicBlocks bundles prefixes. Prefix Op objects do not
    // also occur in BasicBlock.ops; keeping them here prevents later passes from separating a
    // prefix from the operation it governs.
    public IReadOnlyList<Op> Prefixes => prefixes;

    public ushort OpcodeValue => unchecked((ushort)Opcode.Value);

    private OperationEffects? effectsCached;

    // Canonical only in Variables form and empty/defaulted in Stack form. Evaluation-stack
    // values occupy inputs[0..stackInputCount) and outputs[0..stackOutputCount); explicit
    // argument/local operands follow them. The counts retain the operation's intrinsic CIL
    // stack arity even if a Variables-form optimization rewrites which values are used.
    public readonly List<Variable> inputs = [];
    public readonly List<Variable> outputs = [];
    public int stackInputCount;
    public int stackOutputCount;
    public Op(OpCode opcode) : this(opcode, null, []) { }

    // Canonical in both forms. After MakeBasicBlocks, branch operands are ControlFlowEdge
    // objects rather than labels. In Variables form a storage opcode's encoded Operand may be
    // stale after rewriting; GetStorageAccess().Variable is the canonical storage target.
    public OpCode Opcode { get; } = opcode;
    public object? Operand { get; } = operand;

    /// <summary>
    ///     Requires Variables form with valid stack counts. Returns the explicit storage operand
    ///     attached by <see cref="StackToVariableConversion" />, or <see langword="null" /> for an
    ///     operation which does not directly access an argument or local. This is the canonical
    ///     storage-opcode decoder for variable-form passes.
    /// </summary>
    internal StorageAccess? GetStorageAccess()
    {
        return OpcodeValue switch
        {
            OpCodeValues.Ldarg_0 or OpCodeValues.Ldarg_1 or OpCodeValues.Ldarg_2 or OpCodeValues.Ldarg_3 or
                OpCodeValues.Ldarg or OpCodeValues.Ldarg_S =>
                new(VariableAccessKind.Read, VariableKind.Argument, inputs[stackInputCount]),
            OpCodeValues.Ldarga or OpCodeValues.Ldarga_S =>
                new(VariableAccessKind.Address, VariableKind.Argument, inputs[stackInputCount]),
            OpCodeValues.Starg or OpCodeValues.Starg_S =>
                new(VariableAccessKind.Write, VariableKind.Argument, outputs[stackOutputCount]),
            OpCodeValues.Ldloc_0 or OpCodeValues.Ldloc_1 or OpCodeValues.Ldloc_2 or OpCodeValues.Ldloc_3 or
                OpCodeValues.Ldloc or OpCodeValues.Ldloc_S =>
                new(VariableAccessKind.Read, VariableKind.Local, inputs[stackInputCount]),
            OpCodeValues.Ldloca or OpCodeValues.Ldloca_S =>
                new(VariableAccessKind.Address, VariableKind.Local, inputs[stackInputCount]),
            OpCodeValues.Stloc_0 or OpCodeValues.Stloc_1 or OpCodeValues.Stloc_2 or OpCodeValues.Stloc_3 or
                OpCodeValues.Stloc or OpCodeValues.Stloc_S =>
                new(VariableAccessKind.Write, VariableKind.Local, outputs[stackOutputCount]),
            _ => null,
        };
    }

    /// <summary>
    ///     Classifies the <c>ldobj</c>/<c>stobj</c> and <c>ldind</c>/<c>stind</c> opcode
    ///     families. Other memory operations are not indirect value accesses for this purpose.
    /// </summary>
    internal VariableAccessKind? GetIndirectAccessKind() =>
        OpcodeValue switch
        {
            OpCodeValues.Ldobj or
                OpCodeValues.Ldind_I1 or OpCodeValues.Ldind_U1 or
                OpCodeValues.Ldind_I2 or OpCodeValues.Ldind_U2 or
                OpCodeValues.Ldind_I4 or OpCodeValues.Ldind_U4 or
                OpCodeValues.Ldind_I8 or OpCodeValues.Ldind_I or
                OpCodeValues.Ldind_R4 or OpCodeValues.Ldind_R8 or OpCodeValues.Ldind_Ref =>
                VariableAccessKind.Read,
            OpCodeValues.Stobj or
                OpCodeValues.Stind_I1 or OpCodeValues.Stind_I2 or OpCodeValues.Stind_I4 or
                OpCodeValues.Stind_I8 or OpCodeValues.Stind_I or
                OpCodeValues.Stind_R4 or OpCodeValues.Stind_R8 or OpCodeValues.Stind_Ref =>
                VariableAccessKind.Write,
            _ => null,
        };

    public int GetStackPops(Type returnType)
    {
        if (Opcode == OpCodes.Ret)
            return returnType == typeof(void) ? 0 : 1;
        if (Opcode == OpCodes.Jmp)
            return 0;
        if (Opcode.StackBehaviourPop != StackBehaviour.Varpop || Operand is not MethodBase calledMethod)
            return StackPops;

        int receiverCount = Opcode != OpCodes.Newobj && !calledMethod.IsStatic ? 1 : 0;
        return calledMethod.GetParameters().Length + receiverCount;
    }

    private static int ToLocalIndex(object? value)
    {
        if (value is LocalBuilder lb)
            return lb.LocalIndex;
        return Convert.ToInt32(value);
    }

    // Copies only opcode/encoded operand. It is suitable for Stack form and prefix logging, but
    // does not lower canonical Variables-form operands back to storage instructions.
    public CodeInstruction ToCodeInstruction() => new(Opcode, Operand);

    public void Deconstruct(out OpCode opcode, out object? operand)
    {
        opcode = Opcode;
        operand = Operand;
    }

    /// <summary>
    ///     Returns the value encoded by a CIL literal-loading opcode, or false when this
    ///     operation is not a supported literal.
    /// </summary>
    public bool TryGetLiteral([NotNullWhen(true)] out ConstantValue? constant)
    {
        constant = OpcodeValue switch
        {
            OpCodeValues.Ldnull => ConstantValue.Null,
            OpCodeValues.Ldstr when Operand is string text => ConstantValue.FromString(text),
            OpCodeValues.Ldc_I4_M1 => ConstantValue.FromInt32(-1),
            OpCodeValues.Ldc_I4_0 => ConstantValue.FromInt32(0),
            OpCodeValues.Ldc_I4_1 => ConstantValue.FromInt32(1),
            OpCodeValues.Ldc_I4_2 => ConstantValue.FromInt32(2),
            OpCodeValues.Ldc_I4_3 => ConstantValue.FromInt32(3),
            OpCodeValues.Ldc_I4_4 => ConstantValue.FromInt32(4),
            OpCodeValues.Ldc_I4_5 => ConstantValue.FromInt32(5),
            OpCodeValues.Ldc_I4_6 => ConstantValue.FromInt32(6),
            OpCodeValues.Ldc_I4_7 => ConstantValue.FromInt32(7),
            OpCodeValues.Ldc_I4_8 => ConstantValue.FromInt32(8),
            OpCodeValues.Ldc_I4_S => ConstantValue.FromInt32(Convert.ToSByte(Operand)),
            OpCodeValues.Ldc_I4 => ConstantValue.FromInt32(Convert.ToInt32(Operand)),
            OpCodeValues.Ldc_I8 => ConstantValue.FromInt64(Convert.ToInt64(Operand)),
            OpCodeValues.Ldc_R4 => ConstantValue.FromFloat32(Convert.ToSingle(Operand)),
            OpCodeValues.Ldc_R8 => ConstantValue.FromFloat64(Convert.ToDouble(Operand)),
            _ => null,
        };
        return constant != null;
    }
}
