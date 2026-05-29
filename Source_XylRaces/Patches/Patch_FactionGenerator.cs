using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace XylXenos.Patches
{
    [HarmonyPatch(typeof(FactionGenerator))]
    public static class Patch_FactionGenerator
    {
        [Feature(nameof(Settings.useDistinctiveFactionColors))]
        [HarmonyPostfix]
        [HarmonyPatch("InitializeFactions")]
        public static void InitializeFactions_Postfix(PlanetLayer layer)
        {
            if (!Settings.instance.useDistinctiveFactionColors)
                return;

            PatchHelpers.ReassignFactionColors(layer);
        }
    }
}
