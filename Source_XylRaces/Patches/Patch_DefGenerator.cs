using HarmonyLib;
using JetBrains.Annotations;
using RimWorld;
using Verse;

namespace XylXenos.Patches
{
    [HarmonyPatch(typeof(DefGenerator))]
    public static class Patch_DefGenerator
    {
        [Feature(typeof(GeneDefGenerator_Psy))]
        [HarmonyPostfix]
        [HarmonyPatch(nameof(DefGenerator.GenerateImpliedDefs_PreResolve))]
        public static void GenerateImpliedDefs_PreResolve_Postfix(bool hotReload)
        {
            foreach (GeneDef def in GeneDefGenerator_Psy.ImpliedGeneDefs(hotReload))
                DefGenerator.AddImpliedDef(def, hotReload);
        }
    }
}
