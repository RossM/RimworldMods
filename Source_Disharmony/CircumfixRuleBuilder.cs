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
        InitializeResultLocal();

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

        if (returnLabel is { } label)
        {
            output.Add(new(OpCodes.Nop) { labels = [label] });

            // If returnLabel is null then we can never fall through to the skip label. Adding the store in that
            // case would result in a basic block with no entry, so it will be assumed to have an empty stack,
            // which causes problems in the optimizer.
            if (resultLocal != null)
                output.Add(resultLocal.Store());
        }

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
