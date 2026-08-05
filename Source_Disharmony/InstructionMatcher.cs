namespace Disharmony;

public partial class InstructionMatcher
{
    public enum OutputMode
    {
        /// <summary>
        ///     <see cref="Rule.output" /> replaces the instructions that match <see cref="Rule.pattern" />.
        /// </summary>
        Replace,

        /// <summary>
        ///     <see cref="Rule.output" /> is inserted before the first instruction of each match of the
        ///     <see cref="Rule.pattern" />.
        /// </summary>
        InsertBefore,

        /// <summary>
        ///     <see cref="Rule.output" /> is inserted after the last instruction of each match of the <see cref="Rule.pattern" />.
        /// </summary>
        InsertAfter,

        /// <summary>
        ///     <see cref="Rule.output" /> is inserted at the very start of the instructions. No matching is done.
        /// </summary>
        MethodPrefix,

        /// <summary>
        ///     <see cref="Rule.output" /> is inserted at the very end of the instructions. No matching is done.
        /// </summary>
        MethodPostfix,
    }

    public class Rule
    {
        public int min = 1, max = 1;
        public int priority = 0;
        public OutputMode mode = OutputMode.Replace;
        public CodeInstruction[]? pattern;
        public required CodeInstruction[] output;
        public string? name;
    }

    private class MatchData
    {
        public required Rule rule;
        public int start, end;
        public required Dictionary<int, int> localMap_Match;
        public required Dictionary<Label, Label> labelMap_Match;
        public bool emitted = false;
    }

    public static bool forceDebug = false;

    public List<Rule> rules = [];
    public List<Type> crossRuleLocalTypes = [];
    public List<Label> crossRuleLabels = [];

    public InstructionMatcher() { }

    public InstructionMatcher(params List<Rule> rules)
    {
        this.rules = rules;
    }

    public void MatchAndReplace(
        MethodBase method,
        ref List<CodeInstruction> instructionsList,
        ILGenerator generator,
        [CallerMemberName] string? methodName = null,
        bool debug = false)
    {
        var worker = new Worker(this, method, instructionsList, generator, debug);
        worker.MatchAndReplace();
        instructionsList = worker.outInstructions;
    }

    public static List<CodeInstruction> MatchAndReplace(
        List<Rule> rules,
        MethodBase method,
        IEnumerable<CodeInstruction> instructions,
        ILGenerator generator,
        bool debug = false)
    {
        var instructionsList = new List<CodeInstruction>(instructions);
        new InstructionMatcher(rules).MatchAndReplace(method, ref instructionsList, generator, debug: debug);
        return instructionsList;
    }

    public static Rule MakeRedirectRule(MemberInfo oldMember, MethodBase newMember)
    {
        return new Rule
        {
            min = 1,
            max = 0,
            mode = OutputMode.Replace,
            pattern = [new(OpCodes.Call, oldMember)],
            output = [new(OpCodes.Call, newMember)],
            name = oldMember.FullName,
        };
    }
}
