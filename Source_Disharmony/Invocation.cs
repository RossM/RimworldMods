using System.Globalization;

namespace Disharmony;

/// <summary>
///     Class representing a possible patch method or patch target.
/// </summary>
/// <remarks>
///     <para>
///         Invocation represents a very generic idea of an IL instruction that takes zero or more inputs and
///         produces a result.
///         Method calls are represented with <see cref="MethodInvocation" />, and field references are represented with
///         <see cref="FieldInvocation" />.
///         For convenience there is also a singleton <see cref="EmptyInvocation" /> which represents something that takes
///         no inputs and returns void, intended for use instead of <see langword="null" /> when there is no actual value.
///     </para>
///     <para>
///         All subclasses of Invocation must implement value semantics, so that two invocations that represent calling the
///         same function or accessing the same field compare equal.
///     </para>
/// </remarks>
internal abstract class Invocation
{
    /// <summary>
    ///     A string for use as the parameter name of the instance argument of a non-static invocation.
    ///     Users should <b>not</b> depend on this value! It is only for use in error messages and so on.
    /// </summary>
    protected const string InstanceParameterName = "<instance>";

    public abstract string FullName { get; }
    public abstract Type ReturnType { get; }
    public abstract Type[] ParameterTypes { get; }
    public abstract bool IsStatic { get; }
    public abstract string[] ParameterNames { get; }
    public abstract Type InstanceType { get; }
    public abstract CodeInstruction GetCodeInstruction();

    public static Invocation Create(MemberInfo member)
    {
        Invocation invocation = member switch
        {
            FieldInfo field => new FieldInvocation(field),
            MethodInfo method => new MethodInvocation(method),
            _ => throw new ArgumentOutOfRangeException(),
        };
        return invocation;
    }

    public static implicit operator Invocation(FieldInfo field) => new FieldInvocation(field);
    public static implicit operator Invocation(MethodInfo method) => new MethodInvocation(method);

    public override string ToString() => $"[{GetType().FullName}({FullName})]";
}

internal class EmptyInvocation : Invocation
{
    public override string FullName => "";

    public override Type ReturnType => typeof(void);

    public override Type[] ParameterTypes => [];

    public override bool IsStatic => true;

    public override string[] ParameterNames => [];

    public override Type InstanceType => typeof(void);

    public static readonly EmptyInvocation Instance = new();

    private EmptyInvocation() { }

    public override CodeInstruction GetCodeInstruction() => throw new NotSupportedException();
}

internal class FieldInvocation(FieldInfo fieldInfo) : Invocation
{
    public override string FullName => fieldInfo.FullName;
    public override Type ReturnType => fieldInfo.FieldType;
    public override Type[] ParameterTypes => field ??= fieldInfo.IsStatic ? [] : [fieldInfo.DeclaringType];
    public override bool IsStatic => fieldInfo.IsStatic;
    public override string[] ParameterNames => field ??= fieldInfo.IsStatic ? [] : [InstanceParameterName];
    public override Type InstanceType => fieldInfo.DeclaringType;
    public FieldInfo FieldInfo => fieldInfo;


    private readonly FieldInfo fieldInfo = fieldInfo;

    public static implicit operator FieldInvocation(FieldInfo field) => new(field);

    public override CodeInstruction GetCodeInstruction() => new(fieldInfo.IsStatic ? OpCodes.Ldsfld : OpCodes.Ldfld, fieldInfo);

    public override bool Equals(object? obj)
    {
        if (obj is null)
            return false;
        if (ReferenceEquals(this, obj))
            return true;
        if (obj.GetType() != GetType())
            return false;
        return Equals((FieldInvocation)obj);
    }

    protected bool Equals(FieldInvocation other) => fieldInfo.Equals(other.fieldInfo);

    public override int GetHashCode() => fieldInfo.GetHashCode();

    public static bool operator ==(FieldInvocation? left, FieldInvocation? right) => Equals(left, right);

    public static bool operator !=(FieldInvocation? left, FieldInvocation? right) => !Equals(left, right);
}

internal class MethodInvocation(MethodInfo methodInfo) : Invocation
{
    public override string FullName => methodInfo.FullName;
    public override Type ReturnType => methodInfo.ReturnType;
    public override bool IsStatic => methodInfo.IsStatic;
    public override Type InstanceType => methodInfo.DeclaringType;
    public MethodInfo MethodInfo => methodInfo;

    public override Type[] ParameterTypes => field ??=
        methodInfo.IsStatic
            ? [.. methodInfo.GetParameters().Select(p => p.ParameterType)]
            : [methodInfo.DeclaringType.CallableType, .. methodInfo.GetParameters().Select(p => p.ParameterType)];

    public override string[] ParameterNames => field ??=
        methodInfo.IsStatic
            ? [.. methodInfo.GetParameters().Select(p => p.Name)]
            : [InstanceParameterName, .. methodInfo.GetParameters().Select(p => p.Name)];

    private readonly MethodInfo methodInfo = methodInfo;

    public static implicit operator MethodInvocation(MethodInfo method) => new(method);

    public override CodeInstruction GetCodeInstruction() => new(methodInfo.IsVirtual ? OpCodes.Callvirt : OpCodes.Call, methodInfo);

    public override bool Equals(object? obj)
    {
        if (obj is null)
            return false;
        if (ReferenceEquals(this, obj))
            return true;
        if (obj.GetType() != GetType())
            return false;
        return Equals((MethodInvocation)obj);
    }

    protected bool Equals(MethodInvocation other) => methodInfo.Equals(other.methodInfo);

    public override int GetHashCode() => methodInfo.GetHashCode();

    public static bool operator ==(MethodInvocation? left, MethodInvocation? right) => Equals(left, right);

    public static bool operator !=(MethodInvocation? left, MethodInvocation? right) => !Equals(left, right);
}

internal abstract class ConstantInvocation : Invocation
{
    public override bool IsStatic => true;
    public override Type InstanceType => typeof(void);

    public override Type[] ParameterTypes => [];

    public override string[] ParameterNames => [];
}

internal class ConstantIntInvocation(int value) : ConstantInvocation
{
    public override string FullName => value.ToString();
    public override Type ReturnType => typeof(int);
    public int Value => value;

    public override CodeInstruction GetCodeInstruction() => value switch
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
        >= -128 and <= 127 => new(OpCodes.Ldc_I4_S, value),
        _ => new(OpCodes.Ldc_I4, value),
    };
}

internal class ConstantLongInvocation(long value) : ConstantInvocation
{
    public override string FullName => value.ToString();
    public override Type ReturnType => typeof(long);
    public long Value => value;

    public override CodeInstruction GetCodeInstruction() => new(OpCodes.Ldc_I8, value);
}

internal class ConstantStringInvocation(string value) : ConstantInvocation
{
    public override string FullName => $"\"{value}\"";
    public override Type ReturnType => typeof(string);
    public string Value => value;

    public override CodeInstruction GetCodeInstruction() => new(OpCodes.Ldstr, value);
}

internal class ConstantFloatInvocation(float value) : ConstantInvocation
{
    public override string FullName => value.ToString(CultureInfo.InvariantCulture);
    public override Type ReturnType => typeof(float);
    public float Value => value;

    public override CodeInstruction GetCodeInstruction() => new(OpCodes.Ldc_R4, value);
}

internal class ConstantDoubleInvocation(double value) : ConstantInvocation
{
    public override string FullName => value.ToString(CultureInfo.InvariantCulture);
    public override Type ReturnType => typeof(double);
    public double Value => value;

    public override CodeInstruction GetCodeInstruction() => new(OpCodes.Ldc_R8, value);
}