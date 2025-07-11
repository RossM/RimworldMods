﻿using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace XylRacesCore
{
    public class CompProperties_AbilitySonicWave : CompProperties_AbilityEffectWithDuration
    {
        public float range;
        public float radius;
        public bool canHitFilledCells;
        public bool affectMechanoids;

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
                    if (Props.affectMechanoids || !targetPawn.RaceProps.IsMechanoid)
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
                    foreach (var targetPawn in thingList.OfType<Pawn>())
                    {
                        if (targetPawn.Faction == Pawn.Faction && (Props.affectMechanoids || !targetPawn.RaceProps.IsMechanoid))
                        {
                            return false;
                        }
                    }
                }
            }
            return true;
        }

        [Unsaved()] 
        private readonly List<IntVec3> tmpCells = [];

        private List<IntVec3> AffectedCells(LocalTargetInfo target)
        {
            tmpCells.Clear();
            IntVec3 targetPosition = target.Cell.ClampInsideMap(Pawn.Map);
            if (Pawn.Position == targetPosition)
            {
                return tmpCells;
            }

            int cellsInRadius = GenRadial.NumCellsInRadius(Props.radius);
            for (int i = 0; i < cellsInRadius; i++)
            {
                IntVec3 intVec2 = targetPosition + GenRadial.RadialPattern[i];
                if (CanUseCell(intVec2))
                {
                    tmpCells.Add(intVec2);
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
                if (!c.InHorDistOf(targetPosition, Props.radius))
                {
                    return false;
                }

                return true;
            }
        }
    }
}
