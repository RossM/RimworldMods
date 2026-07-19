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
                    output.EmitLoad(parameter.Index, parameterType.IsByRef);

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

        protected virtual void EmitParameterLookup(ParameterBinding parameter, bool doDereference, bool typeIsByRef)
        {
            switch (parameter.Scope)
            {
                case Scope.Outer: EmitOuterParameter(parameter.Index, doDereference, typeIsByRef); break;
                default: throw new ArgumentOutOfRangeException(nameof(parameter.Scope));
            }
        }

        private void EmitResult(Type parameterType)
        {
            output.EmitLoad(resultLocalIndex, parameterType.IsByRef);
        }

        protected void EmitOuterParameter(int index, bool doDereference, bool typeIsByRef)
        {
            if (outerParameterTypes == null)
                throw new InvalidOperationException("outerParameterTypes is null");

            if (typeIsByRef && !outerParameterTypes[index].IsByRef)
                output.Add(new(OpCodes.Ldarga, index));
            else
                output.Add(CodeInstruction.LoadArgument(index));
            if (!typeIsByRef && outerParameterTypes[index].IsByRef && doDereference)
                output.Add(new(OpCodes.Ldobj, outerParameterTypes[index].GetElementType()));
        }
    }
}
