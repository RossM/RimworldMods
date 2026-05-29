namespace XylXenos;

[UsedFromXml]
public class Recipe_RemoveHediff_Petrified : Recipe_RemoveHediff
{
    // ReSharper disable once ParameterHidesMember
    public override IEnumerable<BodyPartRecord> GetPartsToApplyOn(Pawn pawn, RecipeDef recipe)
    {
        List<Hediff> allHediffs = pawn.health.hediffSet.hediffs;
        return allHediffs.Where(hediff =>
                hediff.Part != null && hediff.def == recipe.removesHediff && hediff.Severity < 1.0f && hediff.Visible)
            .Select(hediff => hediff.Part);
    }

    public override bool AvailableOnNow(Thing thing, BodyPartRecord part = null)
    {
        if (!base.AvailableOnNow(thing, part))
        {
            return false;
        }

        if (!(thing is Pawn pawn))
        {
            return false;
        }

        if (recipe.targetsBodyPart)
            return GetPartsToApplyOn(pawn, recipe).Any();
        else
            return pawn.health.hediffSet.hediffs.Any(hediff =>
                hediff.def == recipe.removesHediff && hediff.Severity < 1.0f && hediff.Visible);
    }

    public override void ApplyOnPawn(Pawn pawn, BodyPartRecord part, Pawn billDoer, List<Thing> ingredients, Bill bill)
    {
        if (billDoer != null)
        {
            if (CheckSurgeryFail(billDoer, pawn, ingredients, part, bill))
            {
                return;
            }

            TaleRecorder.RecordTale(TaleDefOf.DidSurgery, billDoer, pawn);
            if (PawnUtility.ShouldSendNotificationAbout(pawn) || PawnUtility.ShouldSendNotificationAbout(billDoer))
            {
                string text = recipe.successfullyRemovedHediffMessage.NullOrEmpty()
                    ? "MessageSuccessfullyRemovedHediff".Translate(billDoer.LabelShort, pawn.LabelShort,
                        recipe.removesHediff.label.Named("HEDIFF"), billDoer.Named("SURGEON"), pawn.Named("PATIENT"))
                    : (string)recipe.successfullyRemovedHediffMessage.Formatted(billDoer.LabelShort, pawn.LabelShort);
                Messages.Message(text, pawn, MessageTypeDefOf.PositiveEvent);
            }
        }

        if (recipe.targetsBodyPart)
        {
            Hediff hediff = pawn.health.hediffSet.hediffs.Find(hediff =>
                hediff.def == recipe.removesHediff && hediff.Part == part && hediff.Visible);
            if (hediff != null)
            {
                pawn.health.RemoveHediff(hediff);
                if (hediff.def.spawnThingOnRemoved != null && billDoer != null)
                {
                    GenSpawn.Spawn(hediff.def.spawnThingOnRemoved, billDoer.Position, billDoer.Map);
                }
            }

            return;
        }

        for (int num = pawn.health.hediffSet.hediffs.Count - 1; num >= 0; num--)
        {
            Hediff hediff = pawn.health.hediffSet.hediffs[num];
            if (hediff.def == recipe.removesHediff && hediff.Visible)
            {
                pawn.health.RemoveHediff(hediff);
                if (hediff.def.spawnThingOnRemoved != null && billDoer != null)
                {
                    GenSpawn.Spawn(hediff.def.spawnThingOnRemoved, billDoer.Position, billDoer.Map);
                }
            }
        }
    }
}