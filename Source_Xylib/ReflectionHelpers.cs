using System.Reflection;

namespace Xylib;

public static class ReflectionHelpers
{
    private const BindingFlags MethodBindingFlags
        = BindingFlags.Instance | BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.NonPublic;

    /// <summary>
    ///     Determines whether <paramref name="childType" /> or any intermediate base class overrides a method declared by
    ///     <paramref name="baseType" />.
    /// </summary>
    /// <remarks>
    ///     Use this to tell whether calling the method on <paramref name="childType" /> can run type-specific behavior
    ///     instead of only the default implementation from <paramref name="baseType" />.
    /// </remarks>
    /// <param name="childType">The type whose behavior should be checked.</param>
    /// <param name="baseType">The base type that defines the default method behavior.</param>
    /// <param name="methodName">The name of the base method to check.</param>
    /// <returns>
    ///     <see langword="true" /> if <paramref name="childType" /> has an override for the method; otherwise,
    ///     <see langword="false" />.
    /// </returns>
    /// <exception cref="ArgumentException">
    ///     Thrown when <paramref name="methodName" /> is not a method on <paramref name="baseType" />.
    /// </exception>
    public static bool HasOverridingMethod(Type childType, Type baseType, string methodName)
    {
        var baseMethodInfo = baseType.GetMethod(methodName, MethodBindingFlags);
        if (baseMethodInfo == null)
            throw new ArgumentException("Method not found", nameof(methodName));

        for (Type type = childType; type != null && type != baseType; type = type.BaseType)
        {
            MethodInfo methodInfo = type.GetMethod(methodName, MethodBindingFlags);
            if (methodInfo != null && methodInfo.GetBaseDefinition() == baseMethodInfo)
                return true;
        }

        return false;
    }

    [DebugOutput]
    internal static void GeneCompInfo()
    {
        TableDataGetter<Type>[] columns =
        [
            new("class", type => type.FullName),
            new("BaseType", type => type.BaseType?.FullName),
            new("CompTick", type => HasOverridingMethod(type, typeof(GeneComp), "CompTick")),
            new("CompTickInterval", type => HasOverridingMethod(type, typeof(GeneComp), "CompTickInterval")),
        ];

        DebugTables.MakeTablesDialog(typeof(GeneComp).AllSubclassesNonAbstract(), columns);
    }
}
