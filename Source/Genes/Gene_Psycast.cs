using Verse;

namespace XylRacesCore.Genes
{
    public class Gene_Psycast : Gene
    {
        public override void PostAdd()
        {
            base.PostAdd();
            pawn.psychicEntropy.SetInitialPsyfocusLevel();
        }
    }
}
