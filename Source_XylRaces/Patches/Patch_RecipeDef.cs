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

                DefModExtension_GeneDependent extension = __instance.products
                    .Select(t => t.thingDef.GetModExtension<DefModExtension_GeneDependent>()).FirstOrDefault(e => e != null);

                if (extension == null)
                    return;

                __result = extension.Validate();
            }
        }
    }
}
