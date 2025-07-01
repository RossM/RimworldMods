using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace XylRacesCore
{
    public class CompProperties_AbilitySonicWave : CompProperties_AbilityEffectWithDuration
    {
        public float range;
        public float lineWidthEnd;
        public bool canHitFilledCells;

        public PawnCapacityDef durationMultiplierCapacity;

        public CompProperties_AbilitySonicWave()
        {
            compClass = typeof(CompAbilityEffect_SonicWave);
        }
    }

    public class CompAbilityEffect_SonicWave : CompAbilityEffect_WithDuration
    {
        public new CompProperties_AbilitySonicWave Props => (CompProperties_AbilitySonicWave)props;
        private Pawn Pawn => parent.pawn;

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            Map map = parent.pawn.MapHeld;
            foreach (IntVec3 item in AffectedCells(target))
            {
                var thingList = item.GetThingList(map);
                foreach (var targetPawn in thingList.OfType<Pawn>())
                {
                    targetPawn.stances.stunner.StunFor(GetDurationSeconds(targetPawn).SecondsToTicks(), Pawn, addBattleLog: false);
                }
            }
        }

        private new float GetDurationSeconds(Pawn targetPawn)
        {
            var value = base.GetDurationSeconds(targetPawn);

            if (Props.durationMultiplierCapacity != null)
                value *= targetPawn.health.capacities.GetLevel(Props.durationMultiplierCapacity);

            return value;
        }
        

        public override void DrawEffectPreview(LocalTargetInfo target)
        {
            GenDraw.DrawFieldEdges(AffectedCells(target));
        }

        public override bool AICanTargetNow(LocalTargetInfo target)
        {
            if (Pawn.Faction != null)
            {
                foreach (IntVec3 item in AffectedCells(target))
                {
                    List<Thing> thingList = item.GetThingList(Pawn.Map);
                    foreach (Thing t in thingList)
                    {
                        if (t.Faction == Pawn.Faction)
                        {
                            return false;
                        }
                    }
                }
            }
            return true;
        }

        [Unsaved(false)] 
        private readonly List<IntVec3> tmpCells = new();

        private List<IntVec3> AffectedCells(LocalTargetInfo target)
        {
            tmpCells.Clear();
            Vector3 vector = Pawn.Position.ToVector3Shifted().Yto0();
            IntVec3 targetPosition = target.Cell.ClampInsideMap(Pawn.Map);
            if (Pawn.Position == targetPosition)
            {
                return tmpCells;
            }
            float targetDistance = (targetPosition - Pawn.Position).LengthHorizontal;
            float scaledX = (float)(targetPosition.x - Pawn.Position.x) / targetDistance;
            float scaledY = (float)(targetPosition.z - Pawn.Position.z) / targetDistance;
            targetPosition.x = Mathf.RoundToInt((float)Pawn.Position.x + scaledX * Props.range);
            targetPosition.z = Mathf.RoundToInt((float)Pawn.Position.z + scaledY * Props.range);
            float angle = Vector3.SignedAngle(targetPosition.ToVector3Shifted().Yto0() - vector, Vector3.right, Vector3.up);
            float num3 = Props.lineWidthEnd / 2f;
            float num4 = Mathf.Sqrt(Mathf.Pow((targetPosition - Pawn.Position).LengthHorizontal, 2f) + Mathf.Pow(num3, 2f));
            float num5 = 57.29578f * Mathf.Asin(num3 / num4);
            int cellsInRadius = GenRadial.NumCellsInRadius(Props.range);
            for (int i = 0; i < cellsInRadius; i++)
            {
                IntVec3 intVec2 = Pawn.Position + GenRadial.RadialPattern[i];
                if (CanUseCell(intVec2) && Mathf.Abs(Mathf.DeltaAngle(Vector3.SignedAngle(intVec2.ToVector3Shifted().Yto0() - vector, Vector3.right, Vector3.up), angle)) <= num5)
                {
                    tmpCells.Add(intVec2);
                }
            }
            List<IntVec3> list = GenSight.BresenhamCellsBetween(Pawn.Position, targetPosition);
            for (var i = 0; i < list.Count; i++)
            {
                IntVec3 intVec3 = list[i];
                if (!tmpCells.Contains(intVec3) && CanUseCell(intVec3))
                {
                    tmpCells.Add(intVec3);
                }
            }

            return tmpCells;
            bool CanUseCell(IntVec3 c)
            {
                if (!c.InBounds(Pawn.Map))
                {
                    return false;
                }
                if (c == Pawn.Position)
                {
                    return false;
                }
                if (!Props.canHitFilledCells && c.Filled(Pawn.Map))
                {
                    return false;
                }
                if (!c.InHorDistOf(Pawn.Position, Props.range))
                {
                    return false;
                }

                return true;
            }
        }
    }
}
