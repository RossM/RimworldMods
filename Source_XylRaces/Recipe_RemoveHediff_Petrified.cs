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
                RemoveHediff(pawn, billDoer, hediff, bill);
            }

            return;
        }

        for (int num = pawn.health.hediffSet.hediffs.Count - 1; num >= 0; num--)
        {
            Hediff hediff = pawn.health.hediffSet.hediffs[num];
            if (hediff.def == recipe.removesHediff && hediff.Visible)
            {
                RemoveHediff(pawn, billDoer, hediff, bill);
            }
        }
    }

    private static void RemoveHediff(Pawn pawn, Pawn billDoer, Hediff hediff, Bill bill)
    {
        var part = hediff.Part;
        pawn.health.RemoveHediff(hediff);
        if (hediff.def.spawnThingOnRemoved != null && billDoer != null)
        {
            GenSpawn.Spawn(hediff.def.spawnThingOnRemoved, billDoer.Position, billDoer.Map);
        }

        // Removing petrification causes an injury. Tend the injury.

        var injury = pawn.health.hediffSet.hediffs.LastOrDefault(h => h is Hediff_Injury && h.Part == part && !h.IsTended());
        if (injury == null)
            return;

        if (bill is not Bill_Medical bill_medical)
            return;

        var medicine = bill_medical.consumedMedicine.Keys
            .OrderByDescending(medicine => medicine.GetStatValueAbstract(StatDefOf.MedicalPotency)).FirstOrDefault();

        var quality = TendUtility.CalculateBaseTendQuality(billDoer, pawn, medicine);
        var maxQuality = medicine?.GetStatValueAbstract(StatDefOf.MedicalPotency) ?? 0.7f;
        injury.Tended(quality, maxQuality);
    }
}
