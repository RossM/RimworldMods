namespace Disharmony;

public class PatchException : Exception
{
    public PatchException(string message) : base(message) { }
    public PatchException(string message, Exception innerException) : base(message, innerException) { }
}

public class ParameterBindingException : PatchException
{
    public ParameterBindingException(string argumentName, string message) : base($"{argumentName}: {message}") { }
    public ParameterBindingException(string argumentName, string message, Exception innerException) : base($"{argumentName}: {message}", innerException) { }
}

public class PatchDefinitionException(MethodInfo method, string message) : PatchException($"{method.FullName}: {message}");

public class RuntimePatchException(string message, Exception innerException) : PatchException(message, innerException);

public class ReflectionException(string message) : PatchException(message);