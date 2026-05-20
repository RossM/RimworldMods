using JetBrains.Annotations;

namespace XylXenos
{
    [UsedImplicitly]
    public class Hediff_Petrified : HediffWithCompsExt
    {
        public virtual float RelativeSeverity => Severity / Part.def.GetMaxHealth(pawn);

        public override float PartEfficiencyOffset => CurStage.partEfficiencyOffset * RelativeSeverity;

        public override int CurStageIndex => def.StageAtSeverity(RelativeSeverity);
    }
}
