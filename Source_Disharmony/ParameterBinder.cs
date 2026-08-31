using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace Disharmony;

/// <summary>
///     A helper class that analyzes patch method parameters and determines how their values should be emitted in code
///     generation.
/// </summary>
/// <remarks>
///     For patches targeting a compiler-generated iterator state machine, <paramref name="target" /> refers to
///     the declared target, while <paramref name="inner" /> refers to the compiler-generated <c>MoveNext</c> method.
/// </remarks>
/// <param name="target">The declared target of the patch.</param>
/// <param name="outer">The outer method being patched.</param>
/// <param name="inner">The inner invocation being patched, or <see cref="EmptyInvocation" /> for an outer patch.</param>
/// <param name="patchType">The patch type.</param>
/// <param name="stateGroupKey">A string for grouping together <see cref="StateAttribute">state</see> parameters.</param>
internal class ParameterBinder(Invocation target, Invocation outer, Invocation inner, PatchType patchType, PatchOptions options, string stateGroupKey)
{
    private bool IsInfix => inner is not EmptyInvocation;
    private bool IsIterator => outer != target;

    private const string ReadonlyAttributeName = "System.Runtime.CompilerServices.IsReadOnlyAttribute";
    private const string ThisRegexPattern = "^<>[\\d+]__this$";

    public ParameterBinding Bind(ParameterInfo parameter)
    {
        var parameterName = parameter.Name;

        var parameterAttributes = parameter.GetCustomAttributes().ToList();
        ParameterBindingAttribute? parameterBindingAttribute;
        try
        {
            parameterBindingAttribute = parameterAttributes.OfType<ParameterBindingAttribute>().SingleOrDefault();
        }
        catch (InvalidOperationException e)
        {
            throw new ParameterBindingException(parameterName, "Multiple parameter binding attributes", e);
        }

        Scope scope = (parameterBindingAttribute?.Scope ?? Scope.Any) switch
        {
            Scope.Any => IsInfix ? Scope.Inner : Scope.Outer,
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
            case ParameterAttribute { Index: int index }: return BindParameterByIndex(parameter, invocation, scope, index);

            case ParameterAttribute { Name: var name, Scope: var attributeScope }:
                return BindParameterByName(parameter, name ?? parameterName, attributeScope);

            case InstanceAttribute: return BindInstance(parameter, invocation, scope);

            case ReturnValueAttribute: return BindReturnValue(parameter, invocation, scope);

            case StateAttribute { Key: var key }: return BindState(parameter, key ?? parameterName);

            case FieldAttribute { Name: var name, Scope: var attributeScope }:
                return BindFieldByName(parameter, name ?? parameterName, attributeScope);

            case BaseMethodAttribute: return BindBaseMethod(parameter);

            case MethodAttribute { Name: var name }: return BindMethod(parameter, scope, name ?? parameterName);

            case ExceptionAttribute: return BindException(parameter);

            case null: break;

            default: throw new NotSupportedException();
        }

        switch (parameterName)
        {
            case "__caller":
            {
                if (!IsInfix)
                    throw new ParameterBindingException(parameterName, "Can only be used with inner patches");
                return BindInstance(parameter, outer, Scope.Outer);
            }

            case "__instance": return BindInstance(parameter, invocation, scope);

            case "__result": return BindReturnValue(parameter, invocation, scope);

            case "__state": return BindState(parameter, parameterName);

            case "__base": return BindBaseMethod(parameter);

            case "__exception": return BindException(parameter);

            case var _ when parameterName.StartsWith("___"): return BindFieldByName(parameter, parameterName[3..], Scope.Any);

            default: return BindParameterByName(parameter, parameterName, Scope.Any);
        }
    }

    private ParameterBinding BindParameterByIndex(ParameterInfo parameter, Invocation invocation, Scope scope, int index)
    {
        if (invocation.HasThis)
            index++;

        try
        {
            if (IsIterator && scope == Scope.Outer)
                return BindParameterByName(parameter, target.ParameterNames[index], scope);

            Validate(parameter, invocation.ParameterTypes[index], scope, "parameter");
            return new() { parameter = parameter, bindingType = BindingType.Parameter, scope = scope, index = index };
        }
        catch (IndexOutOfRangeException e)
        {
            throw new ParameterBindingException(parameter.Name, "Index is out of range", e);
        }
    }

