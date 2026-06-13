using System.Reflection;

namespace XylXenos;

public readonly struct ProfileBlock : IDisposable
{
    public const bool GlobalEnabled = true;
    private readonly bool _enabled;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ProfileBlock(string labelExtra = null, bool enabled = GlobalEnabled, [CallerMemberName] string methodName = null)
    {
        _enabled = enabled;
        if (!_enabled)
            return;
        string label = "[XylXenos] " + (methodName ?? "<Unknown>");

        DeepProfiler.Start(labelExtra != null ? $"{label} - {labelExtra}" : label);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Dispose()
    {
        if (!_enabled)
            return;
        DeepProfiler.End();
    }
}

public static class Helpers
{
    private static Func<object, object> memberwiseCloneFn;

    public static T MemberwiseClone<T>(T obj)
    {
        if (memberwiseCloneFn == null)
        {
            var method = typeof(object).GetMethod("MemberwiseClone", BindingFlags.Instance | BindingFlags.NonPublic)!;
            memberwiseCloneFn = (Func<object, object>)method.CreateDelegate(typeof(Func<object, object>));
        }

        return (T)memberwiseCloneFn(obj);
    }
}
