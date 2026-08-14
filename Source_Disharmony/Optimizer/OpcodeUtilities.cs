namespace Disharmony.Optimizer;

internal static class OpcodeUtilities
{
    /// <summary>
    ///     Gets the output type of a CIL operation, given its input types.
    /// </summary>
    /// <remarks>
    ///     For operations with the <see cref="OpCodeFlags.Argument" /> or <see cref="OpCodeFlags.Local" /> flags,
    ///     the type of the corresponding argument or local should be the first element of <paramref name="inputTypes"/>,
    ///     with the types of values popped from the stack following it.
    /// </remarks>
    /// <param name="op"></param>
    /// <param name="inputTypes"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public static Type GetOutputType(ILInstruction op, params Type[] inputTypes)
    {
        OpCodeData data = OpCodeData.Get(op.OpCode);
        if (data.resultType is { } resultType)
            return resultType;

        if (data.flags.HasFlag(OpCodeFlags.Load | OpCodeFlags.Indirect))
        {
            if (inputTypes[0] == TypeLattice.Null)
                return TypeLattice.Unknown;
            if (IsSpecialType(inputTypes[0]))
                return inputTypes[0];
            return inputTypes[0].GetElementType();
        }

        if (data.flags.HasFlag(OpCodeFlags.Load) || data.flags.HasFlag(OpCodeFlags.Shift) || data.flags.HasFlag(OpCodeFlags.PushesInput))
            return inputTypes[0];
        if (data.flags.HasFlag(OpCodeFlags.LoadAddress))
            return inputTypes[0].MakeByRefType();

        // See comments on OpCodeFlags.Arithmetic
        if (data.flags.HasFlag(OpCodeFlags.Arithmetic))
        {
            if (inputTypes.Length == 1)
                return inputTypes[0];

            // Double can only combine with another double, so we know both operands must be double
            if (inputTypes.Contains(typeof(double)))
                return typeof(double);

            if (inputTypes.Contains(TypeLattice.Unknown))
                return TypeLattice.Unknown;
            if (inputTypes.Contains(TypeLattice.Any))
                return TypeLattice.Any;

            if (IsReferenceType(inputTypes[0]) && IsReferenceType(inputTypes[1]))
                return typeof(IntPtr);
            if (IsReferenceType(inputTypes[0]))
                return inputTypes[0];
            if (IsReferenceType(inputTypes[1]))
                return inputTypes[1];

            if (inputTypes.Contains(typeof(IntPtr)))
                return typeof(IntPtr);

            return inputTypes[0];
        }

        if (data.flags.HasFlag(OpCodeFlags.TypeFromOperand) && op.Operand is Type operandType)
            return operandType;
        if (data.flags.HasFlag(OpCodeFlags.TypeFromOperandRef) && op.Operand is Type operandType2)
            return operandType2.MakeByRefType();

        return data.canonical switch
        {
            OpCodeValues.Call or OpCodeValues.Callvirt when op.Operand is MethodInfo method => method.ReturnType,
            OpCodeValues.Ldelem_Ref when inputTypes[0].IsArray => inputTypes[0].GetElementType()!,
            OpCodeValues.Ldelem_Ref when inputTypes[0] == TypeLattice.Unknown || inputTypes[0] == TypeLattice.Null => TypeLattice.Unknown,
            OpCodeValues.Ldelem_Ref when inputTypes[0] == TypeLattice.Any => TypeLattice.Any,
            OpCodeValues.Ldfld or OpCodeValues.Ldsfld when op.Operand is FieldInfo field => field.FieldType,
            OpCodeValues.Ldflda or OpCodeValues.Ldsflda when op.Operand is FieldInfo field => field.FieldType.MakeByRefType(),
            OpCodeValues.Ldtoken when op.Operand is FieldInfo => typeof(RuntimeFieldHandle),
            OpCodeValues.Ldtoken when op.Operand is MethodBase => typeof(RuntimeMethodHandle),
            OpCodeValues.Ldtoken when op.Operand is Type => typeof(RuntimeTypeHandle),
            OpCodeValues.Newarr when op.Operand is Type type => type.MakeArrayType(),
            OpCodeValues.Newobj when op.Operand is ConstructorInfo constructor => constructor.DeclaringType!,
            _ => throw new NotImplementedException(),
        };
    }

    public static bool IsSpecialType(Type type) => type == TypeLattice.Any || type == TypeLattice.Unknown || type == TypeLattice.Null;

    public static bool IsReferenceType(Type type) => type.IsByRef || (!type.IsValueType && !IsSpecialType(type)) || type == TypeLattice.Null;
}