    private ParameterBinding BindState(ParameterInfo parameter, string key)
    {
        string stateKey = $"{stateGroupKey}#{parameter.ParameterType.NoRefType.FullName}#{key}";

        // ValidateCast not needed, the type will be checked in StateBuilder
        return new() { parameter = parameter, bindingType = BindingType.State, scope = Scope.Outer, stateKey = stateKey };
    }

    private ParameterBinding BindBaseMethod(ParameterInfo parameter)
    {
        if (outer is not MethodInvocation method || outer.IsStatic)
            throw new ParameterBindingException(parameter.Name, "Must be an instance method");

        ValidateCast(typeof(Delegate), parameter.ParameterType, parameter.Name);

        // Validate the delegate type has the right parameter types
        ValidateInvoke(parameter, method.MethodInfo);

        MethodInfo? baseMethod = null;
        for (Type? parent = method.InstanceType.BaseType; parent != typeof(object) && parent != null; parent = parent.BaseType)
        {
            ParameterInfo[] parameters = method.MethodInfo.GetParameters();
            baseMethod = parent.GetMethod(method.MethodInfo.Name,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                null, parameters.Types(), null);
            if (baseMethod != null)
                break;
        }

        if (baseMethod is null)
            throw new ParameterBindingException(parameter.Name, "Base method not found");
        if (baseMethod.IsAbstract)
            throw new ParameterBindingException(parameter.Name, "Base method is abstract");

        return new() { parameter = parameter, bindingType = BindingType.Delegate, scope = Scope.Outer, methodInfo = baseMethod };
    }

    private ParameterBinding BindMethod(ParameterInfo parameter, Scope scope, string name)
    {
        var invocation = scope switch {
            Scope.Inner => inner,
            Scope.Outer => target,
            _ => throw new ArgumentOutOfRangeException(nameof(scope), scope, null)
        };
        var instanceType = invocation.InstanceType;
        var methodInfo = instanceType.GetMethod(name, AccessTools.all);

        if (methodInfo is null)
            throw new ParameterBindingException(parameter.Name, "Method not found");
        if (invocation.IsStatic && !methodInfo.IsStatic)
            throw new ParameterBindingException(parameter.Name, "Instance required");

        // Getting the instance for an iterator state machine isn't implemented for BindingType.Delegate
        if (IsIterator && scope == Scope.Outer && !methodInfo.IsStatic)
            throw new ParameterBindingException(parameter.Name, "[Method] is not supported for iterator state machines");

        bool isReadonly = methodInfo.CustomAttributes.Any(a => a.AttributeType.FullName == ReadonlyAttributeName) ||
                          instanceType.CustomAttributes.Any(a => a.AttributeType.FullName == ReadonlyAttributeName);

        // Calling a struct method through a delegate requires boxing the struct, which means that any writes
        // by the method won't affect the original struct.
        if (instanceType.IsValueType && !methodInfo.IsStatic && !isReadonly)
            throw new ParameterBindingException(parameter.Name, "[Method] is not supported for non-static methods on structs");

        ValidateCast(typeof(Delegate), parameter.ParameterType, parameter.Name);
        ValidateInvoke(parameter, methodInfo);

        return new()
        {
            parameter = parameter, bindingType = BindingType.Delegate, scope = scope, methodInfo = methodInfo,
            useVirtualDispatch = methodInfo.IsVirtual,
        };
    }

    private static void ValidateInvoke(ParameterInfo parameter, MethodInfo methodInfo)
    {
        var delegateInvoke = parameter.ParameterType.GetMethod("Invoke") ??
                             throw new ParameterBindingException(parameter.Name, "Delegate.Invoke not found");
        if (!delegateInvoke.GetParameters().Types().SequenceEqual(methodInfo.GetParameters().Types()))
            throw new ParameterBindingException(parameter.Name, "Parameter type mismatch");
        if (delegateInvoke.ReturnType != methodInfo.ReturnType)
            throw new ParameterBindingException(parameter.Name, "Return type mismatch");
    }

