using HarmonyLib;
using JetBrains.Annotations;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace XylRacesCore.Patches
{
    [HarmonyPatch(typeof(RecipeDef))]
    public static class Patch_RecipeDef
    {
        private static readonly InstructionMatcher Fixup_AvailableNow = new()
        {
            Rules =
            {
                new()
                {
                    Min = 1, Max = 1,
                    Mode = InstructionMatcher.OutputMode.Replace,
                    Pattern =
                    [
                        CodeInstruction.LoadArgument(0),
                        CodeInstruction.LoadField(typeof(Verse.RecipeDef), "memePrerequisitesAny"),
                    ],
                    Output =
                    [
                        new CodeInstruction(OpCodes.Ldc_I4_0),
                    ]
                }
            }
        };

        [Feature(nameof(DefModExtension_GeneDependent)), HarmonyTranspiler, UsedImplicitly, HarmonyPatch(nameof(RecipeDef.AvailableNow), MethodType.Getter)]
        public static IEnumerable<CodeInstruction> AvailableNow_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var instructionsList = new List<CodeInstruction>(instructions);
            Fixup_AvailableNow.MatchAndReplace(ref instructionsList, generator);
            return instructionsList;
        }

        [Feature(nameof(DefModExtension_GeneDependent)), HarmonyPostfix, UsedImplicitly, HarmonyPatch(nameof(RecipeDef.AvailableNow), MethodType.Getter)]
        public static void AvailableNow_Postfix(RecipeDef __instance, ref bool __result)
        {
            using (new ProfileBlock())
            {
                if (DebugSettings.godMode)
                    return;
                if (__result == false)
                    return;

                DefModExtension_GeneDependent extension =
                    __instance.GetModExtension<DefModExtension_GeneDependent>() ??
                    __instance.products.Select(t => t.thingDef.GetModExtension<DefModExtension_GeneDependent>()).FirstOrDefault(e => e != null);

                if (extension == null && __instance.memePrerequisitesAny == null)
                    return;

                if (extension != null && extension.Validate())
                    return;

                if (__instance.memePrerequisitesAny != null && __instance.memePrerequisitesAny.Any(memeDef => Faction.OfPlayer.ideos.HasAnyIdeoWithMeme(memeDef)))
                    return;

                __result = false;
            }
        }
    }
}
