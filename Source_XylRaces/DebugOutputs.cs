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
                new("defName", x => x.defName),
                new("label", x => x.LabelCap),
                new("displayCategory", x => x.displayCategory.defName),
                new("displayOrderInCategory", x => x.displayOrderInCategory),
            ];
            DebugTables.MakeTablesDialog(DefDatabase<GeneDef>.AllDefs.OrderByDescending(x => x.displayCategory.displayPriorityInXenotype).ThenBy(x => x.displayOrderInCategory), columns);
        }
    }
}
