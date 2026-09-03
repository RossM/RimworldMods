namespace Disharmony.Tests.Support;

internal sealed record TestInvocation(
    Type ReturnType,
    Type[] ParameterTypes,
    bool IsStatic,
    string[] ParameterNames,
    Type InstanceType)
    : Invocation
{
    public override string FullName => nameof(TestInvocation);
    public override Type ReturnType { get; } = ReturnType;

    public override Type[] ParameterTypes { get; } = ParameterTypes;

    public override bool IsStatic { get; } = IsStatic;

    public override string[] ParameterNames { get; } = ParameterNames;

    public override Type InstanceType { get; } = InstanceType;

    protected override CodeInstruction GetCodeInstruction() => new(OpCodes.Nop);

    public void Deconstruct(
        out Type returnType,
        out Type[] parameterTypes,
        out bool isStatic,
        out string[] parameterNames,
        out Type instanceType)
    {
        returnType = ReturnType;
        parameterTypes = ParameterTypes;
        isStatic = IsStatic;
        parameterNames = ParameterNames;
        instanceType = InstanceType;
    }
}
