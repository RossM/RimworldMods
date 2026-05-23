using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using LudeonTK;
using RimWorld;
using Verse;
using Verse.AI;

namespace XylXenos.Genes
{
    public class GeneDefExtension_Flight : GeneDefExtension_WithIcon
    {
        public float autoFlyMinDistance = 25f;
    }

    public class ThingDefExtension_Flight : DefModExtension
    {
        public bool allowsFlight = true;
    }

    public class Flight : Gene, INotificationListener
    {
        public GeneDefExtension_Flight DefExt => def.GetModExtension<GeneDefExtension_Flight>();
        public bool autoFly = true;
        public bool autoFlyDrafted = true;

        [Unsaved] private bool wasFlying;

        public bool flightAllowedByApparel = true;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref autoFly, nameof(autoFly));
            Scribe_Values.Look(ref autoFlyDrafted, nameof(autoFlyDrafted));
            Scribe_Values.Look(ref flightAllowedByApparel, nameof(flightAllowedByApparel));
        }


        public override IEnumerable<Gizmo> GetGizmos()
        {
            if (!Active)
                yield break;
            if (!pawn.Spawned)
                yield break;
            if (!pawn.IsColonistPlayerControlled)
                yield break;

            string flyingDisabledBy = "";
            if (!flightAllowedByApparel)
            {
                List<string> items = [];
                foreach (var item in pawn.apparel.WornApparel)
                {
                    if (!ApparelAllowsFlight(item.def))
                        items.Add(item.Label);
                }

                flyingDisabledBy
                    = $"{"ApparelRequirementDisabledLabel".Translate()}: {items.ToCommaList(useAnd: true).CapitalizeFirst()}\n\n";
            }

            yield return new Command_ActionWithCooldown()
            {
                action = () => { pawn.flight.StartFlying(); },
                defaultLabel = "XylCommandFlyLabel".TranslateSimple(),
                defaultDesc = "XylCommandFlyDesc".TranslateSimple(),
                Disabled = !pawn.flight.CanFlyNow || !flightAllowedByApparel,
                cooldownPercentGetter = () => 1.0f - pawn.flight.flightCooldownTicks / (pawn.GetStatValue(StatDefOf.FlightCooldown) * 60f),
                icon = DefExt.Icon,
                defaultDescPostfix = "\n\n" + $"""
                    {flyingDisabledBy}{"CooldownTime".TranslateSimple()}: {pawn.GetStatValue(StatDefOf.FlightCooldown).ToStringDecimalIfSmall()}{"LetterSecond".TranslateSimple()}
                    {"AbilityDuration".TranslateSimple()}: {pawn.GetStatValue(StatDefOf.MaxFlightTime).ToStringDecimalIfSmall()}{"LetterSecond".TranslateSimple()}
                    """,
            };

            if (flightAllowedByApparel)
            {
                if (pawn.Drafted)
                {
                    yield return new Command_Toggle
                    {
                        defaultLabel = "XylCommandAutoFlyDraftedLabel".TranslateSimple(),
                        defaultDesc = "XylCommandAutoFlyDraftedDesc".TranslateSimple(),
                        isActive = () => autoFlyDrafted,
                        toggleAction = () => { autoFlyDrafted = !autoFlyDrafted; },
                        icon = DefExt.Icon,
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
                        icon = DefExt.Icon,
                    };
                }
            }
        }

        public override void Tick()
        {
            base.Tick();

            Pawn_FlightTracker flight = pawn.flight;
            if (flight == null)
                return;

            if (flight.Flying != wasFlying)
            {
                pawn.Drawer.renderer.SetAllGraphicsDirty();
                wasFlying = flight.Flying;
            }

            if (!flight.CanEverFly)
                return;

            if (!flight.Flying &&
                flightAllowedByApparel &&
                (pawn.Drafted ? autoFlyDrafted : autoFly) &&
                pawn.pather.Moving &&
                pawn.Position.DistanceTo(pawn.pather.Destination.Cell) >= DefExt.autoFlyMinDistance &&
                pawn.CurJob?.locomotionUrgency > LocomotionUrgency.Walk)
            {
                flight.StartFlying();
            }
        }

        // If a downed flying pawn lands on a non-walkable tile, they are killed and their corpse destroyed.
        // This would be unfortunate, so try to move the pawn to a better position.
        public void Notify_Downed()
        {
            var newCell = CellFinder.StandableCellNear(pawn.Position, pawn.Map, 5f);
            if (newCell != IntVec3.Invalid)
                pawn.Position = newCell;
        }

        public static bool ApparelAllowsFlight(ThingDef thingDef)
        {
            return thingDef.GetModExtension<ThingDefExtension_Flight>() is not { allowsFlight: false };
        }

        private void CheckApparel()
        {
            flightAllowedByApparel = true;
            foreach (var item in pawn.apparel.WornApparel)
                flightAllowedByApparel &= ApparelAllowsFlight(item.def);
        }

        public override void PostAdd()
        {
            base.PostAdd();

            CheckApparel();
        }

        [DebugOutput("Economy")]
        [UsedImplicitly]
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

        public void RegisterWith(NotificationManager manager)
        {
            manager.Register(NotificationEvent.PostApparelChanged, pawn, Notify_ApparelChanged);
        }
    }
}
