using HarmonyLib;
using JetBrains.Annotations;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
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

        [Feature(nameof(Genes.GeneDefExtension)), HarmonyTranspiler, UsedImplicitly, HarmonyPatch("GetDescriptionFull")]
        public static IEnumerable<CodeInstruction> GetDescriptionFull_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator, MethodBase method)
        {
            var instructionsList = new List<CodeInstruction>(instructions);
            Fixup_GetDescriptionFull.MatchAndReplace(method, ref instructionsList, generator);
            return instructionsList;
        }

        public static List<string> GeneDef_customEffectDescriptions_Wrapper(GeneDef __instance)
        {
            return GeneDef_customEffectDescriptions_Wrapper_Inner(__instance).ToList();
        }

        public static IEnumerable<string> GeneDef_customEffectDescriptions_Wrapper_Inner(GeneDef __instance)
        {
            if (!__instance.customEffectDescriptions.NullOrEmpty())
            {
                foreach (var s in __instance.customEffectDescriptions)
                    yield return s;
            }

            if (!__instance.modExtensions.NullOrEmpty())
            {
                foreach (var ext in __instance.modExtensions.OfType<GeneDefExtension>())
                {
                    foreach (var s in ext.CustomEffectDescriptions)
                        yield return s;
                }
            }
        }
    }
}
