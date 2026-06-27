namespace XylXenos;

public class GeneTracker_XylXenos : GeneTracker
{
    /// <summary>
    ///     Aggregates <see cref="GeneCompProperties_JoyGiverChances.factors" /> from all genes.<br /><br />
    ///     <inheritdoc cref="GeneCompProperties_JoyGiverChances.factors" />
    /// </summary>
    [CanBeNull] public List<JoyGiverFactor> joyGiverChanceFactors;

    /// <summary>
    ///     Aggregates <see cref="GeneCompProperties_UnlockBuildables.buildables" /> from all genes.<br /><br />
    ///     <inheritdoc cref="GeneCompProperties_UnlockBuildables.buildables" />
    /// </summary>
    [CanBeNull] public List<BuildableDef> unlockedBuildables;

    [CanBeNull] public List<RecipeDef> unlockedRecipes;

    [CanBeNull] public List<FactionDef> disableHostilityFromFactions;

    [CanBeNull] public List<GeneIngestionThoughtOverride> ingestionThoughtOverrides;

    public override void Update()
    {
        joyGiverChanceFactors?.Clear();
        unlockedBuildables?.Clear();
        unlockedRecipes?.Clear();
        disableHostilityFromFactions?.Clear();
        ingestionThoughtOverrides?.Clear();

        if (pawn.genes != null)
        {
            foreach (var gene in pawn.ActiveGenesOfType<GeneWithComps>())
            {
                var def = gene.DefExt;

                Append(ref joyGiverChanceFactors, def.CompProps<GeneCompProperties_JoyGiverChances>()?.factors);
                Append(ref unlockedBuildables, def.CompProps<GeneCompProperties_UnlockBuildables>()?.buildables);
                Append(ref unlockedRecipes, def.CompProps<GeneCompProperties_UnlockRecipes>()?.recipes);
                Append(ref disableHostilityFromFactions, def.CompProps<GeneCompProperties_DisableHostility>()?.factions);
                Append(ref ingestionThoughtOverrides, def.CompProps<GeneCompProperties_IngestionThoughtOverrides>()?.overrides);
            }
        }
    }
}
