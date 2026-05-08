using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JetBrains.Annotations;
using LudeonTK;
using RimWorld;
using UnityEngine;
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

        [DebugOutput, UsedImplicitly]
        public static void FactionXenotypes()
        {
            List<TableDataGetter<FactionDef>> columns =
            [
                new("defName", geneDef => geneDef.defName),
                new("label", geneDef => geneDef.LabelCap),
            ];
            foreach (var xenotypeDef in DefDatabase<XenotypeDef>.AllDefs)
            {
                var xenotypeDefCaptured = xenotypeDef;

                columns.Add(new(xenotypeDefCaptured.defName, factionDef =>
                {
                    float xenotypeChance = GetXenotypeChance(factionDef, xenotypeDefCaptured);
                    return xenotypeChance > 0 ? xenotypeChance.ToStringPercent() : "";
                }));
            }
            DebugTables.MakeTablesDialog(DefDatabase<FactionDef>.AllDefs, columns.ToArray());
            return;

            static float GetXenotypeChance(FactionDef factionDef, XenotypeDef xenotypeDef)
            {
                var weights = PawnGenerator.XenotypesAvailableFor(PawnKindDefOf.Colonist, factionDef);
                var totalWeight = weights.Sum(pair => pair.Value);
                if (weights.TryGetValue(xenotypeDef, out var xenotypeWeight))
                    return xenotypeWeight / totalWeight;
                return 0f;
            }
        }
    }
}
