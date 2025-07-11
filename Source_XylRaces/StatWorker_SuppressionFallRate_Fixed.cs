using RimWorld;
using System.Text;
using JetBrains.Annotations;
using Verse;

namespace XylRacesCore
{
    public class StatWorker_SuppressionFallRate_Fixed : StatWorker
    {
        public override bool ShouldShowFor(StatRequest req)
        {
            if (!base.ShouldShowFor(req))
            {
                return false;
            }
            if (req.Thing is not Pawn pawn)
            {
                return false;
            }
            return pawn.IsSlave;
        }

        private static float CurrentFallRateBasedOnSuppression(float suppression)
        {
            return suppression switch
            {
                > StatWorker_SuppressionFallRate.FastFallRateThreshold => StatWorker_SuppressionFallRate.FastFallRate,
                > StatWorker_SuppressionFallRate.MediumFallRateThreshold => StatWorker_SuppressionFallRate.MediumFallRate,
                _ => StatWorker_SuppressionFallRate.SlowFallRate
            };
        }

        public override float GetBaseValueFor(StatRequest request)
        {
            ((Pawn)request.Thing).needs.TryGetNeed(out Need_Suppression need);
            return CurrentFallRateBasedOnSuppression(need.CurLevelPercentage);
        }

        public override string GetExplanationUnfinalized(StatRequest req, ToStringNumberSense numberSense)
        {
            StringBuilder stringBuilder = new StringBuilder();
            float baseValueFor = GetBaseValueFor(req);
            ((Pawn)req.Thing).needs.TryGetNeed(out Need_Suppression need);
            stringBuilder.Append(
                $"{"CurrentSuppression".Translate()} ({need.CurLevelPercentage.ToStringPercent()}): {CurrentFallRateBasedOnSuppression(need.CurLevelPercentage).ToStringPercent()}");
            GetOffsetsAndFactorsExplanation(req, stringBuilder, baseValueFor);
            return stringBuilder.ToString();
        }

        [UsedImplicitly]
        public string GetExplanationForTooltip(StatRequest req)
        {
            StringBuilder stringBuilder = new StringBuilder();
            float baseValueFor = GetBaseValueFor(req);
            stringBuilder.AppendLine("SuppressionFallRate".Translate() + ": " + GetValue(req.Thing).ToStringPercent());
            ((Pawn)req.Thing).needs.TryGetNeed(out Need_Suppression need);
            stringBuilder.AppendLine(
                $"   {"CurrentSuppression".Translate()} ({need.CurLevelPercentage.ToStringPercent()}): {CurrentFallRateBasedOnSuppression(need.CurLevelPercentage).ToStringPercent()}");
            GetOffsetsAndFactorsExplanation(req, stringBuilder, baseValueFor);
            GetAdditionalOffsetsAndFactorsExplanation(req, ToStringNumberSense.Factor, stringBuilder);
            return stringBuilder.ToString();
        }
    }
}
