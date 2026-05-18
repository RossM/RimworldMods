using RimWorld;

namespace XylXenos
{
    public class XenotypeSetWithDefault : XenotypeSet
    {
        public XenotypeDef defaultXenotype;

        public static XenotypeDef GetDefaultXenotype(XenotypeSet xenotypeSet)
        {
            if (xenotypeSet is XenotypeSetWithDefault withDefault)
                return withDefault.defaultXenotype;
            else
                return XenotypeDefOf.Baseliner;
        }
    }
}
