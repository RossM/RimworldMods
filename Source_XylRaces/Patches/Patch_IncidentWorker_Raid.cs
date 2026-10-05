namespace XylXenos.Patches;

[Patch(typeof(IncidentWorker_Raid))]
public static class Patch_IncidentWorker_Raid
{
    [Feature(nameof(Config.Feature.Bugfix_Misc))]
    [Prefix]
    [Target(nameof(IncidentWorker_Raid.ResolveRaidArriveMode))]
    public static bool ResolveRaidArriveMode_Prefix(IncidentParms parms)
    {
        return parms.raidArrivalMode == null;
    }
}
