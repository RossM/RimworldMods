namespace Disharmony;

/// <summary>
///     This class generates rules implementing <see cref="PatchType.Prefix" /> and <see cref="PatchType.Postfix" />
///     patches for a method.
/// </summary>
internal class CircumfixRuleBuilder : PrefixPostfixRuleBuilder
{
    protected Label? returnLabel = null;

    public CircumfixRuleBuilder(
        RuleBuilderContext context,
        Invocation outer,
        IReadOnlyList<PatchInfo> patches) : base(context, outer)
    {
        prefixes =
        [
            // Prefixes are sorted by priority and then reversed, so prefix-postfix pairs will nest naturally
            // even if priority isn't set
            .. patches.Where(patch => patch is { patchType: PatchType.Prefix, inner: EmptyInvocation })
                .OrderBy(patch => patch.priority).Reverse(),
        ];
        postfixes =
        [
            .. patches.Where(patch => patch is { patchType: PatchType.Postfix, inner: EmptyInvocation })
                .OrderBy(patch => patch.priority),
        ];

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
        InitializeLocals();

        EmitPrefixes();

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

        if (skipLabel == null && postfixes.Count == 0)
            yield break;

        if (postfixes.Count > 0)
        {
            returnLabel = generator.DefineLabel();

            // We need to do the store before each 'ret', instead of after the label, because
            // otherwise a method with no 'ret's will result in a dead basic block that pops
            // a value from an empty stack, which Mono rejects.
            CodeInstruction[] retOutput =
                resultLocal != null ? [resultLocal.Store(), new(OpCodes.Br, returnLabel)] : [new(OpCodes.Br, returnLabel)];

            yield return new Rule
            {
                min = 0,
                max = 0,
                mode = OutputMode.Replace,
                pattern = [new(OpCodes.Ret)],
                output = retOutput,
                name = "return",
            };
        }

        if (returnLabel is { } label)
            output.Add(new(OpCodes.Nop) { labels = [label] });

        EmitPostfixes();

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
