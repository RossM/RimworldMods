namespace XylXenos;

public static class XenotypeSetExtensions
{
    extension(XenotypeSet xenotypeSet)
    {
        public XenotypeDef DefaultXenotype =>
            xenotypeSet is XenotypeSetWithDefault withDefault ? withDefault.defaultXenotype : XenotypeDefOf.Baseliner;
    }
}
