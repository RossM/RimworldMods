using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using RimWorld;
using Verse;

namespace XylRacesCore
{
    [HarmonyPatch(typeof(Toils_LayDown), "ApplyBedThoughts")]
    public class Patch_Toils_LayDown
    {
        [HarmonyPostfix]
        public static void ApplyBedThoughts_Postfix(Pawn actor, Building_Bed bed)
        {
            if (bed != null && bed == actor.ownership.OwnedBed && !bed.ForPrisoners && !actor.story.traits.HasTrait(TraitDefOf.Ascetic))
            {
                Room room = bed.GetRoom();
                if (room != null)
                {
                    ThoughtDef thoughtDef = null;
                    if (room.Role == RoomRoleDefOf.Barracks)
                    {
                        thoughtDef = DefDatabase<ThoughtDef>.GetNamed("XylSleptInBarracks_HerdInstinct");
                    }
                    if (thoughtDef != null)
                    {
                        int scoreStageIndex = RoomStatDefOf.Impressiveness.GetScoreStageIndex(bed.GetRoom().GetStat(RoomStatDefOf.Impressiveness));
                        if (thoughtDef.stages[scoreStageIndex] != null)
                        {
                            actor.needs.mood.thoughts.memories.TryGainMemory(ThoughtMaker.MakeThought(thoughtDef, scoreStageIndex));
                        }
                    }
                }
            }
        }
    }
}
