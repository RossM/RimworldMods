namespace Disharmony;

internal abstract class Invocation
{
    public abstract string FullName { get; }
    public abstract Type GetReturnType();
    public abstract CodeInstruction GetCodeInstruction();
    public abstract Type[] GetParameterTypes();
}

internal class FieldInvocation(FieldInfo member) : Invocation
{
    public override string FullName => member.FullName;

    public override Type GetReturnType() => member.FieldType;

    public override CodeInstruction GetCodeInstruction() => new(member.IsStatic ? OpCodes.Ldsfld : OpCodes.Ldfld, member);

    public override Type[] GetParameterTypes() => member.IsStatic ? [] : [member.DeclaringType];
}

internal class MethodInvocation(MethodInfo member) : Invocation
{
    public override string FullName => member.FullName;

    public override Type GetReturnType() => member.ReturnType;

    public override CodeInstruction GetCodeInstruction() => new(member.IsVirtual ? OpCodes.Callvirt : OpCodes.Call, member);

    public override Type[] GetParameterTypes() => member.IsStatic
        ? [.. member.GetParameters().Select(p => p.ParameterType)]
        : [member.DeclaringType, .. member.GetParameters().Select(p => p.ParameterType)];
}
