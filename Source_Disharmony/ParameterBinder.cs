using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Disharmony;

public class ParameterBindingException(string argumentName, string message) : Exception(message)
{
    public override string Message => $"{argumentName}: {base.Message}";
}

internal class ParameterBinder(Invocation target, Invocation outer, Invocation inner, PatchType patchType, string className)
{
    private readonly bool infix = patchType is PatchType.InnerPrefix or PatchType.InnerPostfix;
    private readonly bool isIterator = outer != target;

    public ParameterBinding Bind(ParameterInfo parameter)
    {
        var parameterName = parameter.Name;

        var attributes = parameter.GetCustomAttributes();
        var parameterBindingAttribute = attributes.OfType<ParameterBindingAttribute>().SingleOrDefault();

        Scope scope = (parameterBindingAttribute?.scope ?? Scope.Any) switch
        {
            Scope.Any => infix ? Scope.Inner : Scope.Outer,
            Scope.Inner => Scope.Inner,
            Scope.Outer => Scope.Outer,
            _ => throw new ArgumentOutOfRangeException(),
        };
        Invocation invocation = scope switch
        {
            Scope.Inner => inner,
            Scope.Outer => outer,
            _ => throw new ArgumentOutOfRangeException(),
        };

        if (invocation is EmptyInvocation)
            throw new ParameterBindingException(parameterName, "Invalid scope");

        switch (parameterBindingAttribute)
        {
            case ParameterAttribute { index: int index }: return BindParameterByIndex(parameter, invocation, scope, index);

            case ParameterAttribute { name: var name, scope: var attributeScope }: return BindParameterByName(parameter, name ?? parameterName, attributeScope);

            case InstanceAttribute: return BindInstance(parameter, invocation, scope);

            case ReturnValueAttribute: return BindReturnValue(parameter, invocation, scope);

            case StateAttribute { key: var key }: return BindState(parameter, key ?? parameterName);

            case FieldAttribute { name: var name, scope: var attributeScope }: return BindFieldByName(parameter, name ?? parameterName, attributeScope);

            case BaseMethodAttribute: return BindBaseMethod(parameter);

            case null: break;

            default: throw new NotSupportedException();
        }

        switch (parameterName)
        {
            case "__caller":
            {
                if (!infix)
                    throw new ParameterBindingException(parameterName, "Can only be used with inner patches");
                return BindInstance(parameter, outer, Scope.Outer);
            }

            case "__instance": return BindInstance(parameter, invocation, scope);

            case "__result": return BindReturnValue(parameter, invocation, scope);

            case "__state": return BindState(parameter, parameterName);

            case "__base": return BindBaseMethod(parameter);

            case var _ when parameterName.StartsWith("___"): return BindFieldByName(parameter, parameterName[3..], Scope.Any);

            default: return BindParameterByName(parameter, parameterName, Scope.Any);
        }
    }

    private ParameterBinding BindParameterByIndex(ParameterInfo parameter, Invocation invocation, Scope scope, int index)
    {
        if (isIterator && scope == Scope.Outer)
            return BindParameterByName(parameter, target.ParameterNames[index], scope);

        if (!invocation.IsStatic)
            index++;

        Validate(parameter, invocation.ParameterTypes[index], scope, "parameter");
        return new() { Parameter = parameter, BindingType = BindingType.Parameter, Scope = scope, Index = index };
    }

    private ParameterBinding BindState(ParameterInfo parameter, string key)
    {
        string stateKey = $"{className}#{parameter.ParameterType.NoRefType.FullName}#{key}";

        // ValidateCast not needed, the type will be checked in StateBuilder
        return new() { Parameter = parameter, BindingType = BindingType.State, Scope = Scope.Outer, StateKey = stateKey };
    }

    private ParameterBinding BindBaseMethod(ParameterInfo parameter)
    {
        if (outer is not MethodInvocation method || outer.IsStatic)
            throw new ParameterBindingException(parameter.Name, "Must be an instance method");

        ValidateCast(typeof(Delegate), parameter.ParameterType, parameter.Name);

        // Validate the delegate type has the right parameter types
        var delegateInvoke = parameter.ParameterType.GetMethod("Invoke");
        if (delegateInvoke is null)
            throw new ParameterBindingException(parameter.Name, "Delegate.Invoke not found");
        if (!delegateInvoke.GetParameters().Types().SequenceEqual(method.MethodInfo.GetParameters().Types()))
            throw new ParameterBindingException(parameter.Name, "Parameter type mismatch");
        if (delegateInvoke.ReturnType != method.MethodInfo.ReturnType)
            throw new ParameterBindingException(parameter.Name, "Return type mismatch");

        return new() { Parameter = parameter, BindingType = BindingType.BaseMethod, Scope = Scope.Outer };
    }

