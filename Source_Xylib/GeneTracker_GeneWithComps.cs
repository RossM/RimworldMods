namespace Xylib;

public class GeneTracker_GeneWithComps : GeneTracker
{
    /// <summary>
    ///     Aggregates <see cref="DefModExtension_GeneWithComps.bodySizeFactor" /> from all genes.<br /><br />
    ///     <inheritdoc cref="DefModExtension_GeneWithComps.bodySizeFactor" />
    /// </summary>
    public float bodySizeFactor = 1f;

    /// <summary>
    ///     Aggregates <see cref="DefModExtension_GeneWithComps.healthScaleFactor" /> from all genes.<br /><br />
    ///     <inheritdoc cref="DefModExtension_GeneWithComps.healthScaleFactor" />
    /// </summary>
    public float healthScaleFactor = 1f;

    /// <summary>
    ///     Aggregates <see cref="DefModExtension_GeneWithComps.hasPsycast" /> from all genes.<br /><br />
    ///     <inheritdoc cref="DefModExtension_GeneWithComps.hasPsycast" />
    /// </summary>
    public bool hasPsycast = false;

    /// <summary>
    ///     Aggregates <see cref="DefModExtension_GeneWithComps.renderNodeModifiers" /> from all genes.<br /><br />
    ///     <inheritdoc cref="DefModExtension_GeneWithComps.renderNodeModifiers" />
    /// </summary>
    [CanBeNull] public List<RenderNodeModifier> renderNodeModifiers;

    /// <summary>
    ///     Aggregates <see cref="GeneCompProperties_UnlockBuildables.buildables" /> from all genes.<br /><br />
    ///     <inheritdoc cref="GeneCompProperties_UnlockBuildables.buildables" />
    /// </summary>
    [CanBeNull] public List<BuildableDef> unlockedBuildables;

    [CanBeNull] public List<RecipeDef> unlockedRecipes;

    public override void Update()
    {
        bodySizeFactor = 1f;
        healthScaleFactor = 1f;
        renderNodeModifiers?.Clear();
        unlockedBuildables?.Clear();
        unlockedRecipes?.Clear();
        hasPsycast = false;

        if (pawn.genes != null)
        {
            foreach (var gene in pawn.ActiveGenesOfType<GeneWithComps>())
            {
                var def = gene.DefExt;

                bodySizeFactor *= def.bodySizeFactor;
                healthScaleFactor *= def.healthScaleFactor;
                hasPsycast |= def.hasPsycast;

                Append(ref renderNodeModifiers, def.renderNodeModifiers);
                Append(ref unlockedBuildables, def.CompProps<GeneCompProperties_UnlockBuildables>()?.buildables);
                Append(ref unlockedRecipes, def.CompProps<GeneCompProperties_UnlockRecipes>()?.recipes);
            }
        }
    }
}
