namespace XylXenos;

[UsedFromXml]
public class Recipe_RemoveHediff_Petrified : Recipe_RemoveHediff
{
    private static bool ValidHediff(Pawn pawn, RecipeDef recipe, Hediff hediff)
    {
        return hediff.def == recipe.removesHediff &&
               hediff.Visible &&
               hediff.Severity < pawn.health.hediffSet.GetPartHealth(hediff.Part);
    }

    // ReSharper disable once ParameterHidesMember
    public override IEnumerable<BodyPartRecord> GetPartsToApplyOn(Pawn pawn, RecipeDef recipe)
    {
        List<Hediff> allHediffs = pawn.health.hediffSet.hediffs;
        return allHediffs.Where(hediff => hediff.Part != null && ValidHediff(pawn, recipe, hediff)).Select(hediff => hediff.Part);
    }

    public override bool AvailableOnNow(Thing thing, BodyPartRecord? part = null)
    {
        if (!base.AvailableOnNow(thing, part))
        {
            return false;
        }

        if (thing is not Pawn pawn)
        {
            return false;
        }

        if (recipe.targetsBodyPart)
            return GetPartsToApplyOn(pawn, recipe).Any();
        else
            return pawn.health.hediffSet.hediffs.Any(hediff => ValidHediff(pawn, recipe, hediff));
    }

    public override void ApplyOnPawn(Pawn pawn, BodyPartRecord part, Pawn? billDoer, List<Thing> ingredients, Bill bill)
    {
        DebugAssert.NotNull(recipe.removesHediff);

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
            // ReSharper disable once VariableHidesOuterVariable
            if (pawn.health.hediffSet.hediffs.Find(hediff =>
                    hediff.def == recipe.removesHediff && hediff.Part == part && hediff.Visible) is { } hediff)
            {
                RemoveHediff(pawn, billDoer, hediff, bill);
            }

            return;
        }

        foreach (var hediff in pawn.health.hediffSet.hediffs.ToList())
        {
            if (hediff.def == recipe.removesHediff && hediff.Visible)
            {
                RemoveHediff(pawn, billDoer, hediff, bill);
            }
        }
    }

    private static void RemoveHediff(Pawn pawn, Pawn? billDoer, Hediff hediff, Bill bill)
    {
        var part = hediff.Part;
        var severity = hediff.Severity;
        pawn.health.RemoveHediff(hediff);
        if (hediff.def.spawnThingOnRemoved != null && billDoer != null)
        {
            GenSpawn.Spawn(hediff.def.spawnThingOnRemoved, billDoer.Position, billDoer.Map);
        }

        // Give an injury and tend it immediately
        var injury = HediffMaker.MakeHediff(HediffDefOf.SurgicalCut, pawn, part);
        injury.Severity = severity;
        pawn.health.AddHediff(injury);

        // Adding the injury could have destroyed the body part. This should have been prevented by AvailableOnNow, but just in case, check again before tending.
        if (pawn.health.hediffSet.PartIsMissing(part))
            return;

        var medicine = (bill as Bill_Medical)?.consumedMedicine?.Keys
            .OrderByDescending(medicine => medicine.GetStatValueAbstract(StatDefOf.MedicalPotency)).FirstOrDefault();

        var quality = TendUtility.CalculateBaseTendQuality(billDoer, pawn, medicine);
        var maxQuality = medicine?.GetStatValueAbstract(StatDefOf.MedicalPotency) ?? 0.7f;
        injury.Tended(quality, maxQuality);
    }
}
