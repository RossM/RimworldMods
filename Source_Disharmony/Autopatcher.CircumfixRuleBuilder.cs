namespace Disharmony;

public static partial class Autopatcher
{
    private class CircumfixRuleBuilder : RuleBuilder
    {
        private readonly Type targetType;

        private readonly List<PatchInfo> prefixes;
        private readonly List<PatchInfo> postfixes;
        private Label? skipLabel = null;
        private Label? returnLabel = null;

        public CircumfixRuleBuilder(
            RuleBuilderContext context,
            Invocation outer,
            List<PatchInfo> patches) : base(context, outer)
        {
            prefixes = patches.Where(patch => patch.patchType == PatchType.Prefix).ToList();
            postfixes = patches.Where(patch => patch.patchType == PatchType.Postfix).ToList();

            targetType = outer.ReturnType;
        }

        public override IEnumerable<Label> CrossRuleLabels
        {
            get
            {
                if (skipLabel is { } label)
                    yield return label;
                if (returnLabel is { } label2)
                    yield return label2;
            }
        }

        public override IEnumerable<Rule> BuildRules()
        {
            var prefixesUsingResult = prefixes.Where(patch => patch.HasBindingType(BindingType.Result)).ToList();
            var postfixesUsingResult = postfixes.Where(patch => patch.HasBindingType(BindingType.Result)).ToList();
            bool canSkip = prefixes.Any(patch => !patch.patchMethod.ReturnType.IsVoid());

            if (canSkip && !targetType.IsVoid() || prefixesUsingResult.Count > 0 || postfixesUsingResult.Count > 0)
            {
                resultLocalIndex = output.AddLocal(targetType);

                if (prefixesUsingResult.Count > 0 &&
                    !prefixesUsingResult[0].parameters.Single(a => a.BindingType == BindingType.Result).Parameter.IsOut)
                {
                    output.EmitLocalInitializer(resultLocalIndex);
                }
            }

            foreach (var prefix in prefixes)
            {
                MethodInvocation patchMethod = prefix.patchMethod;
                foreach (var parameter in prefix.parameters)
                    EmitParameterValue(parameter);

                output.Add(CodeInstruction.Annotation($"{prefix.patchType} {patchMethod.FullName}"));
                output.Add(patchMethod.GetCodeInstruction());

                if (!patchMethod.ReturnType.IsVoid())
                {
                    output.Add(new(OpCodes.Brfalse, skipLabel ??= generator.DefineLabel()));
                }
            }

            if (output.Instructions.Count > 0)
            {
                yield return new Rule
                {
                    Mode = InstructionMatcher.OutputMode.MethodPrefix,
                    Output = output.Instructions.ToArray(),
                    Name = "prefixes",
                };
                output.Instructions.Clear();
            }

            if (postfixes.Count > 0)
            {
                returnLabel = generator.DefineLabel();

                yield return new Rule
                {
                    Min = 0,
                    Max = 0,
                    Mode = InstructionMatcher.OutputMode.Replace,
                    Pattern = [new(OpCodes.Ret)],
                    Output = [new(OpCodes.Br, returnLabel)],
                    Name = "return",
                };
            }

            if (skipLabel != null || returnLabel != null || postfixes.Count > 0)
            {
                if (returnLabel is { } label)
                {
                    output.Add(new(OpCodes.Nop) { labels = [label] });

                    if (resultLocalIndex >= 0)
                        output.Add(CodeInstruction.StoreLocal(resultLocalIndex));
                }

                if (skipLabel is { } label2)
                    output.Add(new(OpCodes.Nop) { labels = [label2] });

                foreach (var postfix in postfixes)
                {
                    MethodInvocation patchMethod = postfix.patchMethod;
                    foreach (var parameter in postfix.parameters)
                        EmitParameterValue(parameter);

                    output.Add(CodeInstruction.Annotation($"{postfix.patchType} {patchMethod.FullName}"));
                    output.Add(patchMethod.GetCodeInstruction());

                    if (!patchMethod.ReturnType.IsVoid())
                        output.Add(new(OpCodes.Pop));
                }

                if (resultLocalIndex >= 0)
                    output.EmitLoad(resultLocalIndex);

                output.Add(new(OpCodes.Ret));

                yield return new Rule
                {
                    Mode = InstructionMatcher.OutputMode.MethodPostfix,
                    Output = output.Instructions.ToArray(),
                    Name = "postfixes",
                };
                output.Instructions.Clear();
            }
        }
    }
}
