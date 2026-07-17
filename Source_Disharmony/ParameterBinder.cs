namespace Disharmony;

internal class ParameterBinder(MethodInfo outer, MemberInfo? inner)
{
    public readonly MethodInfo outer = outer;
    public readonly MemberInfo? inner = inner;

    public ParameterBinding BindParameter(ParameterInfo parameter)
    {
        var parameterName = parameter.Name;

        switch (parameterName)
        {
            case "__caller" when inner is not null:
            case "__instance" when inner is null:
            {
                if (outer.IsStatic)
                    throw new ArgumentException($"{parameterName} argument cannot be used with static outer method");
                return new() { Parameter = parameter, BindingType = BindingType.Instance, Scope = Scope.Outer };
            }

            case "__instance":
            {
                if (inner is MethodInfo { IsStatic: true } or PropertyInfo { GetMethod.IsStatic: true } or FieldInfo { IsStatic: true })
                    throw new ArgumentException($"{parameterName} argument cannot be used with static inner method");
                return new() { Parameter = parameter, BindingType = BindingType.Instance, Scope = Scope.Inner };
            }

            case "__result" when inner is null:
            {
                if (outer.ReturnType.IsVoid())
                    throw new ArgumentException($"{parameterName} argument cannot be used with method returning void");
                return new() { Parameter = parameter, BindingType = BindingType.Result, Scope = Scope.Outer };
            }

            case "__result":
            {
                if (inner is MethodInfo info && info.ReturnType.IsVoid())
                    throw new ArgumentException($"{parameterName} argument cannot be used with method returning void");
                return new() { Parameter = parameter, BindingType = BindingType.Result, Scope = Scope.Inner };
            }

            case "__state":
            {
                return new() { Parameter = parameter, BindingType = BindingType.State, Scope = Scope.Outer };
            }

            case not null when parameterName.StartsWith("___"):
            {
                var fieldName = parameterName[3..];

                // Look in target instance fields
                if (inner is FieldInfo { IsStatic: false } or MethodInfo { IsStatic: false } or PropertyInfo { GetMethod.IsStatic: false })
                {
                    var field = inner.DeclaringType!.GetField(fieldName, AccessTools.all);
                    if (field != null)
                        return new() { Parameter = parameter, BindingType = BindingType.Instance, Scope = Scope.Inner, Fields = [field] };
                }

                // Look in target instance fields
                if (outer is { IsStatic: false })
                {
                    var field = outer.DeclaringType!.GetField(fieldName, AccessTools.all);
                    if (field != null)
                        return new() { Parameter = parameter, BindingType = BindingType.Instance, Scope = Scope.Outer, Fields = [field] };
                }

                throw new ArgumentException($"Field not found: {fieldName}");
            }

            default:
            {
                // Look in target parameters
                if (inner is MethodInfo innerMethod)
                {
                    int index = Array.FindIndex(innerMethod.GetParameters(), p => p.Name == parameterName);
                    if (index >= 0)
                    {
                        if (!innerMethod.IsStatic)
                            index++;
                        return new() { Parameter = parameter, BindingType = BindingType.Parameter, Scope = Scope.Inner, Index = index };
                    }
                }

                // Look in caller parameters
                {
                    ParameterInfo[] parameters = outer.GetParameters();
                    int index = Array.FindIndex(parameters, p => p.Name == parameterName);
                    if (index >= 0)
                    {
                        // Don't allow writing through a ref parameter to an argument of the outer method. This would
                        // be wildly unreliable, as the compiler is free to copy those to locals any time it wants.
                        if (inner != null && parameter.ParameterType.IsByRef && !parameters[index].ParameterType.IsByRef)
                            throw new ArgumentException("Outer method parameters can't be accessed by ref");

                        if (!outer.IsStatic)
                            index++;
                        return new() { Parameter = parameter, BindingType = BindingType.Parameter, Scope = Scope.Outer, Index = index };
                    }
                }

                // Look in closure fields
                if (inner is MethodInfo innerMethod2)
                {
                    int closureIndex = Array.FindLastIndex(innerMethod2.GetParameters(), p => ReflectionTools.IsClosureType(p.ParameterType));
                    if (closureIndex >= 0)
                    {
                        var type = innerMethod2.GetParameters()[closureIndex].ParameterType;
                        if (type.IsByRef)
                            type = type.GetElementType();

                        var field = type.GetField(parameterName, AccessTools.all);

                        if (!innerMethod2.IsStatic)
                            closureIndex++;

                        if (field != null)
                            return new()
                            {
                                Parameter = parameter,
                                BindingType = BindingType.Parameter,
                                Scope = Scope.Inner,
                                Index = closureIndex,
                                Fields = [field],
                            };
                    }
                }

                throw new ArgumentException($"Argument not found: {parameterName}");
            }
        }
    }
}
