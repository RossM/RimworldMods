namespace Disharmony;

public class ParameterBindingException(string argumentName, string message) : Exception(message)
{
    public override string Message => $"{argumentName}: {base.Message}";
}

public class PatchException : Exception
{
    public PatchException(string message) : base(message) { }
    public PatchException(string message, Exception innerException) : base(message, innerException) { }
}

public class RuntimePatchException(string message, Exception innerException) : PatchException(message, innerException);
