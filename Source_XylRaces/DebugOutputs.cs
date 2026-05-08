using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JetBrains.Annotations;
using LudeonTK;
using Verse;

namespace XylRacesCore
{
    public static class DebugOutputs
    {
        [DebugOutput, UsedImplicitly]
        public static void GeneDisplayOrder()
        {
            TableDataGetter<GeneDef>[] columns =
            [
                new("defName", geneDef => geneDef.defName),
                new("label", geneDef => geneDef.LabelCap),
                new("displayCategory", geneDef => geneDef.displayCategory.defName),
                new("displayOrderInCategory", geneDef => geneDef.displayOrderInCategory),
            ];
            DebugTables.MakeTablesDialog(DefDatabase<GeneDef>.AllDefs.OrderByDescending(geneDef => geneDef.displayCategory.displayPriorityInXenotype).ThenBy(geneDef => geneDef.displayOrderInCategory), columns);
        }
    }
}
