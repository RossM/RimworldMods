namespace XylXenos;

public class Config : Def
{
    public enum Feature
    {
        FixLactationBugs,
        Joyless,
        UIChange,
        Parthenogenesis,
    }

    public static Config Instance => field ??= DefDatabase<Config>.AllDefs.Single();

    public List<JobDef> wetnessGivingJobs;

    [NoTranslate] public List<string> ignoreGenesFromMods;
}
