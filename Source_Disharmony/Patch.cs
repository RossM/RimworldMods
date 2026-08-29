using System.Collections;

namespace Disharmony;

public record PatchConfig
{
    internal PatchType? Type { get; init; } = null;
    internal Invocation Target { get; init; } = EmptyInvocation.Instance;
    internal Invocation InnerTarget { get; init; } = EmptyInvocation.Instance;
    internal MethodInfo? PatchMethod { get; init; } = null;
    public PatchOptions Options { get; set; } = PatchOptions.Default;
}

public class PatchHandle
{
    private static int _nextId = 0;

    internal readonly int id;

    internal PatchHandle()
    {
        id = _nextId++;
    }
}

public static class Patch
{
    public static PatchConfig Prefix => new PatchConfig().Prefix;
    public static PatchConfig Postfix => new PatchConfig().Postfix;
    public static PatchConfig Of(MethodBase method) => new PatchConfig().Of(method);
    public static PatchConfig Inner(MethodInfo member) => new PatchConfig().Inner(member);
    public static PatchConfig Inner(ConstructorInfo member) => new PatchConfig().Inner(member);
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
        public PatchConfig Inner(MethodInfo member) => patchConfig with { InnerTarget = new MethodInvocation(member) };
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