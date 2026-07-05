using UnityEngine;

namespace XylIdeos;

public static class PatchHelpers
{
    public static Color? AutoColorColor(Pawn pawn) =>
        PawnData.Get(pawn).autoColorMode switch
        {
            AutoColorMode.UseFavoriteColor => pawn.story?.favoriteColor.color,
            AutoColorMode.UseIdeoligeonColor => pawn.Ideo?.ApparelColor,
            _ => null
        };

    public static bool HasUnnecessaryApparel(Pawn p, IReadOnlyCollection<BodyPartGroupDef> excludedParts = null)
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
            if (p.kindDef.apparelRequired?.Contains(def) is true)
                continue;
            if (excludedParts != null && !def.apparel.bodyPartGroups.Contains(BodyPartGroupDefOf.Torso) &&
                def.apparel.bodyPartGroups.Intersect(excludedParts).Any())
                continue;
            if (p.apparel.ActiveRequirementsForReading.Any(requirement => requirement.ApparelMeetsRequirement(def)))
                continue;
            if (ApparelRequired(p, def))
                continue;

            return true;
        }

        return false;
    }

    public static bool ApparelRequired(Pawn p, ThingDef def)
    {
        return p.ideo.Ideo.GetAllPreceptsOfType<Precept_Apparel>()
            .Any(preceptApparel => ApparelRequiredBy(preceptApparel, def, p.gender));
    }

    public static bool ApparelRequiredBy(Precept_Apparel preceptApparel, ThingDef def, Gender gender)
    {
        return preceptApparel.apparelDef == def &&
               (preceptApparel.TargetGender == Gender.None || preceptApparel.TargetGender == gender);
    }
}
