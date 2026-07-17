namespace Disharmony;

public static class ReflectionTools
{
    private static bool IsClosureType(Type type)
    {
        if (type.IsByRef)
            type = type.GetElementType();

        return Attribute.IsDefined(type, typeof(CompilerGeneratedAttribute));
    }

    public static MemberInfo? GetMember(Type? type, string? memberName, Type[]? parameterTypes, Type[]? genericTypes)
    {
        if (type == null || memberName == null)
            return null;

        string[] nameParts = memberName.Split(':');
        for (int i = 0; i < nameParts.Length - 1; i++)
            type = AccessTools.InnerTypes(type).First(type1 => type1.Name.Contains(nameParts[i]));
        memberName = nameParts[^1];

        if (parameterTypes == null && genericTypes == null)
        {
            if (type.GetField(memberName, AccessTools.all) is { } field)
                return field;
            if (type.GetProperty(memberName, AccessTools.all) is { } property)
                return property.GetMethod;
        }

        return GetMethod(type, memberName, parameterTypes, genericTypes);
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
