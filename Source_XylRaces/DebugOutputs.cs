using System.Collections.Generic;
using System.Linq;
using System.Text;
using LudeonTK;
using RimWorld;
using Verse;

namespace XylXenos
{
    public static class DebugOutputs
    {
        [DebugOutput]
        public static void GeneDisplayOrder()
        {
            TableDataGetter<GeneDef>[] columns =
            [
                new("defName", geneDef => geneDef.defName),
                new("label", geneDef => geneDef.LabelCap),
                new("displayCategory", geneDef => geneDef.displayCategory.defName),
                new("displayPriorityInXenotype", geneDef => geneDef.displayCategory.displayPriorityInXenotype),
                new("displayPriorityInGenepack", geneDef => geneDef.displayCategory.displayPriorityInGenepack),
                new("displayOrderInCategory", geneDef => geneDef.displayOrderInCategory),
                new("exclusionTags", geneDef => geneDef.exclusionTags?.ToCommaList() ?? "")
            ];
            DebugTables.MakeTablesDialog(
                DefDatabase<GeneDef>.AllDefs.OrderByDescending(geneDef => geneDef.displayCategory.displayPriorityInXenotype)
                    .ThenBy(geneDef => geneDef.displayOrderInCategory), columns);
        }

        [DebugOutput]
        public static void FactionXenotypes()
        {
            List<TableDataGetter<FactionDef>> columns =
            [
                new("defName", factionDef => factionDef.defName),
                new("label", factionDef => factionDef.LabelCap),
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

        [DebugOutput]
        public static void PawnKindXenotypes()
        {
            List<TableDataGetter<PawnKindDef>> columns =
            [
                new("defName", pawnKindDef => pawnKindDef.defName),
                new("label", pawnKindDef => pawnKindDef.LabelCap),
            ];
            foreach (var def in DefDatabase<XenotypeDef>.AllDefs)
            {
                var xenotypeDef = def;

                columns.Add(new(xenotypeDef.defName, pawnKindDef =>
                {
                    float xenotypeChance = GetXenotypeChance(pawnKindDef, xenotypeDef);
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

        [DebugOutput]
        public static void XenotypeSkillAptitudes()
        {
            List<TableDataGetter<XenotypeDef>> columns =
            [
                new("defName", xenotypeDef => xenotypeDef.defName),
                new("label", xenotypeDef => xenotypeDef.LabelCap),
            ];
            foreach (var def in DefDatabase<SkillDef>.AllDefs)
            {
                var skillDef = def;

                columns.Add(new(skillDef.LabelCap, xenotypeDef =>
                {
                    int skillModifier = GetSkillModifier(xenotypeDef, skillDef);

                    return skillModifier != 0 ? skillModifier.ToStringWithSign() : "";
                }));
            }

            DebugTables.MakeTablesDialog(DefDatabase<XenotypeDef>.AllDefs, columns.ToArray());
            return;

            int GetSkillModifier(XenotypeDef xenotypeDef, SkillDef skillDef)
            {
                return xenotypeDef.genes
                    .Where(gene => gene.aptitudes != null)
                    .SelectMany(gene => gene.aptitudes)
                    .Where(aptitude => aptitude.skill == skillDef)
                    .Select(aptitude => aptitude.level)
                    .Sum();
            }
        }

        [DebugOutput]
        public static void FactionMemes()
        {
            List<TableDataGetter<FactionDef>> columns =
            [
                new("defName", factionDef => factionDef.defName),
                new("label", factionDef => factionDef.LabelCap),
            ];
            foreach (var def in DefDatabase<MemeDef>.AllDefs.Where(memeDef => memeDef.category == MemeCategory.Normal)
                         .OrderBy(memeDef => memeDef.label))
            {
                var memeDef = def;

                columns.Add(new(memeDef.LabelCap, factionDef =>
                {
                    if (factionDef.requiredMemes?.Contains(memeDef) == true)
                        return "Req";
                    if (!factionDef.allowedMemes.NullOrEmpty())
                        return factionDef.allowedMemes.Contains(memeDef) ? "\u2713" : "";
                    if (!factionDef.disallowedMemes.NullOrEmpty())
                        return factionDef.disallowedMemes.Contains(memeDef) ? "" : "\u2713";
                    return "";
                }));
            }

            DebugTables.MakeTablesDialog(DefDatabase<FactionDef>.AllDefs.Where(ShouldShow), columns.ToArray());
            return;

            bool ShouldShow(FactionDef factionDef)
            {
                return !factionDef.requiredMemes.NullOrEmpty() ||
                       !factionDef.allowedMemes.NullOrEmpty() ||
                       !factionDef.disallowedMemes.NullOrEmpty();
            }
        }

        [DebugOutput]
        public static void GenerateXenohumanNames()
        {
            List<DebugMenuOption> list = [];

            foreach (XenotypeDef item in DefDatabase<XenotypeDef>.AllDefs.Where(def => def.nameMaker != null).OrderBy(def => def.defName))
            {
                XenotypeDef localDef = item;

                list.Add(new DebugMenuOption(localDef.defName, DebugMenuOptionMode.Action, delegate
                {
                    StringBuilder sb = new();

                    for (int i = 0; i < 30; i++)
                    {
                        var nameMaker = localDef.GetNameMaker(Rand.Chance(0.5f) ? Gender.Female : Gender.Male);
                        var name = NameTriple.FromString(NameGenerator.GenerateName(nameMaker));
                        sb.AppendLine(name.ToStringFull);
                    }

                    Log.Message(sb.Length > 0 ? sb.ToString() : $"No name maker for {localDef.label}");
                }));
            }

            Find.WindowStack.Add(new Dialog_DebugOptionListLister(list));
        }
    }
}
