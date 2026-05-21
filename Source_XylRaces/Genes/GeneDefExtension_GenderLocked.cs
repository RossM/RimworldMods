using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace XylXenos.Genes
{
    [UsedFromXml]
    public class GeneDefExtension_GenderLocked : GeneDefExtension
    {
        public Gender? activeGender;
    }
}
