namespace Xylib.Patches;

[Patch(typeof(Pawn_AgeTracker))]
internal static class Patch_Pawn_AgeTracker
{
    [Feature(nameof(EventDefOf.PostBirthday))]
    [Postfix]
    [Target("BirthdayBiological")]
    public static void BirthdayBiological_Postfix(Pawn ___pawn)
    {
        EventManager.Instance.Notify(EventDefOf.PostBirthday, ___pawn);
    }
}
