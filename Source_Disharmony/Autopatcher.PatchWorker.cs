namespace Disharmony;

public static partial class Autopatcher
{
    private class PatchWorker(PatchRegistry registry, MethodInfo patchedMethod, bool useTrampolines = true)
    {
        private readonly List<PatchInfo> patches = registry.PatchesByMethod[patchedMethod];

        public void UpdateMethod()
        {
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
            List<Rule> rules = [];

            var context = new RuleBuilderContext();

            foreach (var patch in patches.Where(p => p.inline))
            {
                var inlineRuleBuilder = new InlineRuleBuilder(context, patch);
                rules.AddRange(inlineRuleBuilder.BuildRules());
            }

            InstructionMatcher? inlineMatcher = null;
            if (rules.Count > 0)
            {
                inlineMatcher = new InstructionMatcher
                {
                    Rules = rules,
                    CrossRuleLocalTypes = context.localTypes,
                };
            }

            return inlineMatcher;
        }

        private InstructionMatcher MakePatchInstructionMatcher()
        {
            List<RuleBuilder> ruleBuilders = [];

            var context = new RuleBuilderContext();

            StateBuilder stateBuilder = new(context);
            stateBuilder.AssignStateVariableIndexes(patches);
            ruleBuilders.Add(stateBuilder);

            ruleBuilders.Add(new CircumfixRuleBuilder(context, patchedMethod, patches));

            foreach (IGrouping<Invocation, PatchInfo> targetGroup in patches.Where(patch => patch.inner != null)
                         .GroupBy(patch => patch.inner!))
            {
                Invocation inner = targetGroup.Key;
                ruleBuilders.Add(new InfixRuleBuilder(context, patchedMethod, inner, targetGroup.ToList()));
            }

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
                CrossRuleLocalTypes = context.localTypes,
                CrossRuleLabels = labels,
            };

            return patchMatcher;
        }
    }
}
