using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using JetBrains.Annotations;
using RimWorld;
using TranspilerUtil;
using Verse;
using XylXenos.Genes;

namespace XylXenos.Patches
{
    [HarmonyPatch(typeof(GeneDef))]
    public static class Patch_GeneDef
    {
        private static readonly InstructionMatcher.Rule Rule_GeneDef_customEffectDescriptions
            = InstructionMatcher.MakeRedirectRule(nameof(GeneDef.customEffectDescriptions), GeneDef_customEffectDescriptions_Wrapper);

        [Feature(typeof(GeneDefExtension))]
        [HarmonyTranspiler]
        [HarmonyPatch("GetDescriptionFull")]
        public static IEnumerable<CodeInstruction> GetDescriptionFull_Transpiler(
            IEnumerable<CodeInstruction> instructions,
            ILGenerator generator,
            MethodBase method)
        {
            var instructionsList = new List<CodeInstruction>(instructions);
            new InstructionMatcher()
            {
                Rules =
                {
                    Rule_GeneDef_customEffectDescriptions
                }
            }.MatchAndReplace(method, ref instructionsList, generator);
            return instructionsList;
        }

        public static List<string> GeneDef_customEffectDescriptions_Wrapper(GeneDef __instance)
        {
            return __instance.GetGeneEffectDescriptions().ToList();
        }

        [Feature(typeof(GeneDefExtension))]
        [HarmonyPostfix]
        [UsedImplicitly]
        [HarmonyPatch("SpecialDisplayStats")]
        public static void SpecialDisplayStats_Postfix(GeneDef __instance, StatRequest req, ref IEnumerable<StatDrawEntry> __result)
        {
            var extraStats = __instance.GetGeneSpecialDisplayStats().ToList();
            if (extraStats.Count > 0)
                __result = __result.Concat(extraStats);
        }
    }
}
