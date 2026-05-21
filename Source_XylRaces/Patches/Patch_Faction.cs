using HarmonyLib;
using RimWorld;
using Verse;

namespace XylXenos.Patches
{
    [HarmonyPatch(typeof(Faction))]
    public static class Patch_Faction
    {
        [Feature(typeof(IncidentWorker_WildTribe))]
        [HarmonyPrefix]
        [HarmonyPatch(nameof(Faction.Notify_PawnJoined))]
        public static bool Notify_PawnJoined_Prefix(Faction __instance, Pawn p)
        {
            __instance.ideos?.Notify_MemberGainedOrLost();

            if (p.RaceProps.Humanlike && !__instance.def.humanlikeFaction && !p.IsSubhuman && !p.IsCreepJoiner && !p.IsWildMan() &&
                !p.IsDuplicate && !p.Dead)
                Log.Error("Humanlike pawn " + p.LabelShort + " was added to non-humanlike faction " + __instance.def.label);

            return false;
        }
    }
}
