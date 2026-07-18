namespace Disharmony;

internal class ParameterBinder(MethodInfo outer, MemberInfo? inner)
{
    public readonly MethodInfo outer = outer;
    public readonly MemberInfo? inner = inner;

    public ParameterBinding Bind(ParameterInfo parameter)
    {
        var parameterName = parameter.Name;

        var attributes = parameter.GetCustomAttributes();
        var parameterBindingAttribute = attributes.OfType<ParameterBindingAttribute>().SingleOrDefault();

        switch (parameterBindingAttribute)
        {
            case ParameterAttribute { index: int index } parameterAttribute:
            {
                if (parameterAttribute.scope is Scope.Inner && inner == null)
                    throw new ArgumentException("Parameter error: No inner function");

                return ParameterBinding(parameter, index, DefaultScope(parameterAttribute.scope));
            }

            case ParameterAttribute parameterAttribute:
                return BindParameter(parameter, parameterAttribute.name ?? parameterName, parameterAttribute.scope);

            case InstanceAttribute instanceAttribute:
            {
                var scope = DefaultScope(instanceAttribute.scope);
                if (IsStatic(MemberForScope(scope)))
                    throw new ArgumentException($"[Instance] argument cannot be used with static outer method");
                return new() { Parameter = parameter, BindingType = BindingType.Instance, Scope = scope };
            }

            case ReturnAttribute:
            {
                var scope = DefaultScope(Scope.Any);
                if (MemberForScope(scope) is MethodInfo method && method.ReturnType.IsVoid())
                    throw new ArgumentException($"[ReturnValue] argument cannot be used with method returning void");
                return new() { Parameter = parameter, BindingType = BindingType.Result, Scope = scope };
            }

            case StateAttribute: return new() { Parameter = parameter, BindingType = BindingType.State, Scope = Scope.Outer };

            case null: break;

            default: throw new NotSupportedException();
        }

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
                if (IsStatic(inner))
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
                return BindField(parameter, parameterName[3..]);
            }

            default:
            {
                return BindParameter(parameter, parameterName, Scope.Any);
            }
        }
    }

    private MemberInfo? MemberForScope(Scope scope)
    {
        var member = scope switch
        {
            Scope.Inner => inner,
            Scope.Outer => outer,
            _ => throw new ArgumentOutOfRangeException(nameof(scope), scope, null),
        };
        return member;
    }

    private bool IsStatic(MemberInfo? memberInfo) =>
        memberInfo is MethodInfo { IsStatic: true } or PropertyInfo { GetMethod.IsStatic: true } or FieldInfo { IsStatic: true };

    private Scope DefaultScope(Scope scope)
    {
        return scope switch
        {
            Scope.Any => inner is null ? Scope.Outer : Scope.Inner,
            Scope.Inner => Scope.Inner,
            Scope.Outer => Scope.Outer,
            _ => throw new ArgumentOutOfRangeException(),
        };
    }

    private ParameterBinding BindParameter(ParameterInfo parameter, string parameterName, Scope scope)
    {
        // Look in target parameters
        if (scope is Scope.Inner or Scope.Any && inner is MethodInfo innerMethod)
        {
            int index = Array.FindIndex(innerMethod.GetParameters(), p => p.Name == parameterName);
            if (index >= 0)
            {
                return ParameterBinding(parameter, index, Scope.Inner);
            }
        }

        // Look in caller parameters
        if (scope is Scope.Outer or Scope.Any)
        {
            ParameterInfo[] parameters = outer.GetParameters();
            int index = Array.FindIndex(parameters, p => p.Name == parameterName);
            if (index >= 0)
            {
                // Don't allow writing through a ref parameter to an argument of the outer method. This would
                // be wildly unreliable, as the compiler is free to copy those to locals any time it wants.
                if (inner != null && parameter.ParameterType.IsByRef && !parameters[index].ParameterType.IsByRef)
                    throw new ArgumentException("Outer method parameters can't be accessed by ref");

                return ParameterBinding(parameter, index, Scope.Outer);
            }
        }

        // Look in closure fields
        if (scope is Scope.Inner or Scope.Any && inner is MethodInfo innerMethod2)
        {
            int closureIndex = Array.FindLastIndex(innerMethod2.GetParameters(), p => p.ParameterType.IsClosureType);
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

    private ParameterBinding ParameterBinding(ParameterInfo parameter, int index, Scope scope)
    {
        var target = scope switch
        {
            Scope.Inner => (MethodInfo?)inner,
            Scope.Outer => outer,
            Scope.Any or _ => throw new ArgumentOutOfRangeException(nameof(scope), scope, null),
        };

        if (target is not MethodInfo method)
            throw new ArgumentException("Not a method");

        if (!method.IsStatic)
            index++;
        return new() { Parameter = parameter, BindingType = BindingType.Parameter, Scope = scope, Index = index };
    }

    private ParameterBinding BindField(ParameterInfo parameter, string fieldName)
    {
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
}
