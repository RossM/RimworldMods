namespace XylXenos;

public abstract class JobDriver_InteractWithPawn : JobDriver
{
    public Pawn Target => TargetPawnA;

    public abstract bool ValidateTarget(Pawn target);
}
