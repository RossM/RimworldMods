namespace XylXenos.Patches;

// This patch must be applied after defs are loaded, otherwise the static constructor for
// PregnancyUtility reads a null DefOf and throws an exception.

[Patch(typeof(PregnancyUtility))]
[Category("PostLoadDefs")]
public static class Patch_PregnancyUtility
{
    [Feature(typeof(GeneCompProperties_Inheritance))]
    [Postfix]
    [Inner(typeof(PawnGenerator), nameof(PawnGenerator.GeneratePawn), typeof(PawnGenerationRequest))]
    [Target(nameof(PregnancyUtility.ApplyBirthOutcome))]
    public static void GeneratePawn_Postfix(Pawn? geneticMother, Pawn? father, ref Pawn __result)
    {
        List<GeneDef> childGenes = InheritableGenesFrom(__result);

        switch (PatchHelpers.GetDominantParent(geneticMother, father))
        {
            case PatchHelpers.DominantParent.Father:
            {
                DebugAssert.NotNull(father);
                if (geneticMother != null && childGenes.SetsEqual(InheritableGenesFrom(geneticMother)) &&
                    !childGenes.SetsEqual(InheritableGenesFrom(father)))
                    PatchHelpers.CopyXenotype(__result, geneticMother);
                else
                    PatchHelpers.CopyXenotype(__result, father);
                break;
            }
            case PatchHelpers.DominantParent.Mother:
            {
                DebugAssert.NotNull(geneticMother);
                if (father != null && childGenes.SetsEqual(InheritableGenesFrom(father)) &&
                    !childGenes.SetsEqual(InheritableGenesFrom(geneticMother)))
                    PatchHelpers.CopyXenotype(__result, father);
                else
                    PatchHelpers.CopyXenotype(__result, geneticMother);
                break;
            }
        }

        static List<GeneDef> InheritableGenesFrom(Pawn pawn) =>
        [
            .. pawn.genes!.Endogenes.Select(gene => gene.def).Where(gene => gene is
                { biostatArc: <= 0, endogeneCategory: not (EndogeneCategory.HairColor or EndogeneCategory.Melanin) }),
        ];
    }

    [Feature(typeof(GeneCompProperties_Inheritance))]
    [Postfix]
    [Target(nameof(PregnancyUtility.GetInheritedGenes), typeof(Pawn), typeof(Pawn), typeof(Out<bool>))]
    public static void GetInheritedGenes_Postfix(Pawn mother, Pawn father, ref List<GeneDef> __result)
    {
        __result = PatchHelpers.GetDominantParent(mother, father) switch
        {
            PatchHelpers.DominantParent.Mother => InheritableGenesFrom(mother),
            PatchHelpers.DominantParent.Father => InheritableGenesFrom(father),
            _ => __result,
        };
        return;

        static List<GeneDef> InheritableGenesFrom(Pawn pawn) =>
            [.. pawn.genes!.Endogenes.Select(gene => gene.def).Where(gene => gene.biostatArc <= 0)];
    }

    [Feature(typeof(GeneCompProperties_Inheritance))]
    [Postfix]
    [Inner(typeof(PregnancyUtility), "ShouldByHybrid")]
    [Target(nameof(PregnancyUtility.ApplyBirthOutcome))]
    public static void ShouldBeHybrid_Postfix(Pawn mother, Pawn father, ref bool __result)
    {
        if (PatchHelpers.GetDominantParent(mother, father) != PatchHelpers.DominantParent.None)
            __result = false;
    }
}
