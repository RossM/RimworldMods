namespace Xylib;

[PublicAPI]
public class GeneTracker_Xylib : GeneTracker
{
    /// <summary>
    ///     Aggregates <see cref="GeneCompProperties_RaceModifiers.bodySizeFactor" /> from all genes.<br /><br />
    ///     <inheritdoc cref="GeneCompProperties_RaceModifiers.bodySizeFactor" />
    /// </summary>
    public float bodySizeFactor = 1f;

    /// <summary>
    ///     Aggregates <see cref="GeneCompProperties_RaceModifiers.healthScaleFactor" /> from all genes.<br /><br />
    ///     <inheritdoc cref="GeneCompProperties_RaceModifiers.healthScaleFactor" />
    /// </summary>
    public float healthScaleFactor = 1f;

    /// <summary>
    ///     Aggregates <see cref="GeneCompProperties_RenderNodeModifiers.renderNodeModifiers" /> from all genes.<br /><br />
    ///     <inheritdoc cref="GeneCompProperties_RenderNodeModifiers.renderNodeModifiers" />
    /// </summary>
    public List<RenderNodeModifier>? renderNodeModifiers;

    internal readonly List<RenderNodeModifier>?[] renderNodeModifiersByType
        = new List<RenderNodeModifier>[Enum.GetValues<RenderNodeModifierType>().Length];

    /// <summary>
    ///     Aggregates <see cref="GeneCompProperties_UnlockBuildables.buildables" /> from all genes.<br /><br />
    ///     <inheritdoc cref="GeneCompProperties_UnlockBuildables.buildables" />
    /// </summary>
    public List<BuildableDef>? unlockedBuildables;

    public List<RecipeDef>? unlockedRecipes;

    public override void Update()
    {
        bodySizeFactor = 1f;
        healthScaleFactor = 1f;
        renderNodeModifiers?.Clear();
        foreach (List<RenderNodeModifier>? list in renderNodeModifiersByType)
            list?.Clear();
        unlockedBuildables?.Clear();
        unlockedRecipes?.Clear();

        if (Pawn.genes == null)
            return;

        foreach (var gene in Pawn.ActiveGenesOfType<GeneWithComps>())
        {
            var def = gene.DefExt;

            if (def.CompProps<GeneCompProperties_RaceModifiers>() is { } raceModifiers)
            {
                bodySizeFactor *= raceModifiers.bodySizeFactor;
                healthScaleFactor *= raceModifiers.healthScaleFactor;
            }

            Append(ref renderNodeModifiers, def.CompProps<GeneCompProperties_RenderNodeModifiers>()?.renderNodeModifiers);
            for (int i = 0; i < renderNodeModifiersByType.Length; i++)
            {
                Append(ref renderNodeModifiersByType[i],
                    def.CompProps<GeneCompProperties_RenderNodeModifiers>()?.RenderNodeModifiersOfType((RenderNodeModifierType)i));
            }

            Append(ref unlockedBuildables, def.CompProps<GeneCompProperties_UnlockBuildables>()?.buildables);
            Append(ref unlockedRecipes, def.CompProps<GeneCompProperties_UnlockRecipes>()?.recipes);
        }
    }
}
