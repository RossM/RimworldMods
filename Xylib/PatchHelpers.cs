namespace Xylib;

public static class PatchHelpers
{
    public static void ModifyGenderByGenes(Pawn pawn, PawnGenerationRequest request, XenotypeDef xenotype)
    {
        if (request.FixedGender != null)
            return;

        GeneDef gene = request.ForcedEndogenes?.FirstOrDefault(HasGenderRatio) ??
                       request.ForcedXenogenes?.FirstOrDefault(HasGenderRatio) ??
                       request.ForcedCustomXenotype?.genes.FirstOrDefault(HasGenderRatio) ??
                       xenotype?.AllGenes.FirstOrDefault(HasGenderRatio);
        if (gene?.DefExt?.femaleChance is not { } chance)
            return;

        pawn.gender = Rand.Chance(chance) ? Gender.Female : Gender.Male;
    }

    public static bool HasGenderRatio(GeneDef geneDef)
    {
        return geneDef.DefExt?.femaleChance != null;
    }
}
