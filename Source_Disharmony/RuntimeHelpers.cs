using System.Runtime.ExceptionServices;

namespace Disharmony;

public static class RuntimeHelpers
{
    public static void RethrowException(Exception exception, ExceptionDispatchInfo dispatchInfo)
    {
        if (exception == dispatchInfo.SourceException)
            dispatchInfo.Throw();
        throw exception;
    }
}
