using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
