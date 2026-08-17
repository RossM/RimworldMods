namespace Disharmony.Optimizer;

internal interface IVisitor<T>
{
    T Visit(AssignmentOp op);
    T Visit(ILOp op);
    T Visit(StackSlot op);
    T Visit(Argument op);
    T Visit(Local op);
    T Visit(Temporary op);
    T Visit(VoidOp op);
    T Visit(RootRegion region);
    T Visit(ProtectedRegion region);
    T Visit(CatchRegion region);
    T Visit(FinallyRegion region);
    T Visit(FaultRegion region);
    T Visit(ExceptionGroup group);
    T Visit(UnconditionalBranch branch);
    T Visit(Leave branch);
    T Visit(ConditionalBranch branch);
    T Visit(Throw branch);
    T Visit(Rethrow branch);
    T Visit(Return branch);
    T Visit(Jump branch);
    T Visit(BasicBlock block);
    T Visit(Edge edge);
}

internal class Visitor : IVisitor<Node>
{
    public virtual Node Visit(AssignmentOp op) => op;
    public virtual Node Visit(ILOp op) => op;
    public virtual Node Visit(StackSlot op) => op;
    public virtual Node Visit(Argument op) => op;
    public virtual Node Visit(Local op) => op;
    public virtual Node Visit(Temporary op) => op;
    public virtual Node Visit(VoidOp op) => op;
    public virtual Node Visit(RootRegion region) => region;
    public virtual Node Visit(ProtectedRegion region) => region;
    public virtual Node Visit(CatchRegion region) => region;
    public virtual Node Visit(FinallyRegion region) => region;
    public virtual Node Visit(FaultRegion region) => region;
    public virtual Node Visit(ExceptionGroup group) => group;
    public virtual Node Visit(UnconditionalBranch branch) => branch;
    public virtual Node Visit(Leave branch) => branch;
    public virtual Node Visit(ConditionalBranch branch) => branch;
    public virtual Node Visit(Throw branch) => branch;
    public virtual Node Visit(Rethrow branch) => branch;
    public virtual Node Visit(Return branch) => branch;
    public virtual Node Visit(Jump branch) => branch;
    public virtual Node Visit(BasicBlock block) => block;
    public virtual Node Visit(Edge edge) => edge;
}
