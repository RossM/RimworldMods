using Psycast = RimWorld.Psycast;

namespace XylXenos;

[UsedFromReflection]
public class GeneDefGenerator : IDefGenerator<GeneDef>
{
    public static IEnumerable<GeneDef> ImpliedDefs(bool hotReload = false)
    {
        foreach (GeneTemplateDef template in DefDatabase<GeneTemplateDef>.AllDefs)
        {
            switch (template.geneTemplateType)
            {
                case GeneTemplateDef.GeneTemplateType.PsychicAbility:
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

                    break;
                }
                default: throw new NotSupportedException($"Unsupported gene template type: {template.geneTemplateType}");
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

        if (template.modExtensions is { Count: > 0 })
            geneDef.modExtensions = [.. template.modExtensions.Select(ext => ext.MemberwiseClone())];

        return geneDef;
    }
}
