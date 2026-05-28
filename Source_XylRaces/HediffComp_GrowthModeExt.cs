using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace XylXenos
{
    public class GrowthMode
    {
        public bool isActive = false;
        public bool causesNoPain = false;
        public bool allowTend = true;
        public float changeMtbDays = -1;
        public float severityPerDay = 0f;
        public FloatRange severityPerDayRange = FloatRange.Zero;
        [MustTranslate] public string label;
        [MustTranslate] public string message;
        public MessageTypeDef messageType;
        [MustTranslate] public string tipString;
    }

    [UsedFromXml]
    public class HediffCompProperties_GrowthModeExt : HediffCompProperties_SeverityPerDay
    {
        public List<GrowthMode> modes;

        public HediffCompProperties_GrowthModeExt()
        {
            compClass = typeof(HediffComp_GrowthModeExt);
        }

        public override void ResolveReferences(HediffDef parent)
        {
            base.ResolveReferences(parent);

            if (parent.hediffClass == typeof(HediffWithComps))
                parent.hediffClass = typeof(HediffWithCompsExt);
        }

        public override IEnumerable<string> ConfigErrors(HediffDef parentDef)
        {
            foreach (var error in base.ConfigErrors(parentDef))
                yield return error;

            if (!typeof(HediffWithCompsExt).IsAssignableFrom(parentDef.hediffClass))
                yield return "hediffClass must be HediffWithCompsExt or a subclass thereof";
        }
    }

    public class HediffComp_GrowthModeExt : HediffComp_SeverityPerDay
    {
        public HediffCompProperties_GrowthModeExt TProps => (HediffCompProperties_GrowthModeExt)props;

        public override string CompLabelInBracketsExtra => growthMode.label;

        public override string CompTipStringExtra => growthMode.tipString;

        public bool IsActive => growthMode.isActive;
        public bool AllowTend => growthMode.allowTend;

        public GrowthMode growthMode;

        public override void CompExposeData()
        {
            Scribe_Values.Look(ref growthMode, nameof(growthMode));
        }

        public override void CompPostPostAdd(DamageInfo? dinfo)
        {
            base.CompPostPostAdd(dinfo);

            SetGrowthMode(TProps.modes[0]);
        }

        public override float SeverityChangePerDay()
        {
            return severityPerDay;
        }

        public override void CompPostTickInterval(ref float severityAdjustment, int delta)
        {
            base.CompPostTickInterval(ref severityAdjustment, delta);

            float mtbDays = growthMode.changeMtbDays;

            if (mtbDays > 0 && Rand.MTBEventOccurs(mtbDays, GenDate.TicksPerDay, delta))
            {
                ChangeGrowthMode();
            }
        }

        public virtual void ChangeGrowthMode()
        {
            SetGrowthMode(TProps.modes.Where(mode => mode != growthMode).RandomElement());

            if (!growthMode.message.NullOrEmpty() && PawnUtility.ShouldSendNotificationAbout(Pawn))
            {
                Messages.Message(
                    growthMode.message.Formatted(Pawn.Named("PAWN")),
                    Pawn, 
                    growthMode.messageType ?? MessageTypeDefOf.NegativeHealthEvent);
            }
        }

        private void SetGrowthMode(GrowthMode mode)
        {
            growthMode = mode;
            severityPerDay = growthMode.severityPerDay + growthMode.severityPerDayRange.RandomInRange;
            parent.causesNoPain = growthMode.causesNoPain;
        }

        public override IEnumerable<Gizmo> CompGetGizmos()
        {
            if (!DebugSettings.godMode)
                yield break;

            yield return new Command_Action()
            {
                defaultLabel = "DEV: Toggle petrification growth state",
                action = ChangeGrowthMode,
            };
        }
    }
}
