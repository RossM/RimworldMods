namespace Disharmony;

internal abstract class RuleBuilder(RuleBuilderContext context, Invocation outer)
{
    public virtual IEnumerable<Label> CrossRuleLabels => [];
    protected readonly Type[] outerParameterTypes = outer.ParameterTypes;

    protected readonly InstructionList output = context.NewInstructionList();
    protected int resultLocalIndex = -1;
    protected readonly ILGenerator generator = context.generator;

    public abstract IEnumerable<Rule> BuildRules();

    protected void EmitParameterValue(ParameterBinding parameter)
    {
        Type parameterType = parameter.Parameter.ParameterType;
        bool wantRef = parameterType.IsByRef;
        EmitRawParameterValue(parameter, wantRef, out Type resultType);

        if (parameter.Fields is { Length: > 0 })
            EmitFieldLookups(parameter, wantRef, ref resultType);

        if (resultType.IsValueType && parameterType != resultType)
            EmitConversion(parameterType, resultType);
    }

    private void EmitRawParameterValue(ParameterBinding parameter, bool wantRef, out Type resultType)
    {
        Type parameterType = parameter.Parameter.ParameterType;

        resultType = parameterType;

        if (parameter is { Fields.Length: > 0, BindingType: not (BindingType.Parameter or BindingType.Instance) })
            throw new NotSupportedException();

        switch (parameter.BindingType)
        {
            case BindingType.Parameter:
            case BindingType.Instance:
            {
                Type desiredType;
                if (parameter.Fields is { Length: > 0 })
                {
                    desiredType = parameter.Fields[0].DeclaringType!;
                    if (wantRef && desiredType.IsValueType)
                        desiredType = desiredType.MakeByRefType();
                }
                else
                {
                    desiredType = GetParameterType(parameter);
                    if (wantRef && !desiredType.IsByRef)
                        desiredType = desiredType.MakeByRefType();
                    else if (!wantRef && desiredType.IsByRef)
                        desiredType = desiredType.GetElementType();
                }

                EmitParameterLookup(parameter, desiredType);
                resultType = desiredType;
                
                break;
            }

            case BindingType.Result:
            {
                output.Add(CodeInstruction.LoadLocal(resultLocalIndex, wantRef));
                resultType = output.LocalTypes[resultLocalIndex];
                if (wantRef)
                    resultType = resultType.MakeByRefType();
                break;
            }

            case BindingType.State:
            {
                output.Add(CodeInstruction.LoadLocal(parameter.Index, wantRef));
                resultType = output.LocalTypes[parameter.Index];
                if (wantRef)
                    resultType = resultType.MakeByRefType();
                break;
            }

            case BindingType.BaseMethod:
            {
                EmitBaseMethodDelegate(parameter);
                break;
            }

            default:
            {
                throw new ArgumentOutOfRangeException();
            }
        }
    }

    private void EmitBaseMethodDelegate(ParameterBinding parameter)
    {
        if (parameter.Scope != Scope.Outer)
            throw new NotImplementedException();
        if (outer is not MethodInvocation method)
            throw new InvalidOperationException();

        MethodInfo methodInfo = method.MethodInfo;
                
        MethodInfo? baseMethod = null;
        for (Type parent = method.InstanceType.BaseType; parent != typeof(object) && parent != null; parent = parent.BaseType)
        {
            ParameterInfo[] parameters = methodInfo.GetParameters();
            baseMethod = parent.GetMethod(methodInfo.Name,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                null, parameters.Types(), null);
            if (baseMethod != null)
                break;
        }

        if (baseMethod is null)
            throw new InvalidOperationException($"{method.FullName}: Base method not found");

        // ParameterType must be a subclass of Delegate here
        ConstructorInfo delegateConstructor = parameter.Parameter.ParameterType.GetConstructor([typeof(object), typeof(IntPtr)]);

        // Create a delegate
        output.Add(CodeInstruction.LoadArgument(0));
        output.Add(new(OpCodes.Ldftn, baseMethod));
        output.Add(new(OpCodes.Newobj, delegateConstructor));
    }

    private void EmitFieldLookups(ParameterBinding parameter, bool wantRef, ref Type resultType)
    {
        if (parameter.Fields is not { Length: > 0 })
            throw new InvalidOperationException();

        for (var index = 0; index < parameter.Fields.Length; index++)
        {
            FieldInfo field = parameter.Fields[index];
            var byRef = wantRef && (index == parameter.Fields.Length - 1 || field.FieldType.IsValueType);
            output.Add(new(byRef ? OpCodes.Ldflda : OpCodes.Ldfld, field));
        }

        resultType = wantRef ? parameter.Fields[^1].FieldType.MakeByRefType() : parameter.Fields[^1].FieldType;
    }

    private void EmitConversion(Type parameterType, Type resultType)
    {
        if (!parameterType.IsValueType)
            output.Add(new(OpCodes.Box, resultType));
        else if (parameterType.IsGenericType && parameterType.GetGenericTypeDefinition() == typeof(Nullable<>) &&
                 parameterType.GetGenericArguments()[0] == resultType)
            output.Add(new(OpCodes.Newobj, parameterType.GetConstructor([resultType])));
        else
            throw new NotImplementedException($"Can't convert {resultType.FullName} to {parameterType.FullName}");
    }

    protected virtual Type GetParameterType(ParameterBinding parameter)
    {
        switch (parameter.Scope)
        {
            case Scope.Outer: return outerParameterTypes[parameter.Index];
            default: throw new ArgumentOutOfRangeException(nameof(parameter.Scope));
        }
    }

    protected virtual void EmitParameterLookup(ParameterBinding parameter, Type resultType)
    {
        switch (parameter.Scope)
        {
            case Scope.Outer: EmitOuterParameter(parameter.Index, resultType); break;
            default: throw new ArgumentOutOfRangeException(nameof(parameter.Scope));
        }
    }

    protected void EmitOuterParameter(int index, Type targetType)
    {
        Type parameterType = outerParameterTypes[index];
        output.Add(CodeInstruction.LoadArgument(index, targetType.IsByRef && !parameterType.IsByRef));
        if (!targetType.IsByRef && parameterType.IsByRef)
            output.Add(new(OpCodes.Ldobj, parameterType.GetElementType()));
    }
}

internal class RuleBuilderContext
{
    public readonly ILGenerator generator = PatchProcessor.CreateILGenerator();
    public readonly List<Type> localTypes = [];

    public InstructionList NewInstructionList()
    {
        InstructionList result = [];
        result.LocalTypes = localTypes;
        return result;
    }
}
