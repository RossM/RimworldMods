namespace Disharmony.Optimizer;

internal class Optimizer
{
    private static readonly bool forceDebug;
    private static readonly string forceDebugForMethod;
    private readonly List<CodeInstruction> inputInstructions;
    internal readonly ILGenerator generator;
    private readonly bool debug;
    internal readonly List<Type> parameterTypes;
    internal readonly Type returnType;

    internal ControlFlowGraph cfg = new(new RootRegion(new BlockLabel()), [], [], [], []);

    private readonly RootRegion rootRegion = new(new BlockLabel());

    public MethodBase Method { get; }

    public IReadOnlyList<CodeInstruction> Instructions => inputInstructions;
    static Optimizer()
    {
        forceDebug = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("DISHARMONY_DEBUG"));
        forceDebugForMethod = Environment.GetEnvironmentVariable("DISHARMONY_DEBUG_METHOD");
    }

    public Optimizer(MethodBase method, List<CodeInstruction> inputInstructions, ILGenerator generator, bool debug)
    {
        this.Method = method;
        this.inputInstructions = inputInstructions;
        this.generator = generator;
        this.debug = debug || forceDebug || (!string.IsNullOrEmpty(forceDebugForMethod) && method.Name == forceDebugForMethod);

        if (method.HasThis)
            parameterTypes = [method.DeclaringType.CallableType, .. method.GetParameters().Types()];
        else
            parameterTypes = [.. method.GetParameters().Types()];

        returnType = method is MethodInfo methodInfo ? methodInfo.ReturnType : typeof(void);
    }

    public List<CodeInstruction> Optimize()
    {
        new CreateControlFlowGraph(this).Run();

        new MergeStackSlots(this).Run();

        return inputInstructions;
    }
}
