namespace Disharmony;

public static partial class Autopatcher
{
    private class RuleBuilder
    {
        private readonly Type[] outerParameterTypes;
        private readonly Type[] innerParameterTypes;
        private readonly int[] innerParameterLocals;
        private int resultLocalIndex = -1;
        private readonly Type targetType;

        private readonly ILGenerator generator;
        private readonly MethodBase outer;
        private readonly MemberInfo inner;
        private readonly MemberInfo? replacement;
        private readonly List<PatchInfo> prefixes;
        private readonly List<PatchInfo> postfixes;

        private readonly InstructionList output = new();

        private readonly bool debug;

        public RuleBuilder(
            ILGenerator generator,
            MethodBase outer,
            MemberInfo inner,
            MethodInfo? replacement,
            List<PatchInfo> prefixes,
            List<PatchInfo> postfixes,
            List<Type> localTypes)
        {
            this.generator = generator;
            this.outer = outer;
            this.inner = inner;
            this.replacement = replacement;
            this.prefixes = prefixes;
            this.postfixes = postfixes;

            output.LocalTypes.AddRange(localTypes);

            debug = prefixes.Any(p => p.debug) || postfixes.Any(p => p.debug);

            targetType = inner switch
            {
                FieldInfo field => field.FieldType,
                MethodInfo method => method.ReturnType,
                _ => throw new NotSupportedException(),
            };

            outerParameterTypes = GetParameterTypes(outer);
            innerParameterTypes = GetParameterTypes(inner);

            innerParameterLocals = new int[innerParameterTypes.Length];
        }

        private void EmitReplacement()
        {
            if (debug)
                LogDebugInfo();

            EmitPrelude();

            var prefixesUsingResult = prefixes.Where(patch => patch.HasBindingType(BindingType.Result)).ToList();
            var postfixesUsingResult = postfixes.Where(patch => patch.HasBindingType(BindingType.Result)).ToList();

            if (prefixesUsingResult.Count > 0 || postfixesUsingResult.Count > 0)
            {
                resultLocalIndex = output.AddLocal(targetType);

                if (prefixesUsingResult.Count > 0)
                {
                    if (!prefixesUsingResult[0].parameters.Single(a => a.BindingType == BindingType.Result).Parameter.IsOut)
                        output.EmitLocalInitializer(targetType, resultLocalIndex);
                }
            }

            Label? skipLabel = null;
            foreach (var prefix in prefixes)
            {
                MethodInfo patchMethod = prefix.patchMethod;
                foreach (var parameter in prefix.parameters)
                    EmitParameterValue(parameter);

                output.Add(CodeInstruction.Annotation($"{prefix.patchType} {patchMethod.FullName}"));
                output.Add(new(OpcodeFor(patchMethod), patchMethod));

                if (!patchMethod.ReturnType.IsVoid())
                {
                    output.Add(new(OpCodes.Brfalse, skipLabel ??= generator.DefineLabel()));
                }
            }

            for (int i = 0; i < innerParameterTypes.Length; i++)
            {
                EmitTargetParameter(innerParameterTypes[i], i);
            }

            if (replacement != null)
                output.Add(new(OpCodes.Call, replacement));
            else
                output.Add(new(OpcodeFor(inner), inner));

            if (skipLabel != null || postfixes.Count > 0)
            {
                if (resultLocalIndex >= 0)
                    output.Add(CodeInstruction.StoreLocal(resultLocalIndex));

                if (skipLabel is Label label)
                {
                    var branchTarget = new CodeInstruction(OpCodes.Nop);
                    branchTarget.labels.Add(label);
                    output.Add(branchTarget);
                }

                foreach (var postfix in postfixes)
                {
                    MethodInfo patchMethod = postfix.patchMethod;
                    foreach (var parameter in postfix.parameters)
                        EmitParameterValue(parameter);

                    output.Add(CodeInstruction.Annotation($"{postfix.patchType} {patchMethod.FullName}"));
                    output.Add(new(OpcodeFor(patchMethod), patchMethod));
                    if (!patchMethod.ReturnType.IsVoid())
                        output.Add(new(OpCodes.Pop));
                }

                if (resultLocalIndex >= 0)
                {
                    output.Add(CodeInstruction.LoadLocal(resultLocalIndex));
                }
            }
        }

        private void LogDebugInfo()
        {
            foreach (var patch in prefixes.Concat(postfixes))
            {
                FileLog.Log($"[{patch.patchType}] {patch.patchMethod.FullName}");
                foreach (var parameter in patch.parameters)
                {
                    string fields = "";
                    if (parameter.Fields is { Length: > 0 })
                        fields = $" Fields=[{string.Join(", ", parameter.Fields.Select(f => f.Name))}]";
                    FileLog.Log(
                        $"Name={parameter.Parameter.Name} BindingType={parameter.BindingType} Scope={parameter.Scope} Index={parameter.Index}{fields}");
                }
            }
        }

        private void EmitParameterValue(ParameterBinding parameter)
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
                    if (parameterType.IsByRef)
                        output.Add(CodeInstruction.LoadLocalAddress(parameter.Index));
                    else
                        output.Add(CodeInstruction.LoadLocal(parameter.Index));

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
            output.EmitLoad(parameterType, resultLocalIndex);
        }

        private void EmitCallerParameter(Type type, int index)
        {
            if (type.IsByRef && !outerParameterTypes[index].IsByRef)
                output.Add(new(OpCodes.Ldarga, index));
            else
                output.Add(CodeInstruction.LoadArgument(index));
            if (!type.IsByRef && outerParameterTypes[index].IsByRef)
                output.Add(new(OpCodes.Ldobj, type));
        }

        private void EmitTargetParameter(Type type, int index)
        {
            if (type.IsByRef && !innerParameterTypes[index].IsByRef)
                output.Add(CodeInstruction.LoadLocalAddress(innerParameterLocals[index]));
            else
                output.Add(CodeInstruction.LoadLocal(innerParameterLocals[index]));
            if (!type.IsByRef && innerParameterTypes[index].IsByRef)
                output.Add(new(OpCodes.Ldobj, type));
        }

        private void EmitPrelude()
        {
            // Save all parameters to local. The matcher will handle renumbering the locals to new
            // unused local indexes.
            for (int i = innerParameterTypes.Length - 1; i >= 0; i--)
            {
                innerParameterLocals[i] = output.AddLocal(innerParameterTypes[i]);
                output.Add(CodeInstruction.StoreLocal(innerParameterLocals[i]));
            }
        }

        private static Type[] GetParameterTypes(MemberInfo member)
        {
            return member switch
            {
                FieldInfo { IsStatic: true } => [],
                FieldInfo { IsStatic: false } field => [field.DeclaringType],
                MethodInfo { IsStatic: true } method => [.. method.GetParameters().Select(p => p.ParameterType)],
                MethodInfo { IsStatic: false } method => [method.DeclaringType, .. method.GetParameters().Select(p => p.ParameterType)],
                _ => throw new InvalidOperationException(),
            };
        }

        private static OpCode OpcodeFor(MemberInfo callee)
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

        public InstructionMatcher.Rule BuildRule()
        {
            List<CodeInstruction> pattern =
            [
                new(OpcodeFor(inner), inner),
            ];

            EmitReplacement();

            return new InstructionMatcher.Rule
            {
                Min = 1,
                Max = 0,
                Mode = InstructionMatcher.OutputMode.Replace,
                Pattern = pattern.ToArray(),
                Output = output.Instructions.ToArray(),
                LocalTypes = output.LocalTypes.ToArray(),
                Name = inner.FullName,
            };
        }
    }
}
