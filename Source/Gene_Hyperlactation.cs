using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using UnityEngine;
using Verse;

namespace XylRacesCore
{
    public class GeneDefExtension_Hyperlactation : DefModExtension
    {
        public float chargePerItem = 0.1f;
        public HediffDef hediff;
        public List<ThoughtDef> milkedThoughts;
        public Gender? activeGender;
        public int ticksPerSorenessStage = 60000;

        [NoTranslate]
        public string iconPath = "Things/Item/Resource/Milk";

        [Unsaved(false)] 
        private Texture2D cachedIcon;

        public Texture2D Icon
        {
            get
            {
                cachedIcon ??= iconPath.NullOrEmpty()
                    ? BaseContent.BadTex
                    : ContentFinder<Texture2D>.Get(iconPath) ?? BaseContent.BadTex;
                return cachedIcon;
            }
        }
    }

    public class Gene_Hyperlactation : Gene
    {
        public bool allowMilking = false;
        public bool onlyMilkWhenFull = true;

        public int? fullSinceTick;

        public GeneDefExtension_Hyperlactation DefExt => def.GetModExtension<GeneDefExtension_Hyperlactation>();

        public HediffComp_Lactating Lactating => GetPawnLactationHediff(pawn).TryGetComp<HediffComp_Lactating>();

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
                defaultLabel = "Allow milking",
                defaultDesc = "Allow other characters to milk this character.",
                isActive = () => allowMilking,
                toggleAction = () => { allowMilking = !allowMilking; },
                icon = DefExt.Icon,
            };

            if (allowMilking)
            {
                yield return new Command_Toggle
                {
                    defaultLabel = "Only milk when full",
                    defaultDesc = "Only allow milking when this character's breast milk is full.",
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

        public static Hediff GetPawnLactationHediff(Pawn pawn)
        {
            return pawn.health.hediffSet.hediffs.FirstOrDefault(h => h.TryGetComp<HediffComp_Lactating>() != null);
        }

        public static bool HasPawnLactationHediff(Pawn pawn)
        {
            return GetPawnLactationHediff(pawn) != null;
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
            yield return new StatDrawEntry(StatCategoryDefOf.PawnFood, "Milk production",
                milkPerDay.ToStringByStyle(ToStringStyle.FloatOne) + " / day", "The number of milk items this character can produce in a day if well fed.", 1);
        }
    }
}
