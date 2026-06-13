using HarmonyLib;
using JetBrains.Annotations;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TranspilerUtil;
using Verse;

namespace Source_XylIdeoTweaks
{
    [DefOf]
    public static class MyTraitDefOf
    {
        public static TraitDef Masochist;
    }

    [UsedImplicitly]
    [HarmonyPatch(typeof(JobGiver_OptimizeApparel))]
    public static class Patch_JobGiver_OptimizeApparel
    {
        [InfixPostfix(typeof(ApparelProperties), nameof(ApparelProperties.slaveApparel))]
        [InfixPatch(nameof(JobGiver_OptimizeApparel.ApparelScoreRaw))]
        public static void slaveApparel_Postfix(Pawn pawn, Apparel ap, ref bool __result)
        {
            if (!__result || pawn == null)
                return;

            __result = !pawn.story.traits.HasTrait(MyTraitDefOf.Masochist) && !Patch_ThoughtWorker_Precepts.ApparelRequired(pawn, ap.def);
        }
    }
}
