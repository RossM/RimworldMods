using JetBrains.Annotations;

namespace Disharmony;

[PublicAPI]
public record PatchConfig
{
    public MethodBase? TargetMethod => (Target as MethodBaseInvocation)?.MethodBase;
    public MethodBase? InnerTargetMethod => (InnerTarget as MethodBaseInvocation)?.MethodBase;

    public PatchType? Type { get; init; } = null;
    internal Invocation Target { get; init; } = EmptyInvocation.Instance;
    internal Invocation InnerTarget { get; init; } = EmptyInvocation.Instance;
    public MethodInfo? PatchMethod { get; init; } = null;
    public PatchOptions Options { get; init; } = PatchOptions.Default;
}

[PublicAPI]
public class PatchHandle
{
    private static int _nextId = 0;

    internal readonly int id;

    internal PatchHandle()
    {
        id = _nextId++;
    }
}

[PublicAPI]
public static class Patch
{
    public static PatchConfig Prefix => new PatchConfig().Prefix;
    public static PatchConfig Postfix => new PatchConfig().Postfix;
    public static PatchConfig Of(MethodBase method) => new PatchConfig().Of(method);
    public static PatchConfig Inner(MethodBase member) => new PatchConfig().Inner(member);
    public static PatchConfig InnerGet(PropertyInfo member) => new PatchConfig().InnerGet(member);
    public static PatchConfig InnerGet(FieldInfo member) => new PatchConfig().InnerGet(member);
    public static PatchConfig InnerSet(PropertyInfo member) => new PatchConfig().InnerSet(member);
    public static PatchConfig InnerSet(FieldInfo member) => new PatchConfig().InnerSet(member);
    public static PatchConfig InnerConstant(int value) => new PatchConfig().InnerConstant(value);
    public static PatchConfig InnerConstant(long value) => new PatchConfig().InnerConstant(value);
    public static PatchConfig InnerConstant(float value) => new PatchConfig().InnerConstant(value);
    public static PatchConfig InnerConstant(double value) => new PatchConfig().InnerConstant(value);
    public static PatchConfig InnerConstant(string value) => new PatchConfig().InnerConstant(value);
    public static PatchConfig With(MethodInfo method) => new PatchConfig().With(method);


    extension(PatchConfig patchConfig)
    {
        public PatchConfig Prefix => patchConfig with { Type = PatchType.Prefix };
        public PatchConfig Postfix => patchConfig with { Type = PatchType.Postfix };

        public PatchConfig Of(MethodBase method) => patchConfig with
        {
            Target = method switch
            {
                MethodInfo methodInfo => new MethodInvocation(methodInfo),
                ConstructorInfo constructorInfo => new OuterConstructorInvocation(constructorInfo),
                _ => throw new ArgumentOutOfRangeException(nameof(method), method, null),
            },
        };
        public PatchConfig Inner(MethodBase method) => patchConfig with
        {
            InnerTarget = method switch
            {
                MethodInfo methodInfo => new MethodInvocation(methodInfo),
                ConstructorInfo constructorInfo => new InnerConstructorInvocation(constructorInfo),
                _ => throw new ArgumentOutOfRangeException(nameof(method), method, null),
            },
        };

        public PatchConfig Inner(ConstructorInfo member) => patchConfig with { InnerTarget = new InnerConstructorInvocation(member) };
        public PatchConfig InnerGet(PropertyInfo member) => patchConfig with { InnerTarget = new MethodInvocation(member.GetMethod) };
        public PatchConfig InnerGet(FieldInfo member) => patchConfig with { InnerTarget = new GetFieldInvocation(member) };
        public PatchConfig InnerSet(PropertyInfo member) => patchConfig with { InnerTarget = new MethodInvocation(member.SetMethod) };
        public PatchConfig InnerSet(FieldInfo member) => patchConfig with { InnerTarget = new SetFieldInvocation(member) };
        public PatchConfig InnerConstant(int value) => patchConfig with { InnerTarget = new ConstantIntInvocation(value) };
        public PatchConfig InnerConstant(long value) => patchConfig with { InnerTarget = new ConstantLongInvocation(value) };
        public PatchConfig InnerConstant(float value) => patchConfig with { InnerTarget = new ConstantFloatInvocation(value) };
        public PatchConfig InnerConstant(double value) => patchConfig with { InnerTarget = new ConstantDoubleInvocation(value) };
        public PatchConfig InnerConstant(string value) => patchConfig with { InnerTarget = new ConstantStringInvocation(value) };
        public PatchConfig With(MethodInfo method) => patchConfig with { PatchMethod = method };
        public PatchConfig Options(PatchOptions options) => patchConfig with { Options = options };
    }
}
