namespace Xylib;

public class GeneCompProperties_RaceModifiers : GeneCompProperties
{
    /// <summary>
    ///     Scales pawn body size, which affects many things including the chance of being hit by ranged fire.
    /// </summary>
    public float bodySizeFactor = 1.0f;

    /// <summary>
    ///     Scales body part hit points for all body parts.
    /// </summary>
    public float healthScaleFactor = 1.0f;

    public override IEnumerable<StatDrawEntry> SpecialDisplayStats(StatRequest req)
    {
        if (bodySizeFactor != 1.0f)
        {
            yield return new(StatCategoryDefOf.BasicsPawn, "BodySize".Translate(), bodySizeFactor.ToStringPercent(),
                "Stat_Race_BodySize_Desc".Translate(), 4195);
        }

        if (healthScaleFactor != 1.0f)
        {
            yield return new(StatCategoryDefOf.BasicsPawn, "HitPointsBasic".Translate(), healthScaleFactor.ToStringPercent(),
                "XylHitPointsDesc".Translate(), 4194);
        }
    }

    public override IEnumerable<string> CustomEffectDescriptions()
    {
        if (bodySizeFactor != 1.0f)
            yield return $"{"BodySize".Translate().CapitalizeFirst()}: {bodySizeFactor.ToStringPercent()}";
        if (healthScaleFactor != 1.0f)
            yield return $"{"HitPointsBasic".Translate().CapitalizeFirst()}: {healthScaleFactor.ToStringPercent()}";
    }
}
