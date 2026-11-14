namespace XylXenos.Patches;

[Patch(typeof(PawnGenerator))]
public static class Patch_PawnGenerator
{
    [Feature(FeatureNameOf.Bugfix_Misc)]
    [Prefix]
    [Inner(typeof(PawnGenerator), "GenerateSkills")]
    [Target("TryGenerateNewPawnInternal")]
    public static void GenerateSkills_Prefix(Pawn pawn)
    {
        if (!Settings.instance.fixGeneticPassions || pawn.genes is not { } genes)
            return;

        // Back out genetic passion modifiers so we can re-apply them after natural passions have been rolled
        foreach (var gene in genes.GenesListForReading)
        {
            if (gene.def.passionMod == null)
                continue;

            SkillRecord skill = pawn.skills!.GetSkill(gene.def.passionMod.skill);
            skill.passion = gene.passionPreAdd!.Value;
        }
    }

    [Feature(FeatureNameOf.Bugfix_Misc)]
    [Postfix]
    [Inner(typeof(PawnGenerator), "GenerateSkills")]
    [Target("TryGenerateNewPawnInternal")]
    public static void GenerateSkills_Postfix(Pawn pawn)
    {
        if (!Settings.instance.fixGeneticPassions || pawn.genes is not { } genes)
            return;

        // Apply genetic passion modifiers now that skills have set natural passion
        foreach (var gene in genes.GenesListForReading)
        {
            if (gene.def.passionMod == null)
                continue;

            SkillRecord skill = pawn.skills!.GetSkill(gene.def.passionMod.skill);
            gene.passionPreAdd = skill.passion;
            skill.passion = gene.def.passionMod.NewPassionFor(skill);
        }
    }
}
