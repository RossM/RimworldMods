using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using JetBrains.Annotations;
using RimWorld;
using TranspilerUtil;
using Verse;

namespace XylXenos.Patches
{
    [HarmonyPatch(typeof(Recipe_RemoveBodyPart))]
    public static class Patch_Recipe_RemoveBodyPart
    {
        private static readonly InstructionMatcher Fixup_Hediff_Label = new()
        {
            Rules =
            {
                InstructionMatcher.MakeRedirectRule(
                    AccessTools.PropertyGetter(typeof(Hediff), nameof(Hediff.Label)), 
                    Hediff_Label_Wrapper)
            }
        };

        [Feature("TODO")]
        [HarmonyTranspiler]
        [UsedImplicitly]
        [HarmonyPatch(nameof(Recipe_RemoveBodyPart.GetLabelWhenUsedOn))]
        public static IEnumerable<CodeInstruction> GetLabelWhenUsedOn_Transpiler(
            IEnumerable<CodeInstruction> instructions,
            ILGenerator generator,
            MethodBase method)
        {
            var instructionsList = new List<CodeInstruction>(instructions);
            Fixup_Hediff_Label.MatchAndReplace(method, ref instructionsList, generator);
            return instructionsList;
        }

        public static string Hediff_Label_Wrapper(Hediff __instance)
        {
            return __instance.def == DefOf.XylPetrifiedTotal ? __instance.def.labelNoun : __instance.Label;
        }
    }
}
