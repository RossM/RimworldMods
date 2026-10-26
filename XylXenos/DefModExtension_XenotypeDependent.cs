namespace XylXenos;

/// <summary>
/// This is used as a marker to indicate that a def should only be available if the given xenotype is enabled.
/// </summary>
[UsedFromXml]
public class DefModExtension_XenotypeDependent : DefModExtension
{
    public XenotypeDef? xenotype;
}
