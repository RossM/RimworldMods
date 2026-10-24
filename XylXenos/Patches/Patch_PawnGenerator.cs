namespace XylXenos.Patches;

[Patch(typeof(PawnGenerator))]
public static class Patch_PawnGenerator
{
    [Feature(FeatureNameOf.Bugfix_Misc)]
    [Prefix]
    [Inner(typeof(PawnGenerator), "GenerateSkills.CreatePassion")]
    [Target("GenerateSkills")]
    public static bool CreatePassion_Prefix(Pawn pawn, SkillRecord record, ref int minorPassions)
    {
        if (!Settings.instance.fixGeneticPassions || !PatchHelpers.ShouldGetGeneticPassion(pawn, record, minorPassions))
            return true;

        record.passion = Passion.Major;
        minorPassions--;
        return false;
    }

    [Feature(typeof(GeneCompProperties_GenderRatio))]
    [Prefix]
    [Inner(typeof(PawnBioAndNameGenerator), nameof(PawnBioAndNameGenerator.GiveAppropriateBioAndNameTo))]
    [Target("TryGenerateNewPawnInternal")]
    public static void GiveAppropriateBioAndNameTo_Prefix(
        Pawn pawn,
        in PawnGenerationRequest request,
        XenotypeDef? xenotype)
    {
        PatchHelpers.ModifyGenderByGenes(pawn, request, xenotype);
    }
}
