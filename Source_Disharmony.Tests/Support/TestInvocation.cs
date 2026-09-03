namespace Disharmony.Tests.Support;

internal sealed record TestInvocation(
    Type InstanceType,
    Type ReturnType,
    Type[] ParameterTypes,
    string[] ParameterNames,
    bool IsStatic)
    : Invocation
{
    public override string FullName => nameof(TestInvocation);
    public override Type ReturnType { get; } = ReturnType;
    public override Type[] ParameterTypes { get; } = ParameterTypes;
    public override bool IsStatic { get; } = IsStatic;
    public override string[] ParameterNames { get; } = ParameterNames;
    public override Type InstanceType { get; } = InstanceType;
    protected override CodeInstruction GetCodeInstruction() => new(OpCodes.Nop);
}
