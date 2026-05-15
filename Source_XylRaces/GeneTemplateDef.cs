using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Verse;

namespace XylRacesCore
{
    [UsedImplicitly]
    public class GeneTemplateDef : Def
    {
        public enum GeneTemplateType
        {
            PsychicAbility,
        }

        public class AbilityLevelInfo
        {
            public int biostatArc = 0;
            public int biostatCpx = 0;
            public int biostatMet = 0;
            public bool valid = true;
        }

        public string iconPath;
        public Type geneClass = typeof(Gene);

        public List<AbilityLevelInfo> levels;

        public GeneTemplateType geneTemplateType;

        public GeneCategoryDef displayCategory;

        public int displayOrderOffset;

        public float selectionWeight = 1f;

        public override IEnumerable<string> ConfigErrors()
        {
            foreach (string item in base.ConfigErrors())
            {
                yield return item;
            }

            if (!typeof(Gene).IsAssignableFrom(geneClass))
            {
                yield return "geneClass is not Gene or child thereof.";
            }
        }
    }
}
