using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace Source_XylIdeoTweaks
{
    [HarmonyPatch(typeof(IdeoFoundation))]
    public class Patch_IdeoFoundation
    {
        [HarmonyPrefix, UsedImplicitly, HarmonyPatch("FinalizeIdeo")]
        public static bool FinalizeIdeo_Prefix()
        {
            // Change: Do not remove apparel precepts when nudity is required
            return false;
        }
    }
}
