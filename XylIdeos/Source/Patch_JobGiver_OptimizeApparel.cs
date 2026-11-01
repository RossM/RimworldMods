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
    [Postfix] [Inner(typeof(ApparelProperties), nameof(ApparelProperties.slaveApparel))]
    [Target(nameof(JobGiver_OptimizeApparel.ApparelScoreRaw))]
    public static void slaveApparel_Postfix(Pawn pawn, Apparel ap, ref bool __result)
    {
        if (!__result || pawn == null)
            return;

        __result = !pawn.story.traits.HasTrait(MyTraitDefOf.Masochist) && !PatchHelpers.ApparelRequired(pawn, ap.def);
    }
}
