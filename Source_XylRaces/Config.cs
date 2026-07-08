namespace XylXenos;

[UsedFromXml]
public class Config : Def
{
    public enum Feature
    {
        Bugfix_Lactation,
        Bugfix_Misc,
        UI_Misc,
        Joyless,
        Parthenogenesis,
    }

    public static Config Instance => field ??= DefDatabase<Config>.AllDefs.Single();

    public List<JobDef>? wetnessGivingJobs;
}
