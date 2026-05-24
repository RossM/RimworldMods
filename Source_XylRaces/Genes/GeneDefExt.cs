using Verse;

namespace XylXenos.Genes
{
    [UsedFromXml]
    public class GeneDefExt : GeneDef
    {
        public Gender? gender;

        public float bodySizeFactor = 1.0f;
        public float healthScaleFactor = 1.0f;

        public bool showInDrugPolicies = false;
    }
}
