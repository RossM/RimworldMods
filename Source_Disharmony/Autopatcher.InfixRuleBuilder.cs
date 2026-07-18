namespace Disharmony;

public static partial class Autopatcher
{
    private class InfixRuleBuilder : RuleBuilder
    {
        private readonly Type targetType;

        private readonly List<PatchInfo> innerPrefixes;
        private readonly List<PatchInfo> innerPostfixes;

        public InfixRuleBuilder(
            RuleBuilderContext context,
            MethodBase outer,
            MemberInfo inner,
            List<PatchInfo> patches) : base(context, outer, inner)
        {
            innerPrefixes = patches.Where(patch => patch.patchType == PatchType.InnerPrefix).ToList();
            innerPostfixes = patches.Where(patch => patch.patchType == PatchType.InnerPostfix).ToList();

            targetType = inner switch
            {
                FieldInfo field => field.FieldType,
                MethodInfo method => method.ReturnType,
                _ => throw new NotSupportedException(),
            };
        }

        private void EmitReplacement()
        {
            if (inner is null)
                throw new InvalidOperationException();

            EmitPrelude();

            var prefixesUsingResult = innerPrefixes.Where(patch => patch.HasBindingType(BindingType.Result)).ToList();
            var postfixesUsingResult = innerPostfixes.Where(patch => patch.HasBindingType(BindingType.Result)).ToList();
            bool canSkip = innerPrefixes.Any(patch => !patch.patchMethod.ReturnType.IsVoid());

            if (canSkip && !targetType.IsVoid() || prefixesUsingResult.Count > 0 || postfixesUsingResult.Count > 0)
            {
                resultLocalIndex = output.AddLocal(targetType);

                if (prefixesUsingResult.Count > 0 &&
                    !prefixesUsingResult[0].parameters.Single(a => a.BindingType == BindingType.Result).Parameter.IsOut)
                {
                    output.EmitLocalInitializer(resultLocalIndex);
                }
            }

            Label? skipLabel = null;
            foreach (var prefix in innerPrefixes)
            {
                MethodInfo patchMethod = prefix.patchMethod;
                foreach (var parameter in prefix.parameters)
                    EmitParameterValue(parameter);

                output.Add(CodeInstruction.Annotation($"{prefix.patchType} {patchMethod.FullName}"));
                output.Add(InstructionFor(patchMethod));

                if (!patchMethod.ReturnType.IsVoid())
                {
                    output.Add(new(OpCodes.Brfalse, skipLabel ??= generator.DefineLabel()));
                }
            }

            for (int i = 0; i < innerParameterTypes!.Length; i++)
            {
                EmitTargetParameter(innerParameterTypes![i], i);
            }

            output.Add(InstructionFor(inner));

            if (skipLabel != null || innerPostfixes.Count > 0)
            {
                if (resultLocalIndex >= 0)
                    output.Add(CodeInstruction.StoreLocal(resultLocalIndex));

                if (skipLabel is Label label)
                    output.Add(new(OpCodes.Nop) { labels = [label] });

                foreach (var postfix in innerPostfixes)
                {
                    MethodInfo patchMethod = postfix.patchMethod;
                    foreach (var parameter in postfix.parameters)
                        EmitParameterValue(parameter);

                    output.Add(CodeInstruction.Annotation($"{postfix.patchType} {patchMethod.FullName}"));
                    output.Add(InstructionFor(patchMethod));
                    if (!patchMethod.ReturnType.IsVoid())
                        output.Add(new(OpCodes.Pop));
                }

                if (resultLocalIndex >= 0)
                    output.EmitLoad(resultLocalIndex);
            }
        }

        private void EmitPrelude()
        {
            // Save all parameters to local. The matcher will handle renumbering the locals to new
            // unused local indexes.
            for (int i = innerParameterTypes!.Length - 1; i >= 0; i--)
            {
                innerParameterLocals![i] = output.AddLocal(innerParameterTypes![i]);
                output.Add(CodeInstruction.StoreLocal(innerParameterLocals[i]));
            }
        }

        public override IEnumerable<Rule> BuildRules()
        {
            if (inner is null)
                throw new InvalidOperationException();

            List<CodeInstruction> pattern =
            [
                InstructionFor(inner),
            ];

            EmitReplacement();

            yield return new Rule
            {
                Min = 1,
                Max = 0,
                Mode = InstructionMatcher.OutputMode.Replace,
                Pattern = pattern.ToArray(),
                Output = output.Instructions.ToArray(),
                Name = inner.FullName,
            };
        }
    }
}
