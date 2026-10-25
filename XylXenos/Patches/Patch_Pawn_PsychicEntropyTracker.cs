using RimWorld.Planet;

namespace XylXenos.Patches;

[Patch(typeof(Pawn_PsychicEntropyTracker))]
internal static class Patch_Pawn_PsychicEntropyTracker
{
    [Feature(typeof(Psycast))]
    [Postfix]
    [Target(nameof(Pawn_PsychicEntropyTracker.NeedToShowGizmo))]
    public static void NeedToShowGizmo_Postfix(Pawn_PsychicEntropyTracker __instance, ref bool __result)
    {
        if (__result)
            return;
        if (__instance.Pawn.HasActivePsycastGene)
            __result = true;
    }

    // Note: This patch is performance-sensitive
    [Feature(typeof(Psycast))]
    [Postfix]
    [Target(nameof(Pawn_PsychicEntropyTracker.NeedsPsyfocus), MemberType.Getter)]
    public static void NeedsPsyfocus_Postfix(Pawn_PsychicEntropyTracker __instance, ref bool __result)
    {
        if (__result)
            return;
        if (__instance.Pawn is { HasActivePsycastGene: true, Suspended: false } && (__instance.Pawn.Spawned || __instance.Pawn.IsCaravanMember()))
            __result = true;
    }
}
