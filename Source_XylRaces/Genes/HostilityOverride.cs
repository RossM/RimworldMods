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

        public int lastHostileActionTick = int.MinValue;

        public const int violationDisableTicks = 2500;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref lastHostileActionTick, nameof(lastHostileActionTick), int.MinValue);
        }

        public bool DisableHostilityFrom(Thing thing)
        {
            return DefExt.disableHostilityFromFaction != null && DefExt.disableHostilityFromFaction == thing.Faction?.def;
        }
    }
}