namespace XylXenos;

public class GeneTracker_XylXenos : GeneTracker
{
    /// <summary>
    ///     Aggregates <see cref="GeneCompProperties_JoyGiverChances.factors" /> from all genes.<br /><br />
    ///     <inheritdoc cref="GeneCompProperties_JoyGiverChances.factors" />
    /// </summary>
    public Dictionary<JoyGiverDef, float>? joyGiverChanceFactors;

    public List<FactionDef>? disableHostilityFromFactions;

    public List<GeneIngestionThoughtOverride>? ingestionThoughtOverrides;

    public Dictionary<DamageDef, float>? meleeDamageFactors;

    public bool hasPsycast;

    public float youthfulMaxAge;

    public override void Update()
    {
        joyGiverChanceFactors?.Clear();
        disableHostilityFromFactions?.Clear();
        ingestionThoughtOverrides?.Clear();
        meleeDamageFactors?.Clear();
        hasPsycast = false;
        youthfulMaxAge = float.MaxValue;

        if (Pawn.genes == null)
            return;

        foreach (var gene in Pawn.ActiveGenesOfType<GeneWithComps>())
        {
            var def = gene.DefExt;

            Multiply(ref joyGiverChanceFactors, def.CompProps<GeneCompProperties_JoyGiverChances>()?.factors,
                item => item.joyGiver, item => item.factor);
            Append(ref disableHostilityFromFactions, def.CompProps<GeneCompProperties_DisableHostility>()?.factions);
            Append(ref ingestionThoughtOverrides, def.CompProps<GeneCompProperties_IngestionThoughtOverrides>()?.overrides);
            Multiply(ref meleeDamageFactors, def.CompProps<GeneCompProperties_MeleeDamageFactors>()?.factors, 
                item => item.damageDef, item => item.factor);

            hasPsycast |= def.CompProps<GeneCompProperties_Psycast>() != null;

            if (def.CompProps<GeneCompProperties_Youthful>() is { } youthful)
                youthfulMaxAge = Mathf.Min(youthfulMaxAge, youthful.maxAge);
        }
    }
}
