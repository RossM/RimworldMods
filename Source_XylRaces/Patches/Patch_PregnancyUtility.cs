namespace XylXenos.Patches;

// This patch must be applied after defs are loaded, otherwise the static constructor for
// PregnancyUtility reads a null DefOf and throws an exception.

[HarmonyPatch(typeof(PregnancyUtility))]
[HarmonyPatchCategory("PostLoadDefs")]
public class Patch_PregnancyUtility
{
    [Feature(nameof(DefModExtension_Gene.xenotypeStrength))]
    [HarmonyPostfix]
    [HarmonyPatch(nameof(PregnancyUtility.GetInheritedGenes), 
        [typeof(Pawn), typeof(Pawn), typeof(bool)],
        [ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Out])]
    public static void GetInheritedGenes_Postfix(Pawn father, Pawn mother, ref List<GeneDef> __result)
    {
        Log.Message($"GetInheritedGenes_Postfix: father={father} mother={mother} dominant parent={PatchHelpers.GetDominantParent(father, mother)}");

        switch (PatchHelpers.GetDominantParent(father, mother))
        {
            case PatchHelpers.DominantParent.Mother:
            {
                __result = mother.genes.Endogenes.Select(gene => gene.def).ToList();
                break;
            }
            case PatchHelpers.DominantParent.Father:
            {
                __result = father.genes.Endogenes.Select(gene => gene.def).ToList();
                break;
            }
        }
    }

    [Feature(nameof(DefModExtension_Gene.xenotypeStrength))]
    [InfixPostfix(typeof(PawnGenerator), nameof(PawnGenerator.GeneratePawn), [typeof(PawnGenerationRequest)])]
    [InfixPatch(nameof(PregnancyUtility.ApplyBirthOutcome))]
    public static void GeneratePawn_Postfix(Pawn geneticMother, Pawn father, ref Pawn __result)
    {
        switch (PatchHelpers.GetDominantParent(father, geneticMother))
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
        }
    }

    [Feature(nameof(DefModExtension_Gene.xenotypeStrength))]
    [InfixPostfix(typeof(PregnancyUtility), "ShouldByHybrid")]
    [InfixPatch(nameof(PregnancyUtility.ApplyBirthOutcome))]
    public static void ShouldBeHybrid_Postfix(Pawn mother, Pawn father, ref bool __result)
    {
        if (PatchHelpers.GetDominantParent(father, mother) != PatchHelpers.DominantParent.None)
            __result = false;
    }
}
