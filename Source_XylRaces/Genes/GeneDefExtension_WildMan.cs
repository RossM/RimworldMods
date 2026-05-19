using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace XylXenos.Genes
{
    [UsedImplicitly]
    public class GeneDefExtension_WildMan : GeneDefExtension
    {
        public float? manhunterOnDamageChance;
        public float? manhunterOnTameFailChance;
        public bool allowEnslave;
    }
}
