namespace XylXenos.Patches;

[Patch(typeof(Verb_MeleeAttack))]
public class Patch_Verb_MeleeAttack
{
    [Feature("TODO")]
    [Postfix]
    [Target("GetNonMissChance")]
    public static void GetNonMissChance_Postfix(Verb __instance, ref float __result)
    {
        if (__result >= 1f)
            return;

        float bonus = PatchHelpers.GetHitChanceBonus(__instance);

        __result = Mathf.Clamp01(__result + bonus);
    }
}
