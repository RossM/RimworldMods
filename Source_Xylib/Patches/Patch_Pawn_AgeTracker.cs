namespace Xylib.Patches;

[HarmonyPatch(typeof(Pawn_AgeTracker))]
public class Patch_Pawn_AgeTracker
{
    [Feature(nameof(EventDefOf.PostBirthday))]
    [HarmonyPostfix]
    [HarmonyPatch("BirthdayBiological")]
    public static void BirthdayBiological_Postfix(Pawn ___pawn)
    {
        EventManager.Instance.Notify(EventDefOf.PostBirthday, ___pawn);
    }
}
