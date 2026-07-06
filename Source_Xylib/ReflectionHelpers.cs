using System.Reflection;

namespace Xylib;

public static class ReflectionHelpers
{
    private const BindingFlags MethodBindingFlags = BindingFlags.Instance | BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.NonPublic;

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
