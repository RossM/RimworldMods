using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace XylXenos
{
    [UsedFromXml]
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
            public bool allowDuplicates;

            public bool skipIfAlreadyExists;
            public bool triggeredManually;
            public bool triggeredOnRemoval;
            public bool disappearsAfterGiving;

            public float minSeverity = 0f;
            public float mtbDays = -1;

            public bool ApplyTo(Pawn pawn, Hediff parent, List<Hediff> outAddedHediffs = null)
            {
                bool success = false;
                List<BodyPartDef> parts = sameBodyPart ? [parent.Part.def] : partsToAffect;

                if (canAffectAnyLivePart || parts != null)
                {
                    for (int i = 0; i < countRange.RandomInRange; i++)
                    {
                        IEnumerable<BodyPartRecord> source = pawn.health.hediffSet.GetNotMissingParts();
                        if (parts != null)
                        {
                            source = source.Where(p => ((IEnumerable<BodyPartDef>)parts).Contains(p.def));
                        }

                        if (canAffectAnyLivePart)
                        {
                            source = source.Where(p => p.def.alive);
                        }

                        source = source.Where(p =>
                            (allowDuplicates || !pawn.health.hediffSet.HasHediff(hediff, p)) &&
                            !pawn.health.hediffSet.PartOrAnyAncestorHasDirectlyAddedParts(p)).ToList();
                        if (!source.Any())
                        {
                            break;
                        }

                        BodyPartRecord bodyPartRecord = source.RandomElementByWeight(x => x.coverageAbs);

                        Hediff hediff2
                            = HediffMaker.MakeHediff(
                                partRecord: bodyPartRecord, def: hediff,
                                pawn: pawn);

                        if (inheritSeverity)
                            hediff2.Severity = parent.Severity;
                        else if (severityRange != FloatRange.Zero)
                            hediff2.Severity = severityRange.RandomInRange;

                        pawn.health.AddHediff(hediff2);
                        outAddedHediffs?.Add(hediff2);
                        success = true;
                    }
                }
                else
                {
                    if (!pawn.health.hediffSet.HasHediff(hediff))
                    {
                        Hediff hediff3 = HediffMaker.MakeHediff(hediff, pawn);
                        pawn.health.AddHediff(hediff3);
                        outAddedHediffs?.Add(hediff3);
                        success = true;
                    }
                }

                return success;
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
                if (parent.GetComp<HediffComp_GrowthModeExt>()?.IsActive == false)
                    continue;
                if (hediff.mtbDays > 0 && !Rand.MTBEventOccurs(hediff.mtbDays, GenDate.TicksPerDay, delta))
                    continue;
                toTrigger.Add(hediff);
            }

            if (toTrigger.Count > 0)
                Trigger(toTrigger);

            toTrigger.Clear();
        }

        public override void CompPostPostRemoved()
        {
            base.CompPostPostRemoved();

            if (hasTriggeredForRemoval)
                return;
            hasTriggeredForRemoval = true;

            toTrigger.Clear();
            foreach (var hediff in Props.hediffs)
            {
                if (hediff.triggeredOnRemoval)
                    toTrigger.Add(hediff);
            }

            if (toTrigger.Count > 0)
                Trigger(toTrigger);

            toTrigger.Clear();
        }

        public void Trigger(IEnumerable<HediffCompProperties_GiveHediffExt.TriggeredHediff> hediffs)
        {
            bool shouldRemove = false;

            if (!Props.message.NullOrEmpty() && PawnUtility.ShouldSendNotificationAbout(parent.pawn))
            {
                Messages.Message(Props.message.Formatted(parent.pawn.Named("PAWN")), parent.pawn,
                    Props.messageDef ?? MessageTypeDefOf.NegativeEvent);
            }

            added.Clear();
            foreach (HediffCompProperties_GiveHediffExt.TriggeredHediff triggeredHediff in hediffs)
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

        public override IEnumerable<Gizmo> CompGetGizmos()
        {
            if (!DebugSettings.godMode)
                yield break;

            for (var index = 0; index < Props.hediffs.Count; index++)
            {
                HediffCompProperties_GiveHediffExt.TriggeredHediff hediff = Props.hediffs[index];
                yield return new Command_Action
                {
                    defaultLabel = parent.Part != null
                        ? $"DEV: Trigger {parent.LabelBase} ({parent.Part?.Label}) #{index}"
                        : $"DEV: Trigger {parent.LabelBase} #{index}",
                    action = () => Trigger([hediff]),
                    groupable = false,
                };
            }
        }
    }
}
