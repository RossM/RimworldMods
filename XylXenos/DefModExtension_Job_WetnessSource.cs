namespace XylXenos;

[UsedFromXml]
public class DefModExtension_Job_WetnessSource : DefModExtension
{
    public float? wetnessLevel;
    public int? overrideDuration;
    public bool neverEndsEarly = false;
    public bool notIfMoving = true;
}
