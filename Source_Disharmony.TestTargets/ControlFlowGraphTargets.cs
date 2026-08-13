namespace Disharmony.Tests;

public static class ControlFlowGraphTargets
{
    public static void ReturnVoid() { }

    public static int ReturnInt() => 1;

    public static int Add(int left, int right) => left + right;

    public static void Consume(int left, int right) { }

    public static int MethodWithLocal(int value)
    {
        int local = value;
        Increment(ref local);
        return local;
    }

    private static void Increment(ref int value) => value++;
}

public sealed class ControlFlowGraphInstanceTarget
{
    public int Value;

    public ControlFlowGraphInstanceTarget(int value) => Value = value;

    public int Add(int value) => Value + value;
}
