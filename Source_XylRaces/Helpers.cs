namespace XylXenos;

public readonly struct ProfileBlock : IDisposable
{
    public const bool GlobalEnabled = true;
    private readonly bool _enabled;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ProfileBlock(bool enabled = GlobalEnabled, [CallerMemberName] string methodName = null)
    {
        _enabled = enabled;
        if (!_enabled)
            return;
        string label = methodName ?? "<Unknown>";

        DeepProfiler.Start(label);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Dispose()
    {
        if (!_enabled)
            return;
        DeepProfiler.End();
    }
}
