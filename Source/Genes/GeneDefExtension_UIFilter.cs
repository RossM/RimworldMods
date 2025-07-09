using Verse;

namespace XylRacesCore.Genes
{
    internal class GeneDefExtension_UIFilter : DefModExtension
    {
        public bool? inheritable;

        public bool ShouldBeVisible(bool isInheritable)
        {
            return inheritable == null || inheritable == isInheritable;
        }
    }
}
