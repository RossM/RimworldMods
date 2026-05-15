using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using JetBrains.Annotations;
using TranspilerUtil;
using Verse;
using XylRacesCore.Genes;

namespace XylRacesCore.Patches
{
    [HarmonyPatch(typeof(GeneDef))]
    public static class Patch_GeneDef
    {
        private static readonly InstructionMatcher Fixup_GetDescriptionFull = new()
        {
            Rules =
            {
                InstructionMatcher.MakeRedirectRule(
                    AccessTools.Field(typeof(GeneDef), nameof(GeneDef.customEffectDescriptions)),
                    AccessTools.Method(typeof(Patch_GeneDef), nameof(GeneDef_customEffectDescriptions_Wrapper))
                )
            }
        };

        [Feature(nameof(GeneDefExtension))]
        [HarmonyTranspiler]
        [UsedImplicitly]
        [HarmonyPatch("GetDescriptionFull")]
        public static IEnumerable<CodeInstruction> GetDescriptionFull_Transpiler(
            IEnumerable<CodeInstruction> instructions,
            ILGenerator generator,
            MethodBase method)
        {
            var instructionsList = new List<CodeInstruction>(instructions);
            Fixup_GetDescriptionFull.MatchAndReplace(method, ref instructionsList, generator);
            return instructionsList;
        }

        public static List<string> GeneDef_customEffectDescriptions_Wrapper(GeneDef __instance)
        {
            return GeneHelpers.GetGeneEffectDescriptions(__instance).ToList();
        }
    }
}
