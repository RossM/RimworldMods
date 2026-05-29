using HarmonyLib;
using RimWorld;
using Verse;

namespace XylXenos.Patches
{
    [HarmonyPatch(typeof(FloatMenuOptionProvider_Ingest))]
    public static class Patch_FloatMenuOptionProvider_Ingest
    {
        [Feature(typeof(DefModExtension_Chemical))]
        [HarmonyPrefix]
        [HarmonyPatch("GetSingleOptionFor")]
        public static bool GetSingleOptionFor_Prefix(
            FloatMenuOptionProvider_Ingest __instance,
            Thing clickedThing,
            FloatMenuContext context,
            out FloatMenuOption __result)
        {
            __result = null;
            if (clickedThing.def.ingestible is not { showIngestFloatOption: true })
            {
                return true;
            }

            if (!clickedThing.IngestibleNow || !context.FirstSelectedPawn.RaceProps.CanEverEat(clickedThing.def))
            {
                return true;
            }

            if (!context.FirstSelectedPawn.ChemicalIsAllowedByGenes(clickedThing.def))
            {
                string text = !clickedThing.def.ingestible.ingestCommandString.NullOrEmpty()
                    ? clickedThing.def.ingestible.ingestCommandString.Formatted(clickedThing.LabelShort)
                    : "ConsumeThing".Translate(clickedThing.LabelShort, clickedThing);

                ChemicalDef chemicalDef = DrugStatsUtility.GetChemical(clickedThing.def);
                var defExtension = chemicalDef.GetModExtension<DefModExtension_Chemical>();

                if (!defExtension.prohibitedGenes.NullOrEmpty())
                {
                    GeneDef gene = defExtension.prohibitedGenes.FirstOrDefault(gene => context.FirstSelectedPawn.HasActiveGene(gene));
                    if (gene != null)
                    {
                        __result = new FloatMenuOption($"{text}: {"XylBlockedByGene".Translate(gene.label)}", null);
                        return false;
                    }
                }

                if (!defExtension.requiredGenesAll.NullOrEmpty())
                {
                    GeneDef gene = defExtension.requiredGenesAll.FirstOrDefault(gene => !context.FirstSelectedPawn.HasActiveGene(gene));
                    if (gene != null)
                    {
                        __result = new FloatMenuOption($"{text}: {"XylRequiresGene".Translate(gene.label)}", null);
                        return false;
                    }
                }

                if (!defExtension.requiredGenesAny.NullOrEmpty())
                {
                    __result = new FloatMenuOption($"{text}: {"XylRequiresGene".Translate(defExtension.requiredGenesAny[0].label)}", null);
                    return false;
                }

                // Should never get here
            }

            return true;
        }
    }
}
