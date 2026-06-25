namespace XylXenos.Patches;

[HarmonyPatch(typeof(Pawn_RoyaltyTracker))]
public static class Patch_Pawn_RoyaltyTracker
{
    [Feature(nameof(Config.Feature.Bugfix_Misc))]
    [InfixPrefix(typeof(LetterStack), nameof(LetterStack.ReceiveLetter),
    [
        typeof(TaggedString), typeof(TaggedString), typeof(LetterDef), typeof(LookTargets), typeof(Faction), typeof(Quest),
        typeof(List<ThingDef>), typeof(string), typeof(int), typeof(bool)
    ])]
    [InfixPatch(nameof(Pawn_RoyaltyTracker.RoyaltyTrackerTickInterval))]
    public static bool ReceiveLetter_Prefix(Pawn_RoyaltyTracker __instance)
    {
        return PawnUtility.ShouldSendNotificationAbout(__instance.pawn);
    }
}
