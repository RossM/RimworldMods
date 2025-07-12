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
        public static bool FinalizeIdeo_Prefix(Ideo ideo)
        {
            // Change: Only remove conflicting apparel precepts when nudity is required

            var nudityPrecepts = ideo.PreceptsListForReading.Where(precept => precept.def.prefersNudity).ToList();

            if (nudityPrecepts.Count == 0)
                return false;

            List<Precept> preceptsListForReading = ideo.PreceptsListForReading;
            for (int num = preceptsListForReading.Count - 1; num >= 0; num--)
            {
                if (preceptsListForReading[num] is Precept_Apparel preceptApparel && nudityPrecepts.Any(precept => !preceptApparel.CompatibleWith(precept)))
                {
                    ideo.RemovePrecept(preceptsListForReading[num]);
                }
            }
            return false;
        }
    }
}
