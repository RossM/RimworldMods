using System.Runtime.ExceptionServices;

namespace Disharmony;

/// <summary>
///     Provides run-time operations used by generated patch code.
/// </summary>
public static class RuntimeHelpers
{
    /// <summary>
    ///     Rethrows an exception, preserving its captured dispatch information when available.
    /// </summary>
    /// <param name="exception">The exception to throw.</param>
    /// <param name="dispatchInfo">
    ///     Captured dispatch information for <paramref name="exception" />, or <see langword="null" /> to throw it
    ///     directly.
    /// </param>
    /// <exception cref="Exception">Always thrown. The exception is <paramref name="exception" />.</exception>
    public static void RethrowException(Exception exception, ExceptionDispatchInfo? dispatchInfo)
    {
        if (exception == dispatchInfo?.SourceException)
            dispatchInfo.Throw();
        throw exception;
    }
}
