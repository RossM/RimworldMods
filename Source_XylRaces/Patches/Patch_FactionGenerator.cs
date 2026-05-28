using HarmonyLib;
using RimWorld;
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
        [Feature("TODO")]
        [HarmonyPostfix]
        [HarmonyPatch(nameof(FactionGenerator.NewRandomColorFromSpectrum))]
        public static void NewRandomColorFromSpectrum_Postfix(Faction faction, ref float __result)
        {
            List<Faction> allFactions = Find.FactionManager.AllFactionsListForReading;
            List<Color> factionColors = allFactions.Select(otherFaction => otherFaction.Color).ToList();

            if (faction.def.colorSpectrum.NullOrEmpty())
                return;

            float bestColorFromSpectrum = 0f;
            float bestDistanceMin = -1f;

            for (int i = 0; i < 20; i++)
            {
                float colorFromSpectrum = Rand.Value;
                float distanceMin = float.MaxValue;
                Color color = ColorsFromSpectrum.Get(faction.def.colorSpectrum, colorFromSpectrum);

                foreach (Color otherColor in factionColors)
                    distanceMin = Mathf.Min(distanceMin, ColorDistance(color, otherColor));

                if (distanceMin > bestDistanceMin)
                {
                    bestColorFromSpectrum = colorFromSpectrum;
                    bestDistanceMin = distanceMin;
                }

                Debug.Log($"faction={faction} bestColorFromSpectrum={bestColorFromSpectrum} bestDistanceMin={bestDistanceMin}");
            }

            __result = bestColorFromSpectrum;

            float ColorDistance(Color a, Color b)
            {
                Color diff = a - b;
                return diff.r * diff.r + 2 * diff.g * diff.g + diff.b * diff.b;
            }
        }
    }
}
