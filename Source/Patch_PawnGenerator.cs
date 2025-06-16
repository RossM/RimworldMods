using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace XylRacesCore
{
    [HarmonyPatch(typeof(PawnGenerator), nameof(PawnGenerator.GetXenotypeForGeneratedPawn))]
    public class Patch_PawnGenerator
    {
        [HarmonyPostfix]
        static void Postfix(PawnGenerationRequest request, ref XenotypeDef __result)
        {
            if (Find.Scenario != null)
            {
                foreach (ScenPart allPart in Find.Scenario.AllParts)
                {
                    var part = allPart as ScenPart_ForcedXenotype;
                    if (part == null)
                        continue;

                    part.ModifyXenotype(request.Faction, request.Context, ref __result);
                }
            }
        }
    }
}
