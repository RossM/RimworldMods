namespace XylXenos;

public class GeneTracker_XylXenos : GeneTracker
{
    /// <summary>
    ///     Aggregates <see cref="GeneCompProperties_JoyGiverChances.factors" /> from all genes.<br /><br />
    ///     <inheritdoc cref="GeneCompProperties_JoyGiverChances.factors" />
    /// </summary>
    [CanBeNull] public List<JoyGiverFactor> joyGiverChanceFactors;


    [CanBeNull] public List<FactionDef> disableHostilityFromFactions;

    [CanBeNull] public List<GeneIngestionThoughtOverride> ingestionThoughtOverrides;

    public override void Update()
    {
        joyGiverChanceFactors?.Clear();
        disableHostilityFromFactions?.Clear();
        ingestionThoughtOverrides?.Clear();

        if (pawn.genes != null)
        {
            foreach (var gene in pawn.ActiveGenesOfType<GeneWithComps>())
            {
                var def = gene.DefExt;

                Append(ref joyGiverChanceFactors, def.CompProps<GeneCompProperties_JoyGiverChances>()?.factors);
                Append(ref disableHostilityFromFactions, def.CompProps<GeneCompProperties_DisableHostility>()?.factions);
                Append(ref ingestionThoughtOverrides, def.CompProps<GeneCompProperties_IngestionThoughtOverrides>()?.overrides);
            }
        }
    }
}
