namespace Disharmony;

public class ParameterBindingException(string argumentName, string message) : Exception(message)
{
    public override string Message => $"{argumentName}: {base.Message}";
}

internal class ParameterBinder(Invocation outer, Invocation inner, PatchType patchType)
{
    private readonly bool infix = patchType is PatchType.InnerPrefix or PatchType.InnerPostfix;

    public ParameterBinding Bind(ParameterInfo parameter)
    {
        var parameterName = parameter.Name;

        var attributes = parameter.GetCustomAttributes();
        var parameterBindingAttribute = attributes.OfType<ParameterBindingAttribute>().SingleOrDefault();

        Scope defaultScope = (parameterBindingAttribute?.scope ?? Scope.Any) switch
        {
            Scope.Any => infix ? Scope.Inner : Scope.Outer,
            Scope.Inner => Scope.Inner,
            Scope.Outer => Scope.Outer,
            _ => throw new ArgumentOutOfRangeException(),
        };
        Invocation defaultInvocation = defaultScope switch
        {
            Scope.Inner => inner,
            Scope.Outer => outer,
            _ => throw new ArgumentOutOfRangeException(),
        };

        if (defaultInvocation is EmptyInvocation)
            throw new ParameterBindingException(parameterName, "Invalid scope");

        switch (parameterBindingAttribute)
        {
            case ParameterAttribute { index: int index }:
                return new() { Parameter = parameter, BindingType = BindingType.Parameter, Scope = defaultScope, Index = index };

            case ParameterAttribute { name: var name, scope: var scope }: return BindParameterByName(parameter, name ?? parameterName, scope);

            case InstanceAttribute:
            {
                if (defaultInvocation.IsStatic)
                    throw new ParameterBindingException(parameterName, "Method is static");
                return new() { Parameter = parameter, BindingType = BindingType.Instance, Scope = defaultScope };
            }

            case ReturnValueAttribute:
            {
                if (defaultInvocation.ReturnType.IsVoid())
                    throw new ParameterBindingException(parameterName, "Method returns void");
                return new() { Parameter = parameter, BindingType = BindingType.Result, Scope = defaultScope };
            }

            case StateAttribute: return new() { Parameter = parameter, BindingType = BindingType.State, Scope = Scope.Outer };

            case FieldAttribute { name: var name, scope: var scope }: return BindFieldByName(parameter, name ?? parameterName, scope);

            case null: break;

            default: throw new NotSupportedException();
        }

        switch (parameterName)
        {
            case "__caller":
            {
                if (!infix)
                    throw new ParameterBindingException(parameterName, "Can only be used with inner patches");
                if (outer.IsStatic)
                    throw new ParameterBindingException(parameterName, "Method is static");
                return new() { Parameter = parameter, BindingType = BindingType.Instance, Scope = Scope.Outer };
            }

            case "__instance":
            {
                if (defaultInvocation.IsStatic)
                    throw new ParameterBindingException(parameterName, "Method is static");
                return new() { Parameter = parameter, BindingType = BindingType.Instance, Scope = defaultScope };
            }

            case "__result":
            {
                if (defaultInvocation.ReturnType.IsVoid())
                    throw new ParameterBindingException(parameterName, "Method returns void");
                return new() { Parameter = parameter, BindingType = BindingType.Result, Scope = defaultScope };
            }

            case "__state":
            {
                return new() { Parameter = parameter, BindingType = BindingType.State, Scope = Scope.Outer };
            }

            case var _ when parameterName.StartsWith("___"):
            {
                return BindFieldByName(parameter, parameterName[3..], Scope.Any);
            }

            default:
            {
                return BindParameterByName(parameter, parameterName, Scope.Any);
            }
        }
    }

    private ParameterBinding BindParameterByName(ParameterInfo parameter, string name, Scope scope)
    {
        // Look in target parameters
        if (scope is Scope.Inner or Scope.Any)
        {
            int index = Array.FindIndex(inner.GetParameterNames(), p => p == name);
            if (index >= 0)
                return new() { Parameter = parameter, BindingType = BindingType.Parameter, Scope = Scope.Inner, Index = index };
        }

        // Look in caller parameters
        if (scope is Scope.Outer or Scope.Any)
        {
            Type[] parameterTypes = outer.GetParameterTypes();
            int index = Array.FindIndex(outer.GetParameterNames(), p => p == name);
            if (index >= 0)
            {
                // Don't allow writing through a ref parameter to an argument of the outer method. This would
                // be wildly unreliable, as the compiler is free to copy those to locals any time it wants.
                if (infix && parameter.ParameterType.IsByRef && !parameterTypes[index].IsByRef)
                    throw new ParameterBindingException(name, "Outer method parameter can't be accessed by ref");

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

                var field = type.GetField(name, AccessTools.all);

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

        throw new ParameterBindingException(parameter.Name, "Parameter not found");
    }

    private ParameterBinding BindFieldByName(ParameterInfo parameter, string name, Scope scope)
    {
        // Look in inner instance fields
        if (scope is Scope.Inner or Scope.Any && !inner.IsStatic)
        {
            var field = inner.InstanceType.GetField(name, AccessTools.all);
            if (field != null)
                return new() { Parameter = parameter, BindingType = BindingType.Instance, Scope = Scope.Inner, Fields = [field] };
        }

        // Look in outer instance fields
        if (scope is Scope.Outer or Scope.Any && !outer.IsStatic)
        {
            var field = outer.InstanceType.GetField(name, AccessTools.all);
            if (field != null)
                return new() { Parameter = parameter, BindingType = BindingType.Instance, Scope = Scope.Outer, Fields = [field] };
        }

        throw new ParameterBindingException(parameter.Name, "Field not found");
    }
}
