using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using HarmonyLib;
using JetBrains.Annotations;
using RimWorld;
using TranspilerUtil;
using Verse;

namespace XylXenos.Patches
{
    [HarmonyPatch(typeof(RecipeDef))]
    public static class Patch_RecipeDef
    {
        private static readonly InstructionMatcher Fixup_AvailableNow = new()
        {
            Rules =
            {
                InstructionMatcher.MakeRedirectRule("memePrerequisitesAny", RecipeDef_memePrerequisitesAny_Wrapper)
            }
        };

        [Feature(nameof(DefModExtension_GeneDependent))]
        [HarmonyTranspiler]
        [UsedImplicitly]
        [HarmonyPatch(nameof(RecipeDef.AvailableNow), MethodType.Getter)]
        public static IEnumerable<CodeInstruction> AvailableNow_Transpiler(
            IEnumerable<CodeInstruction> instructions,
            ILGenerator generator,
            MethodBase method)
        {
            var instructionsList = new List<CodeInstruction>(instructions);
            Fixup_AvailableNow.MatchAndReplace(method, ref instructionsList, generator);
            return instructionsList;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static List<MemeDef> RecipeDef_memePrerequisitesAny_Wrapper()
        {
            return null;
        }

        [Feature(nameof(DefModExtension_GeneDependent))]
        [HarmonyPostfix]
        [UsedImplicitly]
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
