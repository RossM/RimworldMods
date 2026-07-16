namespace Disharmony;

public static partial class Autopatcher
{
    private class CircumfixRuleBuilder : RuleBuilder
    {
        public CircumfixRuleBuilder(
            MethodBase outer,
            List<PatchInfo> patches,
            List<Type> localTypes)
        {
        }

        public override IEnumerable<Rule> BuildRules()
        {
            yield break;
        }
    }
}
