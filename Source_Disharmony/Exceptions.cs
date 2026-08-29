namespace Disharmony;

public class ParameterBindingException(string argumentName, string message) : PatchException($"{argumentName}: {message}");

public class PatchDefinitionException(MethodInfo method, string message) : PatchException($"{method.FullName}: {message}");

public class PatchException : Exception
{
    public PatchException(string message) : base(message) { }
    public PatchException(string message, Exception innerException) : base(message, innerException) { }
}

public class RuntimePatchException(string message, Exception innerException) : PatchException(message, innerException);
