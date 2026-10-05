using System;
using System.Collections.Generic;
using System.Text;

namespace Xylib.Patches
{
    [Patch(typeof(VacuumUtility))]
    public static class Patch_VacuumUtility
    {
        [Feature(nameof(GeneWithComps.ImmuneToVacuumBurns))]
        [Postfix]
        [Inner(typeof(GeneDef), nameof(GeneDef.immuneToVacuumBurns), MemberType.Getter)]
        [Target(nameof(VacuumUtility.CanBeVacuumBurnt))]
        public static void CanBeVacuumBurnt_Postfix(Pawn pawn, GeneDef __instance, ref bool __result)
        {
            if (pawn.genes.GetGene(__instance) is GeneWithComps geneWithComps)
                __result = geneWithComps.ImmuneToVacuumBurns;
        }
    }
}
