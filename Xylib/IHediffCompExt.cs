namespace Xylib;

public interface IHediffCompExt
{
    bool AllowTend { get; }
    void CompUpdateCurStage(HediffStage stage);
}
