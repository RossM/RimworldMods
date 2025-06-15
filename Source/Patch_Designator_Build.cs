using HarmonyLib;
using RimWorld;
using Verse;

namespace XylRacesCore
{
    [HarmonyPatch(typeof(Designator_Build), nameof(Designator_Build.Visible), MethodType.Getter)]
    public class Patch_Designator_Build
    {
        [HarmonyPostfix]
        static void Postfix(Designator_Build __instance, ref bool __result)
        {
            if (DebugSettings.godMode)
                return;

            BuildableDef def = __instance.PlacingDef;
            ModExtension_BuildableDef extension = def.GetModExtension<ModExtension_BuildableDef>();
            if (extension == null) 
                return;

            if (extension.genePrerequisites != null)
            {
                foreach (var gene in extension.genePrerequisites)
                {
                    if (!__instance.Map.mapPawns.PawnsInFaction(Faction.OfPlayer)
                            .Any((Pawn p) => p.genes?.HasActiveGene(gene) ?? false))
                    {
                        __result = false;
                        return;
                    }
                }
            }
        }
    }
}
