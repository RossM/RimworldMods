namespace Disharmony.Tests;

public class InlineRuleBuilderUnitTargets
{
    public int Value;
    public int InstanceValue() => Value;
    public static void Empty() { }
    public static byte Narrow(int value) => (byte)value;
    public static string Coalesce(string value) => value ?? "fallback";
    public static void Forward(int number, long wide, string text) => Sink(number, wide, text);
    public static void Sink(int number, long wide, string text) { }
    public static void Touch(ref int value) { }

    public static int FifthArgument(int first, int second, int third, int fourth, int fifth)
    {
        Touch(ref fifth);
        fifth = first;
        Touch(ref fifth);
        return fifth;
    }

    public static int FiveLocals()
    {
        int a = 1, b = 2, c = 3, d = 4, e = 5;
        Touch(ref a);
        Touch(ref b);
        Touch(ref c);
        Touch(ref d);
        Touch(ref e);
        return a + e;
    }

    public static string Conditional(bool condition)
    {
        if (condition)
            return "yes";
        return "no";
    }

    public static int Loop(int value)
    {
        while (value > 0)
            value--;
        return value;
    }

    public static int Switch(int value)
    {
        switch (value)
        {
            case 0: return 10;
            case 1: return 11;
            case 2: return 12;
            case 3: return 13;
            case 4: return 14;
            default: return 99;
        }
    }

    public static void Catch()
    {
        try { Empty(); }
        catch (InvalidOperationException) { Empty(); }
    }

    public static void Finally()
    {
        try { Empty(); }
        finally { Empty(); }
    }
}

public struct InlineRuleBuilderStructTarget
{
    public int Value;
    public int InstanceValue() => Value;
}

public abstract class InlineRuleBuilderAbstractTarget
{
    public abstract int Method();
}
