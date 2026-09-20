using System.Diagnostics.CodeAnalysis;

namespace XylXenos;

[UsedFromXml]
public class DamageFactor
{
    public required DamageDef damageDef;
    public float factor = 1f;
    public float chanceBonus = 0f;
}

[UsedFromXml]
public class GeneCompProperties_MeleeDamageFactors : GeneCompProperties
{
    public required List<DamageFactor> factors;

    [SuppressMessage("ReSharper", "ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract")]
    public override IEnumerable<string> ConfigErrors(GeneDef? gene)
    {
        foreach (var error in base.ConfigErrors(gene))
            yield return error;

        if (factors is null)
            yield break;

        foreach (var damageFactor in factors)
        {
            if (damageFactor.damageDef is null)
                yield return $"null {nameof(damageFactor.damageDef)} in {nameof(factors)}";
            if (float.IsNaN(damageFactor.factor) || float.IsInfinity(damageFactor.factor) || damageFactor.factor < 0f)
                yield return "factor must be finite and non-negative";
        }
    }

    public override IEnumerable<string> CustomEffectDescriptions()
    {
        foreach (var damageFactor in factors)
            yield return "XylMeleeDamageFactor".Translate(damageFactor.damageDef.LabelCap, damageFactor.factor.ToStringPercent());
    }
}
