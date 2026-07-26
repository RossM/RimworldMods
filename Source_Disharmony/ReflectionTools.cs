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
        if (name is null && memberType is not MemberType.Constructor)
            throw new ArgumentException("name expected");

        if (parameterTypes != null && memberType is not (MemberType.Any or MemberType.Method or MemberType.Constructor))
            throw new ArgumentException($"parameterTypes is not supported for memberType {memberType}");

        if (genericTypes != null && memberType is not (MemberType.Any or MemberType.Method))
            throw new ArgumentException($"genericTypes is not supported for memberType {memberType}");

        // Harmony uses ':' to separate the type name from the method name, so if it's there, use it
        if (name?.Split([':'], 2) is [string typeName, string memberName])
        {
            type = AccessTools.TypeByName(typeName) ??
                throw new InvalidOperationException($"Type not found: {typeName}");
            name = memberName;
        }

        var nameParts = name?.Split('.').ToList() ?? [];

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

        IEnumerable<MemberInfo> candidates = nameParts.Count switch
        {
            0 => type.GetConstructors(),

            1 => type.GetMembers(DeclaredOnly).Where(m => m.Name == nameParts[0]),

            2 when nameParts[1] == "*" =>
                type.GetNestedTypes(DeclaredOnly).Where(t => t.IsClosureType).Append(type)
                    .SelectMany(t => t.GetMethods(DeclaredOnly)).Where(m => m.Name.StartsWith($"<{nameParts[0]}>b__")),

            2 => type.GetNestedTypes(DeclaredOnly).Where(t => t.IsClosureType).Append(type)
                .SelectMany(t => t.GetMethods(DeclaredOnly)).Where(m => m.Name.StartsWith($"<{nameParts[0]}>g__{nameParts[1]}|")),

            _ => throw new NotSupportedException("Nested local functions are not supported"),
        };

        candidates = memberType switch
        {
            MemberType.Any => candidates.Where(m => m is MethodInfo or FieldInfo or PropertyInfo),
            MemberType.Method => candidates.Where(m => m is MethodInfo),
            MemberType.Getter or MemberType.Setter => candidates.Where(m => m is FieldInfo or PropertyInfo),
            MemberType.Constructor => candidates.Where(m => m is ConstructorInfo),
            _ => throw new ArgumentOutOfRangeException(nameof(memberType), memberType, null),
        };

        if (parameterTypes != null || genericTypes != null)
            candidates = FilterMethods(candidates, parameterTypes, genericTypes);

        candidates = candidates.Select(result =>
            result switch
            {
                PropertyInfo property => memberType == MemberType.Setter ? property.SetMethod : property.GetMethod,
                _ => result,
            }
        ).Where(m => m is not null);

        return [.. candidates];
    }

    private static IEnumerable<MethodBase> FilterMethods(IEnumerable<MemberInfo> candidates, Type[]? parameterTypes, Type[]? genericTypes)
    {
        foreach (var candidate in candidates)
        {
            if (candidate is not MethodBase method)
                continue;

            if (method.IsGenericMethod)
            {
                if (method is not MethodInfo methodInfo)
                    throw new NotSupportedException();

                if (genericTypes is null)
                    continue;
                if (genericTypes.Length != method.GetGenericArguments().Length)
                    continue;

                try
                {
                    method = methodInfo.MakeGenericMethod(genericTypes);
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

    internal static int ILSize(OpCode opCode)
    {
        int size = opCode.Size;
        size += opCode.OperandType switch
        {
            OperandType.InlineNone => 0,
            OperandType.ShortInlineBrTarget => 1,
            OperandType.ShortInlineI => 1,
            OperandType.ShortInlineVar => 1,
            OperandType.InlineVar => 2,
            OperandType.InlineI8 => 8,
            OperandType.InlineR => 8,
            _ => 4,
        };
        return size;
    }
}
