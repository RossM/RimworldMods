namespace Disharmony.Optimizer;

/// <summary>
///     One element of the SCCP value lattice. The default value is deliberately
///     <see cref="ValueLatticeKind.Unreached"/>, making zero-initialized analysis maps valid.
/// </summary>
internal readonly struct ValueLatticeElement : IEquatable<ValueLatticeElement>
{
    private readonly ConstantValue? constant;

    private ValueLatticeElement(ValueLatticeKind kind, ConstantValue? constant = null)
    {
        Kind = kind;
        this.constant = constant;
    }

    public ValueLatticeKind Kind { get; }
    public static ValueLatticeElement Unreached => default;
    public static ValueLatticeElement Varying { get; } = new(ValueLatticeKind.Varying);

    public ConstantValue Constant => Kind == ValueLatticeKind.Constant
        ? constant!
        : throw new InvalidOperationException($"A {Kind} lattice value has no constant");

    public static ValueLatticeElement ForConstant(ConstantValue value) =>
        new(ValueLatticeKind.Constant, value ?? throw new ArgumentNullException(nameof(value)));

    /// <summary>Returns the least upper bound of this element and <paramref name="other"/>.</summary>
    public ValueLatticeElement Join(ValueLatticeElement other)
    {
        if (Kind == ValueLatticeKind.Unreached)
            return other;
        if (other.Kind == ValueLatticeKind.Unreached || Equals(other))
            return this;
        return Varying;
    }

    public bool Equals(ValueLatticeElement other) =>
        Kind == other.Kind && Equals(constant, other.constant);

    public override bool Equals(object? obj) => obj is ValueLatticeElement other && Equals(other);
    public override int GetHashCode() => ((int)Kind * 397) ^ (constant?.GetHashCode() ?? 0);
    public override string ToString() => Kind == ValueLatticeKind.Constant ? Constant.ToString() : Kind.ToString();
}

/// <summary>The three states of the sparse conditional constant propagation value lattice.</summary>
internal enum ValueLatticeKind
{
    /// <summary>Bottom: no executable definition has supplied evidence about this value yet.</summary>
    Unreached,

    /// <summary>Exactly one CIL constant is known.</summary>
    Constant,

    /// <summary>Top: the value cannot be represented by a single known constant.</summary>
    Varying,
}

/// <summary>
///     A strongly typed CIL constant. Floating-point constants compare by their encoded bits so
///     signed zero and distinct NaN payloads remain distinguishable to later folding passes.
/// </summary>
internal sealed class ConstantValue : IEquatable<ConstantValue>
{
    private readonly long bits;
    private readonly string? text;
    private readonly Variable? referencedVariable;

    private ConstantValue(
        ConstantValueKind kind,
        long bits = 0,
        string? text = null,
        Variable? referencedVariable = null)
    {
        Kind = kind;
        this.bits = bits;
        this.text = text;
        this.referencedVariable = referencedVariable;
    }

    public ConstantValueKind Kind { get; }

    /// <summary>The CIL evaluation-stack type produced when this value is materialized.</summary>
    public Type StackType => Kind switch
    {
        ConstantValueKind.Null => typeof(TypeLattice.NullType),
        ConstantValueKind.Int32 => typeof(int),
        ConstantValueKind.Int64 => typeof(long),
        ConstantValueKind.NativeInt => typeof(IntPtr),
        ConstantValueKind.Float32 or ConstantValueKind.Float64 => typeof(double),
        ConstantValueKind.String => typeof(string),
        ConstantValueKind.ManagedReference => TypeLattice.ToRef(
            GetReferencedVariable().type ?? typeof(TypeLattice.AnyType)),
        _ => throw new ArgumentOutOfRangeException(),
    };

    public static ConstantValue Null { get; } = new(ConstantValueKind.Null);
    public static ConstantValue FromInt32(int value) => new(ConstantValueKind.Int32, value);
    public static ConstantValue FromInt64(long value) => new(ConstantValueKind.Int64, value);
    public static ConstantValue FromNativeInt(IntPtr value) => new(ConstantValueKind.NativeInt, value.ToInt64());
    public static ConstantValue FromFloat32(float value) =>
        new(ConstantValueKind.Float32, BitConverter.ToInt32(BitConverter.GetBytes(value), 0));
    public static ConstantValue FromFloat64(double value) =>
        new(ConstantValueKind.Float64, BitConverter.DoubleToInt64Bits(value));
    public static ConstantValue FromString(string value) =>
        new(ConstantValueKind.String, text: value ?? throw new ArgumentNullException(nameof(value)));

    public static ConstantValue ReferenceTo(Variable variable)
    {
        if (variable == null)
            throw new ArgumentNullException(nameof(variable));
        if (variable.kind is not (VariableKind.Argument or VariableKind.Local))
        {
            throw new ArgumentException(
                "A known managed reference must identify argument or local storage",
                nameof(variable));
        }

        return new ConstantValue(ConstantValueKind.ManagedReference, referencedVariable: variable);
    }

    public int GetInt32() => Kind == ConstantValueKind.Int32
        ? (int)bits
        : throw WrongKind(ConstantValueKind.Int32);

    public long GetInt64() => Kind == ConstantValueKind.Int64
        ? bits
        : throw WrongKind(ConstantValueKind.Int64);

    public IntPtr GetNativeInt() => Kind == ConstantValueKind.NativeInt
        ? new IntPtr(bits)
        : throw WrongKind(ConstantValueKind.NativeInt);

