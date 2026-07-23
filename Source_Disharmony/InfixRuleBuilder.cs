namespace Disharmony;

internal class InfixRuleBuilder : RuleBuilder
{
    private readonly Type targetType;

    private readonly List<PatchInfo> innerPrefixes;
    private readonly List<PatchInfo> innerPostfixes;
    private readonly Invocation inner;

    private readonly Type[] innerParameterTypes;
    private readonly int[] innerParameterLocals;

    public InfixRuleBuilder(
        RuleBuilderContext context,
        Invocation outer,
        Invocation inner,
        List<PatchInfo> patches) : base(context, outer)
    {
        innerPrefixes = patches.Where(patch => patch.patchType == PatchType.InnerPrefix).ToList();
        innerPostfixes = patches.Where(patch => patch.patchType == PatchType.InnerPostfix).ToList();

        this.inner = inner;

        innerParameterTypes = this.inner.ParameterTypes;
        innerParameterLocals = new int[innerParameterTypes.Length];

        targetType = this.inner.ReturnType;
    }

    private void EmitReplacement()
    {
        EmitPrelude();

        var prefixesUsingResult = innerPrefixes.Where(patch => patch.HasBindingType(BindingType.Result)).ToList();
        var postfixesUsingResult = innerPostfixes.Where(patch => patch.HasBindingType(BindingType.Result)).ToList();
        bool canSkip = innerPrefixes.Any(patch => !patch.patch.ReturnType.IsVoid());

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
            foreach (var parameter in prefix.parameters)
                EmitParameterValue(parameter);

            output.Add(CodeInstruction.Annotation($"{prefix.patchType} {prefix.patch.FullName}"));
            output.AddRange(prefix.patch.GetCodeInstructions());

            if (!prefix.patch.ReturnType.IsVoid())
            {
                output.Add(new(OpCodes.Brfalse, skipLabel ??= generator.DefineLabel()));
            }
        }

        for (int i = 0; i < innerParameterTypes.Length; i++)
        {
            Type type = innerParameterTypes[i];
            EmitInnerParameter(i, type);
        }

        output.AddRange(inner.GetCodeInstructions());

        if (skipLabel != null || innerPostfixes.Count > 0)
        {
            if (resultLocalIndex >= 0)
                output.Add(CodeInstruction.StoreLocal(resultLocalIndex));

            if (skipLabel is Label label)
                output.Add(new(OpCodes.Nop) { labels = [label] });

            foreach (var postfix in innerPostfixes)
            {
                foreach (var parameter in postfix.parameters)
                    EmitParameterValue(parameter);

                output.Add(CodeInstruction.Annotation($"{postfix.patchType} {postfix.patch.FullName}"));
                output.AddRange(postfix.patch.GetCodeInstructions());
                if (!postfix.patch.ReturnType.IsVoid())
                    output.Add(new(OpCodes.Pop));
            }

            if (resultLocalIndex >= 0)
                output.Add(CodeInstruction.LoadLocal(resultLocalIndex));
        }
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

    protected override Type GetParameterType(ParameterBinding parameter)
    {
        switch (parameter.Scope)
        {
            case Scope.Outer: return outerParameterTypes[parameter.Index];
            case Scope.Inner: return innerParameterTypes[parameter.Index];
            default: throw new ArgumentOutOfRangeException(nameof(parameter.Scope));
        }
    }

    protected override void EmitParameterLookup(ParameterBinding parameter, Type resultType)
    {
        switch (parameter.Scope)
        {
            case Scope.Outer: EmitOuterParameter(parameter.Index, resultType); break;
            case Scope.Inner: EmitInnerParameter(parameter.Index, resultType); break;
            default: throw new ArgumentOutOfRangeException(nameof(parameter.Scope));
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
            Min = 1,
            Max = 0,
            Mode = InstructionMatcher.OutputMode.Replace,
            Pattern = pattern.ToArray(),
            Output = output.Instructions.ToArray(),
            Name = inner.FullName,
        };
    }

    private void EmitInnerParameter(int index, Type resultType)
    {
        Type parameterType = innerParameterTypes[index];
        output.Add(CodeInstruction.LoadLocal(innerParameterLocals[index], resultType.IsByRef && !parameterType.IsByRef));
        if (!resultType.IsByRef && parameterType.IsByRef)
            output.Add(new(OpCodes.Ldobj, parameterType.GetElementType()));
    }
}
