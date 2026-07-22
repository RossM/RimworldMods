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
        Type resultType = parameterType;

        if (parameter.Fields is { Length: > 0 })
        {
            resultType = GetParameterType(parameter);
            if (wantRef && resultType.IsValueType)
                resultType = resultType.MakeByRefType();
            else
            {
                Type? elementType = resultType.GetElementType();
                if (!wantRef && resultType.IsByRef && !elementType!.IsValueType)
                    resultType = elementType;
            }
        }

        switch (parameter.BindingType)
        {
            case BindingType.Parameter:
            case BindingType.Instance:
            {
                EmitParameterLookup(parameter, resultType);
                resultType = GetParameterType(parameter);
                if (wantRef && !resultType.IsByRef)
                    resultType = resultType.MakeByRefType();
                break;
            }

            case BindingType.Result:
            {
                EmitResult(resultType);
                break;
            }

            case BindingType.State:
            {
                output.Add(CodeInstruction.LoadLocal(parameter.Index, resultType.IsByRef));

                break;
            }

            default:
            {
                throw new ArgumentOutOfRangeException();
            }
        }

        if (parameter.Fields is { Length: > 0 })
        {
            for (var index = 0; index < parameter.Fields.Length; index++)
            {
                FieldInfo field = parameter.Fields[index];
                if (wantRef && (index == parameter.Fields.Length - 1 || field.FieldType.IsValueType))
                {
                    output.Add(new(OpCodes.Ldflda, field));
                    resultType = field.FieldType.MakeByRefType();
                }
                else
                {
                    output.Add(new(OpCodes.Ldfld, field));
                    resultType = field.FieldType;
                }
            }
        }

        if (resultType.IsValueType && parameterType != resultType)
        {
            if (!parameterType.IsValueType)
                output.Add(new(OpCodes.Box, resultType));
            else if (parameterType.IsGenericType && parameterType.GetGenericTypeDefinition() == typeof(Nullable<>) &&
                     parameterType.GetGenericArguments()[0] == resultType)
                output.Add(new(OpCodes.Newobj, parameterType.GetConstructor([resultType])));
            else
                throw new NotImplementedException($"Can't convert {resultType.FullName} to {parameterType.FullName}");
        }
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

    private void EmitResult(Type parameterType)
    {
        output.Add(CodeInstruction.LoadLocal(resultLocalIndex, parameterType.IsByRef));
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
