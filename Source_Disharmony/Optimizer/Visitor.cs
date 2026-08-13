namespace Disharmony.Optimizer;

internal class Visitor
{
    public virtual Op Visit(AssignmentOp op) => op;
    public virtual Op Visit(ILOp op) => op;
    public virtual Op Visit(StackSlot op) => op;
    public virtual Op Visit(Argument op) => op;
    public virtual Op Visit(Local op) => op;
    public virtual Op Visit(Temporary op) => op;
    public virtual Op Visit(VoidOp op) => op;
    public virtual Region Visit(RootRegion region) => region;
    public virtual Region Visit(ProtectedRegion region) => region;
    public virtual Region Visit(CatchRegion region) => region;
    public virtual Region Visit(FinallyRegion region) => region;
    public virtual Region Visit(FaultRegion region) => region;
    public virtual ExceptionGroup Visit(ExceptionGroup group) => group;
    public virtual Branch Visit(UnconditionalBranch branch) => branch;
    public virtual Branch Visit(Leave branch) => branch;
    public virtual Branch Visit(ConditionalBranch branch) => branch;
    public virtual Branch Visit(Throw branch) => branch;
    public virtual Branch Visit(Rethrow branch) => branch;
    public virtual Branch Visit(Return branch) => branch;
    public virtual Branch Visit(Jump branch) => branch;
    public virtual BasicBlock Visit(BasicBlock block) => block;
    public virtual Edge Visit(Edge edge) => edge;
}
