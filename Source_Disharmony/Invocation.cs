namespace Disharmony;

internal abstract class Invocation
{
    public abstract string FullName { get; }
    public abstract Type GetReturnType();
    public abstract CodeInstruction GetCodeInstruction();
    public abstract Type[] ParameterTypes();
}

internal class MemberInvocation(MemberInfo member) : Invocation
{
    public override string FullName => member.FullName;

    public override Type GetReturnType()
    {
        return member switch
        {
            FieldInfo field => field.FieldType,
            MethodInfo method => method.ReturnType,
            _ => throw new NotSupportedException(),
        };
    }

    public override CodeInstruction GetCodeInstruction()
    {
        return new(member switch
        {
            FieldInfo { IsStatic: true } => OpCodes.Ldsfld,
            FieldInfo { IsStatic: false } => OpCodes.Ldfld,
            MethodBase { IsVirtual: true } => OpCodes.Callvirt,
            MethodBase { IsVirtual: false } => OpCodes.Call,
            _ => throw new InvalidOperationException(),
        }, member);
    }

    public override Type[] ParameterTypes() => member switch
    {
        FieldInfo { IsStatic: true } => [],
        FieldInfo { IsStatic: false } field => [field.DeclaringType],
        MethodInfo { IsStatic: true } method => [.. method.GetParameters().Select(p => p.ParameterType)],
        MethodInfo { IsStatic: false } method => [method.DeclaringType, .. method.GetParameters().Select(p => p.ParameterType)],
        _ => throw new InvalidOperationException(),
    };
}
