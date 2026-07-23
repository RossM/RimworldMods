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

    /// <summary>
    ///     If <see cref="IsStatic"/> is false, this returns the type of the instance parameter to this invocation.
    ///     If <see cref="IsStatic"/> is true, its behavior is subclass-specific.
    /// </summary>
    public abstract Type InstanceType { get; }

    protected abstract CodeInstruction GetCodeInstruction();

    public virtual IEnumerable<CodeInstruction> GetCodeInstructions() => [GetCodeInstruction()];

    public static Invocation Create(MemberInfo member)
    {
        return member switch
        {
            FieldInfo field => new FieldInvocation(field),
            MethodInfo method => new MethodInvocation(method),
            ConstructorInfo constructor => new ConstructorInvocation(constructor),
            _ => throw new ArgumentOutOfRangeException(),
        };
    }

    public static implicit operator Invocation(FieldInfo field) => new FieldInvocation(field);
    public static implicit operator Invocation(MethodInfo method) => new MethodInvocation(method);
    public static implicit operator Invocation(ConstructorInfo constructor) => new ConstructorInvocation(constructor);

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

    protected override CodeInstruction GetCodeInstruction() => throw new NotSupportedException();
    public override IEnumerable<CodeInstruction> GetCodeInstructions() => [];
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

    protected override CodeInstruction GetCodeInstruction() => new(fieldInfo.IsStatic ? OpCodes.Ldsfld : OpCodes.Ldfld, fieldInfo);

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

internal abstract class MethodBaseInvocation : Invocation
{
    public abstract MethodBase MethodBase { get; }

    public static implicit operator MethodBaseInvocation(MethodInfo method) => new MethodInvocation(method);
    public static implicit operator MethodBaseInvocation(ConstructorInfo constructor) => new ConstructorInvocation(constructor);
}

internal class MethodInvocation(MethodInfo methodInfo) : MethodBaseInvocation
{
    public override string FullName => methodInfo.FullName;
    public override Type ReturnType => methodInfo.ReturnType;
    public override bool IsStatic => methodInfo.IsStatic;
    public override Type InstanceType => methodInfo.DeclaringType;
    public MethodInfo MethodInfo => methodInfo;
    public override MethodBase MethodBase => methodInfo;

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

    protected override CodeInstruction GetCodeInstruction() => new(methodInfo.IsVirtual ? OpCodes.Callvirt : OpCodes.Call, methodInfo);

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

/// <summary>
///     This class represents a <see cref="OpCodes.Newobj"/> call of a constructor.
/// </summary>
/// <param name="constructorInfo"></param>
internal class ConstructorInvocation(ConstructorInfo constructorInfo) : MethodBaseInvocation
{
    public override string FullName => constructorInfo.FullName;
    public override Type ReturnType => constructorInfo.DeclaringType;
    public override Type[] ParameterTypes => field ??= [.. constructorInfo.GetParameters().Select(p => p.ParameterType)];

    public override bool IsStatic => true;
    public override string[] ParameterNames => field ??= [.. constructorInfo.GetParameters().Select(p => p.Name)];
    public override Type InstanceType => constructorInfo.DeclaringType;
    public ConstructorInfo ConstructorInfo => constructorInfo;

    private readonly ConstructorInfo constructorInfo = constructorInfo;
    public override MethodBase MethodBase => constructorInfo;

    protected override CodeInstruction GetCodeInstruction() => new(OpCodes.Newobj, constructorInfo);

    public override bool Equals(object? obj)
    {
        if (obj is null)
            return false;
        if (ReferenceEquals(this, obj))
            return true;
        if (obj.GetType() != GetType())
            return false;
        return Equals((ConstructorInvocation)obj);
    }

    protected bool Equals(ConstructorInvocation other) => constructorInfo.Equals(other.constructorInfo);

    public override int GetHashCode() => constructorInfo.GetHashCode();

    public static bool operator ==(ConstructorInvocation? left, ConstructorInvocation? right) => Equals(left, right);

    public static bool operator !=(ConstructorInvocation? left, ConstructorInvocation? right) => !Equals(left, right);
}

/// <summary>
///     This class represents a constructor call as seen from inside the constructor itself, where it
///     functions like an ordinary instance method returning void.
/// </summary>
/// <param name="constructorInfo"></param>
internal class PatchableConstructorInvocation(ConstructorInfo constructorInfo) : ConstructorInvocation(constructorInfo)
{
    public override Type ReturnType => typeof(void);
    public override bool IsStatic => ConstructorInfo.IsStatic;

    public override Type[] ParameterTypes => field ??=
        IsStatic
            ? [.. ConstructorInfo.GetParameters().Select(p => p.ParameterType)]
            : [ConstructorInfo.DeclaringType.CallableType, .. ConstructorInfo.GetParameters().Select(p => p.ParameterType)];
    public override string[] ParameterNames => field ??=
        IsStatic
            ? [.. ConstructorInfo.GetParameters().Select(p => p.Name)]
            : [InstanceParameterName, .. ConstructorInfo.GetParameters().Select(p => p.Name)];

    protected override CodeInstruction GetCodeInstruction() => throw new NotSupportedException();
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

    protected override CodeInstruction GetCodeInstruction() => value switch
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

    protected override CodeInstruction GetCodeInstruction() => new(OpCodes.Ldc_I8, value);
}

internal class ConstantStringInvocation(string value) : ConstantInvocation
{
    public override string FullName => $"\"{value}\"";
    public override Type ReturnType => typeof(string);
    public string Value => value;

    protected override CodeInstruction GetCodeInstruction() => new(OpCodes.Ldstr, value);
}

internal class ConstantFloatInvocation(float value) : ConstantInvocation
{
    public override string FullName => value.ToString(CultureInfo.InvariantCulture);
    public override Type ReturnType => typeof(float);
    public float Value => value;

    protected override CodeInstruction GetCodeInstruction() => new(OpCodes.Ldc_R4, value);
}

internal class ConstantDoubleInvocation(double value) : ConstantInvocation
{
    public override string FullName => value.ToString(CultureInfo.InvariantCulture);
    public override Type ReturnType => typeof(double);
    public double Value => value;

    protected override CodeInstruction GetCodeInstruction() => new(OpCodes.Ldc_R8, value);
}