using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using JetBrains.Annotations;
using RimWorld;
using TranspilerUtil;

namespace XylXenos.Patches
{
    [HarmonyPatch(typeof(FactionDef))]
    public static class Patch_FactionDef
    {
        private static readonly InstructionMatcher Fixup_DefaultXenotype = new()
        {
            Rules =
            {
                InstructionMatcher.MakeRedirectRule(nameof(XenotypeDefOf.Baseliner), XenotypeDefOf_Baseliner_Wrapper),
            }
        };

        [Feature(typeof(XenotypeSetWithDefault))]
        [HarmonyTranspiler]
        [UsedImplicitly]
        [HarmonyPatch(nameof(FactionDef.Description), MethodType.Getter)]
        public static IEnumerable<CodeInstruction> Description_Transpiler(
            IEnumerable<CodeInstruction> instructions,
            ILGenerator generator,
            MethodBase method)
        {
            var instructionsList = new List<CodeInstruction>(instructions);
            Fixup_DefaultXenotype.MatchAndReplace(method, ref instructionsList, generator);
            return instructionsList;
        }

        public static XenotypeDef XenotypeDefOf_Baseliner_Wrapper(FactionDef __caller)
        {
            return XenotypeSetWithDefault.GetDefaultXenotype(__caller.xenotypeSet);
        }
    }
}
