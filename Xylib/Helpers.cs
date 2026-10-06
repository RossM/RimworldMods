namespace Xylib;

[PublicAPI]
public readonly struct ProfileBlock : IDisposable
{
    public const bool GlobalEnabled = true;
    private readonly bool _enabled;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ProfileBlock(string label, bool enabled = GlobalEnabled, [CallerMemberName] string methodName = "unknown")
    {
        _enabled = enabled;
        if (!_enabled)
            return;

        DeepProfiler.Start($"[{methodName}] {label}");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void IDisposable.Dispose()
    {
        if (!_enabled)
            return;
        DeepProfiler.End();
    }
}
