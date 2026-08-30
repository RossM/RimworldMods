namespace Disharmony;

internal abstract class PrefixPostfixRuleBuilder(RuleBuilderContext context, Invocation outer) : RuleBuilder(context, outer)
{
    protected Type targetType = null!;
    protected List<PatchInfo> prefixes = null!;
    protected List<PatchInfo> postfixes = null!;
    protected Label? skipLabel = null;

    protected void InitializeResultLocal()
    {
        var prefixUsesResult = prefixes.Any(patch => patch.HasBindingType(BindingType.Result));
        var postfixUsesResult = postfixes.Any(patch => patch.HasBindingType(BindingType.Result));
        bool canSkip = prefixes.Any(patch => patch.patch.ReturnType != typeof(void));

        if (targetType != typeof(void) && (canSkip || prefixUsesResult || postfixUsesResult))
        {
            resultLocal = output.AddLocal(targetType);

            // No initialization is needed if there are no prefixes (the function will initialize), or if 
            // there is a prefix which can't be skipped and definitely sets the result using an out variable.
            // Honestly this is overkill as the result local will be zero-initialized anyway.
            var firstResultRelevantPrefix = prefixes.FirstOrDefault(patch =>
                patch.patch.ReturnType != typeof(void) || patch.HasBindingType(BindingType.Result));
            if (firstResultRelevantPrefix.parameters != null && !firstResultRelevantPrefix.parameters
                    .Where(a => a.bindingType == BindingType.Result).All(a => a.parameter.IsOut))
                output.EmitLocalInitializer(resultLocal);
        }
    }
}
