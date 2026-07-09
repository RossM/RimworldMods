using System.Diagnostics;

namespace Xylib;

[PublicAPI]
public static class DebugAssert
{
    [Conditional("DEBUG")]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void NotNull(
        [System.Diagnostics.CodeAnalysis.NotNull]
        object? value,
        [CallerArgumentExpression(nameof(value))]
        string valueExpression = "value",
        [CallerFilePath] string filePath = "??",
        [CallerLineNumber] int lineNumber = 0)
    {
        if (value != null)
            return;

        Log.ErrorOnce($"[{filePath}:{lineNumber}] {value} is null",
            Gen.HashCombineInt(0x59071F35, valueExpression.GetHashCode(), filePath.GetHashCode(), lineNumber));
    }

    [Conditional("DEBUG")]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void True(
        [DoesNotReturnIf(false)]
        bool value,
        [CallerArgumentExpression(nameof(value))]
        string valueExpression = "value",
        [CallerFilePath] string filePath = "??",
        [CallerLineNumber] int lineNumber = 0)
    {
        if (value)
            return;

        Log.ErrorOnce($"[{filePath}:{lineNumber}] {value} is not true",
            Gen.HashCombineInt(0x2168836A, valueExpression.GetHashCode(), filePath.GetHashCode(), lineNumber));
    }
}
