namespace XylXenos;

[UsedFromXml]
public class JobGiver_GetWetness : ThinkNode_JobGiver
{
    public JobDef soakJobDef;

    public const Danger maxDanger = Danger.None;

    public static List<ThingDef> WetnessGivingThings
    {
        get
        {
            if (field == null)
            {
                field = [];
                foreach (var def in DefDatabase<ThingDef>.AllDefs)
                {
                    if (def.GetModExtension<DefModExtension_Thing_WetnessSource>() != null)
                        field.Add(def);
                }
            }

            return field;
        }
    }

    public override ThinkNode DeepCopy(bool resolve = true)
    {
        var obj = (JobGiver_GetWetness)base.DeepCopy(resolve);
        obj.soakJobDef = soakJobDef;
        return obj;
    }

    private static Thing FindBestWetnessSource(Pawn pawn)
    {
        var candidates = new List<Thing>();
        GetSearchSet(pawn, candidates);
        if (candidates.Count == 0)
            return null;

        TraverseParms traverseParams = TraverseParms.For(pawn);
        traverseParams.maxDanger = maxDanger;

        return GenClosest.ClosestThing_Global_Reachable(pawn.Position, pawn.Map, candidates,
            PathEndMode.InteractionCell, traverseParams, validator: t => CanInteractWith(pawn, t));
    }

    public static bool IsValidWaterTileFor(Pawn pawn, IntVec3 x)
    {
        if (!x.InAllowedArea(pawn))
            return false;
        if (PawnUtility.KnownDangerAt(x, pawn.Map, pawn))
            return false;
        TerrainDef terrain = x.GetTerrain(pawn.Map);
        if (terrain.toxicBuildupFactor != 0f && pawn.GetStatValue(StatDefOf.ToxicResistance) < 1.0f
                                             && pawn.GetStatValue(StatDefOf.ToxicEnvironmentResistance) < 1.0f)
            return false;
        if (x.Fogged(pawn.Map))
            return false;
        if (!x.Standable(pawn.Map))
            return false;

        // Bathing in marsh is icky, only do it if really necessary.
        if (terrain.HasTag("WaterMarshy") && pawn.needs?.TryGetNeed<Need_Wetness>() is { CurCategory: >= WetnessCategory.Neutral })
            return false;

        return Need_Wetness.GetWetness(x, pawn.Map) >= 0.5f;
    }


    public static bool TryFindWaterTile(Pawn pawn, out IntVec3 result, int maxSearchRadius = int.MaxValue)
    {
        bool Validator(IntVec3 x) => IsValidWaterTileFor(pawn, x) && pawn.CanReach(new LocalTargetInfo(x), PathEndMode.OnCell, maxDanger);

        return RCellFinder.TryFindRandomCellNearWith(pawn.Position, Validator, pawn.Map, out result,
            maxSearchRadius: maxSearchRadius);
    }

    private static void GetSearchSet(Pawn pawn, List<Thing> outCandidates)
    {
        outCandidates.Clear();

        foreach (ThingDef def in WetnessGivingThings)
        {
            outCandidates.AddRange(pawn.Map.listerThings.ThingsOfDef(def));
        }
    }

    private static bool CanInteractWith(Pawn pawn, Thing t)
    {
        if (!pawn.CanReserve(t))
        {
            return false;
        }

        if (t.IsForbidden(pawn))
        {
            return false;
        }

        if (t.Fogged())
        {
            return false;
        }

        if (!t.IsSociallyProper(pawn))
        {
            return false;
        }

        if (!t.IsPoliticallyProper(pawn))
        {
            return false;
        }

        return true;
    }

    protected override Job TryGiveJob(Pawn pawn)
    {
        if (IsValidWaterTileFor(pawn, pawn.Position))
        {
            return JobMaker.MakeJob(soakJobDef, pawn.Position);
        }

        if (FindBestWetnessSource(pawn) is { } bestThing)
        {
            return JobMaker.MakeJob(bestThing.def.GetModExtension<DefModExtension_Thing_WetnessSource>().job, bestThing);
        }

        if (TryFindWaterTile(pawn, out IntVec3 foundTile))
        {
            return JobMaker.MakeJob(soakJobDef, foundTile);
        }

        return null;
    }

    public override float GetPriority(Pawn pawn)
    {
        var need_wetness = pawn.needs?.TryGetNeed<Need_Wetness>();
        if (need_wetness == null)
            return 0.0f;

        var projectedWetness = need_wetness.CurLevel - 8f * need_wetness.FallPerHour;

        if (need_wetness.CurLevel < Need_Wetness.thresholdWet && projectedWetness < Need_Wetness.thresholdNeutral)
            return ThinkNodePriority.MiscNeed;
        if (need_wetness.CurLevel < 0.95f)
            return ThinkNodePriority.AvoidIdle;
        return 0.0f;
    }
}
