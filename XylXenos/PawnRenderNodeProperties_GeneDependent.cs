namespace XylXenos;

[UsedFromXml]
public class PawnRenderNodeProperties_GeneDependent : PawnRenderNodeProperties
{
    public List<GeneDef>? requiredGenes;
    public List<GeneDef>? disallowedGenes;
    public bool flipWhenCrawling = false;
    public bool useBodyGraphicScale = false;

    public PawnRenderNodeProperties_GeneDependent()
    {
        workerClass = typeof(PawnRenderNodeWorker_GeneDependent);
    }
}
