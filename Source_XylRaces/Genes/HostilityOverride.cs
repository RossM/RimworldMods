using RimWorld;
using Verse;

namespace XylRacesCore.Genes
{
    public class GeneDefExtension_HostilityOverride : DefModExtension
    {
        public FactionDef disableHostilityFromFaction;
    }

    public class HostilityOverride : Gene
    {
        public GeneDefExtension_HostilityOverride DefExt => def.GetModExtension<GeneDefExtension_HostilityOverride>();

        public bool DisableHostilityFrom(Thing thing)
        {
            return DefExt.disableHostilityFromFaction != null && DefExt.disableHostilityFromFaction == thing.Faction?.def;
        }
    }
}