namespace Disharmony;

internal abstract class Invocation
{
    public abstract string FullName { get; }
    public abstract Type GetReturnType();
    public abstract CodeInstruction GetCodeInstruction();
    public abstract Type[] GetParameterTypes();
}

internal class FieldInvocation(FieldInfo field) : Invocation
{
    private readonly FieldInfo field = field;

    public override string FullName => @field.FullName;

    public override Type GetReturnType() => field.FieldType;

    public override CodeInstruction GetCodeInstruction() => new(field.IsStatic ? OpCodes.Ldsfld : OpCodes.Ldfld, field);

    public override Type[] GetParameterTypes() => field.IsStatic ? [] : [field.DeclaringType];

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

    protected bool Equals(FieldInvocation other) => field.Equals(other.field);

    public override int GetHashCode() => field.GetHashCode();

    public static bool operator ==(FieldInvocation? left, FieldInvocation? right) => Equals(left, right);

    public static bool operator !=(FieldInvocation? left, FieldInvocation? right) => !Equals(left, right);
}

internal class MethodInvocation(MethodInfo method) : Invocation
{
    private readonly MethodInfo method = method;

    public override string FullName => method.FullName;

    public override Type GetReturnType() => method.ReturnType;

    public override CodeInstruction GetCodeInstruction() => new(method.IsVirtual ? OpCodes.Callvirt : OpCodes.Call, method);

    public override Type[] GetParameterTypes() => method.IsStatic
        ? [.. method.GetParameters().Select(p => p.ParameterType)]
        : [method.DeclaringType, .. method.GetParameters().Select(p => p.ParameterType)];

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

    protected bool Equals(MethodInvocation other) => method.Equals(other.method);

    public override int GetHashCode() => method.GetHashCode();

    public static bool operator ==(MethodInvocation? left, MethodInvocation? right) => Equals(left, right);

    public static bool operator !=(MethodInvocation? left, MethodInvocation? right) => !Equals(left, right);
}
