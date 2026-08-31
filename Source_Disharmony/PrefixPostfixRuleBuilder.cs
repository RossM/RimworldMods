using System.Runtime.ExceptionServices;

namespace Disharmony;

internal abstract class PrefixPostfixRuleBuilder(RuleBuilderContext context, Invocation outer) : RuleBuilder(context, outer)
{
    private bool ExceptionBlockNeeded => postfixes.Any(p => p.AlwaysRun);

    private bool ResultLocalNeeded =>
        prefixes.Any(patch => patch.patch.ReturnType != typeof(void)) ||
        prefixes.Any(patch => patch.HasBindingType(BindingType.Result)) ||
        postfixes.Any(patch => patch.HasBindingType(BindingType.Result));

    protected Type targetType = null!;
    protected List<PatchInfo> prefixes = null!;
    protected List<PatchInfo> postfixes = null!;
    protected Label? skipLabel = null;

    protected void InitializeLocals()
    {
        if (targetType != typeof(void) && ResultLocalNeeded)
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

        if (ExceptionBlockNeeded)
        {
            exceptionLocal = output.AddLocal(typeof(Exception));
            dispatchInfoLocal = output.AddLocal(typeof(ExceptionDispatchInfo));
            output.EmitLocalInitializer(exceptionLocal);
            output.EmitLocalInitializer(dispatchInfoLocal);
        }
    }

    protected void EmitPrefixes()
    {
        foreach (var prefix in prefixes.Where(p => p.AlwaysRun))
        {
            RunPatch(prefix);

            if (prefix.patch.ReturnType != typeof(void))
                output.Add(new(OpCodes.Pop));
        }

        if (postfixes.Any(p => p.AlwaysRun))
            output.Add(new CodeInstruction(OpCodes.Nop).WithBlocks(new ExceptionBlock(ExceptionBlockType.BeginExceptionBlock)));

        foreach (var prefix in prefixes.Where(p => !p.AlwaysRun))
        {
            RunPatch(prefix);

            if (prefix.patch.ReturnType != typeof(void))
                output.Add(new(OpCodes.Brfalse, skipLabel ??= generator.DefineLabel()));
        }
    }

    protected void EmitPostfixes()
    {
        if (skipLabel is { } label)
            output.Add(new(OpCodes.Nop) { labels = [label] });

        foreach (var postfix in postfixes.Where(p => !p.AlwaysRun))
        {
            RunPatch(postfix);

            if (postfix.patch.ReturnType != typeof(void))
                output.Add(new(OpCodes.Pop));
        }

        if (ExceptionBlockNeeded)
        {
            // Emitted code:
            //      catch (Exception e) {
            //          exception = e;
            //          dispatchInfo = ExceptionDispatchInfo.Capture(e);
            //      }

            output.Add(new CodeInstruction(OpCodes.Nop).WithBlocks(
                new ExceptionBlock(ExceptionBlockType.BeginCatchBlock, typeof(Exception))));
            output.Add(new(OpCodes.Dup));
            output.Add(exceptionLocal!.Store());
            output.Add(new(OpCodes.Call, InfoOf.ExceptionDispatchInfo_Capture));
            output.Add(dispatchInfoLocal!.Store());
            output.Add(new CodeInstruction(OpCodes.Nop).WithBlocks(new ExceptionBlock(ExceptionBlockType.EndExceptionBlock)));
        }

        foreach (var postfix in postfixes.Where(p => p.AlwaysRun))
        {
            RunPatch(postfix);

            if (postfix.patch.ReturnType != typeof(void))
                output.Add(new(OpCodes.Pop));
        }

        if (ExceptionBlockNeeded)
        {
            // Emitted code:
            //      if (exception != null)
            //          RuntimeHelpers.RethrowException(exception, dispatchInfo);

            Label noThrowLabel = output.generator.DefineLabel();
            output.Add(exceptionLocal!.Load());
            output.Add(new(OpCodes.Brfalse_S, noThrowLabel));
            output.Add(exceptionLocal!.Load());
            output.Add(dispatchInfoLocal!.Load());
            output.Add(new(OpCodes.Call, InfoOf.RethrowException));
            output.Add(new CodeInstruction(OpCodes.Nop).WithLabels(noThrowLabel));
        }

        if (resultLocal != null)
            output.Add(resultLocal.Load());
    }

    private void RunPatch(PatchInfo prefix)
    {
        foreach (var parameter in prefix.parameters)
            EmitParameterValue(parameter);

        output.Add(CodeInstruction.Annotation($"{prefix.patchType} {prefix.patch.FullName}"));
        output.AddRange(prefix.patch.GetCodeInstructions());
    }
}
