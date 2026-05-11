using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;

namespace XylRacesCore
{
    public static class GeneDefGenerator_Psy
    {
        public static IEnumerable<GeneDef> ImpliedGeneDefs(bool hotReload = false)
        {
            foreach (GeneTemplateDef g in DefDatabase<GeneTemplateDef>.AllDefs)
            {
                if (g.geneTemplateType == GeneTemplateDef.GeneTemplateType.PsychicAbility)
                {
                    int displayOrderBase = 0;
                    foreach (AbilityDef a in DefDatabase<AbilityDef>.AllDefs.OrderBy(a => a.level).ThenBy(a => a.label))
                    {
                        if (!typeof(Psycast).IsAssignableFrom(a.abilityClass)) 
                            continue;
                        if (a.level >= g.levels.Count)
                            continue;
                        if (!g.levels[a.level].valid)
                            continue;

                        yield return GetFromTemplate(g, a, displayOrderBase, hotReload);
                        displayOrderBase += 1000;
                    }
                }
            }
        }

        private static GeneDef GetFromTemplate(GeneTemplateDef template, AbilityDef def, int displayOrderBase, bool hotReload)
        {
            string defName = template.defName + "_" + def.defName;
            GeneDef geneDef = (hotReload ? (DefDatabase<GeneDef>.GetNamed(defName, errorOnFail: false) ?? new GeneDef()) : new GeneDef());

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
