namespace XylXenos.Patches;

[Patch(typeof(CompUsableImplant))]
public static class Patch_CompUsableImplant
{
    [Feature(nameof(MyDefOf.XylBioRejection))]
    [Postfix]
    [Target("FloatMenuOptionLabel")]
    public static void FloatMenuOptionLabel_Postfix(CompUsableImplant __instance, Pawn pawn, ref string __result)
    {
        if (__instance.parent.def.GetCompProperties<CompProperties_UseEffectInstallImplant>()?.hediffDef
                .countsAsAddedPartOrImplant is not true)
            return;
        if (!pawn.HasActiveGene(MyDefOf.XylBioRejection))
            return;
        __result += $" ({"XylCausesBioRejection".Translate()})";
    }
}
