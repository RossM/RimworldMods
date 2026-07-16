using JetBrains.Annotations;

namespace Disharmony;

public static partial class Autopatcher
{
    private class PatchWorker(PatchRegistry registry, MethodInfo patchedMethod)
    {
        private readonly List<PatchInfo> patches = registry.PatchesByMethod[patchedMethod];

        public void UpdateMethod()
        {
            MethodInfo? iteratorMethod = patchedMethod.GetIteratorImplementation();

            if (iteratorMethod is not null)
            {
                if (patches.Any(p => p.HasBindingType(BindingType.State)))
                    throw new NotSupportedException("__state is not supported for compiler-generated iterator methods");
                if (patches.Any(p => p.HasBindingType(BindingType.Result)))
                    throw new NotSupportedException("__result is not supported for compiler-generated iterator methods");

                throw new NotImplementedException();
            }

            List<InstructionMatcher> matchers = [];

            InstructionMatcher patchMatcher = MakePatchInstructionMatcher();
            matchers.Add(patchMatcher);

            InstructionMatcher? inlineMatcher = MakeInlineInstructionMatcher();
            if (inlineMatcher != null)
                matchers.Add(inlineMatcher);

            ApplyPatch(matchers.ToArray());
        }

        private void ApplyPatch(InstructionMatcher[] matchersArray)
        {
            HarmonyMethod? harmonyMethod;
            if (patcher.TryUpdateTranspiler(patchedMethod, matchersArray))
            {
                FileLog.Log($"# GetHarmonyMethod: Reusing transpiler for {patchedMethod.FullName}");

                // Using null as our HarmonyMethod will cause Harmony to simply rerun the patch, including
                // the updated transpiler.
                harmonyMethod = null;
            }
            else
            {
                MethodInfo transpiler = patcher.MakeTranspiler(matchersArray,
                    $"{patchedMethod.DeclaringType?.FullName?.Replace('.', '_')}_{patchedMethod.Name}_Transpiler", patchedMethod);

                bool debug = registry.PatchesByMethod[patchedMethod].Any(p => p.debug);

                harmonyMethod = new(transpiler, priority: Priority.LowerThanNormal) { debug = debug };
            }

            if (patcher.useTrampolines)
                patcher.AddTranspilerWithoutPatching(patchedMethod, harmonyMethod);
            else
                patcher.RunPatch(patchedMethod, harmonyMethod);
        }

        private InstructionMatcher? MakeInlineInstructionMatcher()
        {
            List<InstructionMatcher.Rule> rules = [];

            foreach (var patch in patches.Where(p => p.inline))
            {
                var ruleBuilder = new InlineRuleBuilder(patch);

                var rule = ruleBuilder.BuildRule();
                if (rule != null)
                    rules.Add(rule);
            }

            InstructionMatcher? inlineMatcher = null;
            if (rules.Count > 0)
            {
                inlineMatcher = new InstructionMatcher
                {
                    Rules = rules,
                };
            }

            return inlineMatcher;
        }

        private InstructionMatcher MakePatchInstructionMatcher()
        {
            List<InstructionMatcher.Rule> rules = new();

            StateBuilder stateBuilder = new();

            stateBuilder.AssignStateVariableIndexes(patches);

            if (stateBuilder.LocalTypes.Count > 0)
                rules.Add(stateBuilder.BuildRule());

            foreach (IGrouping<MemberInfo, PatchInfo> targetGroup in patches.GroupBy(patch => patch.inner))
            {
                var inner = targetGroup.Key;

                var ruleBuilder = new InfixRuleBuilder(patchedMethod, inner, targetGroup.ToList(), stateBuilder.LocalTypes);

                rules.Add(ruleBuilder.BuildRule());
            }

            var patchMatcher = new InstructionMatcher
            {
                Rules = rules,
                CrossRuleLocalTypes = stateBuilder.LocalTypes,
            };
            return patchMatcher;
        }
    }
}
