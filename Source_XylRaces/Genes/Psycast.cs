using Verse;

namespace XylRacesCore.Genes
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
