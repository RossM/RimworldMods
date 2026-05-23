using HarmonyLib;
using RimWorld;
using TranspilerUtil;
using Verse;
using XylXenos.Genes;

namespace XylXenos.Patches
{
    [HarmonyPatch]
    public static class PatchBiostatMet
    {
        [Feature(typeof(BonusGene))]
        [InfixPostfix(typeof(GeneDef), nameof(GeneDef.biostatMet))]
        [InfixPatch(typeof(Dialog_CreateXenotype), "DrawGene")]
        [InfixPatch(typeof(GeneCreationDialogBase), "OnGenesChanged")]
        [InfixPatch(typeof(GeneDef), "GetDescriptionFull")]
        [InfixPatch(typeof(GeneDef), "<SpecialDisplayStats>:MoveNext")]
        public static void GeneDef_biostatMet_Postfix(GeneDef __instance, ref int __result)
        {
            __result += __instance.BiostatMetForDisplayBonus();
        }
    }
}
