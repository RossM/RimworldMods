using RimWorld;

namespace XylXenos;

public static class XenotypeSetExtensions
{
    extension(XenotypeSet xenotypeSet)
    {
        public XenotypeDef GetDefaultXenotype()
        {
            if (xenotypeSet is XenotypeSetWithDefault withDefault)
                return withDefault.defaultXenotype;
            else
                return XenotypeDefOf.Baseliner;
        }
    }
}