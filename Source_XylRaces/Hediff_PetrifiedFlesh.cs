using JetBrains.Annotations;
using UnityEngine;

namespace XylXenos
{
    [UsedImplicitly]
    public class Hediff_PetrifiedFlesh : HediffWithCompsExt
    {
        public virtual float RelativeSeverity => Severity / Part.def.GetMaxHealth(pawn);

        public override float PartEfficiencyOffset => CurStage.partEfficiencyOffset * RelativeSeverity;

        public override int CurStageIndex => def.StageAtSeverity(RelativeSeverity);

        public override Color LabelColor => RelativeSeverity >= 1.0f ? FullyPetrifiedColor : base.LabelColor;

        public override string SeverityLabel => Severity == 0f ? null : Severity.ToString("F1");

        private static readonly Color FullyPetrifiedColor = new(0.5f, 0.5f, 0.5f);
    }
}
