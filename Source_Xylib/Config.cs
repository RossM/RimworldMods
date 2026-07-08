namespace Xylib;

[UsedFromXml]
public class Config : Def
{
    public static Config Instance => field ??= DefDatabase<Config>.GetNamed("XylibConfig");

    public Dictionary<HediffDef, StatDef> resistanceStatByHediff;
}
