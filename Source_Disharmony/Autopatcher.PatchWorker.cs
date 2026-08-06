namespace Disharmony;

public static partial class Autopatcher
{
    internal class PatchWorker(PatchRegistry registry, MethodBaseInvocation patchedMethod, bool useTrampolines = true)
    {
        private readonly Invocation outer = patchedMethod;
        private readonly List<PatchInfo> patches = registry.GetPatchesFor(patchedMethod);

        public void UpdateMethod()
        {
            if (patches.Count == 0)
            {
                patcher.Unpatch(patchedMethod.MethodBase);
                return;
            }

            List<Ruleset> matchers = [];

            Ruleset patchMatcher = MakePatchInstructionMatcher();
            matchers.Add(patchMatcher);

            Ruleset? inlineMatcher = MakeInlineInstructionMatcher();
            if (inlineMatcher != null)
                matchers.Add(inlineMatcher);

            patcher.ApplyPatch(patchedMethod, [.. matchers], useTrampolines);
        }

        private Ruleset? MakeInlineInstructionMatcher()
        {
            List<Rule> rules = [];

            var context = new RuleBuilderContext();

            foreach (var patch in patches.Where(p => p.Inline))
            {
                if (patch.patch is not MethodInvocation method)
                    continue;
                var inlineRuleBuilder = new InlineRuleBuilder(context, method);
                rules.AddRange(inlineRuleBuilder.BuildRules());
            }

            Ruleset? inlineMatcher = null;
            if (rules.Count > 0)
            {
                inlineMatcher = new Ruleset
                {
                    rules = rules,
                    crossRuleLocalTypes = context.localTypes,
                };
            }

            return inlineMatcher;
        }

        private Ruleset MakePatchInstructionMatcher()
        {
            List<RuleBuilder> ruleBuilders = [];

            var context = new RuleBuilderContext();

            StateBuilder stateBuilder = new(context);
            stateBuilder.AssignStateVariableIndexes(patches);
            ruleBuilders.Add(stateBuilder);

            ruleBuilders.Add(new CircumfixRuleBuilder(context, outer, patches));

            foreach (IGrouping<Invocation, PatchInfo> targetGroup in patches
                         .Where(patch => patch.patchType is PatchType.InnerPrefix or PatchType.InnerPostfix).GroupBy(patch => patch.inner))
            {
                Invocation inner = targetGroup.Key;
                ruleBuilders.Add(new InfixRuleBuilder(context, outer, inner, [.. targetGroup]));
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

            var patchMatcher = new Ruleset
            {
                rules = rules,
                crossRuleLocalTypes = context.localTypes,
                crossRuleLabels = labels,
            };

            return patchMatcher;
        }
    }
}
