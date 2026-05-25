namespace XylXenos.Genes
{
    public class Psycast : GeneExt
    {
        public override void PostAdd()
        {
            base.PostAdd();
            pawn.psychicEntropy.SetInitialPsyfocusLevel();
        }
    }
}
