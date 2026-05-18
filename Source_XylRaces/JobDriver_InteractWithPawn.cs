using Verse;
using Verse.AI;

namespace XylXenos;

public abstract class JobDriver_InteractWithPawn : JobDriver
{
    protected Pawn Target => TargetPawnA;

    public abstract bool ValidateTarget(Pawn target);
}
