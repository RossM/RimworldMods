namespace XylXenos;

[UsedFromXml]
public class ScenPart_StartingThing_Defined_Charged : ScenPart_StartingThing_Defined
{
    public override IEnumerable<Thing> PlayerStartingThings()
    {
        foreach (var thing in base.PlayerStartingThings())
        {
            if (thing.GetInnerIfMinified().TryGetComp<CompPowerBattery>() is { } battery)
                battery.SetStoredEnergyPct(1f);

            yield return thing;
        }
    }
}
