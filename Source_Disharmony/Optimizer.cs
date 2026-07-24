namespace Disharmony;

internal class Optimizer(List<CodeInstruction> inputInstructions, ILGenerator generator)
{
    public static List<CodeInstruction> Transpiler(
        MethodBase method,
        IEnumerable<CodeInstruction> instructions,
        ILGenerator generator)
    {
        var optimizer = new Optimizer([.. instructions], generator);
        optimizer.Optimize();
        return optimizer.output.Instructions;
    }

    private void Optimize()
    {
        foreach (var inst in inputInstructions)
            output.Add(inst);
    }

    private readonly List<CodeInstruction> inputInstructions = inputInstructions;
    private readonly ILGenerator generator = generator;
    private readonly InstructionList output = new();
}
