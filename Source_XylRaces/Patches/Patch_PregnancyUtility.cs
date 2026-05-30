namespace XylXenos.Patches;

// This does not work because .NET invokes the static constructor for PregnancyUtility, which has a bug
// that causes it to throw an exception.

#if false

[HarmonyPatch(typeof(PregnancyUtility))]
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
    [InfixPostfix(typeof(PregnancyUtility), "TryGetInheritedXenotype")]
    [InfixPatch(nameof(PregnancyUtility.ApplyBirthOutcome))]
    public static void TryGetInheritedXenotype_Postfix(Pawn pawn, Pawn mother, Pawn father, ref bool __result)
    {
        switch (PatchHelpers.GetDominantParent(father, mother))
        {
            case PatchHelpers.DominantParent.Mother:
            {
                PatchHelpers.CopyXenotype(pawn, mother);
                __result = false;
                break;
            }
            case PatchHelpers.DominantParent.Father:
            {
                PatchHelpers.CopyXenotype(pawn, father);
                __result = false;
                break;
            }
        }
    }

    [Feature(nameof(DefModExtension_Gene.strongXenotype))]
    [InfixPostfix(typeof(PregnancyUtility), "ShouldBeHybrid")]
    [InfixPatch(nameof(PregnancyUtility.ApplyBirthOutcome))]
    public static void ShouldBeHybrid_Postfix(Pawn mother, Pawn father, ref bool __result)
    {
        if (PatchHelpers.GetDominantParent(father, mother) != PatchHelpers.DominantParent.None)
            __result = false;
    }
}

#endif