using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using JetBrains.Annotations;
using RimWorld;
using TranspilerUtil;

namespace XylXenos.Patches
{
    [HarmonyPatch(typeof(XenotypeSet))]
    public static class Patch_XenotypeSet
    {
        private static readonly InstructionMatcher Fixup_DefaultXenotype = new()
        {
            Rules =
            {
                InstructionMatcher.MakeRedirectRule(nameof(XenotypeDefOf.Baseliner), XenotypeDefOf_Baseliner_Wrapper),
            }
        };

        [Feature(nameof(XenotypeSetWithDefault))]
        [HarmonyTranspiler]
        [UsedImplicitly]
        [HarmonyPatch(nameof(XenotypeSet.BaselinerChance), MethodType.Getter)]
        public static IEnumerable<CodeInstruction> BaselinerChance_Transpiler(
            IEnumerable<CodeInstruction> instructions,
            ILGenerator generator,
            MethodBase method)
        {
            var instructionsList = new List<CodeInstruction>(instructions);
            Fixup_DefaultXenotype.MatchAndReplace(method, ref instructionsList, generator);
            return instructionsList;
        }

        [Feature(nameof(XenotypeSetWithDefault))]
        [HarmonyTranspiler]
        [UsedImplicitly]
        [HarmonyPatch(nameof(XenotypeSet.Contains))]
        public static IEnumerable<CodeInstruction> Contains_Transpiler(
            IEnumerable<CodeInstruction> instructions,
            ILGenerator generator,
            MethodBase method)
        {
            var instructionsList = new List<CodeInstruction>(instructions);
            Fixup_DefaultXenotype.MatchAndReplace(method, ref instructionsList, generator);
            return instructionsList;
        }

        public static XenotypeDef XenotypeDefOf_Baseliner_Wrapper(XenotypeSet __caller)
        {
            return XenotypeSetWithDefault.GetDefaultXenotype(__caller);
        }
    }
}
