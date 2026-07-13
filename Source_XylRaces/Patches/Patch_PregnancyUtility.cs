namespace XylXenos.Patches;

// This patch must be applied after defs are loaded, otherwise the static constructor for
// PregnancyUtility reads a null DefOf and throws an exception.

[HarmonyPatch(typeof(PregnancyUtility))]
[HarmonyPatchCategory("PostLoadDefs")]
public static class Patch_PregnancyUtility
{
    [Feature(typeof(GeneCompProperties_XenotypeStrength))]
    [InfixPostfix(typeof(PawnGenerator), nameof(PawnGenerator.GeneratePawn), [typeof(PawnGenerationRequest)])]
    [InfixPatch(nameof(PregnancyUtility.ApplyBirthOutcome))]
    public static void GeneratePawn_Postfix(Pawn geneticMother, Pawn father, ref Pawn __result)
    {
        switch (PatchHelpers.GetDominantParent(geneticMother, father))
        {
            case PatchHelpers.DominantParent.Mother:
            {
                PatchHelpers.CopyXenotype(__result, geneticMother);
                break;
            }
            case PatchHelpers.DominantParent.Father:
            {
                PatchHelpers.CopyXenotype(__result, father);
                break;
            }
            case PatchHelpers.DominantParent.None:
            default:
                break;
        }
    }

    [Feature(typeof(GeneCompProperties_XenotypeStrength))]
    [HarmonyPostfix]
    [HarmonyPatch(nameof(PregnancyUtility.GetInheritedGenes),
        [typeof(Pawn), typeof(Pawn), typeof(bool)],
        [ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Out])]
    public static void GetInheritedGenes_Postfix(Pawn mother, Pawn father, ref List<GeneDef> __result)
    {
        DebugAssert.NotNull(mother.genes);
        DebugAssert.NotNull(father.genes);

        __result = PatchHelpers.GetDominantParent(mother, father) switch
        {
            PatchHelpers.DominantParent.Mother => mother.genes.Endogenes.Select(gene => gene.def).ToList(),
            PatchHelpers.DominantParent.Father => father.genes.Endogenes.Select(gene => gene.def).ToList(),
            _ => __result,
        };
    }

    [Feature(typeof(GeneCompProperties_XenotypeStrength))]
    [InfixPostfix(typeof(PregnancyUtility), "ShouldByHybrid")]
    [InfixPatch(nameof(PregnancyUtility.ApplyBirthOutcome))]
    public static void ShouldBeHybrid_Postfix(Pawn mother, Pawn father, ref bool __result)
    {
        if (PatchHelpers.GetDominantParent(mother, father) != PatchHelpers.DominantParent.None)
            __result = false;
    }
}
