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
            public bool sameBodyPart;
            public bool canAffectAnyLivePart;

            public bool skipIfAlreadyExists;
            public bool triggeredManually;
            public bool triggeredOnRemoval;
            public bool disappearsAfterGiving;

            public float minSeverity = 0f;
            public float mtbDays = -1;

            public bool ApplyTo(Pawn pawn, Hediff parent, List<Hediff> outAddedHediffs = null)
            {
                List<Hediff> addedHediffs = [];
                List<BodyPartDef> parts = sameBodyPart ? [parent.Part.def] : partsToAffect;
                HediffGiverUtility.TryApply(pawn, hediff, parts, canAffectAnyLivePart, countRange.RandomInRange, addedHediffs, useCoverage: false);
                foreach (Hediff item in addedHediffs)
                {
                    if (inheritSeverity)
                        item.Severity = parent.Severity;
                    else if (severityRange != FloatRange.Zero)
                        item.Severity = severityRange.RandomInRange;
                }

                outAddedHediffs?.AddRange(addedHediffs);
                return addedHediffs.Count > 0;
            }
        }


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
        private readonly List<HediffCompProperties_GiveHediffExt.TriggeredHediff> toTrigger = [];

        [Unsaved] private bool hasTriggeredForRemoval = false;

        public override void CompPostTickInterval(ref float severityAdjustment, int delta)
        {
            toTrigger.Clear();
            foreach (var hediff in Props.hediffs)
            {
                if (hediff.triggeredManually || hediff.triggeredOnRemoval)
                    continue;
                if (parent.Severity < hediff.minSeverity)
                    continue;
                if (hediff.mtbDays > 0 && !Rand.MTBEventOccurs(hediff.mtbDays, GenDate.TicksPerDay, delta))
                    continue;
                toTrigger.Add(hediff);
            }

            if (toTrigger.Count > 0)
                Trigger();
    
            toTrigger.Clear();
        }

        public override void CompPostPostRemoved()
        {
            base.CompPostPostRemoved();

            if (hasTriggeredForRemoval)
                return;
            hasTriggeredForRemoval = true;
            
            Trigger();
        }

        public void Trigger()
        {
            bool shouldRemove = false;

            if (!Props.message.NullOrEmpty() && PawnUtility.ShouldSendNotificationAbout(parent.pawn))
            {
                Messages.Message(Props.message.Formatted(parent.pawn.Named("PAWN")), parent.pawn,
                    Props.messageDef ?? MessageTypeDefOf.NegativeEvent);
            }

            added.Clear();
            foreach (HediffCompProperties_GiveHediffExt.TriggeredHediff triggeredHediff in toTrigger)
            {
                if (triggeredHediff.skipIfAlreadyExists && Pawn.health.hediffSet.HasHediff(triggeredHediff.hediff))
                    continue;
                bool result = triggeredHediff.ApplyTo(parent.pawn, parent, added);
                if (triggeredHediff.disappearsAfterGiving && result)
                    shouldRemove = true;
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

            if (shouldRemove)
            {
                hasTriggeredForRemoval = true;
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
