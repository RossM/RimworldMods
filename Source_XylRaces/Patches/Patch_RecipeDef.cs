using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using HarmonyLib;
using RimWorld;
using TranspilerUtil;
using Verse;

namespace XylXenos.Patches
{
    [HarmonyPatch(typeof(RecipeDef))]
    public static class Patch_RecipeDef
    {
        private static readonly InstructionMatcher.Rule Rule_RecipeDef_memePrerequisitesAny
            = InstructionMatcher.MakeRedirectRule("memePrerequisitesAny", RecipeDef_memePrerequisitesAny_Wrapper);

        [Feature(typeof(DefModExtension_GeneDependent))]
        [HarmonyTranspiler]
        [HarmonyPatch(nameof(RecipeDef.AvailableNow), MethodType.Getter)]
        public static IEnumerable<CodeInstruction> AvailableNow_Transpiler(
            IEnumerable<CodeInstruction> instructions,
            ILGenerator generator,
            MethodBase method)
        {
            var instructionsList = new List<CodeInstruction>(instructions);
            new InstructionMatcher()
            {
                Rules =
                {
                    Rule_RecipeDef_memePrerequisitesAny
                }
            }.MatchAndReplace(method, ref instructionsList, generator);
            return instructionsList;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static List<MemeDef> RecipeDef_memePrerequisitesAny_Wrapper()
        {
            return null;
        }

        [Feature(typeof(DefModExtension_GeneDependent))]
        [HarmonyPostfix]
        [HarmonyPatch(nameof(RecipeDef.AvailableNow), MethodType.Getter)]
        public static void AvailableNow_Postfix(RecipeDef __instance, ref bool __result)
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

            if (__instance.memePrerequisitesAny != null
                && __instance.memePrerequisitesAny.Any(memeDef => Faction.OfPlayer.ideos.HasAnyIdeoWithMeme(memeDef)))
                return;

            __result = false;
        }
    }
}
