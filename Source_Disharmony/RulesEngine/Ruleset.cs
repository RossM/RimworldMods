namespace Disharmony.RulesEngine;

/// <summary>
///     Applies declarative pattern-matching and replacement rules to a sequence of Harmony IL instructions.
/// </summary>
/// <remarks>
///     Rules are processed in ascending <see cref="Rule.Phase" /> order. Rules in the same phase all match against the
///     phase's original input, and their combined output becomes the input to the next phase. Matching accounts for the
///     compact and expanded forms of equivalent IL opcodes and remaps local variables and labels used by a pattern.
/// </remarks>
public class Ruleset
{
    /// <summary>
    ///     Gets or sets whether debug logging is enabled for every ruleset, regardless of the value passed to
    ///     <see cref="MatchAndReplace(MethodBase, ref List{CodeInstruction}, ILGenerator, bool)" />.
    /// </summary>
    internal static bool forceDebug = false;

    /// <summary>
    ///     Gets or initializes the collection of rules to apply. The default is an empty collection.
    /// </summary>
    public List<Rule> Rules { get; init; } = [];

    /// <summary>
    ///     Gets or initializes local-variable placeholders that resolve to the same emitted local across rule matches.
    /// </summary>
    public List<LocalBuilder> CrossRuleLocals { get; init; } = [];

    /// <summary>
    ///     Gets or initializes label placeholders that resolve to the same emitted label across rule matches.
    /// </summary>
    public List<Label> CrossRuleLabels { get; init; } = [];

    /// <summary>
    ///     Initializes an empty ruleset.
    /// </summary>
    public Ruleset() { }

    /// <summary>
    ///     Initializes a ruleset with the specified rules.
    /// </summary>
    /// <param name="rules">The rules to apply.</param>
    public Ruleset(params List<Rule> rules)
    {
        this.Rules = rules;
    }

    /// <summary>
    ///     Applies this ruleset to an instruction list.
    /// </summary>
    /// <param name="method">The method whose instructions are being transformed.</param>
    /// <param name="instructionsList">
    ///     The instruction list to transform. On success, it is replaced with the transformed list; on failure, the
    ///     original reference is preserved.
    /// </param>
    /// <param name="generator">The IL generator used to define replacement locals and labels.</param>
    /// <param name="debug">Whether to write matching and output details to Harmony's file log.</param>
    /// <exception cref="InvalidOperationException">
    ///     A rule is invalid, does not meet its required match count, overlaps another match, or refers to a local that
    ///     cannot be resolved; or a phase has no matches with a non-null <see cref="Rule.Output" />.
    /// </exception>
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

    /// <summary>
    ///     Applies a set of rules to an instruction sequence and returns the transformed instructions.
    /// </summary>
    /// <param name="rules">The rules to apply.</param>
    /// <param name="method">The method whose instructions are being transformed.</param>
    /// <param name="instructions">The input instruction sequence.</param>
    /// <param name="generator">The IL generator used to define replacement locals and labels.</param>
    /// <param name="debug">Whether to write matching and output details to Harmony's file log.</param>
    /// <returns>A new list containing the transformed instruction sequence.</returns>
    /// <exception cref="InvalidOperationException">
    ///     A rule is invalid, does not meet its required match count, overlaps another match, or refers to a local that
    ///     cannot be resolved; or a phase has no matches with a non-null <see cref="Rule.Output" />.
    /// </exception>
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

/// <summary>
///     Specifies where a rule emits its <see cref="Rule.Output" /> relative to a match.
/// </summary>
public enum OutputMode
{
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
    ///     <see cref="Rule.Output" /> is inserted at the start of the instruction sequence. No pattern matching is done.
    /// </summary>
    MethodPrefix,

    /// <summary>
    ///     <see cref="Rule.Output" /> is inserted at the end of the instruction sequence. No pattern matching is done.
    /// </summary>
    MethodPostfix,
}

/// <summary>
///     Describes an IL instruction pattern and the output to emit for each match.
/// </summary>
/// <remarks>
///     For <see cref="OutputMode.MethodPrefix" /> and <see cref="OutputMode.MethodPostfix" />, leave
///     <see cref="Pattern" /> empty. For other modes, a non-empty pattern is required. Set <see cref="Output" /> to
///     <see langword="null" /> to require the pattern without changing its matches when another rule in the phase emits
///     output, or to an empty array to remove matches in <see cref="OutputMode.Replace" /> mode.
/// </remarks>
public class Rule
{
    /// <summary>
    ///     Gets or initializes the minimum required match count. The default is 1.
    /// </summary>
    public int Min { get; init; } = 1;

    /// <summary>
    ///     Gets or initializes the maximum number of matches to process. Zero or a negative value means no limit.
    ///     The default is 1.
    /// </summary>
    public int Max { get; init; } = 1;

    /// <summary>
    ///     Gets or initializes the priority for zero-length rules at the same insertion point. Higher values are emitted
    ///     first. The default is 0.
    /// </summary>
    public int Priority { get; init; } = 0;

    /// <summary>
    ///     Gets or initializes the processing phase. Lower-numbered phases run first; later phases can match earlier
    ///     output. The default is 1.
    /// </summary>
    public int Phase { get; init; } = 1;

    /// <summary>
    ///     Gets or initializes where <see cref="Output" /> is emitted relative to each match.
    ///     The default is <see cref="OutputMode.Replace" />.
    /// </summary>
    public OutputMode Mode { get; init; } = OutputMode.Replace;

    /// <summary>
    ///     Gets or initializes the instruction pattern, or <see langword="null" /> for
    ///     <see cref="OutputMode.MethodPrefix" /> and <see cref="OutputMode.MethodPostfix" /> rules.
    /// </summary>
    public CodeInstruction[]? Pattern { get; init; }

    /// <summary>
    ///     Gets or initializes the instructions to emit, or <see langword="null" /> to validate matches without emitting
    ///     output. An empty array removes matches in <see cref="OutputMode.Replace" /> mode.
    /// </summary>
    public required CodeInstruction[]? Output { get; init; }

    /// <summary>
    ///     Gets or initializes the name used in debug output and errors, or <see langword="null" /> for no name.
    /// </summary>
    public string? Name { get; init; }
}
