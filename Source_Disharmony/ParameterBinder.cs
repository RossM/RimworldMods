namespace Disharmony;

internal class ParameterBinder(Invocation outer, Invocation inner)
{
    private readonly bool isInfix = inner is not EmptyInvocation;

    public ParameterBinding Bind(ParameterInfo parameter)
    {
        var parameterName = parameter.Name;

        var attributes = parameter.GetCustomAttributes();
        var parameterBindingAttribute = attributes.OfType<ParameterBindingAttribute>().SingleOrDefault();

        var scope = DefaultScope(parameterBindingAttribute?.scope ?? Scope.Any);
        Invocation memberForScope = MemberForScope(scope);

        switch (parameterBindingAttribute)
        {
            case ParameterAttribute { index: int index } parameterAttribute:
            {
                if (parameterAttribute.scope is Scope.Inner && inner == null)
                    throw new ArgumentException("Parameter error: No inner function");

                return new() { Parameter = parameter, BindingType = BindingType.Parameter, Scope = scope, Index = index };
            }

            case ParameterAttribute parameterAttribute:
                return BindParameter(parameter, parameterAttribute.name ?? parameterName, parameterAttribute.scope);

            case InstanceAttribute:
            {
                if (memberForScope.IsStatic)
                    throw new ArgumentException($"[Instance] argument cannot be used with static outer method");
                return new() { Parameter = parameter, BindingType = BindingType.Instance, Scope = scope };
            }

            case ReturnValueAttribute:
            {
                if (memberForScope.ReturnType.IsVoid())
                    throw new ArgumentException($"[ReturnValue] argument cannot be used with method returning void");
                return new() { Parameter = parameter, BindingType = BindingType.Result, Scope = scope };
            }

            case StateAttribute: return new() { Parameter = parameter, BindingType = BindingType.State, Scope = Scope.Outer };

            case FieldAttribute fieldAttribute: return BindField(parameter, fieldAttribute.name ?? parameterName, fieldAttribute.scope);

            case null: break;

            default: throw new NotSupportedException();
        }

        switch (parameterName)
        {
            case null: throw new ArgumentException("Parameter name is null");

            case "__caller":
            {
                if (!isInfix)
                    throw new ArgumentException("__caller can only be used with inner patches");

                if (outer.IsStatic)
                    throw new ArgumentException($"{parameterName} argument cannot be used with static method");
                return new() { Parameter = parameter, BindingType = BindingType.Instance, Scope = Scope.Outer };
            }

            case "__instance":
            {
                if (memberForScope.IsStatic)
                    throw new ArgumentException($"{parameterName} argument cannot be used with static method");
                return new() { Parameter = parameter, BindingType = BindingType.Instance, Scope = scope };
            }

            case "__result":
            {
                if (memberForScope.ReturnType.IsVoid())
                    throw new ArgumentException($"{parameterName} argument cannot be used with method returning void");
                return new() { Parameter = parameter, BindingType = BindingType.Result, Scope = scope };
            }

            case "__state":
            {
                return new() { Parameter = parameter, BindingType = BindingType.State, Scope = Scope.Outer };
            }

            case not null when parameterName.StartsWith("___"):
            {
                return BindField(parameter, parameterName[3..], Scope.Any);
            }

            default:
            {
                return BindParameter(parameter, parameterName, Scope.Any);
            }
        }
    }

    private Invocation MemberForScope(Scope scope)
    {
        var member = scope switch
        {
            Scope.Inner => inner,
            Scope.Outer => outer,
            _ => throw new ArgumentOutOfRangeException(nameof(scope), scope, null),
        };
        return member;
    }

    private Scope DefaultScope(Scope scope)
    {
        return scope switch
        {
            Scope.Any => isInfix ? Scope.Inner : Scope.Outer,
            Scope.Inner => Scope.Inner,
            Scope.Outer => Scope.Outer,
            _ => throw new ArgumentOutOfRangeException(),
        };
    }

    private ParameterBinding BindParameter(ParameterInfo parameter, string parameterName, Scope scope)
    {
        // Look in target parameters
        if (scope is Scope.Inner or Scope.Any)
        {
            int index = Array.FindIndex(inner.GetParameterNames(), p => p == parameterName);
            if (index >= 0)
                return new() { Parameter = parameter, BindingType = BindingType.Parameter, Scope = Scope.Inner, Index = index };
        }

        // Look in caller parameters
        if (scope is Scope.Outer or Scope.Any)
        {
            Type[] parameterTypes = outer.GetParameterTypes();
            int index = Array.FindIndex(outer.GetParameterNames(), p => p == parameterName);
            if (index >= 0)
            {
                // Don't allow writing through a ref parameter to an argument of the outer method. This would
                // be wildly unreliable, as the compiler is free to copy those to locals any time it wants.
                if (isInfix && parameter.ParameterType.IsByRef && !parameterTypes[index].IsByRef)
                    throw new ArgumentException("Outer method parameters can't be accessed by ref");

                return new() { Parameter = parameter, BindingType = BindingType.Parameter, Scope = Scope.Outer, Index = index };
            }
        }

        // Look in closure fields
        if (scope is Scope.Inner or Scope.Any)
        {
            var parameterTypes = inner.GetParameterTypes();
            int closureIndex = Array.FindLastIndex(parameterTypes, p => p.IsClosureType);
            if (closureIndex >= 0)
            {
                var type = parameterTypes[closureIndex];
                if (type.IsByRef)
                    type = type.GetElementType();

                var field = type.GetField(parameterName, AccessTools.all);

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

    private ParameterBinding BindField(ParameterInfo parameter, string fieldName, Scope scope)
    {
        // Look in inner instance fields
        if (scope is Scope.Inner or Scope.Any && !inner.IsStatic)
        {
            var field = inner.InstanceType.GetField(fieldName, AccessTools.all);
            if (field != null)
                return new() { Parameter = parameter, BindingType = BindingType.Instance, Scope = Scope.Inner, Fields = [field] };
        }

        // Look in outer instance fields
        if (scope is Scope.Outer or Scope.Any && !outer.IsStatic)
        {
            var field = outer.InstanceType.GetField(fieldName, AccessTools.all);
            if (field != null)
                return new() { Parameter = parameter, BindingType = BindingType.Instance, Scope = Scope.Outer, Fields = [field] };
        }

        throw new ArgumentException($"Field not found: {fieldName}");
    }
}
