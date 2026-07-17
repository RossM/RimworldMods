namespace Disharmony;

public static class ReflectionTools
{
    public static readonly BindingFlags DeclaredOnly = AccessTools.all | BindingFlags.DeclaredOnly;

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

        List<MemberInfo> candidates = nameParts.Count switch
        {
            1 => type.GetMembers(DeclaredOnly).Where(m => m.Name == nameParts[0]).ToList(),
            2 => type.GetNestedTypes(DeclaredOnly).Where(t => t.IsClosureType).Append(type)
                .SelectMany(t => t.GetMethods(DeclaredOnly)).Where(m => m.Name.StartsWith($"<{nameParts[0]}>g__{nameParts[1]}|"))
                .ToList<MemberInfo>(),
            _ => throw new NotSupportedException("Nested local functions are not supported"),
        };

        if (parameterTypes != null || genericTypes != null)
            candidates = FilterMethods(candidates, parameterTypes, genericTypes).ToList<MemberInfo>();

        if (candidates.Count > 1)
            throw new ArgumentException("Ambiguous match");

        var result = candidates.SingleOrDefault();
        if (result is PropertyInfo property)
            result = property.GetMethod;
        return result;
    }

    private static IEnumerable<MethodInfo> FilterMethods(IEnumerable<MemberInfo> candidates, Type[]? parameterTypes, Type[]? genericTypes)
    {
        foreach (var candidate in candidates)
        {
            if (candidate is not MethodInfo method)
                continue;

            if (method.IsGenericMethod)
            {
                if (genericTypes is null)
                    continue;
                if (genericTypes.Length != method.GetGenericArguments().Length)
                    continue;

                try
                {
                    method = method.MakeGenericMethod(genericTypes);
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
                ParameterInfo[] parameters = method.GetParameters();
                if (parameters.Length != parameterTypes.Length)
                    continue;
                if (!parameters.Zip(parameterTypes, (p, t) => (p, t)).All(x => ParameterTypeMatcher(x.p, x.t)))
                    continue;
            }

            yield return method;
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
    }
}
