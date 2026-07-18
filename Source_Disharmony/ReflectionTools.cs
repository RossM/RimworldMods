namespace Disharmony;

public static class ReflectionTools
{
    public static readonly BindingFlags DeclaredOnly = AccessTools.all | BindingFlags.DeclaredOnly;

    public static MemberInfo GetMember(Type? type, string? name, MemberType memberType, Type[]? parameterTypes, Type[]? genericTypes)
    {
        List<MemberInfo> candidates = GetMembers(type, name, memberType, parameterTypes, genericTypes);

        switch (candidates.Count)
        {
            case > 1: throw new AmbiguousMatchException($"Ambiguous match: {name}");
            case 0: throw new InvalidOperationException($"Member not found: {name}");
        }

        var result = candidates.Single();
        return result;
    }

    public static List<MemberInfo> GetMembers(Type? type, string? name, MemberType memberType, Type[]? parameterTypes, Type[]? genericTypes)
    {
        if (name is null)
            throw new ArgumentException("name expected");

        if (memberType is not (MemberType.Any or MemberType.Method))
        {
            if (parameterTypes != null)
                throw new ArgumentException($"parameterTypes is not supported for memberType {memberType}");
            if (genericTypes != null)
                throw new ArgumentException($"genericTypes is not supported for memberType {memberType}");
        }

        // Harmony uses ':' to separate the type name from the method name, so if it's there, use it
        if (name.Split([':'], 2) is [string typeName, string memberName])
        {
            type = AccessTools.TypeByName(typeName);
            if (type == null)
                throw new InvalidOperationException($"Type not found: {typeName}");
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
            throw new InvalidOperationException($"type not found: {name}");

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

            2 when nameParts[1] == "*" =>
                type.GetNestedTypes(DeclaredOnly).Where(t => t.IsClosureType).Append(type)
                    .SelectMany(t => t.GetMethods(DeclaredOnly)).Where(m => m.Name.StartsWith($"<{nameParts[0]}>b__"))
                    .ToList<MemberInfo>(),

            2 => type.GetNestedTypes(DeclaredOnly).Where(t => t.IsClosureType).Append(type)
                .SelectMany(t => t.GetMethods(DeclaredOnly)).Where(m => m.Name.StartsWith($"<{nameParts[0]}>g__{nameParts[1]}|"))
                .ToList<MemberInfo>(),

            _ => throw new NotSupportedException("Nested local functions are not supported"),
        };

        candidates = memberType switch
        {
            MemberType.Any => candidates,
            MemberType.Method => candidates.Where(m => m is MethodInfo).ToList(),
            MemberType.Getter or MemberType.Setter => candidates.Where(m => m is FieldInfo or PropertyInfo).ToList(),
            _ => throw new ArgumentOutOfRangeException(nameof(memberType), memberType, null),
        };

        if (parameterTypes != null || genericTypes != null)
            candidates = FilterMethods(candidates, parameterTypes, genericTypes).ToList<MemberInfo>();

        candidates = candidates.Select(result =>
            result switch
            {
                PropertyInfo property => memberType == MemberType.Setter ? property.SetMethod : property.GetMethod,
                FieldInfo when memberType == MemberType.Setter =>
                    throw new NotSupportedException("Patching field setters is not supported"),
                _ => result,
            }
        ).ToList();
        return candidates;
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

    public static Type[] GetParameterTypes(MemberInfo member)
    {
        return member switch
        {
            FieldInfo { IsStatic: true } => [],
            FieldInfo { IsStatic: false } field => [field.DeclaringType],
            MethodInfo { IsStatic: true } method => [.. method.GetParameters().Select(p => p.ParameterType)],
            MethodInfo { IsStatic: false } method => [method.DeclaringType, .. method.GetParameters().Select(p => p.ParameterType)],
            _ => throw new InvalidOperationException(),
        };
    }
}
