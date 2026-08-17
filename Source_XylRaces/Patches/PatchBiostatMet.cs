namespace XylXenos.Patches;

[HarmonyPatch]
public static class PatchBiostatMet
{
    [Feature(typeof(GeneComp_BonusGenes))]
    [Postfix] [Inner(typeof(GeneDef), nameof(GeneDef.biostatMet))]
    [Target(typeof(Dialog_CreateXenotype), "DrawGene")]
    [Target(typeof(GeneCreationDialogBase), "OnGenesChanged")]
    [Target(typeof(GeneDef), "GetDescriptionFull")]
    [Target(typeof(GeneDef), "SpecialDisplayStats")]
    public static void GeneDef_biostatMet_Postfix(GeneDef __instance, ref int __result)
    {
        __result += __instance.BiostatMetForDisplayBonus();
    }
}
