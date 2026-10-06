namespace XylXenos.Patches;

[Patch(typeof(PawnGenerator))]
public static class Patch_PawnGenerator
{
    [Feature(nameof(Config.Feature.Bugfix_Misc))]
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
}
