namespace XylXenos;

[UsedFromXml]
public class DefModExtension_Incident_GeneticDisease : DefModExtension
{
    public required List<GeneDef> requiredGenesAny;
    public float chanceFactorPerTarget = 1f;

    private IncidentDef? parent;

    public override void ResolveReferences(Def parentDef)
    {
        base.ResolveReferences(parentDef);

        parent = parentDef as IncidentDef;
    }

    public override IEnumerable<string> ConfigErrors()
    {
        foreach (var configError in base.ConfigErrors())
            yield return configError;

        if (requiredGenesAny is not { Count: > 0 })
            yield return $"{nameof(requiredGenesAny)} must have at least one entry";
        if (parent is null)
            yield return $"{nameof(DefModExtension_Incident_GeneticDisease)} can only be applied to {nameof(IncidentDef)}";
        else if (parent.diseaseIncident is null)
            yield return $"{nameof(parent.diseaseIncident)} is null";
    }
}

[UsedFromXml]
public class IncidentWorker_GeneticDisease : IncidentWorker_DiseaseHuman
{
    public DefModExtension_Incident_GeneticDisease DefExt => def.GetModExtension<DefModExtension_Incident_GeneticDisease>()!;

    protected override IEnumerable<Pawn> PotentialVictimCandidates(IIncidentTarget target)
    {
        return base.PotentialVictimCandidates(target).Where(pawn => DefExt.requiredGenesAny.Any(pawn.HasActiveGene));
    }

    public override float ChanceFactorNow(IIncidentTarget target)
    {
        int candidateCount = PotentialVictimCandidates(target).Count();
        return base.ChanceFactorNow(target) * Mathf.Clamp01(DefExt.chanceFactorPerTarget * candidateCount);
    }

    #region Bugfix

    // The version of this function in IncidentWorker_Disease gets a NullReferenceException when letterSingularForm is true
    // and hediff.Part is null. Oops. So I get to copy-paste the whole thing to fix two lines. I am strongly tempted to
    // use a transpiler instead.
    protected override bool TryExecuteWorker(IncidentParms parms)
    {
        DebugAssert.NotNull(def.diseaseIncident);
        
        List<Pawn> list = ApplyToPawns(ActualVictims(parms).ToList(), out var blockedInfo);
        if (list is not { Count: > 0} && blockedInfo.NullOrEmpty())
        {
            return false;
        }

        TaggedString baseLetterLabel = def.letterLabel;
        TaggedString baseLetterText;
        if (list is { Count: > 0 })
        {
            if (def.letterSingularForm)
            {
                if (list.Count > 1)
                {
                    Log.Error("Incident " + def.defName +
                              " is marked to only generate a letter in a singular format, but multiple victims were provided.");
                }

                Pawn pawn = list[0];
                DebugAssert.NotNull(pawn);
                Hediff? mostRecentHediff = pawn.health.hediffSet.GetMostRecentHediff(def.diseaseIncident);
                DebugAssert.NotNull(mostRecentHediff);
                baseLetterLabel = def.letterLabel.Formatted(pawn.Named("PAWN"));
                if (mostRecentHediff.TryGetComp<HediffComp_SeverityPerDay>() is { } hediffComp_SeverityPerDay)
                {
                    float num = hediffComp_SeverityPerDay.SeverityChangePerDay();
                    int num2 = Mathf.RoundToInt(mostRecentHediff.def.maxSeverity / num);
                    baseLetterText = def.letterText
                        .Formatted(pawn.Named("PAWN"), def.diseaseIncident.label, mostRecentHediff.Part?.Label, num2).Resolve();
                }
                else
                {
                    baseLetterText = def.letterText
                        .Formatted(pawn.Named("PAWN"), def.diseaseIncident.label, mostRecentHediff.Part?.Label).Resolve();
                }

                if (mostRecentHediff.IsAnyStageLifeThreatening() && !def.diseaseLethalLetterText.NullOrEmpty())
                {
                    baseLetterText += "\n\n" + def.diseaseLethalLetterText.Formatted(pawn.Named("PAWN"));
                }
            }
            else
            {
                StringBuilder stringBuilder = new StringBuilder();
                foreach (Pawn pawn in list)
                {
                    if (stringBuilder.Length != 0)
                    {
                        stringBuilder.AppendLine();
                    }

                    stringBuilder.AppendTagged("  - " + pawn.LabelNoCountColored.Resolve());
                }

                baseLetterText
                    = def.letterText.Formatted(list.Count.ToString(), Faction.OfPlayer.def.pawnsPlural, def.diseaseIncident.label)
                        .Resolve() + ":\n\n" + stringBuilder;
            }
        }
        else
        {
            baseLetterText = "";
        }

        if (!blockedInfo.NullOrEmpty())
        {
            if (!baseLetterText.NullOrEmpty())
            {
                baseLetterText += "\n\n";
            }

            baseLetterText += blockedInfo;
        }

        SendStandardLetter(baseLetterLabel, baseLetterText, def.letterDef ?? LetterDefOf.NegativeEvent, parms, list);
        return true;
    }

    #endregion
}
