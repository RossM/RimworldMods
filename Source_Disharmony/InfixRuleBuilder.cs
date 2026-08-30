namespace Disharmony;

/// <summary>
///     This class generates rules implementing inner <see cref="PatchType.Prefix" /> and
///     <see cref="PatchType.Postfix" /> patches for a method.
/// </summary>
internal class InfixRuleBuilder : RuleBuilder
{
    private readonly Type targetType;

    private readonly List<PatchInfo> innerPrefixes;
    private readonly List<PatchInfo> innerPostfixes;
    private readonly Invocation inner;

    private readonly Type[] innerParameterTypes;
    private readonly LocalTracker[] innerParameterLocals;

    public InfixRuleBuilder(
        RuleBuilderContext context,
        Invocation outer,
        Invocation inner,
        List<PatchInfo> patches) : base(context, outer)
    {
        innerPrefixes =
        [
            // Prefixes are sorted by priority and then reversed, so prefix-postfix pairs will nest naturally
            // even if priority isn't set
            .. patches.Where(patch => patch is { patchType: PatchType.Prefix, inner: not EmptyInvocation })
                .OrderByDescending(patch => patch.priority).Reverse(),
        ];
        innerPostfixes =
        [
            .. patches.Where(patch => patch is { patchType: PatchType.Postfix, inner: not EmptyInvocation })
                .OrderByDescending(patch => patch.priority),
        ];

        this.inner = inner;

        innerParameterTypes = this.inner.ParameterTypes;
        innerParameterLocals = new LocalTracker[innerParameterTypes.Length];

        targetType = this.inner.ReturnType;
    }

    private void EmitReplacement()
    {
        EmitPrelude();

        var prefixesUsingResult = innerPrefixes.Where(patch => patch.HasBindingType(BindingType.Result)).ToList();
        var postfixesUsingResult = innerPostfixes.Where(patch => patch.HasBindingType(BindingType.Result)).ToList();
        bool canSkip = innerPrefixes.Any(patch => patch.patch.ReturnType != typeof(void));

        if ((canSkip && targetType != typeof(void)) || prefixesUsingResult.Count > 0 || postfixesUsingResult.Count > 0)
        {
            resultLocal = output.AddLocal(targetType);

            if (prefixesUsingResult.Count > 0 &&
                !prefixesUsingResult[0].parameters.Where(a => a.bindingType == BindingType.Result).All(a => a.parameter.IsOut))
                output.EmitLocalInitializer(resultLocal);
        }

        Label? skipLabel = null;
        foreach (var prefix in innerPrefixes)
        {
            foreach (var parameter in prefix.parameters)
                EmitParameterValue(parameter);

            output.Add(CodeInstruction.Annotation($"{prefix.patchType} {prefix.patch.FullName}"));
            output.AddRange(prefix.patch.GetCodeInstructions());

            if (prefix.patch.ReturnType != typeof(void))
                output.Add(new(OpCodes.Brfalse, skipLabel ??= generator.DefineLabel()));
        }

        for (int i = 0; i < innerParameterTypes.Length; i++)
        {
            Type type = innerParameterTypes[i];
            EmitInnerParameter(i, type);
        }

        output.AddRange(inner.GetCodeInstructions());

        if (skipLabel != null || innerPostfixes.Count > 0)
        {
            if (resultLocal != null)
                output.Add(resultLocal.Store());

            if (skipLabel is Label label)
                output.Add(new(OpCodes.Nop) { labels = [label] });

            foreach (var postfix in innerPostfixes)
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
        }
    }

    private void EmitPrelude()
    {
        // Save all parameters to local. The matcher will handle renumbering the locals to new
        // unused local indexes.
        for (int i = innerParameterTypes.Length - 1; i >= 0; i--)
        {
            innerParameterLocals[i] = output.AddLocal(innerParameterTypes[i]);
            output.Add(innerParameterLocals[i].Store());
        }
    }

    protected override Type GetParameterType(ParameterBinding parameter)
    {
        return parameter.scope switch
        {
            Scope.Outer => outerParameterTypes[parameter.index],
            Scope.Inner => innerParameterTypes[parameter.index],
            _ => throw new ArgumentOutOfRangeException(nameof(parameter.scope)),
        };
    }

    protected override void EmitParameterLookup(Scope scope, int index, Type resultType)
    {
        switch (scope)
        {
            case Scope.Outer: EmitOuterParameter(index, resultType); break;
            case Scope.Inner: EmitInnerParameter(index, resultType); break;
            case Scope.Any:
            default:
                throw new ArgumentOutOfRangeException(nameof(scope));
        }
    }


    public override IEnumerable<Rule> BuildRules()
    {
        List<CodeInstruction> pattern =
        [
            .. inner.GetCodeInstructions(),
        ];

        EmitReplacement();

        yield return new Rule
        {
            min = 1,
            max = 0,
            mode = OutputMode.Replace,
            pattern = [.. pattern],
            output = [.. output.instructions],
            name = inner.FullName,
        };
    }

    private void EmitInnerParameter(int index, Type resultType)
    {
        Type parameterType = innerParameterTypes[index];
        output.Add(innerParameterLocals[index].Load(resultType.IsByRef && !parameterType.IsByRef));
        if (!resultType.IsByRef && parameterType.IsByRef)
            output.Add(new(OpCodes.Ldobj, parameterType.GetElementType()));
    }
}
