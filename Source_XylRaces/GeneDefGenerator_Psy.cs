using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace XylRacesCore
{
    public static class GeneDefGenerator_Psy
    {
        public static IEnumerable<GeneDef> ImpliedGeneDefs(bool hotReload = false)
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
                        if (def.level >= template.levels.Count)
                            continue;
                        if (!template.levels[def.level].valid)
                            continue;

                        yield return GetFromTemplate(template, def, displayOrderBase, hotReload);
                        displayOrderBase += 1000;
                    }
                }
            }
        }

        private static GeneDef GetFromTemplate(GeneTemplateDef template, AbilityDef def, int displayOrderBase, bool hotReload)
        {
            string defName = template.defName + "_" + def.defName;
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

            geneDef.biostatArc = template.levels[def.level].biostatArc;
            geneDef.biostatCpx = template.levels[def.level].biostatCpx;
            geneDef.biostatMet = template.levels[def.level].biostatMet;

            geneDef.abilities = [def];

            return geneDef;
        }
    }
}
