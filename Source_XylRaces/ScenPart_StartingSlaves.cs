namespace XylXenos;

[UsedFromXml]
public class ScenPart_StartingSlaves : ScenPart_PawnModifier
{
    public int count;

    private string? countBuf;

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref count, nameof(count));
    }

    public override void DoEditInterface(Listing_ScenEdit listing)
    {
        Rect scenPartRect = listing.GetScenPartRect(this, RowHeight * 1f + 1f);

        scenPartRect.height = RowHeight;
        Widgets.TextFieldNumeric(scenPartRect, ref count, ref countBuf, 1);
    }

    public override string Summary(Scenario scen)
    {
        return "XylStartingSlaves".Translate(count);
    }

    public override void Randomize()
    {
        count = 1;
    }

    public override void GenerateIntoMap(Map map)
    {
        DebugAssert.NotNull(Find.Scenario);

        List<Pawn> pawns = map.mapPawns.FreeColonists;

        // If the scenario is set up by xenotype, sort the pawn list so that they are ordered
        // the way they were in the scenario. This is used by the Warcat Tribe scenario
        // to ensure that the non-warcats are the slaves.
        if (Find.Scenario.AllParts.OfType<ScenPart_ConfigPage_ConfigureStartingPawns_Xenotypes>().FirstOrDefault() is { } xenotypeConfig)
        {
            pawns =
            [
                .. pawns.OrderBy(p =>
                {
                    DebugAssert.NotNull(xenotypeConfig.xenotypeCounts);

                    int index = xenotypeConfig.xenotypeCounts.FindIndex(x => x.xenotype == p.genes?.Xenotype);
                    return index == -1 ? xenotypeConfig.xenotypeCounts.Count : index;
                }),
            ];
        }

        foreach (var pawn in pawns.Skip(Math.Max(0, pawns.Count - count)))
        {
            DebugAssert.NotNull(pawn.guest);

            pawn.guest.SetGuestStatus(Faction.OfPlayer, GuestStatus.Slave);
        }
    }
}
