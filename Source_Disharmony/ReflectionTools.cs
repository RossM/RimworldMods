namespace Disharmony;

public static class ReflectionTools
{
    public static bool IsClosureType(Type type)
    {
        if (type.IsByRef)
            type = type.GetElementType();

        return Attribute.IsDefined(type, typeof(CompilerGeneratedAttribute));
    }

    public static MemberInfo? GetMember(Type? type, string? name, Type[]? parameterTypes, Type[]? genericTypes)
    {
        if (name == null)
            return null;

        // Harmony uses ':' to separate the type name from the method name, so if it's there, use it
        if (name.Split([':'], 2) is [string typeName, string memberName])
        {
            type = AccessTools.TypeByName(typeName);
            if (type == null)
                return null;
            name = memberName;
        }

        var nameParts = name.Split('.').ToList();

        // Search for the type by considering foo, then foo.bar, then foo.bar.baz, etc.
        if (type is null)
        {
            for (int i = 1; i <= nameParts.Count - 1; i++)
            {
                typeName = string.Join(".", nameParts.Take(i));
                type = AccessTools.TypeByName(typeName);
                if (type is not null)
                {
                    nameParts.RemoveRange(0, i);
                    break;
                }
            }
        }

        if (type is null)
            return null;

        // Look for nested types
        while (nameParts.Count > 1)
        {
            var nestedType = type.GetNestedType(nameParts[0], AccessTools.all);
            if (nestedType == null)
                break;
            type = nestedType;
            nameParts.RemoveAt(0);
        }

        // If we still have multiple parts, we need to find a local function, which isn't implemented
        if (nameParts.Count > 1)
            throw new NotImplementedException();

        if (parameterTypes == null && genericTypes == null)
        {
            if (type.GetField(nameParts[0], AccessTools.all) is { } field)
                return field;
            if (type.GetProperty(nameParts[0], AccessTools.all) is { } property)
                return property.GetMethod;
        }

        return GetMethod(type, nameParts[0], parameterTypes, genericTypes);
    }

    private static MethodInfo? GetMethod(Type type, string memberName, Type[]? parameterTypes, Type[]? genericTypes)
    {
        List<MethodInfo> results = [];

        foreach (var method in type.GetMethods(AccessTools.all | BindingFlags.DeclaredOnly))
        {
            var curMethod = method;

            if (curMethod.Name != memberName)
                continue;

            if (curMethod.IsGenericMethod)
            {
                if (genericTypes is null)
                    continue;
                if (genericTypes.Length != curMethod.GetGenericArguments().Length)
                    continue;

                try
                {
                    curMethod = curMethod.MakeGenericMethod(genericTypes);
                }
                catch
                {
                    continue;
                }
            }
            else if (genericTypes is not null)
                continue;

            if (parameterTypes != null)
            {
                ParameterInfo[] parameters = curMethod.GetParameters();
                if (parameters.Length != parameterTypes.Length)
                    continue;
                if (!parameters.Zip(parameterTypes, (p, t) => (p, t)).All(x => ParameterTypeMatcher(x.p, x.t)))
                    continue;
            }

            results.Add(curMethod);
            continue;

            static bool ParameterTypeMatcher(ParameterInfo parameter, Type type)
            {
                if (parameter.IsOut)
                {
                    return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Out<>) &&
                           type.GetGenericArguments()[0] == parameter.ParameterType.GetElementType();
                }

                if (parameter.IsIn)
                {
                    return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(In<>) &&
                           type.GetGenericArguments()[0] == parameter.ParameterType.GetElementType();
                }

                if (parameter.ParameterType.IsByRef)
                {
                    return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Ref<>) &&
                           type.GetGenericArguments()[0] == parameter.ParameterType.GetElementType();
                }

                return parameter.ParameterType == type;
            }
        }

        if (results.Count > 1)
            throw new ArgumentException("Ambiguous match");
        if (results.Count == 1)
            return results[0];
        return null;
    }
}
