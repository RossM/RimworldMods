namespace XylXenos.Patches;

[HarmonyPatch(typeof(Pawn_RoyaltyTracker))]
public static class Patch_Pawn_RoyaltyTracker
{
    [Feature(nameof(Config.Feature.Bugfix_Misc))]
    [InnerPrefix(typeof(LetterStack), nameof(LetterStack.ReceiveLetter), typeof(TaggedString), typeof(TaggedString), typeof(LetterDef),
        typeof(LookTargets), typeof(Faction), typeof(Quest), typeof(List<ThingDef>), typeof(string), typeof(int), typeof(bool))]
    [Target(nameof(Pawn_RoyaltyTracker.RoyaltyTrackerTickInterval))]
    public static bool ReceiveLetter_Prefix(Pawn_RoyaltyTracker __caller)
    {
        return PawnUtility.ShouldSendNotificationAbout(__caller.pawn);
    }
}
