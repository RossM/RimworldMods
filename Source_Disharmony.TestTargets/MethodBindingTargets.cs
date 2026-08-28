namespace Disharmony.Tests;

public sealed class MethodBindingInstanceTargets
{
    public int InstanceValue { get; set; }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public int TargetInstanceMethod() => 10;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public int CallInnerInstanceMethod(MethodBindingInnerTargets inner) => inner.TargetInstanceMethod();

    [MethodImpl(MethodImplOptions.NoInlining)]
    public int BoundInstanceMethod(int value) => InstanceValue + value;
}

public sealed class MethodBindingInnerTargets
{
    public int InstanceValue { get; set; }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public int TargetInstanceMethod() => 20;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public int BoundInstanceMethod(int value) => InstanceValue + value;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int BoundStaticMethod(int value) => 200 + value;
}

public static class MethodBindingStaticTargets
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int TargetStaticMethod() => 30;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int BoundStaticMethod(int value) => 300 + value;
}
