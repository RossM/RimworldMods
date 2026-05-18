using System;
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
    [HarmonyPatch]
    public static class PatchBiostatMet
    {
        private static readonly InstructionMatcher Fixup_BiostatMet = new()
        {
            Rules =
            {
                InstructionMatcher.MakeRedirectRule(nameof(GeneDef.biostatMet), GeneDef_biostatMet_Wrapper)
            }
        };

        [UsedImplicitly]
        public static IEnumerable<MethodBase> TargetMethods()
        {
            yield return AccessTools.Method(typeof(Dialog_CreateXenotype), "DrawGene");
            yield return AccessTools.Method(typeof(GeneCreationDialogBase), "OnGenesChanged");
            yield return AccessTools.Method(typeof(GeneDef), "GetDescriptionFull");
            Type iteratorType = AccessTools.InnerTypes(typeof(GeneDef)).First(type => type.Name.Contains("<SpecialDisplayStats>"));
            yield return AccessTools.Method(iteratorType, "MoveNext");
        }

        [Feature(nameof(BonusGene))]
        [HarmonyTranspiler]
        [UsedImplicitly]
        public static IEnumerable<CodeInstruction> Transpiler(
            IEnumerable<CodeInstruction> instructions,
            ILGenerator generator,
            MethodBase method)
        {
            var instructionsList = new List<CodeInstruction>(instructions);
            Fixup_BiostatMet.MatchAndReplace(method, ref instructionsList, generator);
            return instructionsList;
        }

        public static int GeneDef_biostatMet_Wrapper(GeneDef __instance)
        {
            return __instance.BiostatMetForDisplay();
        }
    }
}
