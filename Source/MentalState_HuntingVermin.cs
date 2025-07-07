using JetBrains.Annotations;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using Verse.AI;
using XylRacesCore.Genes;

namespace XylRacesCore
{
    [UsedImplicitly]
    public class MentalState_HuntingVermin : MentalState
    {
        public Pawn target;

        private const int NoLongerValidTargetCheckInterval = 120;

        private static readonly List<Pawn> tmpTargets = new();

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
                Log.Message(string.Format("MentalStateTick: {0}, {1}", target, pawn.CurJob.def));
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
            if (target != null && target.SpawnedParentOrMe != null && (!(target.SpawnedParentOrMe is Pawn) || target.SpawnedParentOrMe == target))
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
            Pawn result = tmpTargets.OrderBy(p => (pawn.Position - p.Position).LengthHorizontalSquared).ThenBy(p => Rand.Value).FirstOrDefault();
            tmpTargets.Clear();
            return result;
        }
    }
}
