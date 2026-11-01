namespace XylXenos;

[UsedFromXml]
public class MentalState_SeeingRed : MentalState
{
    public override bool ForceHostileTo(Thing t)
    {
        return pawn.HasActiveGeneWithComp<GeneComp_SeeingRed>(g => g.ForceHostility(t));
    }

    public override RandomSocialMode SocialModeMax()
    {
        return RandomSocialMode.Off;
    }
}
