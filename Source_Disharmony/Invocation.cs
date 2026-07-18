namespace Disharmony;

internal abstract class Invocation
{
    public abstract string FullName { get; }
    public abstract Type ReturnType { get; }
    public abstract Type[] ParameterTypes { get; }
    public abstract bool IsStatic { get; }
    public abstract string[] ParameterNames { get; }
    public abstract Type InstanceType { get; }
    public abstract CodeInstruction GetCodeInstruction();

    public static Invocation Create(MemberInfo? member)
    {
        Invocation invocation = member switch
        {
            FieldInfo field => new FieldInvocation(field),
            MethodInfo method => new MethodInvocation(method),
            null => EmptyInvocation.Instance,
            _ => throw new ArgumentOutOfRangeException(),
        };
        return invocation;
    }

    public override string ToString() => $"[{GetType().FullName}({FullName})]";
}

internal class EmptyInvocation : Invocation
{
    public override string FullName => "";

    public override Type ReturnType => typeof(void);

    public override Type[] ParameterTypes => [];

    public override bool IsStatic => true;

    public override string[] ParameterNames => [];

    public override Type InstanceType => throw new NotSupportedException();

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
    public override string[] ParameterNames => field ??= fieldInfo.IsStatic ? [] : ["<instance>"];
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
            : [methodInfo.DeclaringType, .. methodInfo.GetParameters().Select(p => p.ParameterType)];

    public override string[] ParameterNames => field ??=
        methodInfo.IsStatic
            ? [.. methodInfo.GetParameters().Select(p => p.Name)]
            : ["<instance>", .. methodInfo.GetParameters().Select(p => p.Name)];

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
