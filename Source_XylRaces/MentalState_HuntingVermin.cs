namespace XylXenos;

[UsedFromXml]
public class MentalState_HuntingVermin : MentalState
{
    private const int checkInterval = 120;

    private static readonly List<Pawn> tmpTargets = [];
    public Pawn? target;

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_References.Look(ref target, nameof(target));
    }

    public override RandomSocialMode SocialModeMax()
    {
        return RandomSocialMode.Off;
    }

    public override void PreStart()
    {
        base.PreStart();
        TryFindNewTarget();
    }

    public override void MentalStateTick(int delta)
    {
        base.MentalStateTick(delta);

        DebugAssert.NotNull(pawn);

        if (target is { Dead: true })
        {
            if (pawn.CurJob?.def == JobDefOf.AttackMelee || pawn.CurJob?.def == JobDefOf.Ingest)
                return;

            if (!pawn.HediffsOfType<Hediff_DietDependency>().Any(hediff => hediff.ShouldSatisfy) || Rand.Chance(0.2f) || !TryFindNewTarget())
                RecoverFromState();

            return;
        }

        if (!pawn.IsHashIntervalTick(checkInterval, delta))
            return;
        if (IsTargetStillValidAndReachable())
            return;
        if (!TryFindNewTarget())
            RecoverFromState();
    }

    public override TaggedString GetBeginLetterText()
    {
        DebugAssert.NotNull(pawn);

        if (target == null)
        {
            Log.Error("No target. This should have been checked in this mental state's worker.");
            return "";
        }

        return def.beginLetter.Formatted(pawn.NameShortColored, target.NameShortColored, pawn.Named("PAWN"), target.Named("TARGET"))
            .AdjustedFor(pawn).Resolve()
            .CapitalizeFirst();
    }

    private bool TryFindNewTarget()
    {
        target = FindPawnToKill(pawn);
        return target != null;
    }

    public bool IsTargetStillValidAndReachable()
    {
        if (target is { SpawnedParentOrMe: not null } && (target.SpawnedParentOrMe is not Pawn || target.SpawnedParentOrMe == target))
        {
            return pawn.CanReach(target.SpawnedParentOrMe, PathEndMode.Touch, Danger.Deadly, canBashDoors: true);
        }

        return false;
    }

    public static Pawn? FindPawnToKill(Pawn pawn)
    {
        if (!pawn.Spawned)
            return null;

        DebugAssert.NotNull(pawn.Map);

        tmpTargets.Clear();
        IReadOnlyList<Pawn> allPawnsSpawned = pawn.Map.mapPawns.AllPawnsSpawned;
        foreach (Pawn pawn2 in allPawnsSpawned)
        {
            if (pawn2.Faction == null && pawn2.IsAnimal && pawn2.BodySize <= pawn.BodySize &&
                pawn2.RaceProps.manhunterOnDamageChance <= 0.1f &&
                pawn.CanReach(pawn2, PathEndMode.Touch, Danger.Some))
            {
                tmpTargets.Add(pawn2);
            }
        }

        if (!tmpTargets.Any())
        {
            return null;
        }

        Pawn? result = tmpTargets.OrderBy(p => pawn.Position.DistanceToSquared(p.Position)).ThenBy(_ => Rand.Value).FirstOrDefault();
        tmpTargets.Clear();
        return result;
    }
}
