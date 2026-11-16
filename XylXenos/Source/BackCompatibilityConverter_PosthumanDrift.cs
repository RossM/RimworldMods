using System.Xml;

namespace XylXenos;

[UsedFromReflection]
internal class BackCompatibilityConverter_PosthumanDrift : BackCompatibilityConverter
{
    public override bool AppliesToVersion(int majorVer, int minorVer) => true;

    public override string? BackCompatibleDefName(Type defType, string defName, bool forDefInjections = false, XmlNode? node = null)
    {
        return null;
    }

    public override Type? GetBackCompatibleType(Type baseType, string providedClassName, XmlNode? node)
    {
        return null;
    }

    public override void PostExposeData(object obj) { }
}
