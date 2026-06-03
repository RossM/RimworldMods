using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XylXenos
{
    [UsedFromXml]
    public class ThinkNode_ConditionalHasGene : ThinkNode_Conditional
    {
        public GeneDef gene;

        protected override bool Satisfied(Pawn pawn)
        {
            return pawn.HasActiveGene(gene);
        }

        public override ThinkNode DeepCopy(bool resolve = true)
        {
            ThinkNode_ConditionalHasGene copy = (ThinkNode_ConditionalHasGene)base.DeepCopy(resolve);
            copy.gene = gene;
            return copy;
        }
    }
}
