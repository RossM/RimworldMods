namespace Disharmony.Optimizer;

internal class Optimizer
{
    private static readonly bool forceDebug;
    private static readonly string forceDebugForMethod;
    private readonly MethodBase method;
    private readonly List<CodeInstruction> inputInstructions;
    internal readonly ILGenerator generator;
    private readonly bool debug;
    internal readonly List<Type> parameterTypes;
    internal readonly Type returnType;

    internal ControlFlowGraph cfg = new();
    internal readonly Dictionary<int, Argument> arguments = [];
    internal readonly Dictionary<int, Local> locals = [];

    private readonly RootRegion rootRegion = new(new BlockLabel());

    static Optimizer()
    {
        forceDebug = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("DISHARMONY_DEBUG"));
        forceDebugForMethod = Environment.GetEnvironmentVariable("DISHARMONY_DEBUG_METHOD");
    }

    public Optimizer(MethodBase method, List<CodeInstruction> inputInstructions, ILGenerator generator, bool debug)
    {
        this.method = method;
        this.inputInstructions = inputInstructions;
        this.generator = generator;
        this.debug = debug || forceDebug || !string.IsNullOrEmpty(forceDebugForMethod) && method.Name == forceDebugForMethod;

        if (method.HasThis)
            parameterTypes = [method.DeclaringType.CallableType, .. method.GetParameters().Types()];
        else
            parameterTypes = [.. method.GetParameters().Types()];

        returnType = method is MethodInfo methodInfo ? methodInfo.ReturnType : typeof(void);
    }

    public List<CodeInstruction> Optimize()
    {
        var controlFlowGraphGenerator = new ControlFlowGraphGenerator(method, inputInstructions);
        controlFlowGraphGenerator.CreateControlFlowGraph();
        cfg = controlFlowGraphGenerator.ControlFlowGraph;

        MergeStackSlots();

        return inputInstructions;
    }

    public void MergeStackSlots()
    {
        // Precondition: All edge assignments are between stack slots; no stack slot is live in multiple basic blocks
        // Postcondition: There are no edge assignments

        DisjointSetUnion<Op> tree = new();

        foreach (var edge in cfg.Edges)
        foreach (var assignment in edge.EdgeAssignments)
        {
            tree.Add(assignment.Input);
            tree.Add(assignment.Output);
        }

        foreach (var edge in cfg.Edges)
        foreach (var assignment in edge.EdgeAssignments)
        {
            tree.Merge(assignment.Output, assignment.Input);
        }

        ReplaceVisitor visitor = new();
        foreach (var group in tree)
        foreach (var op in group)
        {
            if (op != group.Key)
                visitor.Replacements[op] = group.Key;
        }

        visitor.Visit(cfg);
    }
}