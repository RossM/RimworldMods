namespace Yobai;

internal sealed class TemporarySleepingSlot : IExposable
{
    public Pawn? Pawn => pawn;
    public int Slot => slot;
    
    [field: Unsaved]
    public int TickCreated { get; } = -1;

    private Pawn? pawn;
    private int slot;

    [UsedImplicitly]
    public TemporarySleepingSlot() { }

    public TemporarySleepingSlot(Pawn pawn, int slot)
    {
        this.pawn = pawn;
        this.slot = slot;
        this.TickCreated = Find.TickManager.TicksGame;
    }

    public void ExposeData()
    {
        Scribe_References.Look(ref pawn, "pawn");
        Scribe_Values.Look(ref slot, "slot");
    }
}
