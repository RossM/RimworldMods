using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using LudeonTK;
using RimWorld;
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
                new("exclusionTags", geneDef => geneDef.exclusionTags?.ToCommaList() ?? "")
            ];
            DebugTables.MakeTablesDialog(
                DefDatabase<GeneDef>.AllDefs.OrderByDescending(geneDef => geneDef.displayCategory.displayPriorityInXenotype)
                    .ThenBy(geneDef => geneDef.displayOrderInCategory), columns);
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
                var defCaptured = xenotypeDef;

                columns.Add(new(defCaptured.defName, factionDef =>
                {
                    float xenotypeChance = GetXenotypeChance(factionDef, defCaptured);
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

        [DebugOutput, UsedImplicitly]
        public static void PawnKindXenotypes()
        {
            List<TableDataGetter<PawnKindDef>> columns =
            [
                new("defName", geneDef => geneDef.defName),
                new("label", geneDef => geneDef.LabelCap),
            ];
            foreach (var xenotypeDef in DefDatabase<XenotypeDef>.AllDefs)
            {
                var defCaptured = xenotypeDef;

                columns.Add(new(defCaptured.defName, pawnKindDef =>
                {
                    float xenotypeChance = GetXenotypeChance(pawnKindDef, defCaptured);
                    return xenotypeChance > 0 ? xenotypeChance.ToStringPercent() : "";
                }));
            }

            DebugTables.MakeTablesDialog(DefDatabase<PawnKindDef>.AllDefs.Where(pawnKindDef => pawnKindDef.xenotypeSet != null),
                columns.ToArray());
            return;

            static float GetXenotypeChance(PawnKindDef pawnKindDef, XenotypeDef xenotypeDef)
            {
                var weights = PawnGenerator.XenotypesAvailableFor(pawnKindDef, pawnKindDef.defaultFactionDef);
                var totalWeight = weights.Sum(pair => pair.Value);
                if (weights.TryGetValue(xenotypeDef, out var xenotypeWeight))
                    return xenotypeWeight / totalWeight;
                return 0f;
            }
        }
    }
}
