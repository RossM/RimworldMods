namespace Yobai;

internal sealed class TemporarySleepingSlot : IExposable
{
    public Pawn? Pawn => pawn;
    public int Slot => slot;
    public int TickCreated => tickCreated;
    private Pawn? pawn;
    private int slot;

    [Unsaved] private readonly int tickCreated = -1;

    [UsedImplicitly]
    public TemporarySleepingSlot() { }

    public TemporarySleepingSlot(Pawn pawn, int slot)
    {
        this.pawn = pawn;
        this.slot = slot;
        this.tickCreated = Find.TickManager.TicksGame;
    }

    public void ExposeData()
    {
        Scribe_References.Look(ref pawn, "pawn");
        Scribe_Values.Look(ref slot, "slot");
    }
}
