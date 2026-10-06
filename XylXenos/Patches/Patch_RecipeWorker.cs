namespace XylXenos.Patches;

[Patch(typeof(RecipeWorker))]
public static class Patch_RecipeWorker
{
    [Feature(nameof(DefOf.XylBioRejection))]
    [Postfix]
    [Target(nameof(RecipeWorker.GetLabelWhenUsedOn))]
    public static void RecipeWorker_Postfix(RecipeWorker __instance, Pawn pawn, BodyPartRecord part, ref string __result)
    {
        if (__instance is not (Recipe_InstallArtificialBodyPart or Recipe_InstallImplant))
            return;
        if (!pawn.HasActiveGene(DefOf.XylBioRejection))
            return;
        if (!__instance.recipe.addsHediff.countsAsAddedPartOrImplant)
            return;
        __result += $" ({"XylCausesBioRejection".Translate()})";
    }
}
