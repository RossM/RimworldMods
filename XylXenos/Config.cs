namespace XylXenos;

[UsedFromXml]
public class Config : Def
{
    public static Config Instance => field ??= DefDatabase<Config>.AllDefs.Single();
}

public static class FeatureNameOf
{
    public const string Bugfix_Lactation = nameof(Bugfix_Lactation);
    public const string Bugfix_Misc = nameof(Bugfix_Misc);
    public const string UI_Misc = nameof(UI_Misc);
    public const string Joyless = nameof(Joyless);
    public const string Parthenogenesis = nameof(Parthenogenesis);
}

public static class ModIdOf
{
    public const string LactationExpansion = "Euterpe.MilkyTitfuck";
}
