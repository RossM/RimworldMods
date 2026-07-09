using System.Diagnostics.CodeAnalysis;

namespace XylXenos;

public class GeneCompProperties_Flight : GeneCompProperties
{
    public float autoFlyMinDistance = 25f;

    public GeneCompProperties_Flight()
    {
        compClass = typeof(GeneComp_Flight);
    }
}

[UsedFromXml]
public class DefModExtension_Thing_Flight : DefModExtension
{
    public bool allowsFlight = true;
}

public class GeneComp_Flight : GeneComp, IEventListener
{
    public GeneCompProperties_Flight Props => (GeneCompProperties_Flight)props;

    public Texture2D? ExtraIcon => parent.DefExt.ExtraIcon;

    public bool CanFlyNow => Pawn is { flight.CanFlyNow: true, Downed: false } && flightAllowedByApparel;

    [MemberNotNullWhen(true, nameof(Flight))]
    [MemberNotNullWhen(true, nameof(Pather))]
    private bool Spawned => Pawn.Spawned;
    private Pawn_FlightTracker? Flight => Pawn.flight;
    private Pawn_PathFollower? Pather => Pawn.pather;

    public bool autoFly = true;
    public bool autoFlyDrafted = true;

    [Unsaved] private bool wasFlying;

    public bool flightAllowedByApparel = true;

    public override void CompExposeData()
    {
        Scribe_Values.Look(ref autoFly, nameof(autoFly));
        Scribe_Values.Look(ref autoFlyDrafted, nameof(autoFlyDrafted));
        Scribe_Values.Look(ref flightAllowedByApparel, nameof(flightAllowedByApparel));
    }


    public override IEnumerable<Gizmo> CompGetGizmos()
    {
        if (!Active)
            yield break;
        if (!Spawned)
            yield break;
        if (!Pawn.IsColonistPlayerControlled)
            yield break;

        string flyingDisabledBy = "";
        if (!flightAllowedByApparel)
        {
            DebugAssert.NotNull(Pawn.apparel?.WornApparel);

            List<string> items = [];
            foreach (var item in Pawn.apparel.WornApparel)
            {
                if (!ApparelAllowsFlight(item.def))
                    items.Add(item.Label);
            }

            flyingDisabledBy
                = $"{"ApparelRequirementDisabledLabel".Translate()}: {items.ToCommaList(useAnd: true).CapitalizeFirst()}\n\n";
        }

        yield return new Command_ActionWithCooldown
        {
            action = () => { Flight.StartFlying(); },
            defaultLabel = "XylCommandFlyLabel".TranslateSimple(),
            defaultDesc = "XylCommandFlyDesc".TranslateSimple(),
            Disabled = !CanFlyNow,
            cooldownPercentGetter = () => 1.0f - Flight.flightCooldownTicks / (Pawn.GetStatValue(StatDefOf.FlightCooldown) * 60f),
            icon = ExtraIcon,
            defaultDescPostfix = "\n\n" + $"""
                    {flyingDisabledBy}{"CooldownTime".TranslateSimple()}: {Pawn.GetStatValue(StatDefOf.FlightCooldown).ToStringDecimalIfSmall()}{"LetterSecond".TranslateSimple()}
                    {"AbilityDuration".TranslateSimple()}: {Pawn.GetStatValue(StatDefOf.MaxFlightTime).ToStringDecimalIfSmall()}{"LetterSecond".TranslateSimple()}
                    """,
        };

        if (flightAllowedByApparel)
        {
            if (Pawn.Drafted)
            {
                yield return new Command_Toggle
                {
                    defaultLabel = "XylCommandAutoFlyDraftedLabel".TranslateSimple(),
                    defaultDesc = "XylCommandAutoFlyDraftedDesc".TranslateSimple(),
                    isActive = () => autoFlyDrafted,
                    toggleAction = () => { autoFlyDrafted = !autoFlyDrafted; },
                    icon = ExtraIcon,
                };
            }
            else
            {
                yield return new Command_Toggle
                {
                    defaultLabel = "XylCommandAutoFlyLabel".TranslateSimple(),
                    defaultDesc = "XylCommandAutoFlyDesc".TranslateSimple(),
                    isActive = () => autoFly,
                    toggleAction = () => { autoFly = !autoFly; },
                    icon = ExtraIcon,
                };
            }
        }
    }

    public override void CompTick()
    {
        if (!Spawned)
            return;

        if (Flight.Flying != wasFlying)
        {
            Pawn.Drawer.renderer.SetAllGraphicsDirty();
            // This forces the pather to recalculate the current path
            if (Pather.Moving)
                Pather.TryResumePathingAfterLoading();
            wasFlying = Flight.Flying;
        }

        if (!CanFlyNow)
            return;

        if (Pawn.IsPlayerControlled)
        {
            if ((Pawn.Drafted ? autoFlyDrafted : autoFly) &&
                Pather.Moving &&
                Pawn.Position.DistanceTo(Pather.Destination.Cell) >= Props.autoFlyMinDistance &&
                Pawn.CurJob?.locomotionUrgency > LocomotionUrgency.Walk)
            {
                Flight.StartFlying();
            }
        }
        else
        {
            if (Pather.Moving && Pawn.CurJob?.locomotionUrgency > LocomotionUrgency.Walk)
            {
                Flight.StartFlying();
            }
        }
    }

    public static bool ApparelAllowsFlight(ThingDef thingDef)
    {
        return thingDef.GetModExtension<DefModExtension_Thing_Flight>() is not { allowsFlight: false };
    }

    private void CheckApparel()
    {
        flightAllowedByApparel = true;
        if (Pawn.apparel?.WornApparel is null)
            return;

        foreach (var item in Pawn.apparel.WornApparel)
            flightAllowedByApparel &= ApparelAllowsFlight(item.def);
    }

    public override void CompPostPostAdd()
    {
        CheckApparel();
    }

    [DebugOutput("Economy")]
    public static void ApparelAllowsFlight()
    {
        TableDataGetter<ThingDef>[] columns =
        [
            new("defName", thingDef => thingDef.defName),
            new("label", thingDef => thingDef.LabelCap),
            new("allowsFlight", thingDef => ApparelAllowsFlight(thingDef))
        ];
        DebugTables.MakeTablesDialog(
            DefDatabase<ThingDef>.AllDefs.Where(thingDef => thingDef.IsApparel).OrderBy(thingDef => thingDef.BaseMarketValue), columns);
    }

    public void Notify_ApparelChanged()
    {
        CheckApparel();
    }

    // If a downed flying pawn lands on a non-walkable tile, they are killed and their corpse destroyed.
    // This would be unfortunate, so try to move the pawn to a better position.
    public void Notify_Downed()
    {
        if (Pawn is { Flying: true, Downed: true } && !Pawn.Position.WalkableBy(Pawn.Map, Pawn))
        {
            var newCell = CellFinder.StandableCellNear(Pawn.Position, Pawn.Map, 5f);
            if (newCell != IntVec3.Invalid)
                Pawn.Position = newCell;
        }
    }

    public void RegisterWith(EventManager manager)
    {
        manager.Register(EventDefOf.PostApparelChanged, Pawn, Notify_ApparelChanged);
        manager.Register(EventDefOf.PostDowned, Pawn, Notify_Downed);
    }

    public void PreUnregister(EventManager manager)
    {
    }
}
