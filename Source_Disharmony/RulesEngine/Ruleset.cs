namespace Disharmony.RulesEngine;

public class Ruleset
{
    public static bool forceDebug = false;

    public List<Rule> rules = [];
    public List<LocalBuilder> crossRuleLocals = [];
    public List<Label> crossRuleLabels = [];

    public Ruleset() { }

    public Ruleset(params List<Rule> rules)
    {
        this.rules = rules;
    }

    public void MatchAndReplace(
        MethodBase method,
        ref List<CodeInstruction> instructionsList,
        ILGenerator generator,
        bool debug = false)
    {
        var worker = new Processor(this, method, instructionsList, generator, debug || forceDebug);
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
        new Ruleset(rules).MatchAndReplace(method, ref instructionsList, generator, debug: debug);
        return instructionsList;
    }
}

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
    public int phase = 1;
    public OutputMode mode = OutputMode.Replace;
    public CodeInstruction[]? pattern;
    public required CodeInstruction[] output;
    public string? name;
}
