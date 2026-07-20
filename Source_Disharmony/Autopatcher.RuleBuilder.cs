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
        private readonly Type[]? outerParameterTypes = outer.ParameterTypes;

        protected readonly InstructionList output = context.NewInstructionList();
        protected int resultLocalIndex = -1;
        protected readonly ILGenerator generator = context.generator;

        public abstract IEnumerable<Rule> BuildRules();

        protected void EmitParameterValue(ParameterBinding parameter)
        {
            Type parameterType = parameter.Parameter.ParameterType;

            switch (parameter.BindingType)
            {
                case BindingType.Parameter:
                case BindingType.Instance:
                {
                    EmitParameterLookup(parameter, parameter.Fields is not { Length: > 0 }, parameterType.IsByRef);
                    break;
                }

                case BindingType.Result:
                {
                    EmitResult(parameterType);
                    break;
                }

                case BindingType.State:
                {
                    output.Add(CodeInstruction.LoadLocal(parameter.Index, parameterType.IsByRef));

                    break;
                }

                default:
                {
                    throw new ArgumentOutOfRangeException();
                }
            }

            if (parameter.Fields is { Length: > 0 })
            {
                foreach (var field in parameter.Fields)
                {
                    if (parameterType.IsByRef)
                        output.Add(new(OpCodes.Ldflda, field));
                    else
                        output.Add(new(OpCodes.Ldfld, field));
                }
            }
        }

        protected virtual void EmitParameterLookup(ParameterBinding parameter, bool directAccess, bool typeIsByRef)
        {
            switch (parameter.Scope)
            {
                case Scope.Outer: EmitOuterParameter(parameter.Index, directAccess, typeIsByRef); break;
                default: throw new ArgumentOutOfRangeException(nameof(parameter.Scope));
            }
        }

        private void EmitResult(Type parameterType)
        {
            output.Add(CodeInstruction.LoadLocal(resultLocalIndex, parameterType.IsByRef));
        }

        protected void EmitOuterParameter(int index, bool directAccess, bool typeIsByRef)
        {
            if (outerParameterTypes == null)
                throw new InvalidOperationException("outerParameterTypes is null");

            output.Add(CodeInstruction.LoadArgument(index,
                typeIsByRef && !outerParameterTypes[index].IsByRef && (directAccess || outerParameterTypes[index].IsStruct())));
            if (!typeIsByRef && outerParameterTypes[index].IsByRef && directAccess)
                output.Add(new(OpCodes.Ldobj, outerParameterTypes[index].GetElementType()));
        }
    }
}
