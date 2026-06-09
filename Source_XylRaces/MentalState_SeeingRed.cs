namespace XylXenos;

public class DefModExtension_MentalState_SeeingRed : DefModExtension
{
    public string iconPath;
}

[UsedFromXml]
public class MentalState_SeeingRed : MentalState
{
    public DefModExtension_MentalState_SeeingRed DefExt => def.GetModExtension<DefModExtension_MentalState_SeeingRed>();
    private MoteBubble mote;

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_References.Look(ref mote, nameof(mote));
    }

    public override bool ForceHostileTo(Thing t)
    {
        return pawn.HasActiveGeneOfType<Gene_SeeingRed>(g => g.ForceHostility(t));
    }

    public override RandomSocialMode SocialModeMax()
    {
        return RandomSocialMode.Off;
    }

    public override void PostStart(string reason)
    {
        base.PostStart(reason);

        if (!DefExt.iconPath.NullOrEmpty())
            mote = MakeThoughtBubble(pawn, DefExt.iconPath);
    }

    public static MoteBubble MakeThoughtBubble(Pawn pawn, string iconPath)
    {
        MoteMaker.ExistingMoteBubbleOn(pawn)?.Destroy();
        MoteBubble obj = (MoteBubble)ThingMaker.MakeThing(DefOf.XylMote_ForceJobMentalState);
        obj.SetupMoteBubble(ContentFinder<Texture2D>.Get(iconPath), null);
        obj.Attach(pawn);
        GenSpawn.Spawn(obj, pawn.Position, pawn.Map);
        return obj;
    }

    public override void PostEnd()
    {
        base.PostEnd();

        if (mote?.Destroyed == false)
            mote?.Destroy();
    }
}
