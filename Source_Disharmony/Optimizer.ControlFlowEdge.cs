namespace Disharmony;

internal partial class Optimizer
{
    internal sealed class VariableAssignment(Variable source, Variable destination)
    {
        // This is a logical value transfer on a CFG edge, not an instruction to emit.
        public Variable Source { get; } = source;
        public Variable Destination { get; } = destination;
    }

    internal sealed class ControlFlowEdge(BasicBlock source, BasicBlock target)
    {
        // Mutated only by the optimizer's edge helpers, which keep both endpoint collections in sync.
        public BasicBlock Source { get; internal set; } = source;
        public BasicBlock Target { get; internal set; } = target;

        // Populated when stack values are materialized as variables. All assignments occur in
        // parallel and remain logical until SSA destruction decides whether any copies are needed.
        public readonly List<VariableAssignment> assignments = [];
    }
}
