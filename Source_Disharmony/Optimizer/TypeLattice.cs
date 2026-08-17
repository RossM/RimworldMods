namespace Disharmony.Optimizer;

internal static class TypeLattice
{
    /// <summary>
    ///     Represents a type that can be any type. This is used in the type lattice to represent the top of the lattice, where
    ///     any type is compatible with it.
    /// </summary>
    public static class AnyType;

    /// <summary>
    ///     Represents a type that has no known information. This is used in the type lattice to represent the bottom of the
    ///     lattice.
    /// </summary>
    public static class UnknownType;

    /// <summary>
    ///     Represents a null pointer. The CLI treats this as an IntPtr, but allows it to be compatible with any reference
    ///     type.
    /// </summary>
    public static class NullPtr;

    public static readonly Type Any = typeof(AnyType);
    public static readonly Type AnyRef = Any.MakeByRefType();
    public static readonly Type Unknown = typeof(UnknownType);
    public static readonly Type UnknownRef = Unknown.MakeByRefType();
    public static readonly Type Null = typeof(NullPtr);

    public static List<Type> GetBaseTypes(Type type)
    {
        if (type.IsInterface)
            return [typeof(object), type];

        List<Type> types = [];
        for (Type? t = type; t != null; t = t.BaseType)
            types.Add(t);
        types.Reverse();
        return types;
    }

    public static bool IsInteger(Type type)
    {
        var typeCode = Type.GetTypeCode(type);
        return typeCode is >= TypeCode.Boolean and <= TypeCode.UInt64;
    }

    public static Type Merge(Type left, Type right)
    {
        if (left == Any || right == Any)
            return Any;

        if (left == Unknown)
            return right;
        if (right == Unknown)
            return left;

        if ((left == AnyRef && right.IsByRef) || (right == AnyRef && left.IsByRef))
            return AnyRef;

        if (left == Null && (!right.IsValueType || IsInteger(right)))
            return right;
        if (right == Null && (!left.IsValueType || IsInteger(left)))
            return left;

        if (left == UnknownRef && right.IsByRef)
            return right;
        if (right == UnknownRef && left.IsByRef)
            return left;

        List<Type> leftBaseTypes = GetBaseTypes(left);
        List<Type> rightBaseTypes = GetBaseTypes(right);

        for (int i = Math.Min(leftBaseTypes.Count, rightBaseTypes.Count) - 1; i >= 0; i--)
        {
            if (leftBaseTypes[i] == rightBaseTypes[i])
                return leftBaseTypes[i];
        }

        if (left.IsByRef && right.IsByRef)
            return AnyRef;
        return Any;
    }
}
