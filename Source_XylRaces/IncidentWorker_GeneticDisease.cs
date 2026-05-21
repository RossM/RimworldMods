using System.Collections.Generic;
using System.Linq;
using System.Text;
using JetBrains.Annotations;
using RimWorld;
using UnityEngine;
using Verse;

namespace XylXenos
{
    [UsedFromXml]
    public class IncidentDefExtension_GeneticDisease : DefModExtension
    {
        public List<GeneDef> requiredGenesAny;
        public float chanceFactorPerTarget = 1f;

        public override IEnumerable<string> ConfigErrors()
        {
            if (requiredGenesAny == null || requiredGenesAny.Count == 0)
                yield return "requiredGenesAny must have at least one entry";
        }
    }

    [UsedImplicitly]
    public class IncidentWorker_GeneticDisease : IncidentWorker_DiseaseHuman
    {
        public IncidentDefExtension_GeneticDisease DefExt => def.GetModExtension<IncidentDefExtension_GeneticDisease>();

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
            string blockedInfo;
            List<Pawn> list = ApplyToPawns(ActualVictims(parms).ToList(), out blockedInfo);
            if (!list.Any() && blockedInfo.NullOrEmpty())
            {
                return false;
            }

            TaggedString baseLetterLabel = def.letterLabel;
            TaggedString baseLetterText;
            if (list.Any())
            {
                if (def.letterSingularForm)
                {
                    if (list.Count > 1)
                    {
                        Log.Error("Incident " + def.defName +
                                  " is marked to only generate a letter in a singular format, but multiple victims were provided.");
                    }

                    Pawn pawn = list[0];
                    Hediff mostRecentHediff = pawn.health.hediffSet.GetMostRecentHediff(def.diseaseIncident);
                    baseLetterLabel = def.letterLabel.Formatted(pawn.Named("PAWN"));
                    HediffComp_SeverityPerDay hediffComp_SeverityPerDay = mostRecentHediff.TryGetComp<HediffComp_SeverityPerDay>();
                    if (hediffComp_SeverityPerDay != null)
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

                    if (mostRecentHediff.IsAnyStageLifeThreatening() && !string.IsNullOrEmpty(def.diseaseLethalLetterText))
                    {
                        baseLetterText += "\n\n" + def.diseaseLethalLetterText.Formatted(pawn.Named("PAWN"));
                    }
                }
                else
                {
                    StringBuilder stringBuilder = new StringBuilder();
                    for (int i = 0; i < list.Count; i++)
                    {
                        if (stringBuilder.Length != 0)
                        {
                            stringBuilder.AppendLine();
                        }

                        stringBuilder.AppendTagged("  - " + list[i].LabelNoCountColored.Resolve());
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

            SendStandardLetter(baseLetterLabel, baseLetterText, def.letterDef, parms, list);
            return true;
        }

        #endregion
    }
}
