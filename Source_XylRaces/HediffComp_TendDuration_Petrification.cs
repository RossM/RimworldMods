using System.Text;
using RimWorld;
using Verse;

namespace XylXenos
{
    public class HediffCompProperties_TendDuration_Petrification : HediffCompProperties_TendDuration
    {
        public float changeModeAtTotalTendQuality;

        public HediffCompProperties_TendDuration_Petrification()
        {
            compClass = typeof(HediffComp_TendDuration_Petrification);
        }
    }

    public class HediffComp_TendDuration_Petrification : HediffComp_TendDuration
    {
        public new HediffCompProperties_TendDuration_Petrification TProps => (HediffCompProperties_TendDuration_Petrification)props;

        public PetrificationGrowthMode GrowthMode =>
            parent.GetComp<HediffComp_GrowthMode_Petrification>()?.growthMode ?? PetrificationGrowthMode.Active;

        public virtual bool AllowTendExt => GrowthMode == PetrificationGrowthMode.Active;

        public override TextureAndColor CompStateIcon =>
            GrowthMode == PetrificationGrowthMode.Active ? base.CompStateIcon : TextureAndColor.None;

        public override string CompTipStringExtra
        {
            get
            {
                if (parent.IsPermanent())
                {
                    return null;
                }

                StringBuilder stringBuilder = new StringBuilder();
                if (!IsTended)
                {
                    if (!Pawn.Dead && parent.TendableNow())
                    {
                        stringBuilder.AppendLine("NeedsTendingNow".Translate());
                    }
                }
                else
                {
                    if (TProps.showTendQuality)
                    {
                        var tendedLabel = ((parent.Part != null && parent.Part.def.IsSolid(parent.Part, Pawn.health.hediffSet.hediffs))
                            ? TProps.labelSolidTendedWell
                            : (parent.Part is not { depth: BodyPartDepth.Inside }
                                ? TProps.labelTendedWell
                                : TProps.labelTendedWellInner));

                        if (tendedLabel != null)
                        {
                            stringBuilder.AppendLine(tendedLabel.CapitalizeFirst() + " (" + "quality".Translate() + " " +
                                                     tendQuality.ToStringPercent("F0") + ")");
                        }
                        else
                        {
                            stringBuilder.AppendLine($"{"TendQuality".Translate()}: {tendQuality.ToStringPercent()}");
                        }

                        if (TProps.disappearsAtTotalTendQuality >= 0)
                        {
                            stringBuilder.AppendLine("DisappearsAtTotalTendQuality".Translate() + ": " +
                                                     totalTendQuality.ToStringPercent() + " / " +
                                                     ((float)TProps.disappearsAtTotalTendQuality).ToStringPercent());
                        }

                        if (TProps.changeModeAtTotalTendQuality >= 0)
                        {
                            stringBuilder.AppendLine("XylBecomesDormantAtTotalTendQuality".Translate() + ": " +
                                                     totalTendQuality.ToStringPercent() + " / " +
                                                     TProps.changeModeAtTotalTendQuality.ToStringPercent());
                        }
                    }

                    if (!Pawn.Dead && !TProps.TendIsPermanent && parent.TendableNow(ignoreTimer: true) &&
                        GrowthMode == PetrificationGrowthMode.Active)
                    {
                        int num = tendTicksLeft - TProps.TendTicksOverlap;
                        if (num < 0)
                        {
                            stringBuilder.AppendLine("CanTendNow".Translate());
                        }
                        else if ("NextTendIn".CanTranslate())
                        {
                            stringBuilder.AppendLine("NextTendIn".Translate(num.ToStringTicksToPeriod()));
                        }
                        else
                        {
                            stringBuilder.AppendLine("NextTreatmentIn".Translate(num.ToStringTicksToPeriod()));
                        }

                        stringBuilder.AppendLine("TreatmentExpiresIn".Translate(tendTicksLeft.ToStringTicksToPeriod()));
                    }
                }

                return stringBuilder.ToString().TrimEndNewlines();
            }
        }

        public override void CompPostTickInterval(ref float severityAdjustment, int delta)
        {
            base.CompPostTickInterval(ref severityAdjustment, delta);

            if (TProps.changeModeAtTotalTendQuality < 0)
                return;
            if (totalTendQuality < TProps.changeModeAtTotalTendQuality)
                return;

            parent.GetComp<HediffComp_GrowthMode_Petrification>()?.ChangeGrowthMode();
            totalTendQuality = 0;
            tendQuality = 0;
        }
    }
}