    private ParameterBinding BindReturnValue(ParameterInfo parameter, Invocation defaultInvocation, Scope defaultScope)
    {
        if (defaultInvocation.ReturnType.IsVoid())
            throw new ParameterBindingException(parameter.Name, "Method returns void");
        ValidateCast(parameter.ParameterType, defaultInvocation.ReturnType, parameter.Name);
        return new() { Parameter = parameter, BindingType = BindingType.Result, Scope = defaultScope };
    }

    private ParameterBinding BindInstance(ParameterInfo parameter, Invocation defaultInvocation, Scope defaultScope)
    {
        if (isIterator && defaultScope == Scope.Outer)
        {
            if (target.IsStatic)
                throw new ParameterBindingException(parameter.Name, "Method is static");
            if (parameter.ParameterType.IsByRef)
                throw new ParameterBindingException(parameter.Name, "Accessing 'this' by reference is not supported for iterator state machine methods");

            var thisField = GetThisField(outer.InstanceType);
            Validate(parameter, thisField.FieldType, defaultScope, "instance");
            return new() { Parameter = parameter, BindingType = BindingType.Instance, Scope = defaultScope, Fields = [thisField] };
        }

        if (defaultInvocation.IsStatic)
            throw new ParameterBindingException(parameter.Name, "Method is static");

        if (!defaultInvocation.InstanceType.IsValueType)
            ValidateReference(parameter, defaultInvocation.InstanceType, defaultScope, "instance");
        ValidateCast(parameter.ParameterType, defaultInvocation.InstanceType, parameter.Name);
        return new() { Parameter = parameter, BindingType = BindingType.Instance, Scope = defaultScope };
    }

    private ParameterBinding BindParameterByName(ParameterInfo parameter, string name, Scope scope)
    {
        // Look in target parameters
        if (scope is Scope.Inner or Scope.Any)
        {
            int index = Array.FindIndex(inner.ParameterNames, p => p == name);
            if (index >= 0)
            {
                Validate(parameter, inner.ParameterTypes[index], Scope.Inner, "parameter");
                return new() { Parameter = parameter, BindingType = BindingType.Parameter, Scope = Scope.Inner, Index = index };
            }
        }

        // Look in caller parameters
        if (scope is Scope.Outer or Scope.Any)
        {
            if (isIterator)
            {
                var iteratorType = outer.InstanceType;
                var field = iteratorType.GetField(name, AccessTools.all);
                if (field != null)
                {
                    Validate(parameter, field.FieldType, Scope.Outer, "parameter");
                    return new() { Parameter = parameter, BindingType = BindingType.Instance, Scope = Scope.Outer, Fields = [field] };
                }

                if (TryGetThisField(iteratorType, out var thisField) && thisField.FieldType.IsClosureType)
                {
                    var type = thisField.FieldType.NoRefType;
                    field = type.GetField(name, AccessTools.all);
                    if (field != null)
                    {
                        Validate(parameter, field.FieldType, Scope.Outer, "parameter");
                        return new() { Parameter = parameter, BindingType = BindingType.Instance, Scope = Scope.Outer, Fields = [thisField, field] };
                    }
                }

                throw new ParameterBindingException(parameter.Name, "Parameter not found");
            }

            Type[] parameterTypes = outer.ParameterTypes;
            int index = Array.FindIndex(outer.ParameterNames, p => p == name);
            if (index >= 0)
            {
                Validate(parameter, outer.ParameterTypes[index], Scope.Outer, "parameter");
                return new() { Parameter = parameter, BindingType = BindingType.Parameter, Scope = Scope.Outer, Index = index };
            }
        }

        // Look in closure fields
        if (scope is Scope.Inner or Scope.Any)
        {
            if (TryBindClosureByName(parameter, name, inner.ParameterTypes, Scope.Inner, out var parameterBinding))
                return parameterBinding;
        }

        // Look in closure fields
        if (scope is Scope.Outer or Scope.Any)
        {
            if (TryBindClosureByName(parameter, name, outer.ParameterTypes, Scope.Outer, out var parameterBinding))
                return parameterBinding;
        }

        throw new ParameterBindingException(parameter.Name, "Parameter not found");
    }

