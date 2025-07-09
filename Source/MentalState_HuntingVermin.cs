using JetBrains.Annotations;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.AI;

namespace XylRacesCore
{
    [UsedImplicitly]
    public class MentalState_HuntingVermin : MentalState
    {
        public Pawn target;

        private static readonly List<Pawn> tmpTargets = [];

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref target, "target");
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
            
            if (target is { Dead: true })
            {
                Log.Message($"MentalStateTick: {target}, {pawn.CurJob.def}");
                if (pawn.CurJob.def != JobDefOf.AttackMelee && pawn.CurJob.def != JobDefOf.Ingest)
                    RecoverFromState();
                return;
            }

            if (!pawn.IsHashIntervalTick(120, delta)) 
                return;
            if (IsTargetStillValidAndReachable()) 
                return;
            if (!TryFindNewTarget()) 
                RecoverFromState();
        }

        public override TaggedString GetBeginLetterText()
        {
            if (target == null)
            {
                Log.Error("No target. This should have been checked in this mental state's worker.");
                return "";
            }
            return def.beginLetter.Formatted(pawn.NameShortColored, target.NameShortColored, pawn.Named("PAWN"), target.Named("TARGET")).AdjustedFor(pawn).Resolve()
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

        public static Pawn FindPawnToKill(Pawn pawn)
        {
            if (!pawn.Spawned)
            {
                return null;
            }
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
            Pawn result = tmpTargets.OrderBy(p => pawn.Position.DistanceToSquared(-p.Position)).ThenBy(_ => Rand.Value).FirstOrDefault();
            tmpTargets.Clear();
            return result;
        }
    }
}