    private ParameterBinding BindReturnValue(ParameterInfo parameter, Invocation invocation, Scope scope)
    {
        if (invocation.ReturnType == typeof(void))
            throw new ParameterBindingException(parameter.Name, "Method returns void");
        if (patchType == PatchType.Prefix && (options & PatchOptions.AlwaysRun) != 0)
            throw new ParameterBindingException(parameter.Name, "Binding return value not allowed for Prefix with AlwaysRun option");
        ValidateCast(parameter, invocation.ReturnType);
        return new() { parameter = parameter, bindingType = BindingType.Result, scope = scope };
    }

    private ParameterBinding BindInstance(ParameterInfo parameter, Invocation invocation, Scope scope)
    {
        if (IsIterator && scope == Scope.Outer)
        {
            if (target.IsStatic)
                throw new ParameterBindingException(parameter.Name, "Method is static");
            if (IsWriteableRef(parameter))
                throw new ParameterBindingException(parameter.Name,
                    "Accessing 'this' by reference is not supported for iterator state machine methods");

            var thisField = GetThisField(outer.InstanceType);
            Validate(parameter, thisField.FieldType, scope, "instance");
            return new() { parameter = parameter, bindingType = BindingType.Instance, scope = scope, fields = [thisField] };
        }

        if (invocation.IsStatic)
            throw new ParameterBindingException(parameter.Name, "Method is static");

        if (!invocation.InstanceType.IsValueType && invocation is not FieldInvocation)
            ValidateReference(parameter, invocation.InstanceType, scope, "instance");
        ValidateCast(parameter, invocation.InstanceType);
        return new() { parameter = parameter, bindingType = BindingType.Instance, scope = scope };
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
                return new() { parameter = parameter, bindingType = BindingType.Parameter, scope = Scope.Inner, index = index };
            }
        }

        // Look in caller parameters
        if (scope is Scope.Outer or Scope.Any)
        {
            if (IsIterator)
            {
                var iteratorType = outer.InstanceType;
                var field = iteratorType.GetField(name, AccessTools.all);
                if (field != null)
                {
                    Validate(parameter, field.FieldType, Scope.Outer, "parameter");
                    return new() { parameter = parameter, bindingType = BindingType.Instance, scope = Scope.Outer, fields = [field] };
                }

                if (TryGetThisField(iteratorType, out var thisField) && thisField.FieldType.IsClosureType)
                {
                    var type = thisField.FieldType.NoRefType;
                    field = type.GetField(name, AccessTools.all);
                    if (field != null)
                    {
                        Validate(parameter, field.FieldType, Scope.Outer, "parameter");
                        return new()
                        {
                            parameter = parameter, bindingType = BindingType.Instance, scope = Scope.Outer, fields = [thisField, field],
                        };
                    }
                }

                throw new ParameterBindingException(parameter.Name, "Parameter not found");
            }

            int index = Array.FindIndex(outer.ParameterNames, p => p == name);
            if (index >= 0)
            {
                Validate(parameter, outer.ParameterTypes[index], Scope.Outer, "parameter");
                return new() { parameter = parameter, bindingType = BindingType.Parameter, scope = Scope.Outer, index = index };
            }
        }

        // Look in closure fields
        if (scope is Scope.Inner or Scope.Any)
            if (TryBindClosureByName(parameter, name, inner.ParameterTypes, Scope.Inner, out var parameterBinding))
                return parameterBinding;

        // Look in closure fields
        if (scope is Scope.Outer or Scope.Any)
            if (TryBindClosureByName(parameter, name, outer.ParameterTypes, Scope.Outer, out var parameterBinding))
                return parameterBinding;

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
                ValidateCast(parameter, field.FieldType);
                parameterBinding = new()
                {
                    parameter = parameter,
                    bindingType = BindingType.Parameter,
                    scope = scope,
                    index = closureIndex,
                    fields = [field],
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
                ValidateCast(parameter, field.FieldType);
                return new() { parameter = parameter, bindingType = BindingType.Instance, scope = Scope.Inner, fields = [field] };
            }
        }

        // Look in outer instance fields
        if (scope is Scope.Outer or Scope.Any && !outer.IsStatic)
        {
            Type curType = outer.InstanceType;
            List<FieldInfo> fields = [];
            if (IsIterator)
            {
                var thisField = GetThisField(curType);
                curType = thisField.FieldType;
                fields.Add(thisField);
            }

            var field = curType.GetField(name, AccessTools.all);
            if (field != null)
            {
                fields.Add(field);
                ValidateCast(parameter, field.FieldType);
                return new() { parameter = parameter, bindingType = BindingType.Instance, scope = Scope.Outer, fields = [.. fields] };
            }
        }

        throw new ParameterBindingException(parameter.Name, "Field not found");
    }

    private ParameterBinding BindException(ParameterInfo parameter)
    {
        if (patchType != PatchType.Postfix || (options & PatchOptions.AlwaysRun) == 0)
            throw new ParameterBindingException(parameter.Name, "Accessing exception is only supported for Postfix with AlwaysRun option");
        ValidateCast(parameter, typeof(Exception));
        return new() { parameter = parameter, bindingType = BindingType.Exception, scope = Scope.Any };
    }

    private static FieldInfo GetThisField(Type iteratorType)
    {
        return iteratorType.GetFields(AccessTools.all).Single(f => Regex.IsMatch(f.Name, ThisRegexPattern));
    }

    private static bool TryGetThisField(Type iteratorType, [NotNullWhen(true)] out FieldInfo? field)
    {
        field = iteratorType.GetFields(AccessTools.all).SingleOrDefault(f => Regex.IsMatch(f.Name, ThisRegexPattern));
        return field != null;
    }

    private static void ValidateCast(Type to, Type from, string parameterName)
    {
        if (!to.NoRefType.IsAssignableFrom(from.NoRefType))
            throw new InvalidCastException($"{parameterName}: Can't convert {from.FullName} to {to.FullName}");
    }

    private static void ValidateCast(ParameterInfo parameter, Type from)
    {
        Type to = parameter.ParameterType;
        string parameterName = parameter.Name;

        if (to.IsByRef && from.NoRefType.IsValueType)
        {
            if (to.NoRefType != from.NoRefType)
                throw new InvalidCastException($"{parameterName}: Can't convert {from.FullName} to {to.FullName}");
        }
        else if (parameter.IsIn)
        {
            if (!to.NoRefType.IsAssignableFrom(from.NoRefType))
                throw new InvalidCastException($"{parameterName}: Can't convert {from.FullName} to 'in' {to.FullName}");
        }
        else if (parameter.IsOut)
        {
            if (!from.NoRefType.IsAssignableFrom(to.NoRefType))
                throw new InvalidCastException($"{parameterName}: Can't convert {from.FullName} to 'out' {to.FullName}");
        }
        else if (to.IsByRef)
        {
            if (to.NoRefType != from.NoRefType)
                throw new InvalidCastException($"{parameterName}: Can't convert {from.FullName} to 'ref' {to.FullName}");
        }
        else
        {
            if (!to.NoRefType.IsAssignableFrom(from.NoRefType))
                throw new InvalidCastException($"{parameterName}: Can't convert {from.FullName} to {to.FullName}");
        }
    }

    private void ValidateReference(ParameterInfo parameter, Type type, Scope scope, string bindingType)
    {
        // Don't allow writing through a ref parameter to an argument of the outer method. This would
        // be wildly unreliable, as the compiler is free to copy those to locals any time it wants.
        if (IsWriteableRef(parameter) && !type.IsByRef)
        {
            if (scope == Scope.Outer && !(patchType == PatchType.Prefix && !IsInfix))
                throw new ParameterBindingException(parameter.Name,
                    $"{patchType} can't access outer method {bindingType} by writeable reference");
            if (scope == Scope.Inner && !(patchType == PatchType.Prefix && IsInfix))
                throw new ParameterBindingException(parameter.Name,
                    $"{patchType} can't access inner method {bindingType} by writeable reference");
        }
    }

    private void Validate(ParameterInfo parameter, Type type, Scope scope, string bindingType)
    {
        ValidateReference(parameter, type, scope, bindingType);
        ValidateCast(parameter, type);
    }

    private static bool IsWriteableRef(ParameterInfo parameter) => parameter.ParameterType.IsByRef && !parameter.IsIn;
}
