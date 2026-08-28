namespace Disharmony.Tests;

public sealed class MethodBindingInstanceTargets
{
    public int InstanceValue { get; set; }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public int TargetInstanceMethod() => 10;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int TargetStaticMethod() => 11;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public int CallInnerInstanceMethod(MethodBindingInnerTargets inner) => inner.TargetInstanceMethod();

    [MethodImpl(MethodImplOptions.NoInlining)]
    public int BoundInstanceMethod(int value) => InstanceValue + value;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private int BoundPrivateInstanceMethod(int value) => 100 + value;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int BoundPrivateStaticMethod(int value) => 200 + value;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public int BoundOverloadedMethod(int value) => value;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public string BoundOverloadedMethod(string value) => value;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public int CallLocalFunction()
    {
        int offset = 1;

        [MethodImpl(MethodImplOptions.NoInlining)]
        int LocalFunction() => InstanceValue + offset;

        return LocalFunction();
    }
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

public struct MethodBindingStructTargets
{
    public int InstanceValue { get; set; }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public int TargetInstanceMethod() => 40;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public int BoundInstanceMethod(int value) => InstanceValue + value;
}

public class MethodBindingVirtualBaseTargets
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public int TargetInstanceMethod() => 50;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public virtual int BoundVirtualMethod(int value) => 500 + value;
}

public sealed class MethodBindingVirtualDerivedTargets : MethodBindingVirtualBaseTargets
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public override int BoundVirtualMethod(int value) => 600 + value;
}

public sealed class MethodBindingIteratorTargets
{
    public int InstanceValue { get; set; }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public IEnumerable<int> EnumerateInnerInstanceMethod(MethodBindingInnerTargets inner)
    {
        yield return inner.TargetInstanceMethod();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public int BoundInstanceMethod(int value) => InstanceValue + value;
}

public class BaseMethodOverloadBaseTargets
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public virtual string Describe(int value) => $"base-int:{value}";

    [MethodImpl(MethodImplOptions.NoInlining)]
    public virtual string Describe(string value) => $"base-string:{value}";
}

public sealed class BaseMethodOverloadDerivedTargets : BaseMethodOverloadBaseTargets
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public override string Describe(int value) => $"derived-int:{value}";

    [MethodImpl(MethodImplOptions.NoInlining)]
    public override string Describe(string value) => $"derived-string:{value}";
}
