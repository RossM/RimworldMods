namespace XylXenos;

[UsedFromXml]
public class DefModExtension_SettingsData : DefModExtension
{
    /// <summary>
    ///     This def will be hidden/disabled if the specified xenotype is disabled in the settings.
    /// </summary>
    public XenotypeDef? requiredXenotype;
}
