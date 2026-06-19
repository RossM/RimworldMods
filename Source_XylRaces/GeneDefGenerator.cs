using Psycast = RimWorld.Psycast;

namespace XylXenos;

[DefGenerator(typeof(GeneDef))]
public static class GeneDefGenerator
{
    [UsedFromReflection]
    public static IEnumerable<GeneDef> ImpliedDefs(bool hotReload = false)
    {
        foreach (GeneTemplateDef template in DefDatabase<GeneTemplateDef>.AllDefs)
        {
            if (template.geneTemplateType == GeneTemplateDef.GeneTemplateType.PsychicAbility)
            {
                int displayOrderBase = 0;
                foreach (AbilityDef def in DefDatabase<AbilityDef>.AllDefs.OrderBy(def => def.level).ThenBy(def => def.label))
                {
                    if (!typeof(Psycast).IsAssignableFrom(def.abilityClass))
                        continue;
                    if (!template.biostats.Any(biostatInfo => biostatInfo.levels.Includes(def.level)))
                        continue;

                    yield return GetFromTemplate(template, def, displayOrderBase, hotReload);
                    displayOrderBase += 1000;
                }
            }
        }
    }

    private static GeneDef GetFromTemplate(GeneTemplateDef template, AbilityDef def, int displayOrderBase, bool hotReload)
    {
        string defName = $"{template.defName}_{def.defName}";
        GeneDef geneDef;
        if (hotReload)
            geneDef = DefDatabase<GeneDef>.GetNamed(defName, errorOnFail: false) ?? new GeneDef();
        else
            geneDef = new GeneDef();

        geneDef.modContentPack = template.modContentPack;

        geneDef.defName = defName;
        geneDef.geneClass = template.geneClass;
        geneDef.label = template.label.Formatted(def.label);
        geneDef.description = template.description.Formatted(def.label, def.description.Named("DESCRIPTION"));
        geneDef.descriptionHyperlinks = [def];
        geneDef.iconPath = template.iconPath.Formatted(def.iconPath);
        geneDef.geneClass = template.geneClass;
        geneDef.selectionWeight = template.selectionWeight;

        geneDef.displayCategory = template.displayCategory;
        geneDef.displayOrderInCategory = displayOrderBase + template.displayOrderOffset;

        var biostatInfo = template.biostats.First(levelInfo => levelInfo.levels.Includes(def.level));
        geneDef.biostatArc = biostatInfo.biostatArc;
        geneDef.biostatCpx = biostatInfo.biostatCpx;
        geneDef.biostatMet = biostatInfo.biostatMet;

        geneDef.abilities = [def];

        if (!template.modExtensions.NullOrEmpty())
            geneDef.modExtensions = [.. template.modExtensions];

        return geneDef;
    }

    public static void FixupChemicalGenes()
    {
        foreach (var geneDef in DefDatabase<GeneDef>.AllDefs)
        {
            if (geneDef.chemical is not { } chemical)
                continue;
            if (chemical.GetModExtension<DefModExtension_Chemical>() is not { } defExtension)
                continue;

            if (!defExtension.requiredGenesAll.NullOrEmpty())
                geneDef.prerequisite = defExtension.requiredGenesAll[0];
            else if (defExtension.requiredGenesAny is { Count: 1 })
                geneDef.prerequisite = defExtension.requiredGenesAny[0];
        }
    }
}
