namespace Xylib;

/// <summary>
///     Tracks whether a pawn's base genes are still being generated.
/// </summary>
[PublicAPI]
public class GeneGenerationData : IPawnData
{
    public Pawn Pawn { get; set; } = null!;

    public bool GenesGenerated { get; internal set; }

    public void Init() { }
}
