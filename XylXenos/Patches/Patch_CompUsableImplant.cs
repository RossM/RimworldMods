namespace XylXenos.Patches;

[Patch(typeof(CompUsableImplant))]
public static class Patch_CompUsableImplant
{
    [Feature(nameof(DefOf.XylBioRejection))]
    [Postfix]
    [Target("FloatMenuOptionLabel")]
    public static void FloatMenuOptionLabel_Postfix(CompUsableImplant __instance, Pawn pawn, ref string __result)
    {
        if (__instance.parent.def.GetCompProperties<CompProperties_UseEffectInstallImplant>()?.hediffDef
                ?.countsAsAddedPartOrImplant is not true)
            return;
        if (!pawn.HasActiveGene(DefOf.XylBioRejection))
            return;
        __result += $" ({"XylCausesBioRejection".Translate()})";
    }
}
