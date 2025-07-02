using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using UnityEngine;
using Verse;

namespace XylRacesCore
{
    public class GeneDefExtension_Hyperlactation : DefModExtension_WithIcon
    {
        public float chargePerItem = 0.1f;
        public HediffDef hediff;
        public List<ThoughtDef> milkedThoughts;
        public Gender? activeGender;
        public int ticksPerSorenessStage = 60000;
    }

    public class Gene_Hyperlactation : Gene
    {
        public bool allowMilking = true;
        public bool onlyMilkWhenFull = true;

        public int? fullSinceTick;

        public GeneDefExtension_Hyperlactation DefExt => def.GetModExtension<GeneDefExtension_Hyperlactation>();

        public HediffComp_Lactating Lactating => pawn.health.hediffSet.GetHediffComps<HediffComp_Lactating>().FirstOrDefault();

        public override bool Active
        {
            get
            {
                if (!base.Active)
                    return false;
                if (DefExt.activeGender == null)
                    return true;
                return pawn?.gender == DefExt.activeGender;
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref fullSinceTick, "fullSinceTick");
            Scribe_Values.Look(ref allowMilking, "allowMilking", false);
            Scribe_Values.Look(ref onlyMilkWhenFull, "onlyMilkWhenFull", true);
        }

        public override IEnumerable<Gizmo> GetGizmos()
        {
            if (!Active)
                yield break;
            if (!pawn.Spawned)
                yield break;
            if (!pawn.IsColonistPlayerControlled)
                yield break;

            yield return new Command_Toggle
            {
                defaultLabel = "XylCommandMilkLabel".TranslateSimple(),
                defaultDesc = "XylCommandMilkDesc".TranslateSimple(),
                isActive = () => allowMilking,
                toggleAction = () => { allowMilking = !allowMilking; },
                icon = DefExt.Icon,
            };

            if (allowMilking)
            {
                yield return new Command_Toggle
                {
                    defaultLabel = "XylCommandMilkOnlyWhenFullLabel".TranslateSimple(),
                    defaultDesc = "XylCommandMilkOnlyWhenFullDesc".TranslateSimple(),
                    isActive = () => onlyMilkWhenFull,
                    toggleAction = () => { onlyMilkWhenFull = !onlyMilkWhenFull; },
                    icon = DefExt.Icon,
                };
            }
        }

        public override void PostAdd()
        {
            base.PostAdd();

            AddHediff();
        }

        public override void TickInterval(int delta)
        {
            if (!Active)
                return;

            base.TickInterval(delta);

            if (!pawn.IsHashIntervalTick(60, delta)) 
                return;

            AddHediff();

            if (Lactating != null && Lactating.Charge >= Lactating.Props.fullChargeAmount)
                fullSinceTick ??= Find.TickManager.TicksGame;
            else
                fullSinceTick = null;
        }

        private void AddHediff()
        {
            if (!Active)
                return;

            if (pawn.health.hediffSet.HasHediff(HediffDefOf.Malnutrition))
                return;

            var lactatingHediff = pawn.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.Lactating);
            if (lactatingHediff != null)
                pawn.health.RemoveHediff(lactatingHediff);

            Hediff hediff = pawn.health.hediffSet.GetFirstHediffOfDef(DefExt.hediff) ??
                            pawn.health.AddHediff(DefExt.hediff);
            hediff.Severity = 1.0f;
        }

        public int MilkCount => Mathf.FloorToInt((Lactating?.Charge ?? 0) / DefExt.chargePerItem);

        public bool ReadyToMilk()
        {
            if (!allowMilking)
                return false;

            var requiredCount = 1;
            if (onlyMilkWhenFull)
                requiredCount = Mathf.FloorToInt(Lactating.Props.fullChargeAmount / DefExt.chargePerItem);

            return MilkCount >= requiredCount;
        }

        public bool TryGetSoreness(out int soreness)
        {
            soreness = -1;
            if (fullSinceTick == null)
                return false;
            soreness = Mathf.FloorToInt((float)(Find.TickManager.TicksGame - fullSinceTick.Value) / DefExt.ticksPerSorenessStage);
            return true;
        }

        public override IEnumerable<StatDrawEntry> SpecialDisplayStats()
        {
            var props = Lactating.Props;
            float milkPerDay = props.fullChargeAmount * 60000f / (props.ticksToFullCharge * DefExt.chargePerItem);
            yield return new StatDrawEntry(StatCategoryDefOf.PawnFood, "XylMilkProductionLabel".TranslateSimple(),
                "PerDay".Translate(milkPerDay.ToStringByStyle(ToStringStyle.FloatOne)),
                "XylMilkProductionDesc".TranslateSimple(), 1);
        }
    }
}