    public float GetFloat32() => Kind == ConstantValueKind.Float32
        ? BitConverter.ToSingle(BitConverter.GetBytes((int)bits), 0)
        : throw WrongKind(ConstantValueKind.Float32);

    public double GetFloat64() => Kind == ConstantValueKind.Float64
        ? BitConverter.Int64BitsToDouble(bits)
        : throw WrongKind(ConstantValueKind.Float64);

    public string GetString() => Kind == ConstantValueKind.String
        ? text!
        : throw WrongKind(ConstantValueKind.String);

    public Variable GetReferencedVariable() => Kind == ConstantValueKind.ManagedReference
        ? referencedVariable!
        : throw WrongKind(ConstantValueKind.ManagedReference);

    /// <summary>
    ///     Returns a fresh, side-effect-free CIL sequence which pushes this value. The target JIT
    ///     does not eliminate local copies, so callers must always use this sequence rather than
    ///     spilling a constant. Int32 uses its compact canonical opcode where one exists.
    /// </summary>
    public IReadOnlyList<Op> Materialize() => Kind switch
    {
        ConstantValueKind.Null => [new Op(OpCodes.Ldnull)],
        ConstantValueKind.Int32 => [MaterializeInt32(GetInt32())],
        ConstantValueKind.Int64 => [new Op(OpCodes.Ldc_I8, GetInt64(), [])],
        ConstantValueKind.NativeInt => MaterializeNativeInt(GetNativeInt()),
        ConstantValueKind.Float32 => [new Op(OpCodes.Ldc_R4, GetFloat32(), [])],
        ConstantValueKind.Float64 => [new Op(OpCodes.Ldc_R8, GetFloat64(), [])],
        ConstantValueKind.String => [new Op(OpCodes.Ldstr, GetString(), [])],
        ConstantValueKind.ManagedReference => [MaterializeManagedReference(GetReferencedVariable())],
        _ => throw new ArgumentOutOfRangeException(),
    };

    public bool Equals(ConstantValue? other) =>
        other != null && Kind == other.Kind && bits == other.bits &&
        string.Equals(text, other.text, StringComparison.Ordinal) && ReferenceEquals(referencedVariable, other.referencedVariable);

    public override bool Equals(object? obj) => obj is ConstantValue other && Equals(other);

    public override int GetHashCode()
    {
        unchecked
        {
            int hash = ((int)Kind * 397) ^ bits.GetHashCode();
            hash = (hash * 397) ^ (text?.GetHashCode() ?? 0);
            return (hash * 397) ^
                   (referencedVariable == null ? 0 : RuntimeHelpers.GetHashCode(referencedVariable));
        }
    }

    public override string ToString() => Kind switch
    {
        ConstantValueKind.Null => "null",
        ConstantValueKind.Int32 => GetInt32().ToString(System.Globalization.CultureInfo.InvariantCulture),
        ConstantValueKind.Int64 => GetInt64().ToString(System.Globalization.CultureInfo.InvariantCulture),
        ConstantValueKind.NativeInt => $"native int({GetNativeInt()})",
        ConstantValueKind.Float32 => GetFloat32().ToString("R", System.Globalization.CultureInfo.InvariantCulture),
        ConstantValueKind.Float64 => GetFloat64().ToString("R", System.Globalization.CultureInfo.InvariantCulture),
        ConstantValueKind.String => $"\"{GetString()}\"",
        ConstantValueKind.ManagedReference => $"&{GetReferencedVariable()}",
        _ => throw new ArgumentOutOfRangeException(),
    };

    private InvalidOperationException WrongKind(ConstantValueKind expected) =>
        new($"Constant is {Kind}, not {expected}");

    private static Op MaterializeInt32(int value) => value switch
    {
        -1 => new(OpCodes.Ldc_I4_M1),
        0 => new(OpCodes.Ldc_I4_0),
        1 => new(OpCodes.Ldc_I4_1),
        2 => new(OpCodes.Ldc_I4_2),
        3 => new(OpCodes.Ldc_I4_3),
        4 => new(OpCodes.Ldc_I4_4),
        5 => new(OpCodes.Ldc_I4_5),
        6 => new(OpCodes.Ldc_I4_6),
        7 => new(OpCodes.Ldc_I4_7),
        8 => new(OpCodes.Ldc_I4_8),
        >= -128 and <= 127 => new(OpCodes.Ldc_I4_S, value, []),
        _ => new(OpCodes.Ldc_I4, value, []),
    };

    private static IReadOnlyList<Op> MaterializeNativeInt(IntPtr value) => IntPtr.Size switch
    {
        4 => [MaterializeInt32(value.ToInt32()), new Op(OpCodes.Conv_I)],
        8 => [new Op(OpCodes.Ldc_I8, value.ToInt64(), []), new Op(OpCodes.Conv_I)],
        _ => throw new PlatformNotSupportedException($"Unsupported native integer size {IntPtr.Size}"),
    };

    private static Op MaterializeManagedReference(Variable storage)
    {
        object operand = storage.kind switch
        {
            VariableKind.Argument => storage.index,
            VariableKind.Local => (object?)storage.localBuilder ?? storage.index,
            _ => throw new InvalidOperationException($"Known reference targets non-storage variable {storage}"),
        };
        return new(storage.kind == VariableKind.Argument ? OpCodes.Ldarga : OpCodes.Ldloca, operand, []);
    }
}

/// <summary>The representation category of a known abstract value.</summary>
internal enum ConstantValueKind
{
    Null,
    Int32,
    Int64,
    NativeInt,
    Float32,
    Float64,
    String,

    /// <summary>A managed reference to a particular argument or local storage slot.</summary>
    ManagedReference,
}
