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

    private abstract class RuleBuilder
    {
        public virtual IEnumerable<Label> CrossRuleLabels => [];
        private readonly Type[]? outerParameterTypes;

        protected readonly InstructionList output;
        protected int resultLocalIndex = -1;
        protected readonly ILGenerator generator;

        protected RuleBuilder(RuleBuilderContext context, MethodBase? outer = null)
        {
            generator = context.generator;
            if (outer is not null)
                outerParameterTypes = ReflectionTools.GetParameterTypes(outer);
            output = context.NewInstructionList();
        }

        public abstract IEnumerable<Rule> BuildRules();

        protected void EmitParameterValue(ParameterBinding parameter)
        {
            Type parameterType = parameter.Parameter.ParameterType;

            switch (parameter.BindingType)
            {
                case BindingType.Parameter:
                case BindingType.Instance:
                {
                    EmitParameterLookup(parameterType, parameter);
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

        protected virtual void EmitParameterLookup(Type type, ParameterBinding parameter)
        {
            switch (parameter.Scope)
            {
                case Scope.Outer: EmitOuterParameter(type, parameter.Index); break;
                default: throw new ArgumentOutOfRangeException(nameof(parameter.Scope));
            }
        }

        private void EmitResult(Type parameterType)
        {
            output.EmitLoad(resultLocalIndex, parameterType.IsByRef);
        }

        protected void EmitOuterParameter(Type type, int index)
        {
            if (outerParameterTypes == null)
                throw new InvalidOperationException("outerParameterTypes is null");

            if (type.IsByRef && !outerParameterTypes[index].IsByRef)
                output.Add(new(OpCodes.Ldarga, index));
            else
                output.Add(CodeInstruction.LoadArgument(index));
            if (!type.IsByRef && outerParameterTypes[index].IsByRef)
                output.Add(new(OpCodes.Ldobj, type));
        }
    }
}
