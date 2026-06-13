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
