namespace Disharmony;

public static partial class Autopatcher
{
    private class RuleBuilderContext
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

    private abstract class RuleBuilder(RuleBuilderContext context, Invocation outer)
    {
        public virtual IEnumerable<Label> CrossRuleLabels => [];
        protected readonly Type[]? outerParameterTypes = outer.ParameterTypes;

        protected readonly InstructionList output = context.NewInstructionList();
        protected int resultLocalIndex = -1;
        protected readonly ILGenerator generator = context.generator;

        public abstract IEnumerable<Rule> BuildRules();

        protected void EmitParameterValue(ParameterBinding parameter)
        {
            Type resultType;
            bool wantRef = parameter.Parameter.ParameterType.IsByRef;

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
            else
            {
                resultType = parameter.Parameter.ParameterType;
            }

            switch (parameter.BindingType)
            {
                case BindingType.Parameter:
                case BindingType.Instance:
                {
                    EmitParameterLookup(parameter, resultType);
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
                    FieldInfo? field = parameter.Fields[index];
                    if (wantRef && (index == parameter.Fields.Length - 1 || field.FieldType.IsValueType))
                        output.Add(new(OpCodes.Ldflda, field));
                    else
                        output.Add(new(OpCodes.Ldfld, field));
                }
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
            if (outerParameterTypes == null)
                throw new InvalidOperationException("outerParameterTypes is null");

            Type parameterType = outerParameterTypes[index];
            output.Add(CodeInstruction.LoadArgument(index, targetType.IsByRef && !parameterType.IsByRef));
            if (!targetType.IsByRef && parameterType.IsByRef)
                output.Add(new(OpCodes.Ldobj, parameterType.GetElementType()));
        }
    }
}
