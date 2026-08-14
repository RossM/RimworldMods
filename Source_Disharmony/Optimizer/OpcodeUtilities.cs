namespace Disharmony.Optimizer;

internal static class OpcodeUtilities
{
    /// <summary>
    ///     Gets the output type of a CIL operation, given its input types.
    /// </summary>
    /// <remarks>
    ///     For operations with the <see cref="OpCodeFlags.Argument" /> or <see cref="OpCodeFlags.Local" /> flags,
    ///     the type of the corresponding argument or local should be the first element of <paramref name="inputTypes"/>,
    ///     with the types of values popped from the stack following it.
    /// </remarks>
    /// <param name="op"></param>
    /// <param name="inputTypes"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public static Type GetOutputType(ILInstruction op, params Type[] inputTypes)
    {
        throw new NotImplementedException();
    }
}
