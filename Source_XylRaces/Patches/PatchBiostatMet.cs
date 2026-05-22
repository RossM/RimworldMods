using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using JetBrains.Annotations;
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
        [WrappedMember(typeof(GeneDef), nameof(GeneDef.biostatMet))]
        [InfixPatch(typeof(Dialog_CreateXenotype), "DrawGene")]
        [InfixPatch(typeof(GeneCreationDialogBase), "OnGenesChanged")]
        [InfixPatch(typeof(GeneDef), "GetDescriptionFull")]
        [InfixPatch(typeof(GeneDef), "<SpecialDisplayStats>:MoveNext")]
        public static int GeneDef_biostatMet_Wrapper(GeneDef __instance)
        {
            return __instance.BiostatMetForDisplay();
        }
    }
}
