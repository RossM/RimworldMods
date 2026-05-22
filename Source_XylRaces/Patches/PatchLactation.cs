using System.Linq;
using System.Runtime.CompilerServices;
using HarmonyLib;
using RimWorld;
using Verse;
using XylXenos.Genes;

namespace XylXenos.Patches
{
    [HarmonyPatch]
    public static class PatchLactation
    {
        [Feature(typeof(Hyperlactation))]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [WrappedMember(typeof(HediffSet), nameof(HediffSet.GetFirstHediffOfDef), [typeof(HediffDef), typeof(bool)])]
        [InfixPatch(typeof(ChildcareUtility), "CanBreastfeedNow")]
        [InfixPatch(typeof(ChildcareUtility), "SuckleFromLactatingPawn")]
        [InfixPatch(typeof(QuestPart_LendColonistsToFaction), "QuestPartTick")]
        [InfixPatch(typeof(Need_Food), "FoodFallPerTickAssumingCategory")]
        [InfixPatch(typeof(ITab_Pawn_Feeding), "DrawRow")]
        public static Hediff GetFirstHediffOfDef_Wrapper(HediffSet __instance, HediffDef def, bool mustBeVisible)
        {
            if (def == HediffDefOf.Lactating && mustBeVisible == false)
                return __instance.pawn.HediffsWithComp<HediffComp_Lactating>().FirstOrDefault();
            return __instance.GetFirstHediffOfDef(def, mustBeVisible);
        }

        [Feature(typeof(Hyperlactation))]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [WrappedMember(typeof(HediffSet), nameof(HediffSet.HasHediff), [typeof(HediffDef), typeof(bool)])]
        [InfixPatch(typeof(ChildcareUtility), "CanBreastfeed")]
        public static bool HasHediff_Wrapper(HediffSet __instance, HediffDef def, bool mustBeVisible)
        {
            if (def == HediffDefOf.Lactating && mustBeVisible == false)
                return __instance.pawn.HediffsWithComp<HediffComp_Lactating>().Any();
            return __instance.HasHediff(def, mustBeVisible);
        }
    }
}
