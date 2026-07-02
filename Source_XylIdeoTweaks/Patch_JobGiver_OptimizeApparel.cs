using UnityEngine;

namespace XylIdeos;

[DefOf]
public static class MyTraitDefOf
{
    public static TraitDef Masochist;
}

[UsedImplicitly]
[HarmonyPatch(typeof(JobGiver_OptimizeApparel))]
public static class Patch_JobGiver_OptimizeApparel
{
    [Feature(Features.MasochistsCanWearCollars)]
    [InfixPostfix(typeof(ApparelProperties), nameof(ApparelProperties.slaveApparel))]
    [InfixPatch(nameof(JobGiver_OptimizeApparel.ApparelScoreRaw))]
    public static void slaveApparel_Postfix(Pawn pawn, Apparel ap, ref bool __result)
    {
        if (!__result || pawn == null)
            return;

        __result = !pawn.story.traits.HasTrait(MyTraitDefOf.Masochist) && !Patch_ThoughtWorker_Precepts.ApparelRequired(pawn, ap.def);
    }

    [Feature(Features.AutoColorApparel)]
    [HarmonyPrefix]
    [HarmonyPatch(nameof(JobGiver_OptimizeApparel.TryCreateRecolorJob))]
    public static void TryCreateRecolorJob_Prefix(Pawn pawn)
    {
        if (PatchHelpers.AutoColorColor(pawn) is not { } color)
            return;

        foreach (var item in pawn.apparel.WornApparel)
        {
            if (item.TryGetComp<CompColorable>() != null)
            {
                Color oldColor = item.GetColorIgnoringTainted();
                item.DesiredColor = color.IndistinguishableFrom(oldColor) ? null : color;
            }
        }
    }
}
