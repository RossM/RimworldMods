using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using RimWorld;
using Verse;

namespace XylXenos
{
    [UsedImplicitly]
    public class HediffCompProperties_GiveHediffExt : HediffCompProperties
    {
        public class TriggeredHediff
        {
            public HediffDef hediff;

            public IntRange countRange = new(1, 1);

            public List<BodyPartDef> partsToAffect;

            public FloatRange severityRange = FloatRange.Zero;
            public bool inheritSeverity;

            public void ApplyTo(Pawn pawn, float severity, List<Hediff> outAddedHediffs = null)
            {
                List<Hediff> addedHediffs = [];
                HediffGiverUtility.TryApply(pawn, hediff, partsToAffect, canAffectAnyLivePart: false, countRange.RandomInRange, addedHediffs, useCoverage: false);
                foreach (Hediff item in addedHediffs)
                {
                    if (inheritSeverity)
                        item.Severity = severity;
                    else if (severityRange != FloatRange.Zero)
                        item.Severity = severityRange.RandomInRange;
                }

                outAddedHediffs?.AddRange(addedHediffs);
            }
        }


        public bool skipIfAlreadyExists;
        public bool triggeredManually;
        public bool triggeredOnRemoval;
        public bool disappearsAfterGiving;

        public float minSeverity = 0f;
        public float mtbDays = -1;

        public List<TriggeredHediff> hediffs;

        public MessageTypeDef messageDef;
        [MustTranslate] public string message;

        public LetterDef letterDef;
        [MustTranslate] public string letterLabel;
        [MustTranslate] public string letterText;


        public HediffCompProperties_GiveHediffExt()
        {
            compClass = typeof(HediffComp_GiveHediffExt);
        }
    }

    public class HediffComp_GiveHediffExt : HediffComp
    {
        public HediffCompProperties_GiveHediffExt Props => (HediffCompProperties_GiveHediffExt)props;

        private readonly List<Hediff> added = [];

        [Unsaved] private bool hasTriggered = false;

        public override void CompPostTickInterval(ref float severityAdjustment, int delta)
        {
            if (Props.triggeredManually || Props.triggeredOnRemoval)
                return;
            if (parent.Severity < Props.minSeverity)
                return;
            if (Props.mtbDays > 0 && !Rand.MTBEventOccurs(Props.mtbDays, GenDate.TicksPerDay, delta))
                return;

            Trigger();
        }

        public override void CompPostPostRemoved()
        {
            base.CompPostPostRemoved();
            if (hasTriggered)
                return;
            hasTriggered = true;
            Trigger();
        }

        public void Trigger()
        {
            if (!Props.message.NullOrEmpty() && PawnUtility.ShouldSendNotificationAbout(parent.pawn))
            {
                Messages.Message(Props.message.Formatted(parent.pawn.Named("PAWN")), parent.pawn,
                    Props.messageDef ?? MessageTypeDefOf.NegativeEvent);
            }

            added.Clear();
            foreach (HediffCompProperties_GiveHediffExt.TriggeredHediff triggeredHediff in Props.hediffs)
            {
                if (Props.skipIfAlreadyExists && Pawn.health.hediffSet.HasHediff(triggeredHediff.hediff))
                    continue;
                triggeredHediff.ApplyTo(parent.pawn, parent.Severity, added);
            }

            if (added.Empty())
            {
                parent.pawn.health.RemoveHediff(parent);
                return;
            }

            if (PawnUtility.ShouldSendNotificationAbout(Pawn))
            {
                SendLetter();
            }

            if (Props.disappearsAfterGiving)
            {
                hasTriggered = true;
                Pawn.health.RemoveHediff(parent);
            }

            added.Clear();
        }

        private void SendLetter()
        {
            if (Props.letterLabel != null)
            {
                string organs = added.Where(x => x.Part != null).Select(x => x.Part.LabelCap).ToLineList("  - ");

                TaggedString label = Props.letterLabel.Formatted(Pawn.Named("PAWN"), organs.Named("ORGANS"));
                TaggedString text = Props.letterText.Formatted(Pawn.Named("PAWN"), organs.Named("ORGANS"));
                Find.LetterStack.ReceiveLetter(label, text, Props.letterDef ?? LetterDefOf.NegativeEvent, Pawn);
            }
        }
    }
}
