namespace XylXenos.Patches;

// This patch must be applied after defs are loaded, otherwise the static constructor for
// PregnancyUtility reads a null DefOf and throws an exception.

[HarmonyPatch(typeof(PregnancyUtility))]
[HarmonyPatchCategory("PostLoadDefs")]
public class Patch_PregnancyUtility
{
    [Feature(nameof(DefModExtension_Gene.strongXenotype))]
    [HarmonyPostfix]
    [HarmonyPatch(nameof(PregnancyUtility.GetInheritedGeneSet), 
        [typeof(Pawn), typeof(Pawn), typeof(bool)],
        [ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Ref])]
    public static void GetInheritedGeneSet_Postfix(Pawn father, Pawn mother, ref RimWorld.GeneSet __result)
    {
        switch (PatchHelpers.GetDominantParent(father, mother))
        {
            case PatchHelpers.DominantParent.Mother:
            {
                __result = PatchHelpers.CreateGeneSetFrom(mother);
                break;
            }
            case PatchHelpers.DominantParent.Father:
            {
                __result = PatchHelpers.CreateGeneSetFrom(father);
                break;
            }
        }
    }

    [Feature(nameof(DefModExtension_Gene.strongXenotype))]
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

    [Feature(nameof(DefModExtension_Gene.strongXenotype))]
    [InfixPostfix(typeof(PregnancyUtility), "ShouldByHybrid")]
    [InfixPatch(nameof(PregnancyUtility.ApplyBirthOutcome))]
    public static void ShouldBeHybrid_Postfix(Pawn mother, Pawn father, ref bool __result)
    {
        if (PatchHelpers.GetDominantParent(father, mother) != PatchHelpers.DominantParent.None)
            __result = false;
    }
}
