using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace XylXenos
{
    public enum PetrificationGrowthMode
    {
        Active,
        Dormant,
    }

    public class HediffCompProperties_PetrificationGrowthMode : HediffCompProperties_SeverityPerDay
    {
        public float becomeActiveMtbDays = -1;
        public float becomeDormantMtbDays = -1;
        [MustTranslate] public string messageBecomeActive;
        [MustTranslate] public string messageBecomeDormant;
        [MustTranslate] public string tipStringActive;
        [MustTranslate] public string tipStringDormant;

        public HediffCompProperties_PetrificationGrowthMode()
        {
            compClass = typeof(HediffComp_PetrificationGrowthMode);
        }
    }

    public class HediffComp_PetrificationGrowthMode : HediffComp_SeverityPerDay
    {
        public HediffCompProperties_PetrificationGrowthMode TProps => (HediffCompProperties_PetrificationGrowthMode)props;

        public override string CompLabelInBracketsExtra => growthMode switch
        {
            PetrificationGrowthMode.Active => "XylGrowthStateActive".Translate(),
            PetrificationGrowthMode.Dormant => "XylGrowthStateDormant".Translate(),
            _ => throw new ArgumentOutOfRangeException()
        };

        public override string CompTipStringExtra => growthMode switch
        {
            PetrificationGrowthMode.Active => TProps.tipStringActive,
            PetrificationGrowthMode.Dormant => TProps.tipStringDormant,
            _ => throw new ArgumentOutOfRangeException()
        };

        public PetrificationGrowthMode growthMode;

        public override void CompExposeData()
        {
            Scribe_Values.Look(ref growthMode, nameof(growthMode));
        }

        public override float SeverityChangePerDay()
        {
            return growthMode switch
            {
                PetrificationGrowthMode.Active => severityPerDay,
                PetrificationGrowthMode.Dormant => 0f,
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        public override void CompPostTickInterval(ref float severityAdjustment, int delta)
        {
            base.CompPostTickInterval(ref severityAdjustment, delta);

            float mtbDays = growthMode switch
            {
                PetrificationGrowthMode.Active => TProps.becomeDormantMtbDays,
                PetrificationGrowthMode.Dormant => TProps.becomeActiveMtbDays,
                _ => throw new ArgumentOutOfRangeException()
            };

            if (mtbDays > 0 && Rand.MTBEventOccurs(mtbDays, GenDate.TicksPerDay, delta))
            {
                ChangeGrowthMode();
            }
        }

        public virtual void ChangeGrowthMode()
        {
            growthMode = ((PetrificationGrowthMode[])Enum.GetValues(typeof(PetrificationGrowthMode))).Where(mode => mode != growthMode)
                .RandomElement();

            switch (growthMode)
            {
                case PetrificationGrowthMode.Active:
                {
                    if (!TProps.messageBecomeActive.NullOrEmpty() && PawnUtility.ShouldSendNotificationAbout(Pawn))
                    {
                        Messages.Message(
                            TProps.messageBecomeActive.Formatted(Pawn.Named("PAWN")),
                            Pawn, MessageTypeDefOf.NegativeHealthEvent);
                    }

                    break;
                }
                case PetrificationGrowthMode.Dormant:
                {
                    if (!TProps.messageBecomeDormant.NullOrEmpty() && PawnUtility.ShouldSendNotificationAbout(Pawn))
                    {
                        Messages.Message(
                            TProps.messageBecomeDormant.Formatted(Pawn.Named("PAWN")),
                            Pawn, MessageTypeDefOf.NeutralEvent);
                    }

                    break;
                }
            }
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
