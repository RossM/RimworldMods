namespace Yobai;

internal sealed class SleepingPlace
{
    public Building_Bed Bed { get; }
    public int Slot { get; }

    public SleepingPlace(Building_Bed bed, int slot)
    {
        ArgumentNullException.ThrowIfNull(bed);
        if (slot < 0 || slot >= bed.SleepingSlotsCount)
            throw new ArgumentOutOfRangeException(nameof(slot));
        Bed = bed;
        Slot = slot;
    }
}
