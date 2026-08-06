namespace Disharmony.Optimizer;

internal static class TypeLattice
{
    /// <summary>
    ///    Represents a type that can be any type. This is used in the type lattice to represent the top of the lattice, where any type is compatible with it.
    /// </summary>
    public static class AnyType;

    /// <summary>
    ///     Represents a type that has no known information. This is used in the type lattice to represent the bottom of the lattice.
    /// </summary>
    public static class UnknownType;

    /// <summary>
    ///     Represents a null pointer. The CLI treats this as an IntPtr, but allows it to be compatible with any reference type.
    /// </summary>
    public static class NullPtr;
}
