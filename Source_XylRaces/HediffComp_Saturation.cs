using JetBrains.Annotations;
using Verse;

namespace XylXenos
{
    [UsedImplicitly]
    public class HediffCompProperties_Saturation : HediffCompProperties
    {
        public HediffDef sourceHediff;
        public float severityLossPerDay;
        public float severityGainPerDay;

        public HediffCompProperties_Saturation()
        {
            compClass = typeof(HediffComp_Saturation);
        }
    }

    public class HediffComp_Saturation : HediffComp_SeverityModifierBase
    {
        public HediffCompProperties_Saturation Props => (HediffCompProperties_Saturation)props;

        public override string CompLabelInBracketsExtra => (parent.Severity / parent.def.maxSeverity).ToStringPercent();

        public override float SeverityChangePerDay()
        {
            return Pawn.health.hediffSet.HasHediff(Props.sourceHediff) ? Props.severityGainPerDay : Props.severityLossPerDay;
        }
    }
}
