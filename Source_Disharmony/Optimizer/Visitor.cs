namespace Disharmony.Optimizer;

internal class Visitor
{
    public Op Visit(AssignmentOp op) => op;
    public Op Visit(ILOp op) => op;
    public Op Visit(StackSlot op) => op;
    public Op Visit(Argument op) => op;
    public Op Visit(Local op) => op;
    public Op Visit(Temporary op) => op;
    public Op Visit(VoidOp op) => op;
    public Region Visit(RootRegion region) => region;
    public Region Visit(ProtectedRegion region) => region;
    public Region Visit(CatchRegion region) => region;
    public Region Visit(FinallyRegion region) => region;
    public Region Visit(FaultRegion region) => region;
    public ExceptionGroup Visit(ExceptionGroup group) => group;
    public Branch Visit(UnconditionalBranch branch) => branch;
    public Branch Visit(Leave branch) => branch;
    public Branch Visit(ConditionalBranch branch) => branch;
    public Branch Visit(Throw branch) => branch;
    public Branch Visit(Rethrow branch) => branch;
    public Branch Visit(Return branch) => branch;
    public Branch Visit(Jump branch) => branch;
    public BasicBlock Visit(BasicBlock block) => block;
    public Edge Visit(Edge edge) => edge;
}
