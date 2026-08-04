using System.Diagnostics.CodeAnalysis;

namespace Disharmony.Optimizer;

internal static class TypeLattice
{
    // Symbolic stack analysis treats types as a lattice joined by CombineTypes.
    // UnknownType and AnyType can also be used as the element type of a managed pointer,
    // preserving the known byref shape even when the referent type is imprecise. NullType is the
    // CLI's transient null verification type and is the bottom of the reference-type sublattice.

    /// <summary>
    ///     The bottom type: no type evidence has reached this value yet. Joining it with another
    ///     type yields that type, so later control-flow information can refine it.
    /// </summary>
    internal struct UnknownType;

    /// <summary>
    ///     The top type: a value exists, but its compatible CIL type is unavailable. Joining it with
    ///     any other type remains <see cref="AnyType" />; missing metadata uses this rather than
    ///     <see cref="UnknownType" /> because additional control-flow evidence cannot restore it.
    /// </summary>
    internal struct AnyType;

    /// <summary>
    ///     A null value produced directly by <c>ldnull</c>. It exists only on the evaluation stack
    ///     and is verifier-assignable to every CLI reference type, including managed pointers.
    /// </summary>
    internal struct NullType;

    internal static bool IsSpecialType(Type type) =>
        type == typeof(AnyType) || type == typeof(UnknownType) || type == typeof(NullType) ||
        type.IsByRef && IsSpecialType(type.GetElementType()!);

    internal static Type FromRef(Type type)
    {
        if (type.IsByRef)
            return type.GetElementType()!;
        if (IsSpecialType(type))
            return type;
        throw new InvalidOperationException();
    }

    private static List<Type> GetBaseTypes(Type type)
    {
        if (type.IsValueType || type.IsByRef)
            return [type];
        if (type.IsInterface)
            return [typeof(object), type];
        List<Type> output = [];
        for (Type? ancestor = type; ancestor != null; ancestor = ancestor.BaseType)
            output.Add(ancestor);
        output.Reverse();
        return output;
    }

    internal static Type CombineTypes(Type left, Type right)
    {
        if (left == typeof(UnknownType) || right == typeof(AnyType) || left == right)
            return right;
        if (right == typeof(UnknownType) || left == typeof(AnyType))
            return left;

        // ECMA-335 III.1.8.1.2.3 makes the transient null type verifier-assignable to every
        // reference type. Pointer types, including managed pointers, are reference types in the
        // CTS even though they are not object types.
        if (left == typeof(NullType))
            return IsReferenceType(right) ? right : typeof(void);
        if (right == typeof(NullType))
            return IsReferenceType(left) ? left : typeof(void);

        // Interfaces and their implementations have a direct least upper bound that is not visible
        // in either type's BaseType chain. Value types are excluded because CIL requires an explicit
        // box instruction before an unboxed value can join an object or interface stack type.
        if (!left.IsValueType && !right.IsValueType && !left.IsByRef && !right.IsByRef)
        {
            if (left.IsAssignableFrom(right))
                return left;
            if (right.IsAssignableFrom(left))
                return right;
        }

        if (left.IsByRef || right.IsByRef)
        {
            if (!left.IsByRef || !right.IsByRef)
                return typeof(void);
            Type elementType = CombineTypes(left.GetElementType()!, right.GetElementType()!);
            return elementType == typeof(void) ? typeof(void) : ToRef(elementType);
        }

        var leftTypes = GetBaseTypes(left);
        var rightTypes = GetBaseTypes(right);
        for (int i = Math.Min(leftTypes.Count, rightTypes.Count) - 1; i >= 0; i--)
        {
            if (leftTypes[i] == typeof(object) && TryGetCommonInterface(left, right, out Type? commonInterface))
                return commonInterface;

            if (leftTypes[i] == rightTypes[i])
                return leftTypes[i];
        }

        // No value is possible
        return typeof(void);
    }

    private static bool IsReferenceType(Type type)
    {
        if (type == typeof(NullType) || type.IsByRef || type.IsPointer)
            return true;
        if (type.IsGenericParameter)
            return false;

        return !type.IsValueType && !IsSpecialType(type);
    }

    private static bool TryGetCommonInterface(Type left, Type right, [NotNullWhen(true)] out Type? value)
    {
        HashSet<Type> interfaces = [.. left.GetInterfaces().Intersect(right.GetInterfaces())];
        List<Type> mostSpecific =
        [
            .. interfaces.Where(i =>
                !interfaces.Any(i2 => i != i2 && i.IsAssignableFrom(i2))),
        ];
        value = mostSpecific.Count == 1 ? mostSpecific[0] : null;
        return mostSpecific.Count == 1;
    }

    internal static List<Type> CombineTypeLists(
        IReadOnlyList<Type> left,
        IReadOnlyList<Type> right,
        bool padIfNeeded = false)
    {
        if (!padIfNeeded && left.Count != right.Count)
            throw new ArgumentException();

        int count = Math.Max(left.Count, right.Count);
        List<Type> output = new(count);
        for (int i = 0; i < count; i++)
        {
            Type leftType = i < left.Count ? left[i] : typeof(UnknownType);
            Type rightType = i < right.Count ? right[i] : typeof(UnknownType);
            output.Add(CombineTypes(leftType, rightType));
        }

        return output;
    }

    // Even when the referent type is imprecise, taking its address establishes that the stack value
    // is a managed pointer. Keeping the lattice marker as the element type retains both facts.
    internal static Type ToRef(Type type) => type.MakeByRefType();
}
