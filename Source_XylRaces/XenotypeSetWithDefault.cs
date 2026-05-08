using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;

namespace XylRacesCore
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
