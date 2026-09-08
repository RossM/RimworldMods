namespace Disharmony.RuleBuilders;

internal abstract class RuleBuilder(RuleBuilderContext context, Invocation outer)
{
    public virtual IEnumerable<Label> CrossRuleLabels => [];
    protected readonly Type[] outerParameterTypes = outer.ParameterTypes;

    protected readonly InstructionList output = context.NewInstructionList();
    protected LocalTrackerBuilder? resultLocal = null;
    protected LocalTrackerBuilder? exceptionLocal = null;
    protected LocalTrackerBuilder? dispatchInfoLocal = null;
    protected readonly ILGenerator generator = context.generator;

    public abstract IEnumerable<Rule> BuildRules();

    protected void EmitParameterValue(ParameterBinding parameter)
    {
        Type parameterType = parameter.parameter.ParameterType;
        bool wantRef = parameterType.IsByRef;
        EmitRawParameterValue(parameter, wantRef, out Type resultType);

        if (resultType.IsValueType && parameterType != resultType)
            EmitConversion(parameterType, resultType);
    }

    private void EmitRawParameterValue(ParameterBinding parameter, bool wantRef, out Type resultType)
    {
        Type parameterType = parameter.parameter.ParameterType;

        resultType = parameterType;

        if (parameter is { fields.Length: > 0, bindingType: not (BindingType.Argument or BindingType.Instance or BindingType.StaticField) })
            throw new NotSupportedException();

        switch (parameter.bindingType)
        {
            case BindingType.Argument:
            case BindingType.Instance:
            {
                Type desiredType;
                if (parameter.fields is { Length: > 0 })
                {
                    desiredType = parameter.fields[0].DeclaringType!;
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

                EmitParameterLookup(parameter.scope, parameter.index, desiredType);
                resultType = desiredType;

                if (parameter.fields is { Length: > 0 })
                    EmitFieldLookups(parameter, wantRef, ref resultType);

                break;
            }

            case BindingType.Result:
            {
                if (wantRef && !resultLocal!.Type.IsByRef)
                {
                    output.Add(resultLocal!.Load(true));
                    resultType = resultLocal.Type.MakeByRefType();
                }
                else if (!wantRef && resultLocal!.Type.IsByRef)
                {
                    output.Add(resultLocal!.Load());
                    output.Add(new(OpCodes.Ldobj, resultLocal.Type.GetElementType()));
                    resultType = resultLocal.Type.GetElementType();
                }
                else
                {
                    output.Add(resultLocal!.Load());
                    resultType = resultLocal.Type;
                }

                break;
            }

            case BindingType.State:
            {
                output.Add(parameter.local!.Load(wantRef));
                resultType = parameter.local.Type;
                if (wantRef)
                    resultType = resultType.MakeByRefType();
                break;
            }

            case BindingType.Delegate:
            {
                EmitDelegate(parameter);
                break;
            }

            case BindingType.Exception:
            {
                output.Add(exceptionLocal!.Load(wantRef));
                resultType = exceptionLocal.Type;
                if (wantRef)
                    resultType = resultType.MakeByRefType();
                break;
            }

            case BindingType.StaticField:
            {
                output.Add(new(wantRef ? OpCodes.Ldsflda : OpCodes.Ldsfld, parameter.fields![0]));
                resultType = parameter.fields![0].FieldType;
                if (wantRef)
                    resultType = resultType.MakeByRefType();
                break;
            }

            case BindingType.MemberInfo:
            {
                output.Add(new(OpCodes.Ldtoken, parameter.memberInfo!));
                switch (parameter.memberInfo)
                {
                    case MethodBase:
                        output.Add(new(OpCodes.Call, InfoOf.MethodBase_GetMethodFromHandle1));
                        break;
                    case FieldInfo:
                        output.Add(new(OpCodes.Call, InfoOf.FieldInfo_GetFieldFromHandle1));
                        break;
                }
                resultType = parameter.memberInfo!.GetType();
                break;
            }

            case BindingType.ArgumentArray:
            {
                var invocation = GetInvocation(parameter);
                int argOffset = invocation.HasThis ? 1 : 0;
                int argCount = invocation.ParameterTypes.Length - argOffset;

                output.Add(new(OpCodes.Ldc_I4, argCount));
                output.Add(new(OpCodes.Newarr, typeof(object)));

                for (int i = 0; i < argCount; i++)
                {
                    var type = invocation.ParameterTypes[argOffset + i].NoRefType;
                    output.Add(new(OpCodes.Dup));
                    output.Add(new(OpCodes.Ldc_I4, i));
                    EmitParameterLookup(parameter.scope, argOffset + i, type);
                    if (type.IsValueType)
                        EmitConversion(typeof(object), type);
                    output.Add(new(OpCodes.Stelem_Ref));
                }

                resultType = typeof(object[]);
                break;
            }

            default:
            {
                throw new ArgumentOutOfRangeException();
            }
        }
    }

    private void EmitDelegate(ParameterBinding parameter)
    {
        // ParameterType must be a subclass of Delegate here
        ConstructorInfo delegateConstructor = parameter.parameter.ParameterType.GetConstructor([typeof(object), typeof(IntPtr)]);

        MethodBase methodBase = (MethodBase)parameter.memberInfo!;

        // Create a delegate
        if (methodBase.IsStatic)
        {
            output.Add(new(OpCodes.Ldnull));
        }
        else
        {
            EmitParameterLookup(parameter.scope, 0, methodBase.DeclaringType);
            if (methodBase.DeclaringType!.IsValueType)
                output.Add(new(OpCodes.Box, methodBase.DeclaringType));
        }

        if (parameter.useVirtualDispatch)
        {
            output.Add(new(OpCodes.Dup));
            output.Add(new(OpCodes.Ldvirtftn, methodBase));
        }
        else
        {
            output.Add(new(OpCodes.Ldftn, methodBase));
        }

        output.Add(new(OpCodes.Newobj, delegateConstructor));
    }

    private void EmitFieldLookups(ParameterBinding parameter, bool wantRef, ref Type resultType)
    {
        if (parameter.fields is not { Length: > 0 })
            throw new InvalidOperationException();

        for (var index = 0; index < parameter.fields.Length; index++)
        {
            FieldInfo field = parameter.fields[index];
            var byRef = wantRef && (index == parameter.fields.Length - 1 || field.FieldType.IsValueType);
            output.Add(new(byRef ? OpCodes.Ldflda : OpCodes.Ldfld, field));
        }

        resultType = wantRef ? parameter.fields[^1].FieldType.MakeByRefType() : parameter.fields[^1].FieldType;
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

    protected virtual Type GetParameterType(ParameterBinding parameter) => parameter.scope switch
    {
        Scope.Outer => outerParameterTypes[parameter.index],
        _ => throw new ArgumentOutOfRangeException(nameof(parameter.scope)),
    };

    protected virtual Invocation GetInvocation(ParameterBinding parameter) => parameter.scope switch
    {
        Scope.Outer => outer,
        _ => throw new ArgumentOutOfRangeException(nameof(parameter.scope)),
    };

    protected virtual void EmitParameterLookup(Scope scope, int index, Type resultType)
    {
        switch (scope)
        {
            case Scope.Outer: EmitOuterParameter(index, resultType); break;
            default: throw new ArgumentOutOfRangeException(nameof(scope));
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
    public readonly List<LocalTrackerBuilder> locals = [];

    public InstructionList NewInstructionList()
    {
        InstructionList result = new(generator)
        {
            locals = locals,
        };
        return result;
    }
}
