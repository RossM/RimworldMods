namespace Disharmony;

public static partial class Autopatcher
{
    private abstract class RuleBuilder
    {
        public virtual IEnumerable<Label> CrossRuleLabels => [];
        protected readonly Type[]? outerParameterTypes;
        protected readonly Type[]? innerParameterTypes;

        protected readonly MethodBase? outer;
        protected readonly MemberInfo? inner;

        protected readonly InstructionList output = [];
        protected readonly int[]? innerParameterLocals;
        protected int resultLocalIndex = -1;
        protected readonly ILGenerator generator;

        protected RuleBuilder(ILGenerator generator) : this(generator, [])
        {
        }

        protected RuleBuilder(ILGenerator generator, List<Type> localTypes, MethodBase? outer = null, MemberInfo? inner = null)
        {
            this.generator = generator;
            this.outer = outer;
            this.inner = inner;
            outerParameterTypes = GetParameterTypes(outer);
            innerParameterTypes = GetParameterTypes(inner);
            output.LocalTypes = localTypes;

            if (innerParameterTypes != null)
                innerParameterLocals = new int[innerParameterTypes.Length];
        }

        public abstract IEnumerable<Rule> BuildRules();

        private static Type[]? GetParameterTypes(MemberInfo? member)
        {
            if (member == null)
                return null;

            return member switch
            {
                FieldInfo { IsStatic: true } => [],
                FieldInfo { IsStatic: false } field => [field.DeclaringType],
                MethodInfo { IsStatic: true } method => [.. method.GetParameters().Select(p => p.ParameterType)],
                MethodInfo { IsStatic: false } method => [method.DeclaringType, .. method.GetParameters().Select(p => p.ParameterType)],
                _ => throw new InvalidOperationException(),
            };
        }

        protected static OpCode OpcodeFor(MemberInfo callee)
        {
            return callee switch
            {
                FieldInfo { IsStatic: true } => OpCodes.Ldsfld,
                FieldInfo { IsStatic: false } => OpCodes.Ldfld,
                MethodBase { IsVirtual: true } => OpCodes.Callvirt,
                MethodBase { IsVirtual: false } => OpCodes.Call,
                _ => throw new InvalidOperationException(),
            };
        }

        protected void EmitParameterValue(ParameterBinding parameter)
        {
            Type parameterType = parameter.Parameter.ParameterType;

            switch (parameter.BindingType)
            {
                case BindingType.Parameter:
                case BindingType.Instance:
                {
                    EmitParameterLookup(parameterType);
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

            void EmitParameterLookup(Type type)
            {
                switch (parameter.Scope)
                {
                    case Scope.Outer: EmitCallerParameter(type, parameter.Index); break;
                    case Scope.Inner: EmitTargetParameter(type, parameter.Index); break;
                    default: throw new ArgumentOutOfRangeException(nameof(parameter.Scope));
                }
            }
        }

        private void EmitResult(Type parameterType)
        {
            output.EmitLoad(resultLocalIndex, parameterType.IsByRef);
        }

        private void EmitCallerParameter(Type type, int index)
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

        protected void EmitTargetParameter(Type type, int index)
        {
            if (innerParameterTypes == null)
                throw new InvalidOperationException("innerParameterTypes is null");
            if (innerParameterLocals == null)
                throw new InvalidOperationException("innerParameterLocals is null");

            output.EmitLoad(innerParameterLocals[index], type.IsByRef && !innerParameterTypes[index].IsByRef);
            if (!type.IsByRef && innerParameterTypes[index].IsByRef)
                output.Add(new(OpCodes.Ldobj, type));
        }
    }
}
