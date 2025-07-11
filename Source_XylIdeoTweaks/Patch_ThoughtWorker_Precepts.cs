﻿using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Verse;

namespace Source_XylIdeoTweaks
{
    [HarmonyPatch]
    public class Patch_ThoughtWorker_Precepts
    {
        private static readonly IReadOnlyCollection<BodyPartGroupDef> GroinBodyParts = [BodyPartGroupDefOf.Legs];
        private static readonly IReadOnlyCollection<BodyPartGroupDef> HairOrFaceBodyParts = [BodyPartGroupDefOf.UpperHead, BodyPartGroupDefOf.FullHead];

        [HarmonyPrefix, UsedImplicitly, HarmonyPatch(typeof(ThoughtWorker_Precept_AnyBodyPartCovered), nameof(ThoughtWorker_Precept_AnyBodyPartCovered.HasUnnecessarilyCoveredBodyParts))]
        public static bool HasUnnecessarilyCoveredBodyParts_Prefix(Pawn p, ref bool __result)
        {
            __result = HasUnnecessaryApparel(p);
            return false;
        }

        [HarmonyPrefix, UsedImplicitly, HarmonyPatch(typeof(ThoughtWorker_Precept_AnyBodyPartButGroinCovered), nameof(ThoughtWorker_Precept_AnyBodyPartButGroinCovered.HasCoveredBodyPartsButGroin))]
        public static bool HasCoveredBodyPartsButGroin_Prefix(Pawn p, ref bool __result)
        {
            __result = HasUnnecessaryApparel(p, GroinBodyParts);
            return false;
        }

        [HarmonyPrefix, UsedImplicitly, HarmonyPatch(typeof(ThoughtWorker_Precept_AnyBodyPartButHairOrFaceCovered), nameof(ThoughtWorker_Precept_AnyBodyPartButHairOrFaceCovered.HasCoveredBodyPartsButHairOrFace))]
        public static bool HasCoveredBodyPartsButHairOrFace_Prefix(Pawn p, ref bool __result)
        {
            __result = HasUnnecessaryApparel(p, HairOrFaceBodyParts);
            return false;
        }

        private static bool HasUnnecessaryApparel(Pawn p, IReadOnlyCollection<BodyPartGroupDef> excludedParts = null)
        {
            // Change: Required apparel doesn't count as unnecessary

            if (p.apparel == null)
                return false;

            if (!GenTemperature.SafeTemperatureRange(p.def).Includes(p.AmbientTemperature))
                return false;

            foreach (Apparel apparel in p.apparel.WornApparel)
            {
                ThingDef def = apparel.def;
                if (!def.apparel.countsAsClothingForNudity)
                    continue;
                if (p.kindDef.apparelRequired?.Contains(def) == true)
                    continue;
                if (excludedParts != null && !def.apparel.bodyPartGroups.Contains(BodyPartGroupDefOf.Torso) && def.apparel.bodyPartGroups.Intersect(excludedParts).Any())
                    continue;
                if (p.apparel.ActiveRequirementsForReading.Any(requirement => requirement.ApparelMeetsRequirement(def)))
                    continue;
                if (p.ideo.Ideo.GetAllPreceptsOfType<Precept_Apparel>().Any(preceptApparel => ApparelRequiredBy(preceptApparel, def, p.gender)))
                    continue;

                return true;
            }

            return false;
        }

        private static bool ApparelRequiredBy(Precept_Apparel preceptApparel, ThingDef def, Gender gender)
        {
            return preceptApparel.apparelDef == def && (preceptApparel.TargetGender == Gender.None || preceptApparel.TargetGender == gender);
        }
    }

}
