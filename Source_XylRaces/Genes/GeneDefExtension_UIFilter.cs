namespace XylRacesCore.Genes
{
    public class GeneDefExtension_UIFilter : GeneDefExtension
    {
        public bool? inheritable;

        public bool ShouldBeVisible(bool isInheritable)
        {
            return inheritable == null || inheritable == isInheritable;
        }
    }
}
