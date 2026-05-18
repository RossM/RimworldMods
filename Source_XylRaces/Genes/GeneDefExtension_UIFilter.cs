namespace XylXenos.Genes
{
    public class GeneDefExtension_UIFilter : GeneDefExtension
    {
        public bool alwaysHide = false;
        public bool? inheritable;

        public bool ShouldBeVisible(bool isInheritable)
        {
            if (alwaysHide)
                return false;
            if (inheritable != null && inheritable != isInheritable)
                return false;
            return true;
        }
    }
}
