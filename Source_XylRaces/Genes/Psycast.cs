using Verse;

namespace XylXenos.Genes
{
    public class Psycast : Gene
    {
        public override void PostAdd()
        {
            base.PostAdd();
            pawn.psychicEntropy.SetInitialPsyfocusLevel();
        }
    }
}
