namespace Disharmony;

internal class CircumfixRuleBuilder : RuleBuilder
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
        prefixes = [.. patches.Where(patch => patch.patchType == PatchType.Prefix)];
        postfixes = [.. patches.Where(patch => patch.patchType == PatchType.Postfix)];

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
        bool canSkip = prefixes.Any(patch => patch.patch.ReturnType != typeof(void));

        if (canSkip && targetType != typeof(void) || prefixesUsingResult.Count > 0 || postfixesUsingResult.Count > 0)
        {
            resultLocal = output.AddLocal(targetType);

            if (prefixesUsingResult.Count > 0 &&
                !prefixesUsingResult[0].parameters.Where(a => a.bindingType == BindingType.Result).All(a => a.parameter.IsOut))
            {
                output.EmitLocalInitializer(resultLocal);
            }
        }

        foreach (var prefix in prefixes)
        {
            foreach (var parameter in prefix.parameters)
                EmitParameterValue(parameter);

            output.Add(CodeInstruction.Annotation($"{prefix.patchType} {prefix.patch.FullName}"));
            output.AddRange(prefix.patch.GetCodeInstructions());

            if (prefix.patch.ReturnType != typeof(void))
            {
                output.Add(new(OpCodes.Brfalse, skipLabel ??= generator.DefineLabel()));
            }
        }

        if (output.instructions.Count > 0)
        {
            yield return new Rule
            {
                mode = OutputMode.MethodPrefix,
                output = [.. output.instructions],
                name = "prefixes",
            };
            output.instructions.Clear();
        }

        if (postfixes.Count > 0)
        {
            returnLabel = generator.DefineLabel();

            yield return new Rule
            {
                min = 0,
                max = 0,
                mode = OutputMode.Replace,
                pattern = [new(OpCodes.Ret)],
                output = [new(OpCodes.Br, returnLabel)],
                name = "return",
            };
        }

        if (skipLabel == null && returnLabel == null && postfixes.Count == 0)
            yield break;

        if (returnLabel is { } label)
        {
            output.Add(new(OpCodes.Nop) { labels = [label] });

            if (resultLocal != null)
                output.Add(resultLocal.Store());
        }

        if (skipLabel is { } label2)
            output.Add(new(OpCodes.Nop) { labels = [label2] });

        foreach (var postfix in postfixes)
        {
            foreach (var parameter in postfix.parameters)
                EmitParameterValue(parameter);

            output.Add(CodeInstruction.Annotation($"{postfix.patchType} {postfix.patch.FullName}"));
            output.AddRange(postfix.patch.GetCodeInstructions());

            if (postfix.patch.ReturnType != typeof(void))
                output.Add(new(OpCodes.Pop));
        }

        if (resultLocal != null)
            output.Add(resultLocal.Load());

        output.Add(new(OpCodes.Ret));

        yield return new Rule
        {
            mode = OutputMode.MethodPostfix,
            output = [.. output.instructions],
            name = "postfixes",
        };
        output.instructions.Clear();
    }
}
