using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using JetBrains.Annotations;
using RimWorld;
using Verse;

namespace XylRacesCore.Patches
{
    [HarmonyPatch(typeof(RecipeDef))]
    public static class Patch_RecipeDef
    {
        [HarmonyPostfix, UsedImplicitly, HarmonyPatch(nameof(RecipeDef.AvailableNow), MethodType.Getter)]
        public static void AvailableNow_Postfix(RecipeDef __instance, ref bool __result)
        {
            using (new ProfileBlock())
            {
                if (DebugSettings.godMode)
                    return;
                if (__result == false)
                    return;

                foreach (ThingDef def in __instance.products.Select(t => t.thingDef))
                {
                    var extension = def.GetModExtension<DefExt_GeneDependent>();
                    if (extension == null)
                        continue;

                    if (!extension.Validate())
                    {
                        __result = false;
                        return;
                    }
                }
            }


        }
    }
}
