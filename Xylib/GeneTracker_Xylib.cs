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
    ///     Aggregates <see cref="GeneCompProperties_UnlockBuildables.buildables" /> from all genes.<br /><br />
    ///     <inheritdoc cref="GeneCompProperties_UnlockBuildables.buildables" />
    /// </summary>
    public List<BuildableDef>? unlockedBuildables;

    public List<RecipeDef>? unlockedRecipes;

    public Vector3 bodyOffset = Vector3.zero;
    public float bodyScale = 1f;
    public Vector3 headOffset = Vector3.zero;
    public float headScale = 1f;
    public Graphic? bodyGraphicOverride = null;

    public override void Update()
    {
        bodySizeFactor = 1f;
        healthScaleFactor = 1f;
        unlockedBuildables?.Clear();
        unlockedRecipes?.Clear();
        bodyGraphicOverride = null;
        bodyOffset = Vector3.zero;
        bodyScale = 1f;
        headOffset = Vector3.zero;
        headScale = 1f;

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

            if (def.CompProps<GeneCompProperties_RenderNodeModifiers>() is { } renderNodeModifiers)
            {
                if (renderNodeModifiers.bodyTypeGraphics?.TryGetValue(Pawn.story.bodyType, out var graphic) is true)
                    bodyGraphicOverride = graphic;
                bodyOffset += renderNodeModifiers.bodyOffset;
                bodyScale *= renderNodeModifiers.bodyScale;
                headOffset += renderNodeModifiers.headOffset;
                headScale *= renderNodeModifiers.headScale;
            }

            Append(ref unlockedBuildables, def.CompProps<GeneCompProperties_UnlockBuildables>()?.buildables);
            Append(ref unlockedRecipes, def.CompProps<GeneCompProperties_UnlockRecipes>()?.recipes);
        }
    }
}