    private bool TryBindClosureByName(
        ParameterInfo parameter,
        string name,
        Type[] parameterTypes,
        Scope scope,
        [NotNullWhen(true)] out ParameterBinding? parameterBinding)
    {
        int closureIndex = Array.FindLastIndex(parameterTypes, p => p.IsClosureType);
        if (closureIndex >= 0)
        {
            var type = parameterTypes[closureIndex].NoRefType;

            var field = type.GetField(name, AccessTools.all);

            if (field != null)
            {
                ValidateCast(parameter.ParameterType, field.FieldType, parameter.Name);
                parameterBinding = new()
                {
                    Parameter = parameter,
                    BindingType = BindingType.Parameter,
                    Scope = scope,
                    Index = closureIndex,
                    Fields = [field],
                };
                return true;
            }
        }

        parameterBinding = null;
        return false;
    }

    private ParameterBinding BindFieldByName(ParameterInfo parameter, string name, Scope scope)
    {
        // Look in inner instance fields
        if (scope is Scope.Inner or Scope.Any && !inner.IsStatic)
        {
            var field = inner.InstanceType.GetField(name, AccessTools.all);
            if (field != null)
            {
                ValidateCast(parameter.ParameterType, field.FieldType, parameter.Name);
                return new() { Parameter = parameter, BindingType = BindingType.Instance, Scope = Scope.Inner, Fields = [field] };
            }
        }

        // Look in outer instance fields
        if (scope is Scope.Outer or Scope.Any && !outer.IsStatic)
        {
            Type curType = outer.InstanceType;
            List<FieldInfo> fields = [];
            if (isIterator)
            {
                var thisField = GetThisField(curType);
                curType = thisField.FieldType;
                fields.Add(thisField);
            }

            var field = curType.GetField(name, AccessTools.all);
            if (field != null)
            {
                fields.Add(field);
                ValidateCast(parameter.ParameterType, field.FieldType, parameter.Name);
                return new() { Parameter = parameter, BindingType = BindingType.Instance, Scope = Scope.Outer, Fields = [.. fields] };
            }
        }

        throw new ParameterBindingException(parameter.Name, "Field not found");
    }

    private static FieldInfo GetThisField(Type iteratorType)
    {
        return iteratorType.GetFields(AccessTools.all).Single(f => Regex.IsMatch(f.Name, "^<>[\\d+]__this$"));
    }

    private static bool TryGetThisField(Type iteratorType, [NotNullWhen(true)] out FieldInfo? field)
    {
        field = iteratorType.GetFields(AccessTools.all).SingleOrDefault(f => Regex.IsMatch(f.Name, "^<>[\\d+]__this$"));
        return field != null;
    }

    private static void ValidateCast(Type to, Type from, string parameterName)
    {
        if (!to.NoRefType.IsAssignableFrom(from.NoRefType))
            throw new InvalidCastException($"{parameterName}: Can't convert {from.FullName} to {to.FullName}");
    }

    private void ValidateReference(ParameterInfo to, Type from, Scope scope, string bindingType)
    {
        // Don't allow writing through a ref parameter to an argument of the outer method. This would
        // be wildly unreliable, as the compiler is free to copy those to locals any time it wants.
        if (to.ParameterType.IsByRef && !from.IsByRef)
        {
            if (scope == Scope.Outer && patchType != PatchType.Prefix)
                throw new ParameterBindingException(to.Name, $"{patchType} can't access outer method {bindingType} by ref");
            if (scope == Scope.Inner && patchType != PatchType.InnerPrefix)
                throw new ParameterBindingException(to.Name, $"{patchType} can't access inner method {bindingType} by ref");
        }
    }
    private void Validate(ParameterInfo to, Type from, Scope scope, string bindingType)
    {
        ValidateReference(to, from, scope, bindingType);
        ValidateCast(to.ParameterType, from, to.Name);
    }

}
