using System.Linq;
using System.Runtime.CompilerServices;
using HarmonyLib;
using RimWorld;
using TranspilerUtil;
using Verse;
using XylXenos.Genes;

namespace XylXenos.Patches
{
    [HarmonyPatch]
    public static class PatchLactation
    {
        [Feature(typeof(Hyperlactation))]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [InfixPrefix(typeof(HediffSet), nameof(HediffSet.GetFirstHediffOfDef), [typeof(HediffDef), typeof(bool)])]
        [InfixPatch(typeof(ChildcareUtility), "CanBreastfeedNow")]
        [InfixPatch(typeof(ChildcareUtility), "SuckleFromLactatingPawn")]
        [InfixPatch(typeof(QuestPart_LendColonistsToFaction), "QuestPartTick")]
        [InfixPatch(typeof(Need_Food), "FoodFallPerTickAssumingCategory")]
        [InfixPatch(typeof(ITab_Pawn_Feeding), "DrawRow")]
        public static bool GetFirstHediffOfDef_Prefix(HediffSet __instance, HediffDef def, bool mustBeVisible, out Hediff __result)
        {
            if (def == HediffDefOf.Lactating && mustBeVisible == false)
            {
                __result = __instance.pawn.HediffsWithComp<HediffComp_Lactating>().FirstOrDefault();
                return false;
            }

            __result = default;
            return true;
        }

        public static Hediff GetLactationHediff(HediffSet hediffSet)
        {
            return hediffSet.pawn.HediffsWithComp<HediffComp_Lactating>().FirstOrDefault();
        }

        [Feature(typeof(Hyperlactation))]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [InfixPrefix(typeof(HediffSet), nameof(HediffSet.HasHediff), [typeof(HediffDef), typeof(bool)])]
        [InfixPatch(typeof(ChildcareUtility), "CanBreastfeed")]
        public static bool HasHediff_Prefix(HediffSet __instance, HediffDef def, bool mustBeVisible, out bool __result)
        {
            if (def == HediffDefOf.Lactating && mustBeVisible == false)
            {
                __result = __instance.pawn.HediffsWithComp<HediffComp_Lactating>().Any();
                return false;
            }

            __result = default;
            return true;
        }
    }
}
