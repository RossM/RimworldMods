using Xylib;

#pragma warning disable IDE0130
namespace XylXenos;

[UsedFromXml]
public class DefModExtension_Thought : DefModExtension
{
    public List<ThoughtDef>? extraThoughts;

    // Only used by ThoughtWorker_HasGene
    public GeneDef? gene;
    public bool requiresSight = false;
}
