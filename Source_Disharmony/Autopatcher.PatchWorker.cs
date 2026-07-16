namespace Disharmony;

public static partial class Autopatcher
{
    private class PatchWorker(PatchRegistry registry, MethodInfo patchedMethod, bool useTrampolines = true)
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

            patcher.ApplyPatch(patchedMethod, matchers.ToArray(), useTrampolines);
        }

        private InstructionMatcher? MakeInlineInstructionMatcher()
        {
            ILGenerator generator = PatchProcessor.CreateILGenerator();
            List<Rule> rules = [];

            foreach (var patch in patches.Where(p => p.inline))
            {
                var inlineRuleBuilder = new InlineRuleBuilder(generator, patch);
                rules.AddRange(inlineRuleBuilder.BuildRules());
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
            ILGenerator generator = PatchProcessor.CreateILGenerator();

            List<RuleBuilder> ruleBuilders = [];
            List<Type> localTypes = [];

            StateBuilder stateBuilder = new(generator, localTypes);
            stateBuilder.AssignStateVariableIndexes(patches);
            ruleBuilders.Add(stateBuilder);

            ruleBuilders.Add(new CircumfixRuleBuilder(generator, patchedMethod, patches, localTypes));

            foreach (IGrouping<MemberInfo, PatchInfo> targetGroup in patches.Where(patch => patch.inner != null)
                         .GroupBy(patch => patch.inner!))
                ruleBuilders.Add(new InfixRuleBuilder(generator, patchedMethod, targetGroup.Key, targetGroup.ToList(), localTypes));

            List<Rule> rules = [];
            List<Label> labels = [];
            foreach (var ruleBuilder in ruleBuilders)
            {
                rules.AddRange(ruleBuilder.BuildRules());
                labels.AddRange(ruleBuilder.CrossRuleLabels);
            }

            if (rules.Count == 0)
                throw new InvalidOperationException("No rules generated");

            var patchMatcher = new InstructionMatcher
            {
                Rules = rules,
                CrossRuleLocalTypes = localTypes,
                CrossRuleLabels = labels,
            };

            return patchMatcher;
        }
    }
}
