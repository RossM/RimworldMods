namespace XylXenos;

[UsedFromXml]
public class DamageWorker_Blind : DamageWorker
{
    public override DamageResult Apply(DamageInfo dinfo, Thing victim)
    {
        if (victim is Pawn pawn)
        {
            foreach (var part in pawn.health.hediffSet.GetNotMissingParts(tag: BodyPartTagDefOf.SightSource))
            {
                Hediff hediff = HediffMaker.MakeHediff(dinfo.Def.hediff, pawn);
                hediff.Severity = dinfo.Amount;
                pawn.health.AddHediff(hediff, part, dinfo);
            }
        }

        return new DamageResult();
    }
}
