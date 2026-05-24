using System.Collections.Generic;
using System.Runtime.CompilerServices;
using HarmonyLib;
using RimWorld;
using TranspilerUtil;
using Verse;

namespace XylXenos.Patches
{
    [HarmonyPatch(typeof(PawnBioAndNameGenerator))]
    public static class Patch_PawnBioAndNameGenerator
    {
        private delegate bool TryGiveSolidBioTo_Fn(Pawn pawn, string requiredLastName, List<BackstoryCategoryFilter> backstoryCategories);

        private static readonly TryGiveSolidBioTo_Fn TryGiveSolidBioTo_Original = AccessTools.MethodDelegate<TryGiveSolidBioTo_Fn>(
            AccessTools.Method(typeof(PawnBioAndNameGenerator), "TryGiveSolidBioTo"));

        [Feature(typeof(XenotypeDefExtension))]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [InfixPrefix(typeof(PawnBioAndNameGenerator), "TryGiveSolidBioTo")]
        [InfixPatch(nameof(PawnBioAndNameGenerator.GiveAppropriateBioAndNameTo))]
        public static bool TryGiveSolidBioTo_Prefix(XenotypeDef xenotype, out bool __result)
        {
            __result = false;
            return Settings.instance.AllowBackerBackstoriesFor(xenotype);
        }
    }
}
