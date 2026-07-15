using JetBrains.Annotations;

namespace Disharmony;

public partial class InstructionMatcher
{
    public enum OutputMode
    {
        /// <summary>
        ///     <see cref="Rule.Pattern" /> is matched against, but no instruction changes are made.
        /// </summary>
        MatchOnly,

        /// <summary>
        ///     <see cref="Rule.Output" /> replaces the instructions that match <see cref="Rule.Pattern" />.
        /// </summary>
        Replace,

        /// <summary>
        ///     <see cref="Rule.Output" /> is inserted before the first instruction of each match of the
        ///     <see cref="Rule.Pattern" />.
        /// </summary>
        InsertBefore,

        /// <summary>
        ///     <see cref="Rule.Output" /> is inserted after the last instruction of each match of the <see cref="Rule.Pattern" />.
        /// </summary>
        InsertAfter,

        /// <summary>
        ///     <see cref="Rule.Output" /> is inserted at the very start of the instructions. No matching is done.
        /// </summary>
        MethodPrefix,

        /// <summary>
        ///     <see cref="Rule.Output" /> is inserted at the very end of the instructions. No matching is done.
        /// </summary>
        MethodPostfix,
    }

    public class Rule
    {
        public int Min = 1, Max = 1;
        public OutputMode Mode = OutputMode.MatchOnly;
        public bool SaveLocals = false;
        public bool Chained = false;
        public CodeInstruction[]? Pattern;
        public CodeInstruction[]? Output;
        public Type[]? LocalTypes;
        public string? Name;
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

    public List<Rule> Rules = [];
    public List<Type> CrossRuleLocalTypes = [];
    public List<Label> CrossRuleLabels = [];

    private readonly List<ExceptionBlock> extraBlocks = [];

    public InstructionMatcher()
    {
    }

    public InstructionMatcher(params List<Rule> rules)
    {
        Rules = rules;
    }

    public void MatchAndReplace(
        MethodBase method,
        ref List<CodeInstruction> instructionsList,
        ILGenerator generator,
        [CallerMemberName] string? methodName = null,
        bool debug = false)
    {
        var worker = new Worker(this, method, instructionsList, generator, debug);
        if (!worker.TryMatchAndReplace())
            throw new InvalidOperationException(worker.Reason);
        instructionsList = worker.OutInstructions;
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

    [UsedImplicitly]
    public static List<CodeInstruction> RunMatchers(
        InstructionMatcher[] matchers,
        MethodBase target,
        IEnumerable<CodeInstruction> instructions,
        ILGenerator generator)
    {
        var instructionsList = instructions.ToList();
        foreach (var matcher in matchers)
        {
            matcher.MatchAndReplace(target, ref instructionsList, generator);
        }

        return instructionsList;
    }

    public static Rule MakeRedirectRule(MemberInfo oldMember, MethodInfo newMember)
    {
        return new Rule
        {
            Min = 1,
            Max = 0,
            Mode = OutputMode.Replace,
            Pattern = [new(OpCodes.Call, oldMember)],
            Output = [new(OpCodes.Call, newMember)],
            LocalTypes = [],
            Name = oldMember.FullName,
        };
    }
}
