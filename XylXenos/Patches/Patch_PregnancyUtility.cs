namespace XylXenos.Patches;

// This patch must be applied after defs are loaded, otherwise the static constructor for
// PregnancyUtility reads a null DefOf and throws an exception.

[Patch(typeof(PregnancyUtility))]
[Category("PostLoadDefs")]
public static class Patch_PregnancyUtility
{
    [Feature(typeof(GeneCompProperties_XenotypeStrength))]
    [Postfix]
    [Inner(typeof(PawnGenerator), nameof(PawnGenerator.GeneratePawn), typeof(PawnGenerationRequest))]
    [Target(nameof(PregnancyUtility.ApplyBirthOutcome))]
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
    [Postfix]
    [Target(nameof(PregnancyUtility.GetInheritedGenes), typeof(Pawn), typeof(Pawn), typeof(Out<bool>))]
    public static void GetInheritedGenes_Postfix(Pawn mother, Pawn father, ref List<GeneDef> __result)
    {
        __result = PatchHelpers.GetDominantParent(mother, father) switch
        {
            PatchHelpers.DominantParent.Mother => GenesFrom(mother),
            PatchHelpers.DominantParent.Father => GenesFrom(father),
            _ => __result,
        };
        return;

        static List<GeneDef> GenesFrom(Pawn pawn) => [.. pawn.genes.Endogenes.Select(gene => gene.def).Where(gene => gene.biostatArc <= 0)];
    }

    [Feature(typeof(GeneCompProperties_XenotypeStrength))]
    [Postfix]
    [Inner(typeof(PregnancyUtility), "ShouldByHybrid")]
    [Target(nameof(PregnancyUtility.ApplyBirthOutcome))]
    public static void ShouldBeHybrid_Postfix(Pawn mother, Pawn father, ref bool __result)
    {
        if (PatchHelpers.GetDominantParent(mother, father) != PatchHelpers.DominantParent.None)
            __result = false;
    }
}
