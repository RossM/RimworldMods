namespace Xylib;

[PublicAPI]
public readonly struct ProfileBlock : IDisposable
{
    public const bool GlobalEnabled = true;
    private readonly bool enabled;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ProfileBlock(string label, bool enabled = GlobalEnabled, [CallerMemberName] string methodName = "unknown")
    {
        this.enabled = enabled;
        if (!this.enabled)
            return;

        DeepProfiler.Start($"[{methodName}] {label}");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void IDisposable.Dispose()
    {
        if (!enabled)
            return;
        DeepProfiler.End();
    }
}
