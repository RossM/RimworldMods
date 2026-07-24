#if ENABLE_DISASSEMBLY
namespace Disharmony;

internal partial class Patcher
{
    // Access is protected by HarmonyInternals.locker, like the other Patcher collections.
    private readonly HashSet<MethodBase> methodsWithAssemblyLogging = [];

    private void SetAssemblyLogging(MethodBase original, bool enabled)
    {
        if (enabled)
            methodsWithAssemblyLogging.Add(original);
        else
            methodsWithAssemblyLogging.Remove(original);
    }

    private void LogAssemblyIfEnabled(MethodBase original, MethodInfo replacement)
    {
        if (methodsWithAssemblyLogging.Contains(original))
            JitAssemblyLogger.TryLog(original, replacement);
    }
}
#endif
